#!/usr/bin/env node

require('dotenv').config({ path: '.env.local' });

// Dynamic import for ES modules
let fkill;
async function getFkill() {
    if (!fkill) {
        const fkillModule = await import('fkill');
        fkill = fkillModule.default;
    }
    return fkill;
}

let open;
async function getOpen() {
    if (!open) {
        const openModule = await import('open');
        open = openModule.default;
    }
    return open;
}

/**
 * Console logging with colors
 */
const colors = {
    reset: '\x1b[0m',
    red: '\x1b[31m',
    green: '\x1b[32m',
    yellow: '\x1b[33m',
    blue: '\x1b[34m',
    cyan: '\x1b[36m',
    gray: '\x1b[90m'
};

/**
 * Create logger with colored output and optional prefix
 * @param {string} prefix - Optional prefix for log messages
 * @returns {object} - Logger with error, success, info, warning, and log methods
 */
function createLogger(prefix = '') {
    const log = (message, color = 'reset') => {
        const prefixStr = prefix ? `[${prefix}] ` : '';
        console.log(`${colors[color]}${prefixStr}${message}${colors.reset}`);
    };

    return {
        error: (message) => log(`❌ ${message}`, 'red'),
        success: (message) => log(`✅ ${message}`, 'green'),
        info: (message) => log(`ℹ️  ${message}`, 'blue'),
        warning: (message) => log(`⚠️  ${message}`, 'yellow'),
        log
    };
}

/**
 * Load environment variables with defaults for common dev server ports
 * @returns {object} - Environment variables object
 */
function getDevServerEnv() {
    return {
        VITE_PORT: getEnvVar('VITE_PORT', 5173, 'int'),
        VITE_HMR_PORT: getEnvVar('VITE_HMR_PORT', 24678, 'int'),
        ASPNETCORE_HTTPS_PORT: getEnvVar('ASPNETCORE_HTTPS_PORT', 7158, 'int'),
        ASPNETCORE_HTTP_PORT: getEnvVar('ASPNETCORE_HTTP_PORT', 5158, 'int'),
        MAILPIT_SMTP_PORT: getEnvVar('MAILPIT_SMTP_PORT', 1025, 'int'),
        MAILPIT_WEB_PORT: getEnvVar('MAILPIT_WEB_PORT', 8025, 'int'),
        VITE_EDITOR: getEnvVar('VITE_EDITOR', 'code')
    };
}

/**
 * Get environment variable with fallback
 * @param {string} key - Environment variable key
 * @param {*} defaultValue - Default value if not set
 * @param {string} type - Type conversion ('int', 'float', 'bool', 'string')
 * @returns {*} - Environment variable value or default
 */
function getEnvVar(key, defaultValue, type = 'string') {
    const value = process.env[key];
    
    if (value === undefined || value === null || value === '') {
        return defaultValue;
    }
    
    switch (type) {
        case 'int':
            const n = parseInt(value, 10);
            return Number.isFinite(n) ? n : defaultValue;
        case 'float':
            return parseFloat(value) || defaultValue;
        case 'bool':
            return value.toLowerCase() === 'true';
        default:
            return value;
    }
}

/**
 * Cross-platform process killing by name or PID
 * @param {string|number} target - Process name or PID
 * @param {object} options - Kill options
 * @returns {Promise<boolean>} - Success status
 */
async function killProcess(target, options = { force: true }) {
    try {
        const fkillFn = await getFkill();
        await fkillFn(target, options);
        return true;
    } catch (error) {
        return false;
    }
}

/**
 * Cross-platform process killing by port
 * @param {number} port - Port number
 * @returns {Promise<boolean>} - Success status
 */
async function killProcessOnPort(port) {
    try {
        const fkillFn = await getFkill();
        await fkillFn(`:${port}`, { force: true });
        return true;
    } catch (error) {
        return false;
    }
}

/**
 * Open URL in default browser (cross-platform)
 * @param {string} url - URL to open
 * @returns {Promise<boolean>} - Success status
 */
async function openBrowser(url) {
    try {
        const openFn = await getOpen();
        await openFn(url);
        return true;
    } catch (error) {
        return false;
    }
}

module.exports = {
    colors,
    createLogger,
    getDevServerEnv,
    getEnvVar,
    killProcess,
    killProcessOnPort,
    openBrowser
};