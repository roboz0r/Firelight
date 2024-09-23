[<RequireQualifiedAccess>]
module LitTailwind

open Lit
open type Lit.Lit
open FSharp.Data.LiteralProviders


// TODO: There's probably a better way to do this.
// This is a workaround to get the file name of the Tailwind CSS bundle after it's built and hashed by Vite.
// [<Literal>]
// let cssFileName = Exec<"pwsh", "-Command dir" + __SOURCE_DIRECTORY__ + "/dist/assets/index*.css | select Name | foreach-object {$_.Name} | out-string">.Output


// [<Literal>]
// let private fileName = //./dist/assets/index-B99bhl_q.css or similar
//     "dist/assets/"
//     + Exec<"pwsh", "-Command dir" + __SOURCE_DIRECTORY__ + "/dist/assets/index*.css | select Name | foreach-object {$_.Name} | out-string">.Output

let private distCss = css $$"""*,:before,:after{--tw-border-spacing-x: 0;--tw-border-spacing-y: 0;--tw-translate-x: 0;--tw-translate-y: 0;--tw-rotate: 0;--tw-skew-x: 0;--tw-skew-y: 0;--tw-scale-x: 1;--tw-scale-y: 1;--tw-pan-x: ;--tw-pan-y: ;--tw-pinch-zoom: ;--tw-scroll-snap-strictness: proximity;--tw-gradient-from-position: ;--tw-gradient-via-position: ;--tw-gradient-to-position: ;--tw-ordinal: ;--tw-slashed-zero: ;--tw-numeric-figure: ;--tw-numeric-spacing: ;--tw-numeric-fraction: ;--tw-ring-inset: ;--tw-ring-offset-width: 0px;--tw-ring-offset-color: #fff;--tw-ring-color: rgb(59 130 246 / .5);--tw-ring-offset-shadow: 0 0 #0000;--tw-ring-shadow: 0 0 #0000;--tw-shadow: 0 0 #0000;--tw-shadow-colored: 0 0 #0000;--tw-blur: ;--tw-brightness: ;--tw-contrast: ;--tw-grayscale: ;--tw-hue-rotate: ;--tw-invert: ;--tw-saturate: ;--tw-sepia: ;--tw-drop-shadow: ;--tw-backdrop-blur: ;--tw-backdrop-brightness: ;--tw-backdrop-contrast: ;--tw-backdrop-grayscale: ;--tw-backdrop-hue-rotate: ;--tw-backdrop-invert: ;--tw-backdrop-opacity: ;--tw-backdrop-saturate: ;--tw-backdrop-sepia: ;--tw-contain-size: ;--tw-contain-layout: ;--tw-contain-paint: ;--tw-contain-style: }::backdrop{--tw-border-spacing-x: 0;--tw-border-spacing-y: 0;--tw-translate-x: 0;--tw-translate-y: 0;--tw-rotate: 0;--tw-skew-x: 0;--tw-skew-y: 0;--tw-scale-x: 1;--tw-scale-y: 1;--tw-pan-x: ;--tw-pan-y: ;--tw-pinch-zoom: ;--tw-scroll-snap-strictness: proximity;--tw-gradient-from-position: ;--tw-gradient-via-position: ;--tw-gradient-to-position: ;--tw-ordinal: ;--tw-slashed-zero: ;--tw-numeric-figure: ;--tw-numeric-spacing: ;--tw-numeric-fraction: ;--tw-ring-inset: ;--tw-ring-offset-width: 0px;--tw-ring-offset-color: #fff;--tw-ring-color: rgb(59 130 246 / .5);--tw-ring-offset-shadow: 0 0 #0000;--tw-ring-shadow: 0 0 #0000;--tw-shadow: 0 0 #0000;--tw-shadow-colored: 0 0 #0000;--tw-blur: ;--tw-brightness: ;--tw-contrast: ;--tw-grayscale: ;--tw-hue-rotate: ;--tw-invert: ;--tw-saturate: ;--tw-sepia: ;--tw-drop-shadow: ;--tw-backdrop-blur: ;--tw-backdrop-brightness: ;--tw-backdrop-contrast: ;--tw-backdrop-grayscale: ;--tw-backdrop-hue-rotate: ;--tw-backdrop-invert: ;--tw-backdrop-opacity: ;--tw-backdrop-saturate: ;--tw-backdrop-sepia: ;--tw-contain-size: ;--tw-contain-layout: ;--tw-contain-paint: ;--tw-contain-style: }*,:before,:after{box-sizing:border-box;border-width:0;border-style:solid;border-color:#e5e7eb}:before,:after{--tw-content: ""}html,:host{line-height:1.5;-webkit-text-size-adjust:100%;-moz-tab-size:4;-o-tab-size:4;tab-size:4;font-family:ui-sans-serif,system-ui,sans-serif,"Apple Color Emoji","Segoe UI Emoji",Segoe UI Symbol,"Noto Color Emoji";font-feature-settings:normal;font-variation-settings:normal;-webkit-tap-highlight-color:transparent}body{margin:0;line-height:inherit}hr{height:0;color:inherit;border-top-width:1px}abbr:where([title]){-webkit-text-decoration:underline dotted;text-decoration:underline dotted}h1,h2,h3,h4,h5,h6{font-size:inherit;font-weight:inherit}a{color:inherit;text-decoration:inherit}b,strong{font-weight:bolder}code,kbd,samp,pre{font-family:ui-monospace,SFMono-Regular,Menlo,Monaco,Consolas,Liberation Mono,Courier New,monospace;font-feature-settings:normal;font-variation-settings:normal;font-size:1em}small{font-size:80%}sub,sup{font-size:75%;line-height:0;position:relative;vertical-align:baseline}sub{bottom:-.25em}sup{top:-.5em}table{text-indent:0;border-color:inherit;border-collapse:collapse}button,input,optgroup,select,textarea{font-family:inherit;font-feature-settings:inherit;font-variation-settings:inherit;font-size:100%;font-weight:inherit;line-height:inherit;letter-spacing:inherit;color:inherit;margin:0;padding:0}button,select{text-transform:none}button,input:where([type=button]),input:where([type=reset]),input:where([type=submit]){-webkit-appearance:button;background-color:transparent;background-image:none}:-moz-focusring{outline:auto}:-moz-ui-invalid{box-shadow:none}progress{vertical-align:baseline}::-webkit-inner-spin-button,::-webkit-outer-spin-button{height:auto}[type=search]{-webkit-appearance:textfield;outline-offset:-2px}::-webkit-search-decoration{-webkit-appearance:none}::-webkit-file-upload-button{-webkit-appearance:button;font:inherit}summary{display:list-item}blockquote,dl,dd,h1,h2,h3,h4,h5,h6,hr,figure,p,pre{margin:0}fieldset{margin:0;padding:0}legend{padding:0}ol,ul,menu{list-style:none;margin:0;padding:0}dialog{padding:0}textarea{resize:vertical}input::-moz-placeholder,textarea::-moz-placeholder{opacity:1;color:#9ca3af}input::placeholder,textarea::placeholder{opacity:1;color:#9ca3af}button,[role=button]{cursor:pointer}:disabled{cursor:default}img,svg,video,canvas,audio,iframe,embed,object{display:block;vertical-align:middle}img,video{max-width:100%;height:auto}[hidden]{display:none}.static{position:static}.m-2{margin:.5rem}.m-4{margin:1rem}.mx-2{margin-left:.5rem;margin-right:.5rem}.flex{display:flex}.hidden{display:none}.h-screen{height:100vh}.w-full{width:100%}.flex-grow{flex-grow:1}.flex-row{flex-direction:row}.flex-col{flex-direction:column}.flex-nowrap{flex-wrap:nowrap}.items-center{align-items:center}.justify-center{justify-content:center}.overflow-auto{overflow:auto}.rounded{border-radius:.25rem}.border{border-width:1px}.border-4{border-width:4px}.border-blue-800{--tw-border-opacity: 1;border-color:rgb(30 64 175 / var(--tw-border-opacity))}.bg-green-300{--tw-bg-opacity: 1;background-color:rgb(134 239 172 / var(--tw-bg-opacity))}.bg-red-800{--tw-bg-opacity: 1;background-color:rgb(153 27 27 / var(--tw-bg-opacity))}.bg-slate-50{--tw-bg-opacity: 1;background-color:rgb(248 250 252 / var(--tw-bg-opacity))}.p-2{padding:.5rem}.px-2{padding-left:.5rem;padding-right:.5rem}.py-4{padding-top:1rem;padding-bottom:1rem}.text-3xl{font-size:1.875rem;line-height:2.25rem}.font-bold{font-weight:700}.text-blue-600{--tw-text-opacity: 1;color:rgb(37 99 235 / var(--tw-text-opacity))}.text-blue-800{--tw-text-opacity: 1;color:rgb(30 64 175 / var(--tw-text-opacity))}.text-red-800{--tw-text-opacity: 1;color:rgb(153 27 27 / var(--tw-text-opacity))}.text-white{--tw-text-opacity: 1;color:rgb(255 255 255 / var(--tw-text-opacity))}.shadow{--tw-shadow: 0 1px 3px 0 rgb(0 0 0 / .1), 0 1px 2px -1px rgb(0 0 0 / .1);--tw-shadow-colored: 0 1px 3px 0 var(--tw-shadow-color), 0 1px 2px -1px var(--tw-shadow-color);box-shadow:var(--tw-ring-offset-shadow, 0 0 #0000),var(--tw-ring-shadow, 0 0 #0000),var(--tw-shadow)}.filter{filter:var(--tw-blur) var(--tw-brightness) var(--tw-contrast) var(--tw-grayscale) var(--tw-hue-rotate) var(--tw-invert) var(--tw-saturate) var(--tw-sepia) var(--tw-drop-shadow)}.hover\:bg-green-400:hover{--tw-bg-opacity: 1;background-color:rgb(74 222 128 / var(--tw-bg-opacity))}.hover\:bg-red-700:hover{--tw-bg-opacity: 1;background-color:rgb(185 28 28 / var(--tw-bg-opacity))}.disabled\:bg-gray-300:disabled{--tw-bg-opacity: 1;background-color:rgb(209 213 219 / var(--tw-bg-opacity))}"""
 
let cssBundle =
#if DEBUG
    css $""
#else
    unsafeCSS distCss
#endif


// Load the css via external link in debug mode
// Loading styles via external link is explicitly discourgaed by Lit, but I think it's fine for development.
// https://lit.dev/docs/components/styles/#external-stylesheet

#if DEBUG
open Browser
open Browser.Types
open Fable.Core.JsInterop
let mutable private stylesLink = None
let devModeStylesLink (): Renderable =
    match stylesLink with
    | Some link -> link
    | None ->
        let href =
            let el: HTMLLinkElement = !! document.getElementById ("app-styles")
            el.getAttribute("href")

        console.log $"StylesHref {href}"
        let link = html $"<link rel='stylesheet' href='{href}'>"
        stylesLink <- Some link
        link
#else
let devModeStylesLink (): Renderable = nothing
#endif
