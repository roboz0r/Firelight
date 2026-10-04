/// Opening pages in Chromium, recording what goes wrong while they load, and reading the DOM
/// including open shadow roots.
module Site.E2E.Browser

open System
open System.Threading.Tasks
open Microsoft.Playwright

type OpenPage =
    {
        Page: IPage
        Context: IBrowserContext
        /// Console errors and warnings, uncaught exceptions and failed requests, as messages.
        Problems: ResizeArray<string>
        /// Every JavaScript file the page loaded from the site, as a URL path.
        Scripts: ResizeArray<string>
    }

// A JavaScript function that collects every element of the page, including those inside
// (nested) open shadow roots, in document order.
let private allElements =
    """
    const allElements = () => {
      const found = [];
      const walk = (root) => {
        for (const el of root.querySelectorAll("*")) {
          found.push(el);
          if (el.shadowRoot) walk(el.shadowRoot);
        }
      };
      walk(document);
      return found;
    };
    """

// Waits until every custom element on the page is defined (or 5 s pass, and the registration
// check reports it) and has finished rendering. Rendering can add more elements, so repeat until
// the count is stable. Gives up after 10 s, returning the tags still rendering.
let private settleScript =
    $$"""
    async () => {
      {{allElements}}
      const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
      let pending = new Set();
      const settle = async () => {
        let count = -1;
        for (let round = 0; round < 5; round++) {
          const custom = allElements().filter((el) => el.localName.includes("-"));
          if (custom.length === count) return [];
          count = custom.length;
          const tags = [...new Set(custom.map((el) => el.localName))];
          await Promise.race([Promise.all(tags.map((tag) => customElements.whenDefined(tag))), delay(5000)]);
          pending = new Set(custom.filter((el) => el.updateComplete));
          await Promise.all(custom.map((el) => el.updateComplete?.then(() => pending.delete(el))));
        }
        return [];
      };
      const timedOut = () => [...new Set([...pending].map((el) => `<${el.localName}>`))];
      return await Promise.race([settle(), delay(10000).then(timedOut)]);
    }
    """

let private record (problems: ResizeArray<string>) (message: string) =
    lock problems (fun () -> problems.Add message)

/// A blank page in a fresh browser context, recording problems from here on. `initScript`, if any,
/// runs in every page before the page's own scripts.
let private openPage (options: BrowserNewContextOptions) (initScript: string option) =
    task {
        let! context = Server.browserInstance().NewContextAsync(options)

        match initScript with
        | Some script -> do! context.AddInitScriptAsync(script)
        | None -> ()

        let! page = context.NewPageAsync()
        let problems = ResizeArray()
        let scripts = ResizeArray()

        page.Console.Add(fun msg ->
            if msg.Type = "error" || msg.Type = "warning" then
                record problems $"console.{msg.Type}: {msg.Text} (at {msg.Location})"
        )

        page.PageError.Add(fun error -> record problems $"uncaught exception: {error}")

        page.RequestFailed.Add(fun request ->
            record problems $"request failed: {request.Method} {request.Url} ({request.Failure})"
        )

        page.Response.Add(fun response ->
            if response.Status >= 400 then
                record problems $"HTTP {response.Status}: {response.Request.Method} {response.Url}"
            // A server with an SPA fallback answers a missing script or image with a page.
            elif
                response.Request.ResourceType <> "document"
                && response.Request.ResourceType <> "fetch"
                && (
                    match response.Headers.TryGetValue "content-type" with
                    | true, contentType -> contentType.StartsWith "text/html"
                    | _ -> false
                )
            then
                record problems $"missing file, served as an HTML page: {response.Request.Method} {response.Url}"

            let url = Uri response.Url

            if response.Url.StartsWith Server.origin && url.AbsolutePath.EndsWith ".js" then
                record scripts url.AbsolutePath
        )

        return
            {
                Page = page
                Context = context
                Problems = problems
                Scripts = scripts
            }
    }

