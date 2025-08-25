#!/usr/bin/env node

require("dotenv").config({ path: ".env.local" })

// Dynamic import for ES modules
let fkill = null
async function getFkill() {
    if (!fkill) {
        const fkillModule = await import("fkill")
        fkill = fkillModule.default
    }
    return fkill
}

let open = null
async function getOpen() {
    if (!open) {
        const openModule = await import("open")
        open = openModule.default
    }
    return open
}

/**
 * Console logging with colors
 */
const colors = {
    reset: "\u001B[0m",
    red: "\u001B[31m",
    green: "\u001B[32m",
    yellow: "\u001B[33m",
    blue: "\u001B[34m",
    cyan: "\u001B[36m",
    gray: "\u001B[90m",
}

/**
 * Create logger with colored output and optional prefix
 * @param {string} prefix - Optional prefix for log messages
 * @returns {object} - Logger with error, success, info, warning, log methods, and plain methods without prefix
 */
function createLogger(prefix = "") {
    const log = (message, color = "reset", usePrefix = true) => {
        const prefixStr = usePrefix && prefix ? `[${prefix}] ` : ""
        console.log(`${colors[color]}${prefixStr}${message}${colors.reset}`)
    }

    return {
        // Existing methods with prefix
        error: (message) => log(`❌ ${message}`, "red"),
        success: (message) => log(`✅ ${message}`, "green"),
        info: (message) => log(`ℹ️  ${message}`, "blue"),
        warning: (message) => log(`⚠️  ${message}`, "yellow"),
        log,

        // New methods without prefix for clean output
        plain: (message) => log(message, "reset", false),
        plainError: (message) => log(message, "red", false),
        plainWarning: (message) => log(message, "yellow", false),
        plainInfo: (message) => log(message, "blue", false),
        plainSuccess: (message) => log(message, "green", false),
    }
}

// Default port constants
const DEFAULT_PORTS = {
    VITE: 5173,
    VITE_HMR: 24_678,
    ASPNETCORE_HTTPS: 7158,
    ASPNETCORE_HTTP: 5158,
    MAILPIT_SMTP: 1025,
    MAILPIT_WEB: 8025,
}

/**
 * Load environment variables with defaults for common dev server ports
 * @returns {object} - Environment variables object
 */
function getDevServerEnv() {
    return {
        VITE_PORT: getEnvVar("VITE_PORT", DEFAULT_PORTS.VITE, "int"),
        VITE_HMR_PORT: getEnvVar("VITE_HMR_PORT", DEFAULT_PORTS.VITE_HMR, "int"),
        ASPNETCORE_HTTPS_PORT: getEnvVar("ASPNETCORE_HTTPS_PORT", DEFAULT_PORTS.ASPNETCORE_HTTPS, "int"),
        ASPNETCORE_HTTP_PORT: getEnvVar("ASPNETCORE_HTTP_PORT", DEFAULT_PORTS.ASPNETCORE_HTTP, "int"),
        MAILPIT_SMTP_PORT: getEnvVar("MAILPIT_SMTP_PORT", DEFAULT_PORTS.MAILPIT_SMTP, "int"),
        MAILPIT_WEB_PORT: getEnvVar("MAILPIT_WEB_PORT", DEFAULT_PORTS.MAILPIT_WEB, "int"),
        VITE_EDITOR: getEnvVar("VITE_EDITOR", "code"),
    }
}

/**
 * Get environment variable with fallback
 * @param {string} key - Environment variable key
 * @param {*} defaultValue - Default value if not set
 * @param {string} type - Type conversion ('int', 'float', 'bool', 'string')
 * @returns {*} - Environment variable value or default
 */
function getEnvVar(key, defaultValue, type = "string") {
    const value = process.env[key]

    if (!value && value !== 0 && value !== false) {
        return defaultValue
    }

    switch (type) {
        case "int": {
            const n = Number.parseInt(value, 10)
            return Number.isFinite(n) ? n : defaultValue
        }
        case "float": {
            return Number.parseFloat(value) || defaultValue
        }
        case "bool": {
            return value.toLowerCase() === "true"
        }
        default: {
            return value
        }
    }
}

/**
 * Cross-platform process killing by name or PID
 * @param {string|number} target - Process name or PID
 * @param {object} options - Kill options
 * @returns {Promise<boolean>} - Success status
 */
async function killProcess(target, options) {
    const opts = { force: true, ...options }
    try {
        const fkillFn = await getFkill()
        await fkillFn(target, opts)
        return true
    } catch {
        return false
    }
}

/**
 * Cross-platform process killing by port
 * @param {number} port - Port number
 * @returns {Promise<boolean>} - Success status
 */
async function killProcessOnPort(port) {
    try {
        const fkillFn = await getFkill()
        await fkillFn(`:${port}`, { force: true })
        return true
    } catch {
        return false
    }
}

/**
 * Open URL in default browser (cross-platform)
 * @param {string} url - URL to open
 * @returns {Promise<boolean>} - Success status
 */
async function openBrowser(url) {
    try {
        const openFn = await getOpen()
        await openFn(url)
        return true
    } catch {
        return false
    }
}

module.exports = {
    colors,
    createLogger,
    getDevServerEnv,
    getEnvVar,
    killProcess,
    killProcessOnPort,
    openBrowser,
}
