#!/usr/bin/env node

const path = require("node:path")
const {
    parseArguments,
    sanitizeFilePath,
    runCommand,
    parseJsonOutput,
    categorizeIssuesBySeverity,
    displayCategorizedIssues,
    handleCommitDecisionForCategorizedIssues,
    filterTypeScriptErrors,
} = require("./lib/lint-staged-common")
const { categorizeRule } = require("./lib/critical-rules")
const { createLogger } = require("./lib/script-utils")

// Parse command line arguments
const { fixFlag, rawFiles } = parseArguments()

const logger = createLogger("Vue")

if (rawFiles.length === 0) {
    logger.success("No Vue files to check.")
    process.exit(0)
}

// Base directories for different contexts
const projectRoot = path.join(__dirname, "..")
const vueAppDir = path.join(__dirname, "..", "VueApp")

// Sanitize file paths - primarily expecting .vue files
const files = rawFiles
    .map((filePath) => sanitizeFilePath(filePath, projectRoot, [".vue", ".js", ".ts", ".jsx", ".tsx", ".mjs"]))
    .filter((file) => file !== null)

// Check for JS/TS files and warn that Oxlint should be used instead
const jstsFiles = files.filter((f) => /\.(js|jsx|ts|tsx|mjs)$/.test(f))
if (jstsFiles.length > 0) {
    logger.warning(`Found ${jstsFiles.length} JS/TS files. Consider using Oxlint instead:`)
    logger.warning("   npm run lint <files>  (uses smart routing to Oxlint)")
    logger.warning("   OR: node scripts/lint-staged-ts.js <files>")
    logger.warning("   Vue linter is optimized for .vue files only.")
}

/**
 * Parse ESLint JSON output and convert to standardized issue format
 * @param {string} stdout - ESLint stdout
 * @param {string} stderr - ESLint stderr
 * @returns {Array} - Array of standardized issue objects
 */
function parseESLintOutput(stdout, stderr) {
    const issues = []

    // Use shared JSON parsing utility
    const eslintResults = parseJsonOutput(stdout, stderr, "ESLint")

    // Process each file's results
    for (const fileResult of eslintResults) {
        for (const message of fileResult.messages) {
            issues.push({
                file: fileResult.filePath,
                line: message.line,
                col: message.column,
                severity: message.severity === 2 ? "error" : "warning",
                message: message.message,
                rule: message.ruleId || "unknown",
            })
        }
    }

    return issues
}

/**
 * Handle TypeScript errors with filtering and logging
 * @param {Object} appResult - TypeScript command result
 * @param {string[]} rawFiles - Raw file paths being linted
 * @param {string} projectRoot - Project root directory
 * @param {Object} logger - Logger instance
 * @returns {boolean} - Whether type errors were found
 */
function handleTypeScriptErrors(appResult, rawFiles, projectRoot, logger) {
    // Filter the TypeScript errors to only show those related to the files being linted
    const combinedOutput = appResult.stdout + appResult.stderr
    const filteredOutput = filterTypeScriptErrors(combinedOutput, rawFiles, projectRoot)

    if (!filteredOutput.trim()) {
        logger.success("No TypeScript errors found in the specified files")
        return false
    }

    logger.error("TypeScript type checking failed:")
    logger.plain(filteredOutput)
    return true
}

