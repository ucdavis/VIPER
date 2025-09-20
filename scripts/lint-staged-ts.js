#!/usr/bin/env node

const path = require("node:path")
const {
    parseArguments,
    sanitizeFilePath,
    runCommand,
    shouldBlockOnWarnings,
    categorizeIssuesBySeverity,
    displayCategorizedIssues,
    handleCommitDecisionForCategorizedIssues,
    filterTypeScriptErrors,
} = require("./lib/lint-staged-common")
const { categorizeRule } = require("./lib/critical-rules")
const { createLogger } = require("./lib/script-utils")

// Parse command line arguments
const { fixFlag, rawFiles } = parseArguments()
const logger = createLogger("TypeScript/JavaScript")

if (rawFiles.length === 0) {
    logger.success("No TypeScript/JavaScript files to check.")
    process.exit(0)
}

// Base directories for different contexts
const projectRoot = path.join(__dirname, "..")

// Sanitize file paths
const files = rawFiles
    .map((filePath) => sanitizeFilePath(filePath, projectRoot, [".js", ".jsx", ".ts", ".tsx"]))
    .filter((file) => file !== null && !file.endsWith(".vue")) // Exclude Vue files

/**
 * Parse Oxlint output and convert to standardized issue format
 * @param {string} stdout - Oxlint stdout
 * @param {string} stderr - Oxlint stderr
 * @returns {Array} - Array of standardized issue objects
 */
function parseOxlintOutput(stdout, stderr) {
    const issues = []
    const output = stdout + stderr

    // Strip ANSI color codes first
    const ESC_CHAR_CODE = 27
    const ESC_CODE = String.fromCodePoint(ESC_CHAR_CODE)
    const ansiRegex = new RegExp(`${ESC_CODE}[[0-9;]*[mGK]`, "g")
    const cleanOutput = output.replaceAll(ansiRegex, "")

    // Parse Oxlint stylish output with file headers
    // Format typically looks like:
    // path/to/file.ts
    //   2:7   error  Variable 'unused' is declared but never used  eslint(no-unused-vars)
    //   5:10  warn   Console log statement  oxlint(no-console)

    const lines = cleanOutput.split("\n")
    let currentFile = ""

    for (const line of lines) {
        // Check if line is a file header (not indented, likely a file path)
        if (line.trim() && !line.startsWith(" ") && !line.startsWith("\t")) {
            // This looks like a file path - update current file context
            let filePath = line.trim()

            // Clean Windows long path prefix (\\?\) for cleaner display
            const WINDOWS_LONG_PATH_PREFIX = "\\\\?\\"
            if (filePath.startsWith(WINDOWS_LONG_PATH_PREFIX)) {
                filePath = filePath.slice(WINDOWS_LONG_PATH_PREFIX.length)
            }

            currentFile = filePath
        } else {
            // Check if line is an issue (indented with position info)
            const issueMatch = line.match(/^\s+(\d+:\d+)\s+(error|warning)\s+(.+?)\s+([a-z-_/()0-9:]+)$/i)
            if (issueMatch) {
                const [, position, severity, message, ruleInfo] = issueMatch

                // Extract rule name from ruleInfo (format: "rule-name" or "plugin(rule-name)")
                const ruleName = ruleInfo.includes("(") ? ruleInfo.match(/\(([^)]+)\)/)?.[1] || ruleInfo : ruleInfo

                // Create standardized issue object with file context
                const [line, col] = position.split(":")
                issues.push({
                    file: currentFile || "unknown", // Use captured file context
                    line: Number.parseInt(line, 10),
                    col: Number.parseInt(col, 10),
                    severity: severity,
                    message: message.trim(),
                    rule: ruleName,
                    position: position,
                })
            }
        }
    }

    return issues
}

/**
 * Helper function to check TypeScript files with a specific config
 * @param {string[]} files - Array of file paths
 * @param {string} command - Command to run (tsc or vue-tsc)
 * @param {string} configPath - Path to tsconfig file
 * @param {string} description - Description for logging
 * @param {string[]} rawFiles - Original file list for filtering
 * @param {string} projectRoot - Project root directory
 * @param {Object} logger - Logger instance
 * @returns {boolean} - Whether there are type errors
 */
