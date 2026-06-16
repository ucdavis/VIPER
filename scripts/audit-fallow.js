#!/usr/bin/env node

// Whole-project fallow audit: dead code + duplicates + health (human-readable).
// For CI regression detection, see .github/workflows/code-quality.yml.

const fs = require("node:fs")
const path = require("node:path")
const { spawnSync } = require("node:child_process")

const PROJECT_ROOT = path.join(__dirname, "..")
const VUEAPP_DIR = path.join(PROJECT_ROOT, "VueApp")

const JS_ENTRY = path.join(PROJECT_ROOT, "node_modules", "fallow", "bin", "fallow")

const runFallow = (subcommand, extraArgs = []) => {
    const args = [JS_ENTRY, subcommand, ...extraArgs]

    console.log(`\n━━━ fallow ${subcommand} ━━━`)
    const result = spawnSync(process.execPath, args, {
        cwd: VUEAPP_DIR,
        stdio: "inherit",
        windowsHide: true,
    })
    if (result.error) {
        console.error(`❌ fallow ${subcommand} failed to start: ${result.error.message}`)
        return 1
    }
    if (result.signal) {
        console.error(`❌ fallow ${subcommand} terminated by signal ${result.signal}`)
        return 1
    }
    // A non-zero status means findings were reported (dead-code exits non-zero
    // whenever anything is found), not a crash. Surface it via the aggregate
    // exit code, but keep running the remaining analyses.
    return result.status ?? 1
}

if (!fs.existsSync(JS_ENTRY)) {
    console.error("❌ fallow not found. Run 'npm install' at the repo root.")
    process.exit(1)
}

// Run every analysis unconditionally so findings in one don't hide the others'
// reports; aggregate exit codes at the end (mirrors scripts/audit.js).
const codes = ["dead-code", "dupes", "health"].map((cmd) => runFallow(cmd))
const max = Math.max(0, ...codes)
if (max !== 0) {
    process.exit(max)
}
