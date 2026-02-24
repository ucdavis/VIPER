// Build configuration file - performs filesystem operations with dynamic paths
import { fileURLToPath, URL } from "node:url"

import { defineConfig, loadEnv } from "vite"
import plugin from "@vitejs/plugin-vue"
import Inspector from "vite-plugin-vue-inspector"
import fs from "node:fs"
import path, { resolve } from "node:path"
// oxlint-disable-next-line import/max-dependencies -- Vite config requires multiple build tool integrations
import child_process from "node:child_process"
import { env } from "node:process"
import { quasar } from "@quasar/vite-plugin"
import { visualizer } from "rollup-plugin-visualizer"
import { codecovVitePlugin } from "@codecov/vite-plugin"

// Port constants
const MAX_PORT = 65_535

const baseFolder = env.APPDATA && env.APPDATA !== "" ? `${env.APPDATA}/ASP.NET/https` : `${env.HOME}/.aspnet/https`

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

let target = "https://localhost:5001" // Default target
if (env.ASPNETCORE_HTTPS_PORT) {
    target = `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
} else if (env.ASPNETCORE_URLS) {
    ;[target] = env.ASPNETCORE_URLS.split(";")
}

// https://vitejs.dev/config/
// oxlint-disable-next-line max-lines-per-function -- Vite config function handles multiple build concerns in one place
export default defineConfig(({ mode }) => {
    // Load environment variables from parent directory only (root of project)
    const viteEnv = loadEnv(mode, path.resolve(process.cwd(), ".."), ["VITE_"])

    // Parse port numbers with fallbacks to defaults
    const vitePort = Number.parseInt(viteEnv.VITE_PORT || process.env.VITE_PORT || "5173", 10)
    const hmrPort = Number.parseInt(viteEnv.VITE_HMR_PORT || process.env.VITE_HMR_PORT || "24678", 10)

    // Validate port numbers
    if (Number.isNaN(vitePort) || vitePort < 1 || vitePort > MAX_PORT) {
        throw new Error(
            `Invalid VITE_PORT (${viteEnv.VITE_PORT || process.env.VITE_PORT}). Must be an integer between 1 and ${MAX_PORT}.`,
        )
    }
    if (Number.isNaN(hmrPort) || hmrPort < 1 || hmrPort > MAX_PORT) {
        throw new Error(
            `Invalid VITE_HMR_PORT (${viteEnv.VITE_HMR_PORT || process.env.VITE_HMR_PORT}). Must be an integer between 1 and ${MAX_PORT}.`,
        )
    }

    return {
        plugins: [
            plugin(),
            // Vue Inspector - enables clicking components in browser to open in IDE (dev mode only)
            // Toggle with Ctrl+Shift, supports VS Code and Visual Studio
            mode === "development" &&
                // oxlint-disable-next-line new-cap -- Inspector is a factory function from vite-plugin-vue-inspector
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
            // Set ANALYZE=true to generate bundle-stats.html
            // PowerShell: $env:ANALYZE='true'; npm run build-only-dev
            // CMD: set ANALYZE=true && npm run build-only-dev
            process.env.ANALYZE === "true" &&
                visualizer({
                    filename: "bundle-stats.html",
                    open: false,
                    gzipSize: true,
                }),
            codecovVitePlugin({
                enableBundleAnalysis: process.env.CODECOV_TOKEN !== undefined,
                bundleName: "viper-frontend",
                uploadToken: process.env.CODECOV_TOKEN,
            }),
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
                "^/ClinicalScheduler": {
                    target,
                    secure: false,
                },
                "^/Students": {
                    target,
                    secure: false,
                },
                "^/CMS": {
                    target,
                    secure: false,
                },
                "^/CAHFS": {
                    target,
                    secure: false,
                },
                "^/Effort": {
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
                protocol: "wss", // Use secure WebSocket since we're on HTTPS
                port: hmrPort, // Use a different port for HMR WebSocket to avoid proxy conflicts
                clientPort: hmrPort, // Tell browser clients to connect to this port for HMR
                host: "localhost", // Ensure HMR connects to localhost even when accessed via backend proxy
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
                    cms: resolve(__dirname, "src/cms/index.html"),
                    cahfs: resolve(__dirname, "src/cahfs/index.html"),
                    clinicalscheduler: resolve(__dirname, "src/clinicalscheduler/index.html"),
                    effort: resolve(__dirname, "src/Effort/index.html"),
                },
                output: {
                    manualChunks(id) {
                        // Chart.js library - only loaded when charts are used
                        if (id.includes("chart.js") || id.includes("vue-chartjs")) {
                            return "vendor-charts"
                        }
                    },
                },
            },
        },
        define: {
            // DevTools enabled only in dev server mode.
            __VUE_PROD_DEVTOOLS__: mode === "development",
        },
        base: "/2/vue/",
        test: {
            environment: "happy-dom",
            globals: true,
            coverage: {
                provider: "v8",
                reporter: ["text", "cobertura"],
                reportsDirectory: "./coverage",
            },
        },
    }
})