/// Like `withPageAndScript`, in a context with the given options (colour scheme, viewport,
/// JavaScript).
let withPageIn
    (options: BrowserNewContextOptions)
    (initScript: string option)
    (path: string)
    (f: OpenPage -> Task<'T>)
    =
    task {
        let javaScript = options.JavaScriptEnabled |> Option.ofNullable |> Option.defaultValue true
        let! opened = openPage options initScript

        try
            let! _ =
                opened.Page.GotoAsync(Server.url path, PageGotoOptions(WaitUntil = WaitUntilState.NetworkIdle))

            if javaScript then
                let! stillRendering = opened.Page.EvaluateAsync<string[]>(settleScript)

                if stillRendering.Length > 0 then
                    record
                        opened.Problems
                        $"""still rendering after 10 s (updateComplete never resolved): {String.Join(", ", stillRendering)}"""

            return! f opened
        finally
            opened.Context.CloseAsync().GetAwaiter().GetResult()
    }

/// Like `withPage`, with `initScript` run before the page's own scripts, e.g. to remove a browser API.
let withPageAndScript (initScript: string option) (javaScript: bool) (path: string) (f: OpenPage -> Task<'T>) =
    withPageIn (BrowserNewContextOptions(JavaScriptEnabled = javaScript)) initScript path f

/// Waits until the page's custom elements have rendered (as `withPage` does after loading), for a
/// page reached by clicking or reloading. Returns the tags still rendering after 10 s.
let settle (page: IPage) = page.EvaluateAsync<string[]>(settleScript)

/// Opens `path` on a fresh page and runs `f` once the network is idle and, with JavaScript, the
/// page's custom elements have rendered. Closes the page's context afterwards.
let withPage (javaScript: bool) (path: string) (f: OpenPage -> Task<'T>) =
    withPageAndScript None javaScript path f

/// The page's demos (`.demo` boxes, which `::: example` and `::: demo` render) that show
/// nothing: no custom element in them rendered any content, or the box has no height.
let emptyDemos (page: IPage) =
    page.EvaluateAsync<string[]>(
        """
        () => {
          const ignored = new Set(["style", "link", "script", "template"]);
          const hasContent = (root) =>
            [...root.childNodes].some((node) =>
              node.nodeType === Node.ELEMENT_NODE ? !ignored.has(node.localName)
              : node.nodeType === Node.TEXT_NODE && node.data.trim() !== "");
          const empty = [];
          document.querySelectorAll(".demo").forEach((demo, i) => {
            const custom = [...demo.querySelectorAll("*")].filter((el) => el.localName.includes("-"));
            const tags = custom.map((el) => `<${el.localName}>`).join(", ") || "no custom elements";
            const rendered = custom.filter((el) => (el.shadowRoot && hasContent(el.shadowRoot)) || hasContent(el));
            if (custom.length === 0 || rendered.length === 0) empty.push(`demo #${i + 1} (${tags}) rendered nothing`);
            else if (demo.getBoundingClientRect().height === 0) empty.push(`demo #${i + 1} (${tags}) has no height`);
          });
          return empty;
        }
        """
    )

/// Custom elements on the page that aren't registered, as "<tag> where (n times)".
let unregisteredElements (page: IPage) =
    page.EvaluateAsync<string[]>(
        """
        () => {
          // Hyphenated names that the HTML spec reserves for SVG and MathML, not custom elements.
          const reserved = new Set(["annotation-xml", "color-profile", "font-face", "font-face-src",
            "font-face-uri", "font-face-format", "font-face-name", "missing-glyph"]);
          const missing = new Map();
          const walk = (root, where) => {
            for (const el of root.querySelectorAll("*")) {
              const tag = el.localName;
              // A demo that defines an element on purpose later (a click) marks it data-defined-later.
              if (tag.includes("-") && !reserved.has(tag) && !customElements.get(tag) &&
                  !el.hasAttribute("data-defined-later")) {
                const key = `<${tag}> ${where}`;
                missing.set(key, (missing.get(key) ?? 0) + 1);
              }
              if (el.shadowRoot) walk(el.shadowRoot, `in the shadow root of <${tag}>`);
            }
          };
          walk(document, "in the page");
          return [...missing].map(([key, n]) => (n > 1 ? `${key} (${n} times)` : key));
        }
        """
    )

/// What a shadow root shows, for comparing a prerendered component before and after hydration.
[<CLIMutable>]
type ShadowSnapshot =
    {
        /// "<my-rating> #1": the tag and its position among the page's elements with that tag.
        Key: string
        /// Elements in the shadow root, not counting <style>, <link>, <script> and <template>.
        Elements: int
        /// The shadow root's text with whitespace collapsed (not that of nested shadow roots).
        Text: string
        /// Whether the shadow root holds Lit SSR's `<!--lit-part-->` markers at the top level.
        HasLitParts: bool
        /// Top-level content outside the outermost lit-part markers: a second render.
        OutsideLitParts: string[]
    }

/// A snapshot of every element on the page that has a shadow root. With JavaScript disabled the
/// only shadow roots are declarative ones, so those are exactly the prerendered components.
let shadowSnapshots (page: IPage) =
    page.EvaluateAsync<ShadowSnapshot[]>(
        $$"""
        () => {
          {{allElements}}
          const ignored = new Set(["style", "link", "script", "template"]);
          const textOf = (root) => {
            let text = "";
            const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
            for (let node = walker.nextNode(); node; node = walker.nextNode()) {
              if (!ignored.has(node.parentNode?.localName)) text += node.data;
            }
            return text.replace(/\s+/g, " ").trim();
          };
          const describe = (node) =>
            node.nodeType === Node.ELEMENT_NODE ? `<${node.localName}>` : `text "${node.data.trim().slice(0, 40)}"`;
          const outsideParts = (root) => {
            const outside = [];
            let depth = 0;
            for (const node of root.childNodes) {
              if (node.nodeType === Node.COMMENT_NODE) {
                if (node.data.startsWith("lit-part")) depth++;
                else if (node.data.startsWith("/lit-part")) depth--;
              } else if (depth === 0) {
                if (node.nodeType === Node.ELEMENT_NODE && !ignored.has(node.localName)) outside.push(describe(node));
                else if (node.nodeType === Node.TEXT_NODE && node.data.trim()) outside.push(describe(node));
              }
            }
            return outside;
          };
          const seen = new Map();
          const snapshots = [];
          for (const el of allElements()) {
            const n = (seen.get(el.localName) ?? 0) + 1;
            seen.set(el.localName, n);
            const root = el.shadowRoot;
            if (!root) continue;
            const hasLitParts = [...root.childNodes].some((c) => c.nodeType === Node.COMMENT_NODE && c.data.startsWith("lit-part"));
            snapshots.push({
              key: `<${el.localName}> #${n}`,
              elements: [...root.querySelectorAll("*")].filter((e) => !ignored.has(e.localName)).length,
              text: textOf(root),
              hasLitParts,
              outsideLitParts: hasLitParts ? outsideParts(root) : [],
            });
          }
          return snapshots;
        }
        """
    )
