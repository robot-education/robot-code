import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
// import fs from "fs";

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [react()]
    // server: {
    //     https: {
    //         key: fs.readFileSync("./certificates/localhost-key.pem"),
    //         cert: fs.readFileSync("./certificates/localhost.pem")
    //     },
    //     port: 3000,
    //     strictPort: true,
    //     proxy: {
    //         "/api": {
    //             target: "http://localhost:8080",
    //             changeOrigin: true,
    //             rewrite: (path) => path.replace(/^\/api/, "")
    //         }
    //     }
    // }
});
