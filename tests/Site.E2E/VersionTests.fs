/// The version the site documents: PackageVersion in Directory.Build.props, in every footer and in
/// each package page's NuGet link.
module Site.E2E.VersionTests

open System.IO
open System.Text.RegularExpressions
open Expecto
open Site.E2E.Site

let private version =
    lazy
        (let props = Path.Combine(Server.siteDir, "..", "Directory.Build.props")

         let m =
             Regex.Match(File.ReadAllText props, "<PackageVersion>([^<]+)</PackageVersion>")

         if not m.Success then
             failtest $"No <PackageVersion> in {props}."

         m.Groups[1].Value.Trim())

let private html (page: Page) =
    File.ReadAllText(Path.Combine(Server.distDir, page.File))

let all () =
    testList "Version" [
        test "every page's footer has the version" {
            let footer = $"<p>Firelight {version.Value} is MIT licensed."

            let missing =
                pages
                |> List.filter (isDemoApp >> not)
                |> List.filter (fun p -> not ((html p).Contains footer))

            if not missing.IsEmpty then
                failtest $"""No "{footer}" on {System.String.Join(", ", missing |> List.map _.Path)}"""
        }

        test "each package page links its package on NuGet at that version" {
            let packagePages = pages |> List.filter (fun p -> p.File.StartsWith "packages/")

            if packagePages.IsEmpty then
                failtest "No package pages in dist/packages/."

            for page in packagePages do
                let html = html page
                // A package page's heading is the package's id.
                let id = Regex.Match(html, "<h1>([^<]+)</h1>").Groups[1].Value.Trim()

                let expected =
                    $"""<a href="https://www.nuget.org/packages/{id}/{version.Value}">NuGet {version.Value}</a>"""

                let links =
                    Regex.Matches(html, "<a href=\"https://www\\.nuget\\.org/packages/[^\"]*\">[^<]*</a>")
                    |> Seq.map _.Value
                    |> List.ofSeq

                if links <> [ expected ] then
                    failtest $"{page.Path}: expected one NuGet link, {expected}; found {links}."
        }
    ]
