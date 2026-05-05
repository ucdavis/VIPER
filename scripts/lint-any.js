#!/usr/bin/env node

const fs = require("node:fs")
const os = require("node:os")
const path = require("node:path")
const { spawn, spawnSync } = require("node:child_process")

const { env } = process

// Windows command line limit is ~8191 chars. Use a conservative threshold
// to account for the node executable path and script path overhead.
const MAX_ARG_LENGTH = 7000

/**
 * Smart linter that automatically routes files to the correct linter based on location and type
 * Shows all warnings (equivalent to running with LINT_BLOCK_ON_WARNINGS=false)
 *
 * Usage:
 *   npm run lint <file>           - Lint a specific file
 *   npm run lint <folder>         - Lint all files in a folder
 *   npm run lint <pattern>        - Lint files matching a pattern
 *
 * Examples:
 *   npm run lint web/Views/Home/Index.cshtml
 *   npm run lint VueApp/src/components/HelloWorld.vue
 *   npm run lint web/wwwroot/css/directory.css
 *   npm run lint VueApp/src
 */

// Get command line arguments
const args = process.argv.slice(2)
const shouldFix = args.includes("--fix")
const shouldClearCache = args.includes("--clear-cache")
const inputArgs = args.filter((arg) => !["--fix", "--clear-cache"].includes(arg))

if (inputArgs.length === 0) {
    console.log(`
🔍 Smart Linter - Routes files to the correct linter automatically

Usage:
  npm run lint [-- --fix] [-- --clear-cache] <file|folder|pattern>

Options:
  --fix          Auto-fix issues where possible
  --clear-cache  Clear the build cache before linting

Examples:
  npm run lint web/Views/Home/Index.cshtml
  npm run lint VueApp/src/components/HelloWorld.vue
  npm run lint web/wwwroot/css/directory.css
  npm run lint VueApp/src
  npm run lint -- --fix VueApp/src  (auto-fix issues)
  npm run lint -- --clear-cache web/  (clear cache and lint)

Supported file types:
  📄 .css, .vue → CSS/Stylelint (accessibility & style)
  🎯 .vue → Vue ESLint (security & quality)
  ⚡ .js, .ts → Oxlint (security & quality)
  🌐 .cshtml → CSHTML ESLint (security & accessibility)
  🔷 .cs → .NET/SonarAnalyzer (security & quality)
`)
    process.exit(0)
}

// Project root directory
const projectRoot = process.cwd()

/**
 * Expand input arguments to actual file paths
 * @param {string[]} inputs - Input arguments (files, folders, patterns)
 * @returns {string[]} - Array of resolved file paths
 */
function expandInputsToFiles(inputs) {
    const files = []

    for (const input of inputs) {
        const fullPath = path.resolve(input)

        try {
            if (fs.existsSync(fullPath)) {
                const stats = fs.statSync(fullPath)

                if (stats.isFile()) {
                    files.push(fullPath)
                } else if (stats.isDirectory()) {
                    // Recursively find all lintable files in directory
                    const dirFiles = findLintableFiles(fullPath)
                    files.push(...dirFiles)
                }
            } else {
                // Try as glob pattern - let each linter handle pattern matching
                files.push(input)
            }
        } catch (error) {
            console.warn(`⚠️  Warning: Could not process "${input}": ${error.message}`)
        }
    }

    return files
}

/**
 * Recursively find all lintable files in a directory
 * @param {string} dir - Directory path
 * @returns {string[]} - Array of file paths
 */
function findLintableFiles(dir) {
    const files = []
    const entries = fs.readdirSync(dir)

    for (const entry of entries) {
        const fullPath = path.join(dir, entry)
        const stats = fs.statSync(fullPath)

        if (stats.isDirectory()) {
            // Skip directories that our linters are configured to ignore
            // Based on actual directories in codebase and stylelint.config.mjs ignoreFiles
            const shouldSkip = [
                "node_modules", // Dependencies
                "bin", // .NET build output
                "obj", // .NET intermediate files
                "wwwroot", // Web output directory
            ].includes(entry)

            if (!shouldSkip) {
                files.push(...findLintableFiles(fullPath))
            }
        } else if (stats.isFile()) {
            const ext = path.extname(entry).toLowerCase()
            // Include common lintable file types
            if ([".css", ".vue", ".js", ".ts", ".cshtml", ".cs"].includes(ext)) {
                files.push(fullPath)
            }
        }
    }

    return files
}

/**
 * Categorize files by their appropriate linter
 * @param {string[]} files - Array of file paths
 * @returns {Object} - Object with arrays of files for each linter
 */