function runTypeCheck(files, command, configPath, description, rawFiles, projectRoot, logger) {
    if (files.length === 0) {
        return false
    }

    logger.info(`Checking ${files.length} ${description}...`)
    const result = runCommand(
        command,
        ["--project", configPath, "--noEmit"],
        `TypeScript ${description} checking`,
        projectRoot,
    )

    if (!result.success) {
        const combinedOutput = result.stdout + result.stderr
        const filteredOutput = filterTypeScriptErrors(combinedOutput, rawFiles, projectRoot)

        if (filteredOutput.trim()) {
            logger.error("TypeScript type checking failed:")
            logger.plain(filteredOutput)
            return true // Has errors
        }
    }
    return false // No errors
}

try {
    let hasTypeErrors = false

    logger.info(`Running Oxlint security and quality checks on ${files.length} TypeScript/JavaScript files...`)

    if (files.length === 0) {
        logger.success("No TypeScript/JavaScript files to lint")
        process.exit(0)
    }

    // Build Oxlint command arguments
    const oxlintArgs = [...(fixFlag ? ["--fix"] : []), "--format", "stylish"]

    // Add type-aware checking for VueApp files
    const vueAppFiles = files.filter((file) => file.startsWith("VueApp/"))
    if (vueAppFiles.length > 0) {
        oxlintArgs.push("--type-aware", "--tsconfig=VueApp/tsconfig.json")
    }

    // Only deny warnings in strict mode (lint:staged)
    const blockOnWarnings = shouldBlockOnWarnings()
    if (blockOnWarnings) {
        oxlintArgs.push("--deny-warnings")
    }

    // Add files to check
    oxlintArgs.push(...files)

    // Run Oxlint using shared command runner
    const oxlintResult = runCommand("oxlint", oxlintArgs, "Oxlint", projectRoot)

    // Handle Oxlint exit codes: 0 = no issues, 1 = linting issues found, >1 = fatal error
    if (oxlintResult.status > 1) {
        logger.plainError("\n❌ Oxlint command failed:")
        if (oxlintResult.stdout) {
            logger.plainError(oxlintResult.stdout)
        }
        if (oxlintResult.stderr) {
            logger.plainError(oxlintResult.stderr)
        }
        logger.plainError("\n🛑 COMMIT BLOCKED - Oxlint execution failed")
        process.exit(1)
    }

    // Parse and categorize Oxlint output
    const issues = parseOxlintOutput(oxlintResult.stdout || "", oxlintResult.stderr || "")
    const categorizedIssues = categorizeIssuesBySeverity(issues, categorizeRule, "critical-security")

    // Display categorized issues using shared function
    displayCategorizedIssues(
        categorizedIssues,
        {
            criticalLabel: "CRITICAL SECURITY ERRORS",
            nonCriticalLabel: "ERRORS",
            warningLabel: "CODE QUALITY WARNINGS",
        },
        "TypeScript/JavaScript",
    )

    // Run TypeScript type checking for all TypeScript files
    const tsFiles = files.filter((file) => /\.(ts|tsx)$/.test(file))

    if (tsFiles.length > 0) {
        logger.info("Running TypeScript type checking...")

        // Check VueApp files
        const vueAppTsFiles = tsFiles.filter((file) => file.startsWith("VueApp/"))

        if (vueAppTsFiles.length > 0) {
            // Separate app files from config files
            const appFiles = vueAppTsFiles.filter((file) => file.startsWith("VueApp/src/"))
            const nodeConfigFiles = vueAppTsFiles.filter((file) => !file.startsWith("VueApp/src/"))

            // Check app files with tsconfig.app.json
            const appHasErrors = runTypeCheck(
                appFiles,
                "vue-tsc",
                "VueApp/tsconfig.app.json",
                "application files",
                rawFiles,
                projectRoot,
                logger,
            )

            // Check Node config files with tsconfig.node.json
            const nodeHasErrors = runTypeCheck(
                nodeConfigFiles,
                "tsc",
                "VueApp/tsconfig.node.json",
                "Node.js config files",
                rawFiles,
                projectRoot,
                logger,
            )

            hasTypeErrors = appHasErrors || nodeHasErrors
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
    handleCommitDecisionForCategorizedIssues(categorizedIssues, {}, "TypeScript/JavaScript")
} catch (error) {
    logger.plainError(`Unexpected error: ${error}`)
    process.exit(1)
}
