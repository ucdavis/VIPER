#!/usr/bin/env node

// Run jscpd against VueApp/src (.ts/.js/.vue) and/or web/Areas (.cs) as needed,
// then report clones that involve any of the input files. Findings block the
// commit when LINT_BLOCK_ON_WARNINGS=true (matches lint-staged-ts.js); otherwise
// they're informational. Whole-project runs live in `npm run audit:dupes`; CI
// runs with regression detection (see .github/workflows/code-quality.yml).
//
// Usage:
//   node scripts/lint-staged-jscpd.js <file> [<file>...]
//   node scripts/lint-staged-jscpd.js --files-from=<path>

const fs = require("node:fs")
const os = require("node:os")
const path = require("node:path")
const { spawnSync } = require("node:child_process")
const { parseArguments, shouldBlockOnWarnings } = require("./lib/lint-staged-common")
const { createLogger } = require("./lib/script-utils")

const { rawFiles } = parseArguments()
const logger = createLogger("JSCPD")

const PROJECT_ROOT = path.join(__dirname, "..")
const VUEAPP_SRC = path.join(PROJECT_ROOT, "VueApp", "src")
const WEB_AREAS = path.join(PROJECT_ROOT, "web", "Areas")

// Per-tree min-lines: C# gets a higher floor because ~half its 10-14 line
// matches are boilerplate (using headers, attribute blocks, property lists);
// Vue/TS duplicates cluster at 20+ lines, so 10 stays meaningful there.
const TS_MIN_LINES = "10"
const CS_MIN_LINES = "15"
const MIN_TOKENS = "75"

// Running fallow/jscpd can exceed Node's 1MB default (256 MB).
const MAX_BUFFER_BYTES = 268_435_456

const TS_EXTENSIONS = new Set([".ts", ".tsx", ".js", ".jsx", ".vue"])
const CS_EXTENSIONS = new Set([".cs"])

// Normalize Windows backslashes to forward slashes (jscpd's glob layer misreads backslash paths).
const toForwardSlashes = (p) => p.replaceAll("\\", "/")

function toAbs(filePath) {
    return path.resolve(PROJECT_ROOT, filePath)
}

function toRelative(base, abs) {
    return path.relative(base, abs).replaceAll("\\", "/")
}

// Bucket input files by which tree they live in. Files outside these trees are ignored.
const tsFiles = new Set()
const csFiles = new Set()
for (const f of rawFiles) {
    const abs = toAbs(f)
    const ext = path.extname(abs).toLowerCase()
    if (TS_EXTENSIONS.has(ext)) {
        const rel = toRelative(VUEAPP_SRC, abs)
        if (!rel.startsWith("..")) {
            tsFiles.add(rel)
        }
    } else if (CS_EXTENSIONS.has(ext)) {
        const rel = toRelative(WEB_AREAS, abs)
        if (!rel.startsWith("..")) {
            csFiles.add(rel)
        }
    }
}

if (tsFiles.size === 0 && csFiles.size === 0) {
    logger.success("No files to check with jscpd.")
    process.exit(0)
}

function runJscpd(scanDir, jsonOutPath, minLines, formatFlags) {
    // Invoke the JS entry point via node rather than the .cmd shim so we avoid
    // Windows argument-escaping bugs when paths contain backslashes.
    const jsEntry = path.join(PROJECT_ROOT, "node_modules", "jscpd", "bin", "jscpd")
    if (!fs.existsSync(jsEntry)) {
        logger.error("jscpd not found in node_modules. Run 'npm install' at the repo root.")
        return null
    }

    const args = [
        jsEntry,
        toForwardSlashes(scanDir),
        "--min-lines",
        minLines,
        "--min-tokens",
        MIN_TOKENS,
        "--silent",
        "--reporters",
        "json",
        "--output",
        toForwardSlashes(jsonOutPath),
        ...formatFlags,
    ]

    const result = spawnSync(process.execPath, args, {
        cwd: PROJECT_ROOT,
        encoding: "utf8",
        windowsHide: true,
        maxBuffer: MAX_BUFFER_BYTES,
    })

    if (result.error) {
        logger.error(`Failed to run jscpd: ${result.error.message}`)
        return null
    }

    const reportPath = path.join(jsonOutPath, "jscpd-report.json")
    if (!fs.existsSync(reportPath)) {
        return null
    }

    try {
        return JSON.parse(fs.readFileSync(reportPath, "utf8"))
    } catch {
        logger.error("Failed to parse jscpd JSON report.")
        return null
    }
}

function filterCloneGroupsByFiles(duplicates, baseDir, scopedSet) {
    const groups = []
    for (const dup of duplicates || []) {
        const a = (dup.firstFile && dup.firstFile.name) || ""
        const b = (dup.secondFile && dup.secondFile.name) || ""
        // JSCPD returns names relative to cwd. Rebase to the scan dir.
        const aRel = path.relative(baseDir, path.resolve(PROJECT_ROOT, a)).replaceAll("\\", "/")
        const bRel = path.relative(baseDir, path.resolve(PROJECT_ROOT, b)).replaceAll("\\", "/")
        if (scopedSet.has(aRel) || scopedSet.has(bRel)) {
            groups.push({
                a: `${aRel}:${dup.firstFile.start}-${dup.firstFile.end}`,
                b: `${bRel}:${dup.secondFile.start}-${dup.secondFile.end}`,
                lines: dup.lines,
                format: dup.format,
            })
        }
    }
    return groups
}

function printCloneGroups(label, groups) {
    if (groups.length === 0) {
        return
    }
    logger.warning(`${label} (${groups.length}):`)
    for (const g of groups) {
        logger.plain(`  ${g.lines} lines [${g.format}] — ${g.a}  <=>  ${g.b}`)
    }
}

function withTempDir(fn) {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), "jscpd-lint-"))
    try {
        return fn(dir)
    } finally {
        try {
            fs.rmSync(dir, { recursive: true, force: true })
        } catch {
            /* Best effort */
        }
    }
}

let totalFindings = 0

if (tsFiles.size > 0) {
    logger.plain(`🔎 JSCPD — scanning VueApp/src (${tsFiles.size} staged file(s) in scope)`)
    const tsGroups = withTempDir((tmp) =>
        filterCloneGroupsByFiles(runJscpd(VUEAPP_SRC, tmp, TS_MIN_LINES, [])?.duplicates, VUEAPP_SRC, tsFiles),
    )
    printCloneGroups("Clone groups involving staged VueApp files", tsGroups)
    totalFindings += tsGroups.length
}

if (csFiles.size > 0) {
    logger.plain(`🔎 JSCPD — scanning web/Areas (${csFiles.size} staged file(s) in scope)`)
    const csGroups = withTempDir((tmp) =>
        filterCloneGroupsByFiles(
            runJscpd(WEB_AREAS, tmp, CS_MIN_LINES, ["--format", "csharp", "--pattern", "**/*.cs"])?.duplicates,
            WEB_AREAS,
            csFiles,
        ),
    )
    printCloneGroups("Clone groups involving staged C# files", csGroups)
    totalFindings += csGroups.length
}

if (totalFindings === 0) {
    logger.success("No duplication findings involving staged files.")
    process.exit(0)
}

const blocking = shouldBlockOnWarnings()
const blockSuffix = blocking ? "blocking commit" : "non-blocking"
logger.plain(`\n📊 JSCPD Summary: ${totalFindings} clone group(s) touching staged files (${blockSuffix}).`)
logger.plain("💡 Run 'npm run audit:dupes' for the whole-project report.")

process.exit(blocking ? 1 : 0)
