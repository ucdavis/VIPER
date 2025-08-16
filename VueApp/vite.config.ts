import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';
import { quasar, transformAssetUrls } from '@quasar/vite-plugin'
import vue from '@vitejs/plugin-vue'

import { resolve } from 'node:path'
const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "VueApp";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:5001';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
    plugins: [
        plugin(),
        // @quasar/plugin-vite options list:
        // https://github.com/quasarframework/quasar/blob/dev/vite-plugin/index.d.ts
        //quasar({
         //   sassVariables: 'src/quasar-variables.sass'
        //})
        quasar()
    ],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/CTS': {
                target,
                secure: false
            }
        },
        port: 5173,
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    },
    build: {
        minify: false,
        outDir: "../web/wwwroot/vue",
        emptyOutDir: true,
        rollupOptions: {
            input: {
                main: resolve(__dirname, 'index.html'),
                cts: resolve(__dirname, 'src/cts/index.html'),
                computing: resolve(__dirname, 'src/computing/index.html'),
                students: resolve(__dirname, 'src/students/index.html'),
                shared: resolve(__dirname, 'src/shared/index.html'),
            },
            output: {
                entryFileNames: (chunkInfo) => {
                    // Use stable filename for shared bundle so Razor views can reference it
                    return chunkInfo.name === 'shared' ? 'shared.js' : '[name].[hash].js'
                },
                assetFileNames: (assetInfo) => {
                    // Use stable filename for shared CSS (Quasar styles)
                    if (assetInfo.name?.includes('quasar') || 
                        (assetInfo.source && assetInfo.source.toString().includes('quasar'))) {
                        return 'shared.css'
                    }
                    return '[name].[hash][extname]'
                }
            }
        }
    },
    define: {
        __VUE_PROD_DEVTOOLS__: mode !== 'production'
    },
    base: '/2/vue/'
}))