function routeVueFile(categories, normalizedPath) {
    categories.css.push(normalizedPath)
    if (!normalizedPath.startsWith("VueApp/")) {
        return
    }
    categories.vue.push(normalizedPath)
    categories.fallow.push(normalizedPath)
    if (normalizedPath.startsWith("VueApp/src/")) {
        categories.jscpd.push(normalizedPath)
    }
}

function routeJsTsFile(categories, normalizedPath) {
    if (normalizedPath.startsWith("VueApp/") || normalizedPath.startsWith("scripts/")) {
        categories.ts.push(normalizedPath)
    }
    if (normalizedPath.startsWith("VueApp/src/")) {
        categories.fallow.push(normalizedPath)
        categories.jscpd.push(normalizedPath)
    }
}

function routeCsFile(categories, normalizedPath) {
    categories.dotnet.push(normalizedPath)
    if (normalizedPath.startsWith("web/Areas/")) {
        categories.jscpd.push(normalizedPath)
    }
}

function categorizeFiles(files) {
    const categories = {
        css: [], // CSS and Vue files → lint-staged-css.js
        vue: [], // Vue files → lint-staged-vue.js
        ts: [], // JS/TS files → lint-staged-ts.js (Oxlint)
        cshtml: [], // CSHTML files → lint-staged-cshtml.js (ESLint security + accessibility)
        dotnet: [], // C# files → lint-staged-dotnet.js
        fallow: [], // VueApp/src/**/*.{ts,js,vue} → lint-staged-fallow.js (dead code, project-graph)
        jscpd: [], // VueApp/src + web/Areas → lint-staged-jscpd.js (duplication)
    }

    for (const file of files) {
        const ext = path.extname(file).toLowerCase()
        const relativePath = path.relative(projectRoot, file)
        // Normalize path separators for consistent matching.
        // Use normalizedPath (forward slashes) so output is copy-pasteable into commands.
        const normalizedPath = relativePath.replaceAll("\\", "/")

        // Skip config files like .eslintrc.js
        if (path.basename(file).startsWith(".eslintrc")) {
            // No-op — filtered out.
        } else if (ext === ".css") {
            categories.css.push(normalizedPath)
        } else if (ext === ".vue") {
            routeVueFile(categories, normalizedPath)
        } else if ([".js", ".ts"].includes(ext)) {
            routeJsTsFile(categories, normalizedPath)
        } else if (ext === ".cshtml") {
            categories.cshtml.push(normalizedPath)
        } else if (ext === ".cs") {
            routeCsFile(categories, normalizedPath)
        }
    }

    return categories
}

/**
 * Run oxfmt check on a batch of files
 * @param {string[]} files - Array of file paths
 * @returns {{passed: boolean, failed: string[]}} - Result with pass status and failed files
 */
function runOxfmtCheckBatch(files) {
    const result = spawnSync("npx", ["oxfmt", "--check", ...files], {
        stdio: "pipe",
        cwd: projectRoot,
        encoding: "utf8",
    })

    if (result.error) {
        console.error("❌ Failed to run oxfmt:", result.error.message)
        return { passed: false, failed: files }
    }

    if (result.status !== 0) {
        const output = (result.stdout || "") + (result.stderr || "")
        const failedFiles = files.filter((f) => {
            const fwd = f.replaceAll("\\", "/")
            return output.includes(f) || output.includes(fwd)
        })
        return { passed: false, failed: failedFiles.length > 0 ? failedFiles : files }
    }

    return { passed: true, failed: [] }
}

/**
 * Run oxfmt check/fix on files
 * @param {string[]} files - Array of file paths
 * @param {boolean} fix - Whether to fix issues
 * @returns {boolean} - Whether oxfmt check passed
 */
