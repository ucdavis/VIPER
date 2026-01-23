#!/usr/bin/env node
/* oxlint-disable no-await-in-loop -- Polling requires sequential await */

// Shared utilities for Node.js scripts (cross-platform)
// Environment variables loaded via --env-file-if-exists=.env.local in package.json

const { exec, execFileSync } = require("node:child_process")
const { promisify } = require("node:util")
const os = require("node:os")

const execAsync = promisify(exec)

const { env } = process

// Process termination wait settings (Windows)
const DEFAULT_TERMINATION_WAIT_MS = 5000
const TERMINATION_CHECK_INTERVAL_MS = 100
const KILL_TERMINATION_WAIT_MS = 3000

/**
 * Get staged files that have unstaged changes (partially staged files)
 * @param {string[]} stagedFiles - Array of staged file paths
 * @returns {string[]} Array of partially staged file paths
 */
function getPartiallyStaged(stagedFiles) {
    if (stagedFiles.length === 0) {
        return []
    }
    try {
        const result = execFileSync("git", ["diff", "--name-only", "--", ...stagedFiles], {
            encoding: "utf8",
        })
        return result.trim().split("\n").filter(Boolean)
    } catch {
        return []
    }
}

/**
 * Check for partially staged files and exit with error if found
 * @param {string[]} stagedFiles - Array of staged file paths
 * @param {Object} logger - Logger instance with error method
 * @returns {void} Exits process with code 1 if partially staged files found
 */
function checkPartiallyStaged(stagedFiles, logger) {
    const partiallyStaged = getPartiallyStaged(stagedFiles)
    if (partiallyStaged.length > 0) {
        logger.error("Some staged files have unstaged changes:")
        for (const file of partiallyStaged) {
            console.error(`   - ${file}`)
        }
        console.error("")
        console.error("The linter would check your working directory, but the staged")
        console.error("content is different. Please either:")
        console.error("  1. Stage all changes: git add <files>")
        console.error("  2. Stash unstaged changes: git stash -k")
        console.error("")
        process.exit(1)
    }
}

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

// Get development server environment variables from process.env or defaults
// Environment is loaded via Node's --env-file-if-exists=.env.local flag
function getDevServerEnv() {
    const mergedEnv = { ...DEFAULT_ENV_VARS }
    for (const key in DEFAULT_ENV_VARS) {
        if (Object.hasOwn(DEFAULT_ENV_VARS, key)) {
            const value = Number.parseInt(env[key], 10)
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
        // pkill exits with code 1 when no processes match, kill exits with an error
        // taskkill includes "not found" in its error message
        if (
            error.message.includes("not found") ||
            error.message.includes("No such process") ||
            error.message.includes("ERROR: The process") ||
            error.code === 1 // pkill returns exit code 1 when no processes match
        ) {
            return false
        }
        // Re-throw other errors
        throw error
    }
}

// Check if a process exists by PID (Windows only)
async function checkProcessExists(pid) {
    try {
        // The /FI "PID eq ${pid}" filter ensures exact PID matching (no false positives)
        const { stdout } = await execAsync(`tasklist /FI "PID eq ${pid}" /NH`)
        // If the process is gone, tasklist will show "INFO: No tasks are running"
        return !stdout.includes("No tasks")
    } catch {
        // If tasklist fails, assume process is gone
        return false
    }
}

// Helper function to wait for a process to fully terminate (Windows-specific)
async function waitForProcessTermination(pid, maxWaitMs = DEFAULT_TERMINATION_WAIT_MS) {
    const isWindows = os.platform() === "win32"
    if (!isWindows) {
        return true // Non-Windows doesn't need explicit waiting
    }

    const startTime = Date.now()
    while (Date.now() - startTime < maxWaitMs) {
        const exists = await checkProcessExists(pid)
        if (!exists) {
            return true // Process is terminated
        }
        await new Promise((resolve) => setTimeout(resolve, TERMINATION_CHECK_INTERVAL_MS))
    }
    return false // Timeout reached
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
                    // Wait for the process to fully terminate before continuing
                    await waitForProcessTermination(pid, KILL_TERMINATION_WAIT_MS)
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
    checkPartiallyStaged,
    createLogger,
    getDevServerEnv,
    getPartiallyStaged,
    killProcess,
    killProcessOnPort,
    openBrowser,
    DEFAULT_ENV_VARS,
}
