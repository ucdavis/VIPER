#!/usr/bin/env node

const fs = require("node:fs")
const path = require("node:path")
const { spawnSync } = require("node:child_process")

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
const inputArgs = args.filter((arg) => arg !== "--fix")

if (inputArgs.length === 0) {
    console.log(`
🔍 Smart Linter - Routes files to the correct linter automatically

Usage:
  npm run lint [--fix] <file|folder|pattern>

Examples:
     npm run lint web/Views/Home/Index.cshtml
  npm run lint VueApp/src/components/HelloWorld.vue
  npm run lint web/wwwroot/css/directory.css
  npm run lint VueApp/src
  npm run lint --fix VueApp/src  (auto-fix issues)

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
function categorizeFiles(files) {
    const categories = {
        css: [], // CSS and Vue files → lint-staged-css.js
        vue: [], // Vue files → lint-staged-vue.js
        ts: [], // JS/TS files → lint-staged-ts.js (Oxlint)
        cshtml: [], // CSHTML files → lint-staged-cshtml.js (ESLint security + accessibility)
        dotnet: [], // C# files → lint-staged-dotnet.js
    }

    for (const file of files) {
        const ext = path.extname(file).toLowerCase()
        const relativePath = path.relative(projectRoot, file)
        // Normalize path separators for consistent matching
        const normalizedPath = relativePath.replaceAll("\\", "/")

        // Skip config files like .eslintrc.js
        if (!path.basename(file).startsWith(".eslintrc")) {
            // Categorize based on extension and location
            if (ext === ".css") {
                categories.css.push(relativePath)
            } else if (ext === ".vue") {
                // Vue files can be linted by both CSS and Vue linters
                categories.css.push(relativePath)
                // Only send Vue files to Vue linter if they're in VueApp directory
                if (normalizedPath.startsWith("VueApp/")) {
                    categories.vue.push(relativePath)
                }
            } else if ([".js", ".ts"].includes(ext)) {
                // Send JS/TS files to Oxlint if they're in VueApp or scripts directory
                if (normalizedPath.startsWith("VueApp/") || normalizedPath.startsWith("scripts/")) {
                    categories.ts.push(relativePath)
                }
            } else if (ext === ".cshtml") {
                categories.cshtml.push(relativePath)
            } else if (ext === ".cs") {
                categories.dotnet.push(relativePath)
            }
        }
    }

    return categories
}

/**
 * Run prettier check on files
 * @param {string[]} files - Array of file paths
 * @param {boolean} fix - Whether to fix issues
 * @returns {boolean} - Whether prettier check passed
 */
function runPrettierCheck(files, fix) {
    if (files.length === 0) {
        return true
    }

    console.log(`\n🎨 Prettier ${fix ? "fixing" : "checking"} formatting (${files.length} files)`)

    const prettierArgs = fix ? ["--write"] : ["--check"]
    prettierArgs.push(...files)

    const result = spawnSync("npx", ["prettier", ...prettierArgs], {
        stdio: "inherit",
        cwd: projectRoot,
    })

    if (result.error) {
        console.error("❌ Failed to run prettier:", result.error.message)
        return false
    }

    if (!fix && result.status !== 0) {
        console.log("\n💡 Files need prettier formatting. Run with --fix to auto-format:")
        console.log("   npm run lint --fix <files>")
        console.log("   or: npm run pretty:staged:fix")
    }

    return result.status === 0
}

/**
 * Run a linter script with files
 * @param {string} script - Script name (e.g., 'lint-staged-css.js')
 * @param {string[]} files - Array of file paths
 * @param {string} description - Description for logging
 * @param {boolean} fix - Whether to pass --fix to the linter
 */
function runLinter(script, files, description, fix) {
    if (files.length === 0) {
        return
    }

    console.log(`\n🔍 ${description} (${files.length} files)`)

    const scriptPath = path.join(__dirname, script)
    const scriptArgs = fix ? [...files, "--fix"] : files

    const result = spawnSync("node", [scriptPath, ...scriptArgs], {
        stdio: "inherit",
        cwd: projectRoot,
        env: {
            ...process.env,
            LINT_BLOCK_ON_WARNINGS: "false", // Always show warnings, never block
        },
    })

    if (result.error) {
        console.error(`❌ Failed to run ${script}:`, result.error.message)
    }
}

// Main execution
try {
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

    // Get all unique files for prettier check
    const allUniqueFiles = [
        ...new Set([
            ...categories.css,
            ...categories.vue,
            ...categories.ts,
            ...categories.cshtml,
            ...categories.dotnet,
        ]),
    ]

    // Run prettier check first
    const prettierPassed = runPrettierCheck(allUniqueFiles, shouldFix)

    // Run each linter with its files
    runLinter("lint-staged-css.js", categories.css, "CSS/Stylelint - Accessibility & Style", shouldFix)
    runLinter("lint-staged-vue.js", categories.vue, "Vue ESLint - Security & Quality", shouldFix)
    runLinter("lint-staged-ts.js", categories.ts, "JS/TS Oxlint - Security & Quality", shouldFix)
    // Route CSHTML files through the dedicated ESLint-based CSHTML linter (security + accessibility)
    runLinter("lint-staged-cshtml.js", categories.cshtml, "CSHTML - Security & Accessibility", shouldFix)
    runLinter("lint-staged-dotnet.js", categories.dotnet, ".NET/SonarAnalyzer - Security & Quality", shouldFix)

    console.log("\n✅ Smart linting complete!")

    if (!prettierPassed && !shouldFix) {
        console.log("\n💡 Some files have formatting issues. Use --fix to auto-format:")
        console.log("   npm run lint --fix <files>")
    }
} catch (error) {
    console.error("❌ Unexpected error:", error.message)
    process.exit(1)
}
