import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
// import fs from "fs";

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [react()],
    server: {
        // https: {
        //     key: fs.readFileSync("./credentials/key.pem"),
        //     cert: fs.readFileSync("./credentials/cert.pem")
        // },
        origin: "https://localhost:3000",
        port: 5173,
        strictPort: true
    },
    build: {
        outDir: "../backend/dist",
        emptyOutDir: true
        // manifest: true
    }
});