function runOxfmtCheck(files, fix) {
    if (files.length === 0) {
        return true
    }

    console.log(`\n🎨 Oxfmt ${fix ? "fixing" : "checking"} formatting (${files.length} files)`)

    // Batch files to avoid command line length limits on Windows
    // Windows has ~8191 char limit; use conservative batch size
    const MAX_BATCH_SIZE = 50
    let allPassed = true
    const allFailedFiles = []

    for (let i = 0; i < files.length; i += MAX_BATCH_SIZE) {
        const batch = files.slice(i, i + MAX_BATCH_SIZE)
        const batchNum = Math.floor(i / MAX_BATCH_SIZE) + 1
        const totalBatches = Math.ceil(files.length / MAX_BATCH_SIZE)

        if (totalBatches > 1) {
            process.stdout.write(`  ${fix ? "Fixing" : "Checking"} batch ${batchNum}/${totalBatches}...\r`)
        }

        if (fix) {
            const result = spawnSync("npx", ["oxfmt", "--write", ...batch], {
                stdio: "inherit",
                cwd: projectRoot,
            })
            if (result.error || result.status !== 0) {
                allPassed = false
            }
        } else {
            const { passed, failed } = runOxfmtCheckBatch(batch)
            if (!passed) {
                allPassed = false
                allFailedFiles.push(...failed)
            }
        }
    }

    if (files.length > MAX_BATCH_SIZE) {
        process.stdout.write("                                        \r")
    }

    if (!allPassed && !fix) {
        console.log(`\n❌ ${allFailedFiles.length} file(s) have formatting issues`)
        for (const f of allFailedFiles) {
            console.log(`  - ${f}`)
        }
        console.log("\n💡 Files need formatting. Run with --fix to auto-format:")
        console.log("   npm run lint -- --fix <files>")
    }

    return allPassed
}

/**
 * Build script args, using a temp file when the file list is too long for
 * the Windows command-line limit (~8191 chars).
 * @param {string[]} files - Array of file paths
 * @param {boolean} fix - Whether to pass --fix
 * @param {boolean} clearCache - Whether to pass --clear-cache
 * @returns {{ scriptArgs: string[], tempFile: string | null }}
 */
function buildScriptArgs(files, fix, clearCache) {
    const scriptArgs = []
    let tempFile = null

    const totalLength = files.reduce((sum, f) => sum + f.length + 1, 0)
    if (totalLength > MAX_ARG_LENGTH) {
        const RADIX = 36
        const SUFFIX_LENGTH = 8
        tempFile = path.join(
            os.tmpdir(),
            `lint-files-${Date.now()}-${Math.random()
                .toString(RADIX)
                .slice(2, 2 + SUFFIX_LENGTH)}.txt`,
        )
        fs.writeFileSync(tempFile, files.join("\n"), "utf8")
        scriptArgs.push(`--files-from=${tempFile}`)
    } else {
        scriptArgs.push(...files)
    }

    if (fix) {
        scriptArgs.push("--fix")
    }
    if (clearCache) {
        scriptArgs.push("--clear-cache")
    }

    return { scriptArgs, tempFile }
}

/**
 * Run a linter script with files (sync — used within sequential groups)
 * @param {string} script - Script name (e.g., 'lint-staged-css.js')
 * @param {string[]} files - Array of file paths
 * @param {string} description - Description for logging
 * @param {boolean} fix - Whether to pass --fix to the linter
 * @param {boolean} clearCache - Whether to pass --clear-cache to the linter
 */
function runLinter(script, files, description, fix, clearCache) {
    if (files.length === 0) {
        return 0
    }

    console.log(`\n🔍 ${description} (${files.length} files)`)

    const scriptPath = path.join(__dirname, script)
    const { scriptArgs, tempFile } = buildScriptArgs(files, fix, clearCache)

    const result = spawnSync("node", [scriptPath, ...scriptArgs], {
        stdio: "inherit",
        cwd: projectRoot,
        env,
    })

    if (tempFile) {
        try {
            fs.unlinkSync(tempFile)
        } catch {
            /* Best-effort cleanup */
        }
    }

    if (result.error) {
        console.error(`❌ Failed to run ${script}:`, result.error.message)
        return 1
    }

    if (result.signal) {
        console.error(`❌ ${script} terminated by signal ${result.signal}`)
        return 1
    }

    return result.status ?? 1
}

/**
 * Run a linter script with files (async — for parallel execution)
 */
function runLinterAsync(script, files, description, fix, clearCache) {
    if (files.length === 0) {
        return Promise.resolve(0)
    }

    console.log(`\n🔍 ${description} (${files.length} files)`)

    const scriptPath = path.join(__dirname, script)
    const { scriptArgs, tempFile } = buildScriptArgs(files, fix, clearCache)

    return new Promise((resolve) => {
        const child = spawn("node", [scriptPath, ...scriptArgs], {
            stdio: "inherit",
            cwd: projectRoot,
            env,
        })

        child.on("close", (code, signal) => {
            if (tempFile) {
                try {
                    fs.unlinkSync(tempFile)
                } catch {
                    /* Best-effort cleanup */
                }
            }
            if (signal) {
                console.error(`❌ ${script} terminated by signal ${signal}`)
                resolve(1)
                return
            }
            resolve(code ?? 1)
        })
        child.on("error", (err) => {
            if (tempFile) {
                try {
                    fs.unlinkSync(tempFile)
                } catch {
                    /* Best-effort cleanup */
                }
            }
            console.error(`❌ Failed to run ${script}:`, err.message)
            resolve(1)
        })
    })
}

