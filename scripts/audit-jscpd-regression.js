#!/usr/bin/env node

// Count jscpd clones for a given tree and either save as baseline or compare
// against an existing baseline (for CI regression detection).
//
// Modes:
//   --save <baseline.json> <scanDir> [extra jscpd args]
//   --check <baseline.json> <scanDir> [extra jscpd args]

const fs = require("node:fs")
const os = require("node:os")
const path = require("node:path")
const { spawnSync } = require("node:child_process")

const PROJECT_ROOT = path.join(__dirname, "..")

// Running fallow/jscpd can exceed Node's 1MB default (256 MB).
const MAX_BUFFER_BYTES = 268_435_456

// Normalize Windows backslashes to forward slashes (jscpd's glob layer misreads backslash paths).
const toForwardSlashes = (p) => p.replaceAll("\\", "/")

function runJscpd(scanDir, extraArgs) {
    const jsEntry = path.join(PROJECT_ROOT, "node_modules", "jscpd", "bin", "jscpd")
    if (!fs.existsSync(jsEntry)) {
        console.error("❌ jscpd not found. Run 'npm install' at the repo root.")
        process.exit(1)
    }

    const outDir = fs.mkdtempSync(path.join(os.tmpdir(), "jscpd-reg-"))

    // Default min-lines=10 suits Vue/TS; C# callers pass --min-lines 15 to filter
    // boilerplate-heavy matches. Honor an explicit override from extraArgs.
    const callerMinLines = extraArgs.includes("--min-lines")
    const defaultMinLinesArgs = callerMinLines ? [] : ["--min-lines", "10"]

    const args = [
        jsEntry,
        toForwardSlashes(scanDir),
        ...defaultMinLinesArgs,
        "--min-tokens",
        "75",
        "--silent",
        "--reporters",
        "json",
        "--output",
        toForwardSlashes(outDir),
        ...extraArgs,
    ]

    const result = spawnSync(process.execPath, args, {
        cwd: PROJECT_ROOT,
        encoding: "utf8",
        windowsHide: true,
        maxBuffer: MAX_BUFFER_BYTES,
    })

    if (result.error || result.status !== 0) {
        const reason = result.error?.message ?? `exit ${result.status}`
        console.error(`❌ jscpd failed: ${reason}`)
        process.exit(1)
    }

    const reportPath = path.join(outDir, "jscpd-report.json")
    if (!fs.existsSync(reportPath)) {
        console.error(`❌ jscpd did not produce a report at ${reportPath}`)
        process.exit(1)
    }

    const report = JSON.parse(fs.readFileSync(reportPath, "utf8"))
    fs.rmSync(outDir, { recursive: true, force: true })
    return {
        clones: report.statistics.total.clones,
        duplicatedLines: report.statistics.total.duplicatedLines,
    }
}

const [mode, baselinePath, scanDir, ...extra] = process.argv.slice(2)
if (!mode || !baselinePath || !scanDir || !["--save", "--check"].includes(mode)) {
    console.error("Usage: audit-jscpd-regression.js --save|--check <baseline.json> <scanDir> [extra jscpd args]")
    process.exit(2)
}

const stats = runJscpd(scanDir, extra)

if (mode === "--save") {
    fs.writeFileSync(baselinePath, JSON.stringify(stats, null, 2))
    console.log(
        `✅ jscpd baseline saved to ${baselinePath}: ${stats.clones} clones, ${stats.duplicatedLines} duplicated lines`,
    )
    process.exit(0)
}

// --check
if (!fs.existsSync(baselinePath)) {
    console.error(`❌ Baseline file not found: ${baselinePath}`)
    process.exit(1)
}
const baseline = JSON.parse(fs.readFileSync(baselinePath, "utf8"))
const delta = stats.clones - baseline.clones
const lineDelta = stats.duplicatedLines - baseline.duplicatedLines

console.log(
    `📊 jscpd regression check\n  main:     ${baseline.clones} clones / ${baseline.duplicatedLines} lines\n  current:  ${stats.clones} clones / ${stats.duplicatedLines} lines\n  delta:    ${delta >= 0 ? "+" : ""}${delta} clones / ${lineDelta >= 0 ? "+" : ""}${lineDelta} lines`,
)

if (delta > 0) {
    console.error(`\n❌ REGRESSION: +${delta} new clone group(s) introduced vs main. Please refactor before merging.`)
    process.exit(1)
}

console.log("\n✅ No regression (clone count did not increase).")
process.exit(0)
