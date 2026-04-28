#!/usr/bin/env node

// PR-scoped ReSharper inspectcode gate.
//
// Runs inspectcode on the working tree, parses the SARIF report, filters to
// findings located at lines the PR added or modified relative to a base ref,
// and exits non-zero if any are found. This avoids requiring a baseline scrub
// of pre-existing issues — only NEW findings at PR-touched lines block.
//
// Usage:
//   node scripts/audit-resharper-regression.js [--base origin/main]
//                                              [--sarif inspect-report/inspect.sarif]
//                                              [--skip-scan]
//
// --skip-scan reuses the SARIF from a prior `audit:resharper` run, useful in
// CI where the full scan and the gate are split into separate steps so the
// expensive scan output can be uploaded as an artifact.

const fs = require("node:fs")
const path = require("node:path")
const { spawnSync } = require("node:child_process")

const PROJECT_ROOT = path.join(__dirname, "..")
const DEFAULT_SARIF = path.join(PROJECT_ROOT, "inspect-report", "inspect.sarif")

// `git diff` output for a large solution can exceed Node's 1 MB default.
const MAX_BUFFER_BYTES = 268_435_456

// Cap how many findings we print per rule before summarising the rest.
const MAX_FINDINGS_PER_RULE = 5

function parseArgs(argv) {
    const args = { base: "origin/main", sarif: DEFAULT_SARIF, skipScan: false }
    const remaining = [...argv]
    while (remaining.length > 0) {
        const flag = remaining.shift()
        if (flag === "--base") {
            args.base = remaining.shift()
        } else if (flag === "--sarif") {
            args.sarif = remaining.shift()
        } else if (flag === "--skip-scan") {
            args.skipScan = true
        } else {
            console.error(`Unknown arg: ${flag}`)
            process.exit(2)
        }
    }
    return args
}

// Returns Map<repoRelativePath, Set<lineNumber>> of lines added/modified in
// the PR, derived from `git diff --unified=0 base...HEAD`.
function getChangedLines(baseRef) {
    const result = spawnSync("git", ["diff", "--unified=0", `${baseRef}...HEAD`, "--", "*.cs"], {
        cwd: PROJECT_ROOT,
        encoding: "utf8",
        windowsHide: true,
        maxBuffer: MAX_BUFFER_BYTES,
    })
    if (result.error || result.status !== 0) {
        const reason = result.error?.message ?? `exit ${result.status}`
        console.error(`❌ git diff failed: ${reason}`)
        process.exit(1)
    }

    const map = new Map()
    let currentFile = null
    for (const line of result.stdout.split(/\r?\n/)) {
        const fileMatch = line.match(/^\+\+\+ b\/(?<file>.+)$/)
        if (fileMatch?.groups) {
            currentFile = fileMatch.groups.file
            if (!map.has(currentFile)) {
                map.set(currentFile, new Set())
            }
        } else if (currentFile) {
            const hunkMatch = line.match(/^@@ -\d+(?:,\d+)? \+(?<start>\d+)(?:,(?<count>\d+))? @@/)
            if (hunkMatch?.groups) {
                const start = Number(hunkMatch.groups.start)
                // A hunk count of 0 means lines were removed only at this position; nothing was added.
                const count = hunkMatch.groups.count === undefined ? 1 : Number(hunkMatch.groups.count)
                for (let offset = 0; offset < count; offset += 1) {
                    map.get(currentFile).add(start + offset)
                }
            }
        }
    }
    // Drop entries for files where nothing was added (pure deletions).
    for (const [file, lines] of map) {
        if (lines.size === 0) {
            map.delete(file)
        }
    }
    return map
}

function runScan() {
    console.log("Running inspectcode (this takes a few minutes on a full solution)...")
    // Skip HTML; the gate only consumes SARIF, and inspectcode runs are expensive.
    const result = spawnSync(process.execPath, [path.join(__dirname, "audit-resharper.js"), "--format=sarif"], {
        cwd: PROJECT_ROOT,
        stdio: "inherit",
        windowsHide: true,
    })
    if (result.error || result.status !== 0) {
        const reason = result.error?.message ?? `exit ${result.status}`
        console.error(`❌ inspectcode scan failed: ${reason}`)
        process.exit(1)
    }
}

// SARIF artifactLocation URIs use forward slashes and are repo-relative; git
// diff emits forward slashes too, so a direct case-sensitive compare suffices
// on Linux/macOS. On Windows match case-insensitively.
function normalizeUri(uri) {
    let s = uri.replaceAll("\\", "/")
    if (s.startsWith("./")) {
        s = s.slice(2)
    }
    if (process.platform === "win32") {
        s = s.toLowerCase()
    }
    return s
}

function findRegressions(sarifPath, changedLines) {
    const sarif = JSON.parse(fs.readFileSync(sarifPath, "utf8"))
    const results = sarif.runs?.[0]?.results ?? []

    const changedNorm = new Map()
    for (const [file, lines] of changedLines) {
        changedNorm.set(normalizeUri(file), lines)
    }

    const regressions = []
    for (const r of results) {
        const ruleId = r.ruleId ?? "?"
        for (const loc of r.locations ?? []) {
            const uri = loc.physicalLocation?.artifactLocation?.uri
            const line = loc.physicalLocation?.region?.startLine
            if (uri && line) {
                const lines = changedNorm.get(normalizeUri(uri))
                if (lines?.has(line)) {
                    regressions.push({
                        file: uri,
                        line,
                        ruleId,
                        message: r.message?.text ?? "",
                    })
                }
            }
        }
    }
    return regressions
}

const args = parseArgs(process.argv.slice(2))

if (!args.skipScan) {
    runScan()
}

if (!fs.existsSync(args.sarif)) {
    console.error(`❌ SARIF not found: ${args.sarif}`)
    process.exit(1)
}

const changed = getChangedLines(args.base)
console.log(`Comparing against base ${args.base}: ${changed.size} C# file(s) with added/modified lines`)
if (changed.size === 0) {
    console.log("✅ No C# changes in this PR; nothing to gate.")
    process.exit(0)
}

const regressions = findRegressions(args.sarif, changed)
if (regressions.length === 0) {
    console.log("✅ No new ReSharper warnings at PR-touched lines.")
    process.exit(0)
}

console.error(`\n❌ ${regressions.length} new ReSharper warning(s) at PR-touched lines:\n`)
const byRule = new Map()
for (const r of regressions) {
    if (!byRule.has(r.ruleId)) {
        byRule.set(r.ruleId, [])
    }
    byRule.get(r.ruleId).push(r)
}
for (const [ruleId, items] of byRule) {
    console.error(`  ${ruleId} (${items.length})`)
    for (const it of items.slice(0, MAX_FINDINGS_PER_RULE)) {
        console.error(`    ${it.file}:${it.line}  ${it.message}`)
    }
    if (items.length > MAX_FINDINGS_PER_RULE) {
        console.error(`    ... and ${items.length - MAX_FINDINGS_PER_RULE} more`)
    }
}
process.exit(1)