/**
 * Run frontend linters sequentially (they may share .vue files in --fix mode).
 * Returns the highest exit code seen so callers can propagate failure.
 */
function runFrontendLinters(categories, fix, clearCache) {
    const codes = [
        runLinter("lint-staged-css.js", categories.css, "CSS/Stylelint - Accessibility & Style", fix, clearCache),
        runLinter("lint-staged-vue.js", categories.vue, "Vue ESLint - Security & Quality", fix, clearCache),
        runLinter("lint-staged-ts.js", categories.ts, "JS/TS Oxlint - Security & Quality", fix, clearCache),
        runLinter("lint-staged-cshtml.js", categories.cshtml, "CSHTML - Security & Accessibility", fix, clearCache),
    ]
    return Math.max(0, ...codes)
}

function deduplicate(files) {
    return [...new Set(files)]
}

// Main execution
async function main() {
    console.log("🚀 Smart Linter - Analyzing files and routing to appropriate linters...\n")

    // Expand inputs to actual files
    const allFiles = expandInputsToFiles(inputArgs)

    if (allFiles.length === 0) {
        console.log("❌ No files found to lint. Check your file paths or patterns.")
        process.exit(1)
    }

    // Categorize files by linter
    const categories = categorizeFiles(allFiles)

    // Show what we found
    const totalFiles = new Set([
        ...categories.css,
        ...categories.vue,
        ...categories.ts,
        ...categories.cshtml,
        ...categories.dotnet,
    ]).size

    // Dedup category lists (a .vue file can appear in several)
    categories.fallow = deduplicate(categories.fallow)
    categories.jscpd = deduplicate(categories.jscpd)

    console.log(`📊 Found ${totalFiles} files to lint:`)
    if (categories.css.length > 0) {
        console.log(`  🎨 CSS/Stylelint: ${categories.css.length} files`)
    }
    if (categories.vue.length > 0) {
        console.log(`  🎯 Vue ESLint: ${categories.vue.length} files`)
    }
    if (categories.ts.length > 0) {
        console.log(`  ⚡ JS/TS Oxlint: ${categories.ts.length} files`)
    }
    if (categories.cshtml.length > 0) {
        console.log(`  🌐 CSHTML: ${categories.cshtml.length} files`)
    }
    if (categories.dotnet.length > 0) {
        console.log(`  🔷 .NET/SonarAnalyzer: ${categories.dotnet.length} files`)
    }
    if (categories.fallow.length > 0) {
        console.log(`  🧹 Fallow (dead code): ${categories.fallow.length} files`)
    }
    if (categories.jscpd.length > 0) {
        console.log(`  📎 JSCPD (duplication): ${categories.jscpd.length} files`)
    }

    // Run oxfmt on JS/TS/CSS/Vue files only (C# formatting is handled by dotnet format)
    const oxfmtFiles = [...new Set([...categories.css, ...categories.vue, ...categories.ts])]

    const oxfmtPassed = runOxfmtCheck(oxfmtFiles, shouldFix)

    // Run frontend linters and dotnet linter in parallel
    // Frontend linters run sequentially among themselves (they share .vue files in --fix mode)
    // Dotnet linter is independent and runs concurrently
    const linterCodes = await Promise.all([
        runFrontendLinters(categories, shouldFix, shouldClearCache),
        runLinterAsync(
            "lint-staged-dotnet.js",
            categories.dotnet,
            ".NET/SonarAnalyzer - Security & Quality",
            shouldFix,
            shouldClearCache,
        ),
        runLinterAsync(
            "lint-staged-fallow.js",
            categories.fallow,
            "Fallow - Dead Code / Unused Exports",
            shouldFix,
            shouldClearCache,
        ),
        runLinterAsync(
            "lint-staged-jscpd.js",
            categories.jscpd,
            "JSCPD - Code Duplication",
            shouldFix,
            shouldClearCache,
        ),
    ])

    const maxLinterCode = Math.max(0, ...linterCodes)

    console.log("\n✅ Smart linting complete!")

    if (!oxfmtPassed && !shouldFix) {
        console.log("\n💡 Some files have formatting issues. Use --fix to auto-format:")
        console.log("   npm run lint -- --fix <files>")
        process.exit(1)
    }

    if (maxLinterCode !== 0) {
        process.exit(maxLinterCode)
    }
}

// oxlint-disable-next-line promise/prefer-await-to-then -- Top-level entry point; async IIFE adds no value
main().catch((error) => {
    console.error("❌ Unexpected error:", error.message)
    process.exit(1)
})
