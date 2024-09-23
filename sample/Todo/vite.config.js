import { defineConfig } from "vite";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [],
  build: {
    rollupOptions: {
      input: ["./index.html"],
    },
  },
  server: {
    host: false, // Set to true to expose to other clients
  },
});
