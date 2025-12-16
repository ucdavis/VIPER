#!/usr/bin/env node

/**
 * Cross-platform precommit runner
 * Invokes .husky/pre-commit using Git's bash (works on Windows)
 */

const { spawnSync } = require("node:child_process")
const path = require("node:path")

const { env } = process
const hookPath = path.join(process.cwd(), ".husky", "pre-commit")

// Try to find bash - Git for Windows installs it
const bashPaths = [
    "bash", // If in PATH
    // Standard Git for Windows locations
    String.raw`C:\Program Files\Git\bin\bash.exe`,
    String.raw`C:\Program Files (x86)\Git\bin\bash.exe`,
    // User-local Git installation (e.g., from winget or portable)
    env.LOCALAPPDATA && path.join(env.LOCALAPPDATA, "Programs", "Git", "bin", "bash.exe"),
    env.USERPROFILE && path.join(env.USERPROFILE, "AppData", "Local", "Programs", "Git", "bin", "bash.exe"),
    // Program Files variants
    env.PROGRAMFILES && path.join(env.PROGRAMFILES, "Git", "bin", "bash.exe"),
    env["PROGRAMFILES(X86)"] && path.join(env["PROGRAMFILES(X86)"], "Git", "bin", "bash.exe"),
].filter(Boolean)

let bashPath = null
for (const p of bashPaths) {
    try {
        // Test if bash works
        const result = spawnSync(p, ["--version"], { encoding: "utf8", timeout: 2000 })
        if (result.status === 0) {
            bashPath = p
            break
        }
    } catch {
        // Try next path
    }
}

if (!bashPath) {
    console.error("❌ Could not find bash. Please install Git for Windows.")
    console.error("   Download from: https://git-scm.com/download/win")
    process.exit(1)
}

// Run the pre-commit hook
const result = spawnSync(bashPath, [hookPath], {
    stdio: "inherit",
    cwd: process.cwd(),
    env,
})

if (result.error) {
    console.error(`❌ Failed to run pre-commit hook: ${result.error.message}`)
    process.exit(1)
}

process.exit(result.status || 0)
