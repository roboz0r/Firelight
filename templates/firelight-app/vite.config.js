import { defineConfig } from "vite";

// https://vite.dev/config/
export default defineConfig({
  // `npm run dev` starts Vite from `dotnet fable watch`, so keep Fable's compiler output on screen.
  clearScreen: false,
});
