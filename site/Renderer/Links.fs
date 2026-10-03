/// The internal link check: every `<a href>` that stays on the site must point at a page or file
/// that exists, and any `#anchor` at an id on that page. Run over the built pages, so links from
/// Markdown, frontmatter and the layout are all covered.
module Site.Renderer.Links

open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop

[<Import("existsSync", "node:fs")>]
let private existsSync (path: string) : bool = jsNative

[<Import("statSync", "node:fs")>]
let private statSync (path: string) : {| isFile: unit -> bool |} = jsNative

[<Import("readFileSync", "node:fs")>]
let private readFileSync (path: string, encoding: string) : string = jsNative

[<Import("resolve", "node:path")>]
let private resolvePath (dir: string, file: string) : string = jsNative

[<Emit("decodeURIComponent($0)")>]
let private decodeUri (text: string) : string = jsNative

/// A page of the site, as the browser gets it.
type Built =
    {
        /// The HTML file, relative to `dist/`: `packages/firelight/index.html`, `index.html`, `404.html`.
        output: string
        /// The file it was made from, relative to the site root, for error messages:
        /// `content/packages/firelight.md`, `index.html`.
        source: string
        /// The page's HTML, or `null` to only use it as a link target (its anchors aren't checked).
        html: string
    }

/// What the check found. Errors fail the build; warnings are for links that only work once
/// something else has run (the demo apps from `npm run build:demos`).
type Report =
    { errors: string[]; warnings: string[] }

[<Import("parse", "parse5")>]
let private parseHtml (html: string) : obj = jsNative

// parse5's tree: elements have `nodeName`, `attrs` ({ name, value }, references decoded) and
// `childNodes`; a <template>'s children are in its `content` fragment.
let private attr (name: string) (node: obj) : string option =
    match node?attrs with
    | null -> None
    | attrs ->
        (attrs: obj[])
        |> Array.tryFind (fun a -> a?name = name)
        |> Option.map (fun a -> string a?value)

let rec private walk (intoTemplate: obj -> bool) (visit: obj -> unit) (node: obj) =
    visit node

    match node?childNodes with
    | null -> ()
    | children ->
        for child in (children: obj[]) do
            walk intoTemplate visit child

    if node?nodeName = "template" && intoTemplate node then
        walk intoTemplate visit node?content

/// Every `<a href>` on the page, including in prerendered shadow roots, but not in 404.html's
/// copies of single-page apps (checked on the apps' own pages). Comments and scripts hold none.
let private hrefsIn (html: string) =
    let hrefs = ResizeArray<string>()

    parseHtml html
    |> walk
        (fun template -> (attr "data-spa" template).IsNone)
        (fun node ->
            if node?nodeName = "a" then
                attr "href" node |> Option.iter hrefs.Add
        )

    List.ofSeq hrefs

/// The ids an anchor on `html` can target: elements in the document itself, not in a <template>
/// (which includes prerendered shadow roots).
let private idsIn (html: string) =
    let ids = Collections.Generic.HashSet<string>()

    parseHtml html
    |> walk (fun _ -> false) (fun node -> attr "id" node |> Option.iter (ids.Add >> ignore))

    Set.ofSeq ids

/// `packages/firelight/index.html` is served at `packages/firelight/`.
let private routeOf (output: string) =
    if output = "index.html" then
        ""
    elif output.EndsWith "/index.html" then
        output.Substring(0, output.Length - "index.html".Length)
    else
        output

/// Where `href` is written in `source`: the first line containing it, in the forms an author uses
/// (`/packages/router/` in Markdown, or with the base path).
let private locate (root: string) (``base``: string) (source: string) (href: string) =
    let text =
        try
            readFileSync(resolvePath (root, source), "utf8").Replace("\r\n", "\n")
        with _ ->
            ""

    let forms =
        if href.StartsWith ``base`` then
            let rest = href.Substring ``base``.Length
            [ href; "/" + rest ]
        else
            [ href ]

    let lines = text.Split '\n'

    forms
    |> List.tryPick (fun form ->
        lines
        |> Array.tryFindIndex (fun line -> line.Contains form)
        |> Option.map (fun i -> $"{source}:{i + 1}")
    )
    |> Option.defaultValue $"{source} (from the layout)"

