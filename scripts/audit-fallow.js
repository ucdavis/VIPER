#!/usr/bin/env node

// Whole-project fallow audit: dead code + duplicates + health (human-readable).
// For CI regression detection, see .github/workflows/code-quality.yml.

const fs = require("node:fs")
const path = require("node:path")
const { spawnSync } = require("node:child_process")

const PROJECT_ROOT = path.join(__dirname, "..")
const VUEAPP_DIR = path.join(PROJECT_ROOT, "VueApp")

function runFallow(subcommand, extraArgs = []) {
    const jsEntry = path.join(PROJECT_ROOT, "node_modules", "fallow", "bin", "fallow")
    if (!fs.existsSync(jsEntry)) {
        console.error("❌ fallow not found. Run 'npm install' at the repo root.")
        process.exit(1)
    }

    const args = [jsEntry, subcommand, ...extraArgs]

    console.log(`\n━━━ fallow ${subcommand} ━━━`)
    const result = spawnSync(process.execPath, args, {
        cwd: VUEAPP_DIR,
        stdio: "inherit",
        windowsHide: true,
    })
    if (result.error) {
        console.error(`❌ fallow ${subcommand} failed: ${result.error.message}`)
        process.exit(1)
    }
}

runFallow("dead-code")
runFallow("dupes")
runFallow("health")
