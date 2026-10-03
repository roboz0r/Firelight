# FirelightApp

A web app written in F# with [Firelight](https://github.com/roboz0r/Firelight), the F# bindings for
[Lit](https://lit.dev/). [Fable](https://fable.io/) compiles the F# to JavaScript and
[Vite](https://vite.dev/) serves it.

You need the .NET 10 SDK (or later) and Node.js (20.19 or later). To run it:

```sh
dotnet tool restore
npm install
npm run dev
```

Then open the URL that Vite prints, and edit `App.fs`. The page reloads when you save.

`npm run build` writes a production build to `dist/`. Run `npm run preview` to serve it.