try {
    let hasTypeErrors = false

    logger.info(`Running ESLint security and quality checks on ${files.length} files...`)

    let allIssues = []

    // Separate Vue files from JS/TS files
    const vueFiles = files.filter((f) => f.endsWith(".vue"))
    const jstsFiles = files.filter((f) => /\.(js|jsx|ts|tsx|mjs)$/.test(f))

    // Run ESLint for Vue files
    if (vueFiles.length > 0) {
        logger.plain(`Checking ${vueFiles.length} Vue files with VueApp ESLint...`)
        const relativeVueFiles = vueFiles.map((f) => path.relative(vueAppDir, path.resolve(projectRoot, f)))
        const vueEslintArgs = [
            ...(fixFlag ? ["--fix"] : []),
            "--format",
            "json",
            "--no-warn-ignored",
            ...relativeVueFiles,
        ]

        const vueEslintResult = runCommand("eslint", vueEslintArgs, "ESLint (Vue)", vueAppDir)

        if (vueEslintResult.status > 1) {
            logger.plainError("\n❌ ESLint command failed:")
            if (vueEslintResult.stdout) {
                logger.plainError(vueEslintResult.stdout)
            }
            if (vueEslintResult.stderr) {
                logger.plainError(vueEslintResult.stderr)
            }
            logger.plainError("\n🛑 COMMIT BLOCKED - ESLint execution failed")
            process.exit(1)
        }

        const vueIssues = parseESLintOutput(vueEslintResult.stdout, vueEslintResult.stderr)
        allIssues.push(...vueIssues)
    }

    // Handle JS/TS files as backup (with warning)
    if (jstsFiles.length > 0) {
        logger.warning(`\nProcessing ${jstsFiles.length} JS/TS files with ESLint as fallback...`)
        logger.warning("💡 For better performance, use Oxlint instead:")
        logger.warning("   npm run lint <files>  (auto-routes to Oxlint)")
        logger.warning("   OR: node scripts/lint-staged-ts.js <files>\n")

        logger.plain(`Checking ${jstsFiles.length} JS/TS files with ESLint fallback...`)
        const relativeJSTSFiles = jstsFiles.map((f) => path.relative(vueAppDir, path.resolve(projectRoot, f)))
        const eslintArgs = [
            ...(fixFlag ? ["--fix"] : []),
            "--config",
            "../eslint.config.mjs",
            "--format",
            "json",
            "--no-warn-ignored",
            ...relativeJSTSFiles,
        ]

        const eslintResult = runCommand("eslint", eslintArgs, "ESLint (JS/TS fallback)", vueAppDir)

        if (eslintResult.status > 1) {
            logger.plainError("\n❌ ESLint command failed:")
            if (eslintResult.stdout) {
                logger.plainError(eslintResult.stdout)
            }
            if (eslintResult.stderr) {
                logger.plainError(eslintResult.stderr)
            }
            logger.plainError("\n🛑 COMMIT BLOCKED - ESLint execution failed")
            process.exit(1)
        }

        const scriptIssues = parseESLintOutput(eslintResult.stdout, eslintResult.stderr)
        allIssues.push(...scriptIssues)
    }

    // Categorize all issues using shared function
    const categorizedIssues = categorizeIssuesBySeverity(allIssues, categorizeRule, "critical-security")

    // Display categorized issues using shared function
    displayCategorizedIssues(
        categorizedIssues,
        {
            criticalLabel: "CRITICAL SECURITY ERRORS",
            nonCriticalLabel: "ERRORS",
            warningLabel: "CODE QUALITY WARNINGS",
        },
        "Vue",
    )

    // 2. Run TypeScript type checking using proper project configurations
    const tsFiles = files.filter((file) => /\.(ts|tsx|vue)$/.test(file))

    if (tsFiles.length > 0) {
        // Only check Vue app files
        const appFiles = tsFiles.filter((file) => file.startsWith("src/") || file.endsWith(".vue"))

        logger.info("Running TypeScript type checking...")

        // Check app files with tsconfig.app.json
        if (appFiles.length > 0) {
            logger.info(`Checking ${appFiles.length} application files...`)
            const appResult = runCommand(
                "tsc",
                ["--project", "tsconfig.app.json", "--noEmit"],
                "TypeScript app checking",
                vueAppDir,
            )
            if (!appResult.success) {
                hasTypeErrors = handleTypeScriptErrors(appResult, rawFiles, projectRoot, logger)
            }
        }

        if (!hasTypeErrors) {
            logger.success("TypeScript type checking passed")
        }
    }

    // Handle commit decision using shared function, including type errors
    if (hasTypeErrors) {
        logger.error("COMMIT BLOCKED - TypeScript type errors must be fixed.")
        process.exit(1)
    }

    // Use shared commit decision handler
    handleCommitDecisionForCategorizedIssues(categorizedIssues, {}, "Vue")
} catch (error) {
    logger.plainError(`Unexpected error: ${error}`)
    process.exit(1)
}
