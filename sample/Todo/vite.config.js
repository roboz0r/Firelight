import { defineConfig } from "vite";
import tailwindcss from "@tailwindcss/vite";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [tailwindcss()],
  build: {
    rollupOptions: {
      input: ["./index.html"],
    },
  },
  server: {
    host: false,
  },
});
