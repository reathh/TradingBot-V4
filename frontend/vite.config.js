import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import { VitePWA } from "vite-plugin-pwa";
import path from "path";

// https://vitejs.dev/config/
export default defineConfig( {
  plugins: [
    vue(),
    VitePWA( {
      registerType: "autoUpdate",
      includeAssets: [ "favicon.ico", "apple-touch-icon.png", "mask-icon.svg" ],
      manifest: {
        name: "Dashboard App",
        short_name: "Dashboard",
        description: "Dashboard application with dark theme",
        theme_color: "#1e1e2f",
        icons: [
          {
            src: "pwa-192x192.png",
            sizes: "192x192",
            type: "image/png",
          },
          {
            src: "pwa-512x512.png",
            sizes: "512x512",
            type: "image/png",
          },
        ],
      },
    } ),
  ],
  resolve: {
    alias: {
      "@": path.resolve( __dirname, "./src" ),
      "~bootstrap": path.resolve( __dirname, "node_modules/bootstrap" ),
    },
  },
  css: {
    preprocessorOptions: {
      scss: {
        additionalData: `@import "@/assets/sass/dashboard/custom/_variables.scss";`,
      },
    },
  },
  server: {
    host: "0.0.0.0",
    port: 5001,
    proxy: {
      "/api": {
        target: "http://localhost:5025",
        changeOrigin: true,
        rewrite: ( path ) => path.replace( /^\/api/, "/api" ),
      },
    },
  },
} );
