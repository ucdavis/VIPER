// Build configuration file - performs filesystem operations with dynamic paths
import { fileURLToPath, URL } from 'node:url';

import { defineConfig, loadEnv } from 'vite';
import plugin from '@vitejs/plugin-vue';
import Inspector from "vite-plugin-vue-inspector"
import * as fs from "fs"
import * as path from "path"
import * as child_process from "child_process"
import { env } from "process"
import { quasar } from "@quasar/vite-plugin"

import { resolve } from "node:path"
const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== "" ? `${env.APPDATA}/ASP.NET/https` : `${env.HOME}/.aspnet/https`

const certificateName = "VueApp"
const certFilePath = path.join(baseFolder, `${certificateName}.pem`)
const keyFilePath = path.join(baseFolder, `${certificateName}.key`)

// Ensure the baseFolder exists before exporting the certificate
if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true })
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    const certResult = child_process.spawnSync(
        "dotnet",
        ["dev-certs", "https", "--export-path", certFilePath, "--format", "Pem", "--no-password"],
        { encoding: "utf8" },
    )
    if (certResult.error) {
        throw new Error(`Failed to run 'dotnet dev-certs': ${certResult.error.message}`)
    }
    if (certResult.status !== 0) {
        throw new Error(
            `'dotnet dev-certs' failed (exit ${certResult.status}): ${certResult.stdout || ""}${certResult.stderr || ""}`,
        )
    }
}

const target = env.ASPNETCORE_HTTPS_PORT
    ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
    : env.ASPNETCORE_URLS
      ? env.ASPNETCORE_URLS.split(";")[0]
      : "https://localhost:5001"

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    // Load environment variables from parent directory only (root of project)
    const viteEnv = loadEnv(mode, path.resolve(process.cwd(), ".."), ["VITE_"])

    // Parse port numbers with fallbacks to defaults
    const vitePort = parseInt(viteEnv.VITE_PORT || process.env.VITE_PORT || "5173", 10)
    const hmrPort = parseInt(viteEnv.VITE_HMR_PORT || process.env.VITE_HMR_PORT || "24678", 10)

    // Validate port numbers
    if (isNaN(vitePort) || vitePort < 1 || vitePort > 65535) {
        throw new Error(
            `Invalid VITE_PORT (${viteEnv.VITE_PORT || process.env.VITE_PORT}). Must be an integer between 1 and 65535.`,
        )
    }
    if (isNaN(hmrPort) || hmrPort < 1 || hmrPort > 65535) {
        throw new Error(
            `Invalid VITE_HMR_PORT (${viteEnv.VITE_HMR_PORT || process.env.VITE_HMR_PORT}). Must be an integer between 1 and 65535.`,
        )
    }

    return {
        plugins: [
            plugin(),
            // Vue Inspector - enables clicking components in browser to open in IDE (dev mode only)
            // Toggle with Ctrl+Shift, supports VS Code and Visual Studio
            mode === "development" &&
                Inspector({
                    toggleButtonVisibility: "active", // Show toggle button when inspector is active
                    toggleComboKey: "control-shift", // Keyboard shortcut to toggle inspector
                    launchEditor:
                        viteEnv.VITE_EDITOR === "visual-studio"
                            ? path.resolve(process.cwd(), "..", "visualstudio.bat") // Use batch file wrapper for Visual Studio
                            : viteEnv.VITE_EDITOR || "code", // Default to VS Code, or use VITE_EDITOR setting
                }),
            // @quasar/plugin-vite options list:
            // https://github.com/quasarframework/quasar/blob/dev/vite-plugin/index.d.ts
            //quasar({
            //   sassVariables: 'src/quasar-variables.sass'
            //})
            quasar(),
        ].filter(Boolean),
        resolve: {
            alias: {
                "@": fileURLToPath(new URL("./src", import.meta.url)),
            },
        },
        server: {
            proxy: {
                "^/CTS": {
                    target,
                    secure: false,
                },
                "^/Computing": {
                    target,
                    secure: false,
                },
                "^/Students": {
                    target,
                    secure: false,
                },
                "^/api": {
                    target,
                    secure: false,
                },
            },
            port: vitePort,
            strictPort: true, // Fail immediately if port is in use instead of trying other ports
            https: {
                key: fs.readFileSync(keyFilePath),
                cert: fs.readFileSync(certFilePath),
            },
            hmr: {
                overlay: false,
                port: hmrPort, // Use a different port for HMR WebSocket to avoid proxy conflicts
            },
        },
        build: {
            minify: true,
            outDir: "../web/wwwroot/vue",
            emptyOutDir: true,
            rollupOptions: {
                input: {
                    main: resolve(__dirname, "index.html"),
                    cts: resolve(__dirname, "src/cts/index.html"),
                    computing: resolve(__dirname, "src/computing/index.html"),
                    students: resolve(__dirname, "src/students/index.html"),
                },
            },
        },
        define: {
            __VUE_PROD_DEVTOOLS__: mode !== "production",
        },
        assetsInclude: ["**/*.html"],
        base: "/2/vue/",
    }
})