/// Checks the links on every page that has `html`. Links that aren't pages can be files in
/// `fileDirs` (relative to `root`: `public`, and `dist` in the build). `strict` (the build) makes a
/// missing demo app an error; in dev it is a warning, as `npm run build:demos` may not have run.
let check (root: string) (``base``: string) (strict: bool) (fileDirs: string[]) (pages: Built[]) : Report =
    let errors = ResizeArray<string>()
    let warnings = ResizeArray<string>()

    // Pages answer at their directory URL and at the file itself (packages/firelight/index.html).
    let byPath =
        pages
        |> Array.collect (fun p -> [| routeOf p.output, p; p.output, p |])
        |> Array.distinctBy fst
        |> dict

    let idCache = Collections.Generic.Dictionary<string, Set<string>>()

    let cachedIds key (html: unit -> string) =
        match idCache.TryGetValue key with
        | true, found -> found
        | _ ->
            let found = idsIn (html ())
            idCache[key] <- found
            found

    for page in pages |> Array.filter (fun p -> not (isNull p.html)) do
        let fail (href: string) (problem: string) =
            errors.Add $"{locate root ``base`` page.source href}: {href} {problem}"

        // `ids` is None when the target's anchors aren't known (a page not rendered, in dev).
        let checkAnchor (href: string) (anchor: string) (where: string) (ids: Set<string> option) =
            match ids with
            | Some ids when anchor <> "" && not (ids.Contains anchor) -> fail href $"has no #{anchor} on {where}."
            | _ -> ()

        for href in hrefsIn page.html do
            let href = href.Trim()

            let withoutHash, rawAnchor =
                match href.IndexOf '#' with
                | -1 -> href, ""
                | i -> href.Substring(0, i), href.Substring(i + 1)

            let rawPath = withoutHash.Split('?').[0]

            match
                (try
                    Some(decodeUri rawPath, decodeUri rawAnchor)
                 with _ ->
                     None)
            with
            | _ when href = "" -> fail "href=\"\"" "is empty."
            | _ when Regex.IsMatch(href, "^[a-zA-Z][a-zA-Z0-9+.-]*:") || href.StartsWith "//" -> () // another site, or mailto: and the like
            | None -> fail href "is not a valid URL (bad % escape)."
            | Some(path, anchor) ->
                let segments = path.Split '/'

                if withoutHash = "" then
                    checkAnchor href anchor page.source (Some(cachedIds page.output (fun () -> page.html)))
                elif segments |> Array.exists (fun s -> s = "." || s = "..") then
                    fail href "has . or .. segments; write the path it means."
                elif path.StartsWith ``base`` then
                    let rest = path.Substring ``base``.Length

                    match byPath.TryGetValue rest with
                    | true, target ->
                        let ids =
                            if isNull target.html then
                                None
                            else
                                Some(cachedIds target.output (fun () -> target.html))

                        checkAnchor href anchor target.source ids
                    | _ when byPath.ContainsKey(rest + "/") -> fail href $"needs a trailing slash: {path}/"
                    | _ ->
                        // Any other file, such as a demo app in public/ or sitemap.xml in dist/.
                        let name =
                            if rest = "" || rest.EndsWith "/" then
                                rest + "index.html"
                            else
                                rest

                        let file =
                            fileDirs
                            |> Array.map (fun dir -> resolvePath (resolvePath (root, dir), name))
                            |> Array.tryFind (fun file -> existsSync file && (statSync file).isFile())

                        match file with
                        | Some file ->
                            if file.EndsWith ".html" then
                                checkAnchor
                                    href
                                    anchor
                                    name
                                    (Some(cachedIds file (fun () -> readFileSync (file, "utf8"))))
                        | None when rest.StartsWith "demos/" ->
                            let problem =
                                $"{locate root ``base`` page.source href}: {href} is not a demo app that has been built (npm run build:demos)."

                            if strict then errors.Add problem else warnings.Add problem
                        | None -> fail href "is not a page or file on this site."
                elif path.StartsWith "/" then
                    fail href $"is missing the base path {``base``}. (Markdown links get it automatically.)"
                else
                    fail href "is a relative link; write it root-relative, as in /packages/firelight/."

    {
        errors = errors.ToArray()
        warnings = warnings.ToArray()
    }
