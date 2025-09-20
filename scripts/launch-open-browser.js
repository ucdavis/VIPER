const { spawn } = require("node:child_process")
const fs = require("node:fs")
const { getDevServerEnv, openBrowser, DEFAULT_ENV_VARS } = require("./lib/script-utils")

// Cross-platform script for launching browser with debugging

// Get backend port from environment or use default
function getBackendPort() {
    const env = getDevServerEnv()

    // Use ASPNETCORE_HTTPS_PORT from env, with fallback to shared default
    return env.ASPNETCORE_HTTPS_PORT || DEFAULT_ENV_VARS.ASPNETCORE_HTTPS_PORT
}

// Wait for backend to be ready then open browser
async function waitForBackendAndOpenBrowser() {
    const port = getBackendPort()
    const url = `https://localhost:${port}`

    console.log(`Waiting for backend on port ${port}...`)

    // Wait longer for servers to start
    const INITIAL_DELAY = 8000
    await new Promise((resolve) => setTimeout(resolve, INITIAL_DELAY))

    // Check if backend is ready (simple approach)
    const maxRetries = 60
    let isReady = false

    for (let retries = 0; retries < maxRetries; retries += 1) {
        try {
            // Use a simple TCP connection check instead of HTTP request
            const net = require("node:net")

            // Sequential await is intentional here - retry logic requires waiting for each attempt
            // eslint-disable-next-line no-await-in-loop
            const isConnected = await new Promise((resolve) => {
                const socket = new net.Socket()
                const SOCKET_TIMEOUT = 1000
                socket.setTimeout(SOCKET_TIMEOUT)

                socket.on("connect", () => {
                    socket.destroy()
                    resolve(true)
                })

                socket.on("error", () => {
                    resolve(false)
                })

                socket.on("timeout", () => {
                    socket.destroy()
                    resolve(false)
                })

                socket.connect(port, "localhost")
            })

            if (isConnected) {
                isReady = true
                break
            }
        } catch {
            // Continue to next retry
        }

        // Only log every 10 retries to be less noisy
        const LOG_INTERVAL = 5
        if ((retries + 1) % LOG_INTERVAL === 0) {
            console.log(`Still waiting for backend... (${retries + 1}/${maxRetries})`)
        }

        if (retries < maxRetries - 1) {
            const RETRY_DELAY = 1000
            // Sequential delay is intentional here - we want to wait between retry attempts
            // eslint-disable-next-line no-await-in-loop
            await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY))
        }
    }

    if (isReady) {
        console.log(`Opening browser at ${url}`)
        await launchBrowser(url)
    } else {
        console.log(`Backend failed to start on port ${port} after ${maxRetries} retries`)
    }
}

// Launch browser with Chrome debugging support
async function launchBrowser(url) {
    // Launch Chrome with remote debugging enabled (Windows)
    // Need separate user data dir for debugging when regular Chrome is running
    const chromeArgs = ["--remote-debugging-port=9222", String.raw`--user-data-dir=C:\temp\chrome-debug-viper`, url]

    try {
        // Try common Chrome installation paths
        const chromePaths = [
            String.raw`C:\Program Files\Google\Chrome\Application\chrome.exe`,
            String.raw`C:\Program Files (x86)\Google\Chrome\Application\chrome.exe`,
        ]

        let chromeFound = false
        for (const chromePath of chromePaths) {
            if (fs.existsSync(chromePath)) {
                const child = spawn(chromePath, chromeArgs, { detached: true })
                if (typeof child.unref === "function") {
                    child.unref()
                }
                chromeFound = true
                break
            }
        }

        if (!chromeFound) {
            // Fallback to default browser (cross-platform)
            await openBrowser(url)
        }
    } catch {
        // Fallback to default browser (cross-platform)
        await openBrowser(url)
    }
}

async function main() {
    try {
        await waitForBackendAndOpenBrowser()
    } catch (error) {
        console.error(error)
    }
}

main()
