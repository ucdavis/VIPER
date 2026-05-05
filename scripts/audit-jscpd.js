#!/usr/bin/env node

// Whole-project jscpd run across VueApp/src (JS/TS/Vue) and web/Areas (C#).
// Reports to ./jscpd-report/ (html + json). For CI regression detection see
// .github/workflows/code-quality.yml.

const fs = require("node:fs")
const path = require("node:path")
const { spawnSync } = require("node:child_process")

const PROJECT_ROOT = path.join(__dirname, "..")
const OUT_DIR = path.join(PROJECT_ROOT, "jscpd-report")

// Normalize Windows backslashes to forward slashes (jscpd's glob layer misreads backslash paths).
const toForwardSlashes = (p) => p.replaceAll("\\", "/")

function runJscpd(label, scanDir, minLines, extraArgs) {
    const jsEntry = path.join(PROJECT_ROOT, "node_modules", "jscpd", "bin", "jscpd")
    if (!fs.existsSync(jsEntry)) {
        console.error("❌ jscpd not found. Run 'npm install' at the repo root.")
        process.exit(1)
    }

    const args = [
        jsEntry,
        toForwardSlashes(scanDir),
        "--min-lines",
        minLines,
        "--min-tokens",
        "75",
        "--reporters",
        "console,json,html",
        "--output",
        toForwardSlashes(path.join(OUT_DIR, label)),
        ...extraArgs,
    ]

    console.log(`\n━━━ jscpd ${label} (${scanDir}) ━━━`)
    const result = spawnSync(process.execPath, args, {
        cwd: PROJECT_ROOT,
        stdio: "inherit",
        windowsHide: true,
    })
    if (result.error) {
        console.error(`❌ jscpd ${label} failed: ${result.error.message}`)
        process.exit(1)
    }
    if (result.signal) {
        console.error(`❌ jscpd ${label} terminated by signal ${result.signal}`)
        process.exit(1)
    }
    if (result.status !== 0) {
        console.error(`❌ jscpd ${label} exited with code ${result.status}`)
        process.exit(result.status ?? 1)
    }
}

// C# uses min-lines=15 to filter boilerplate (controller headers, attribute blocks, property lists)
// that dominate its 10-14 line bucket; Vue/TS stays at 10 since its clones cluster at 20+ lines.
runJscpd("vue", path.join(PROJECT_ROOT, "VueApp", "src"), "10", [])
runJscpd("csharp", path.join(PROJECT_ROOT, "web", "Areas"), "15", ["--format", "csharp", "--pattern", "**/*.cs"])

console.log(`\n✅ Reports written to ${path.relative(PROJECT_ROOT, OUT_DIR)}/`)
