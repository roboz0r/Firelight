---
title: Using Firelight with coding agents
pageTitle: Using Firelight with coding agents
description: "Give coding agents this site as Markdown (llms.txt, llms-full.txt and every page), add the Firelight skill to Claude Code, and have agents check their work in a build and a browser."
section: start
order: 30
summary: "Markdown docs for agents, a Claude Code skill, and what helps"
lead: "Give a coding agent this site as Markdown and the Firelight skill, and have it check its work in a build and a browser. Firelight templates are HTML and its components are Lit's, so most of what an agent needs is what it already knows; these cover the F# part."
toc: true
---

<!--
  This page is agents/index.md, not agents.md: on a case-insensitive file system, agents.md is
  AGENTS.md, which coding agents read as instructions for the folder.
  Claude Code skills (checked 3 October 2026): https://code.claude.com/docs/en/skills.
  Project skills live at .claude/skills/<name>/SKILL.md; Claude loads one when its description
  matches the task, and `/name` invokes it directly.
  The skill link points at main, which is also what the site deploys from, so the page and the
  skill it links go live together. (On 3 October 2026 main still had the 0.2 skill, which says
  Firelight is SPA only; the skill on this branch covers Lit SSR.)
-->

HTML, Lit and F# are all widely used and documented, and a Firelight component is those three put
together. A template is HTML with [Lit's bindings](https://lit.dev/docs/templates/overview/), a
component has the shape of Lit's own classes, and [Fable](https://fable.io/) compiles it to an
ordinary Lit component in JavaScript. In this project's experience, agents rarely struggle with
Firelight code, and this is why. What's new to an agent is the thin layer
that is Firelight's: the F# names for Lit's API, and the F# mistakes a template can hide. The
files below cover that layer.

## This site as Markdown

Every page of this site, apart from search and "page not found", is also published as Markdown,
for agents and anything else that reads text:

| File | What it holds |
|---|---|
| [`llms.txt`](/llms.txt) | An index: each page's Markdown address, with a line about it |
| [`llms-full.txt`](/llms-full.txt) | Every page in one file, in the order of the index |
| `<page>/index.md` | One page, such as [the Templates guide](/guides/templates/index.md) |

In the Markdown, each example's F# source file is inline, followed by the demo's HTML, and every
link is absolute. Each page's `<head>` also links its Markdown version
(`<link rel="alternate" type="text/markdown">`), so a tool that fetches a page can find it.

Give an agent `llms.txt` and let it fetch the pages it needs, or `llms-full.txt` when it should
read everything. A line in your project's `CLAUDE.md` or `AGENTS.md` does it:

```md
Firelight (F# bindings for Lit) docs, as Markdown: https://roboz0r.github.io/Firelight/llms.txt
```

## The Firelight skill for Claude Code

The Firelight repository has a [skill](https://code.claude.com/docs/en/skills) for Claude Code,
[`.claude/skills/firelight/SKILL.md`](https://github.com/roboz0r/Firelight/blob/main/.claude/skills/firelight/SKILL.md).
It is one Markdown file with F# examples, and covers:

- the packages, and when a piece of UI should be a component or a template function;
- defining components: reactive properties, styles, `render` and lifecycle;
- events, with typed handlers (`Ev`) and custom events;
- Elmish in a component, context, reactive controllers and routing;
- directives, and the attribute or property choice in bindings;
- whole-app patterns, using web component libraries and wrapping imperative JavaScript libraries;
- rules (immutability, pure Elmish, Lit SSR in Node only, an app shell) and anti-patterns.

To use it in your project, copy it into your project's `.claude/skills/` folder:

```sh
mkdir -p .claude/skills/firelight
curl -o .claude/skills/firelight/SKILL.md https://raw.githubusercontent.com/roboz0r/Firelight/main/.claude/skills/firelight/SKILL.md
```

Claude Code loads the skill when a task matches its description, such as building UI with
Firelight, and `/firelight` loads it directly. Commit it, so everyone working on the project gets
it. The skill on `main` follows the latest Firelight. For an older version, copy the skill from
that version's tag, such as `v0.2.0`.

## What helps agents on a Fable and Lit codebase

These come from building this site and its examples with agents.

**Have it build after every change.** `dotnet fable` type-checks the F# and reports errors with
file and line, which an agent can fix on its own. A build doesn't catch everything, because a
template hole accepts any value: the [Templates guide](/guides/templates/#common-mistakes) lists
the mistakes that compile silently. This site goes further and compiles the F# examples in its
pages (`tests/Docs.Snippets`), so an example an agent writes that doesn't compile, or stops
compiling when the API changes, fails the build.

**Write down the rules the code doesn't show.** This site prerenders its demos in Node at build
time with [Lit SSR](https://lit.dev/docs/ssr/authoring/), so a component may only touch `window` or `document` in `connectedCallback`, `firstUpdated`,
`updated` or event handlers. Nothing in a component says that, so the rule is in the site's
`CLAUDE.md`, where agents read it before they start. Do the same for any rule of your own.

**Have it check in a browser.** Some Lit mistakes compile and fail only at runtime, and some fail
without a console message. This site's end-to-end tests (Playwright) open every page and fail on
console errors, failed requests, custom element tags that were never defined, and prerendered
components that render a second copy of themselves when they load. Ask an agent to write such a
check for what it builds, and to run it.

**Point it at the Markdown, not the HTML.** A page's HTML wraps the text in navigation,
highlighted code and prerendered shadow DOM. Its Markdown has the text and the source files, and
nothing else.
