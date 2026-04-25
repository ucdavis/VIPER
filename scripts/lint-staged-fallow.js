#!/usr/bin/env node

// Run fallow dead-code on VueApp and report findings scoped to input files.
// fallow's analysis is whole-project, so the file-list filter is applied to
// the output only. Findings block the commit when LINT_BLOCK_ON_WARNINGS=true
// (matches the lint-staged-ts.js convention); otherwise they're informational.
// CI runs the full whole-project check with regression detection
// (see .github/workflows/code-quality.yml).
//
// Usage:
//   node scripts/lint-staged-fallow.js <file> [<file>...]
//   node scripts/lint-staged-fallow.js --files-from=<path>

const fs = require("node:fs")
const path = require("node:path")
const { spawnSync } = require("node:child_process")
const { parseArguments, shouldBlockOnWarnings } = require("./lib/lint-staged-common")
const { createLogger } = require("./lib/script-utils")

const { rawFiles } = parseArguments()
const logger = createLogger("Fallow")

const PROJECT_ROOT = path.join(__dirname, "..")
const VUEAPP_DIR = path.join(PROJECT_ROOT, "VueApp")

// Running fallow/jscpd can exceed Node's 1MB default (256 MB).
const MAX_BUFFER_BYTES = 268_435_456

// Fallow only analyzes files inside VueApp/src. Restrict inputs accordingly.
const SCOPED_EXTENSIONS = new Set([".ts", ".tsx", ".js", ".jsx", ".vue"])

function toVueAppRelative(filePath) {
    const abs = path.resolve(PROJECT_ROOT, filePath)
    const rel = path.relative(VUEAPP_DIR, abs)
    if (rel.startsWith("..") || path.isAbsolute(rel)) {
        return null
    }
    return rel.replaceAll("\\", "/")
}

const scopedFiles = new Set()
for (const f of rawFiles) {
    const ext = path.extname(f).toLowerCase()
    if (SCOPED_EXTENSIONS.has(ext)) {
        const rel = toVueAppRelative(f)
        if (rel && rel.startsWith("src/")) {
            scopedFiles.add(rel)
        }
    }
}

if (scopedFiles.size === 0) {
    logger.success("No VueApp files to check with fallow.")
    process.exit(0)
}

function runFallow() {
    // Invoke the JS entry point via node to sidestep Windows .cmd-shim arg-escaping bugs.
    const jsEntry = path.join(PROJECT_ROOT, "node_modules", "fallow", "bin", "fallow")
    if (!fs.existsSync(jsEntry)) {
        logger.error("fallow not found in node_modules. Run 'npm install' at the repo root.")
        return null
    }

    const args = [jsEntry, "dead-code", "--format", "json", "--quiet"]

    const result = spawnSync(process.execPath, args, {
        cwd: VUEAPP_DIR,
        encoding: "utf8",
        windowsHide: true,
        maxBuffer: MAX_BUFFER_BYTES,
    })

    if (result.error) {
        logger.error(`Failed to run fallow: ${result.error.message}`)
        return null
    }

    try {
        return JSON.parse(result.stdout)
    } catch {
        logger.error("Failed to parse fallow JSON output.")
        return null
    }
}

function normalizeIssuePath(issuePath) {
    if (!issuePath) {
        return ""
    }
    const abs = path.resolve(issuePath)
    const rel = path.relative(VUEAPP_DIR, abs).replaceAll("\\", "/")
    return rel
}

function filterIssuesToFiles(issues, pathKey = "path") {
    return (issues || []).filter((issue) => {
        const rel = normalizeIssuePath(issue[pathKey])
        return scopedFiles.has(rel)
    })
}

function filterDuplicateExportsToFiles(issues) {
    return (issues || []).filter((issue) =>
        (issue.locations || []).some((loc) => scopedFiles.has(normalizeIssuePath(loc.path))),
    )
}

function printIssues(label, items, formatItem) {
    if (items.length === 0) {
        return
    }
    logger.warning(`${label} (${items.length}):`)
    for (const item of items) {
        logger.plain(`  ${formatItem(item)}`)
    }
}

logger.plain(`🔎 Fallow — dead-code check (${scopedFiles.size} file(s) in scope)`)

const report = runFallow()
if (!report) {
    // Non-fatal: fallow crashed, let developer keep going.
    process.exit(0)
}

const unusedFiles = filterIssuesToFiles(report.unused_files).map((i) => ({
    file: normalizeIssuePath(i.path),
}))

const unusedExports = filterIssuesToFiles(report.unused_exports).map((i) => ({
    file: normalizeIssuePath(i.path),
    line: i.line,
    name: i.export_name,
}))

const unusedTypes = filterIssuesToFiles(report.unused_types).map((i) => ({
    file: normalizeIssuePath(i.path),
    line: i.line,
    name: i.export_name,
}))

const duplicateExports = filterDuplicateExportsToFiles(report.duplicate_exports).map((i) => ({
    name: i.export_name,
    locations: (i.locations || []).map((l) => `${normalizeIssuePath(l.path)}:${l.line}`),
}))

const totalFindings = unusedFiles.length + unusedExports.length + unusedTypes.length + duplicateExports.length

if (totalFindings === 0) {
    logger.success("No fallow findings in scope.")
    process.exit(0)
}

logger.plain("")
printIssues("Unused files", unusedFiles, (x) => `${x.file} — file appears unreachable from entry points`)
printIssues("Unused exports", unusedExports, (x) => `${x.file}:${x.line} — export '${x.name}' has no importers`)
printIssues("Unused type exports", unusedTypes, (x) => `${x.file}:${x.line} — type '${x.name}' has no importers`)
printIssues("Duplicate exports", duplicateExports, (x) => `'${x.name}' exported from: ${x.locations.join(", ")}`)

const blocking = shouldBlockOnWarnings()
const blockSuffix = blocking ? "blocking commit" : "non-blocking"
logger.plain(`\n📊 Fallow Summary: ${totalFindings} finding(s) in staged files (${blockSuffix}).`)
logger.plain("💡 Run 'npm run audit:fallow' for the whole-project report.")

process.exit(blocking ? 1 : 0)
