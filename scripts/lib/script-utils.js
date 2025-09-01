#!/usr/bin/env node

// Shared utilities for Node.js scripts (cross-platform)

const { exec } = require("node:child_process")
const { promisify } = require("node:util")
const dotenv = require("dotenv")
const path = require("node:path")
const fs = require("node:fs")
const os = require("node:os")

const execAsync = promisify(exec)

// Default environment variables for Vite and ASP.NET Core
const DEFAULT_ENV_VARS = {
    VITE_PORT: 5173,
    VITE_HMR_PORT: 24_678,
    ASPNETCORE_HTTPS_PORT: 7157,
    MAILPIT_SMTP_PORT: 1025,
    MAILPIT_WEB_PORT: 8025,
}

/**
 * Create a logger with a specific prefix
 * @param {string} prefix - The prefix to display in log messages
 * @returns {Object} Logger object with methods:
 *   - info(message): Log informational message with prefix (cyan/custom color)
 *   - success(message): Log success message with prefix (green)
 *   - warning(message): Log warning message with prefix (yellow)
 *   - error(message): Log error message with prefix (red)
 *   - plain(message): Log plain message without prefix
 *   - plainError(message): Log error without prefix
 *   - plainWarning(message): Log warning without prefix
 *   - plainInfo(message): Log info without prefix
 *   - plainSuccess(message): Log success without prefix
 */
function createLogger(prefix) {
    const colorMap = {
        "Dev Stop": "\u001B[35m", // Magenta
        "Dev Start": "\u001B[34m", // Blue
        Browser: "\u001B[33m", // Yellow
    }
    const color = colorMap[prefix] || "\u001B[36m" // Default to cyan
    const reset = "\u001B[0m"

    return {
        info: (message) => console.log(`${color}[${prefix}]${reset} ${message}`),
        success: (message) => console.log(`\u001B[32m[${prefix}] ${message}${reset}`),
        error: (message) => console.error(`\u001B[31m[${prefix}] Error: ${message}${reset}`),
        warning: (message) => console.warn(`\u001B[33m[${prefix}] Warning: ${message}${reset}`),
        // Plain methods for raw output without prefix decoration
        plain: (message) => console.log(message),
        plainError: (message) => console.error(message),
        plainWarning: (message) => console.warn(message),
        plainInfo: (message) => console.log(message),
        plainSuccess: (message) => console.log(message),
    }
}

// Get development server environment variables from .env.local or defaults
function getDevServerEnv() {
    // Always look for .env.local in the project root (where package.json is)
    const projectRoot = path.resolve(__dirname, "../..")
    const envPath = path.join(projectRoot, ".env.local")
    const envConfig = fs.existsSync(envPath) ? dotenv.config({ path: envPath }).parsed || {} : {}

    const mergedEnv = { ...DEFAULT_ENV_VARS }
    for (const key in DEFAULT_ENV_VARS) {
        if (Object.hasOwn(DEFAULT_ENV_VARS, key)) {
            const value = Number.parseInt(envConfig[key] || process.env[key], 10)
            if (!Number.isNaN(value)) {
                mergedEnv[key] = value
            }
        }
    }
    return mergedEnv
}

// Open a URL in the default browser (cross-platform)
async function openBrowser(url) {
    try {
        const isWindows = os.platform() === "win32"
        if (isWindows) {
            await execAsync(`start "" "${url}"`, { shell: true })
        } else if (os.platform() === "darwin") {
            await execAsync(`open "${url}"`)
        } else {
            await execAsync(`xdg-open "${url}"`)
        }
        return true
    } catch {
        return false
    }
}

// Kill a process by its name (cross-platform)
async function killProcess(name) {
    try {
        const isWindows = os.platform() === "win32"
        let command = ""
        if (isWindows) {
            const hasExe = /\.exe$/i.test(name)
            command = /^\d+$/.test(name)
                ? `taskkill /F /PID ${name} /T`
                : `taskkill /F /IM "${hasExe ? name : `${name}.exe`}" /T`
        } else {
            command = /^\d+$/.test(name) ? `kill -9 ${name}` : `pkill -f "${name}"`
        }

        await execAsync(command)
        return true
    } catch (error) {
        // If the process doesn't exist, an error is thrown. We can ignore it.
        if (error.message.includes("not found")) {
            return false
        }
        // Re-throw other errors
        throw error
    }
}

// Kill a process by the port it's listening on (cross-platform)
const MAX_PORT_NUMBER = 65_535
async function killProcessOnPort(port) {
    const n = Number(port)
    if (!Number.isInteger(n) || n < 1 || n > MAX_PORT_NUMBER) {
        return false // invalid port
    }

    try {
        const isWindows = os.platform() === "win32"

        if (isWindows) {
            // Step 1: Find PID by port. Suppress errors if no process is found.
            const netstatCmd = `netstat -aon | findstr ":${n}" | findstr "LISTENING"`
            const { stdout } = await execAsync(netstatCmd).catch(() => ({ stdout: "" }))

            if (!stdout) {
                return false // No process found
            }

            // Step 2: Parse the output to get all PIDs. A single port can have multiple processes.
            const lines = stdout.trim().split(/\r?\n/).filter(Boolean)
            const pids = lines.map((line) => line.trim().split(/\s+/).pop()).filter((pid) => pid && !Number.isNaN(pid))

            if (pids.length === 0) {
                return false // No PID found in the output
            }

            // Step 3: Kill each process by PID
            const killPromises = pids.map(async (pid) => {
                try {
                    await execAsync(`taskkill /F /PID ${pid}`)
                    return true
                } catch (error) {
                    // taskkill throws an error if the process doesn't exist, which is fine.
                    if (!error.message.includes("not found") && !error.message.includes("ERROR: The process")) {
                        throw error // Re-throw unexpected errors
                    }
                    return false
                }
            })
            const results = await Promise.all(killPromises)
            return results.some(Boolean)
        }
        // Non-Windows: Original command is generally reliable.
        const command = `lsof -ti :${n} | xargs -r kill -9`
        await execAsync(command)
        return true
    } catch (error) {
        // This outer catch handles errors from the commands themselves (e.g., lsof not found).
        // We can ignore errors indicating no process was found.
        if (error.message.includes("not found") || error.message.includes("No tasks running")) {
            return false
        }
        // Re-throw other critical errors
        throw error
    }
}

module.exports = {
    createLogger,
    getDevServerEnv,
    killProcess,
    killProcessOnPort,
    openBrowser,
    DEFAULT_ENV_VARS,
}
