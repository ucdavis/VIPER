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

    // Run TypeScript type checking for Node.js config files
    const nodeConfigFiles = vueAppFiles.filter((file) => !file.startsWith("VueApp/src/"))

    if (nodeConfigFiles.length > 0) {
        logger.info(`Running TypeScript type checking for ${nodeConfigFiles.length} Node.js config files...`)
        const tscResult = runCommand(
            "tsc",
            ["--project", "VueApp/tsconfig.node.json", "--noEmit"],
            "TypeScript node checking",
            projectRoot,
        )
        if (tscResult.success) {
            logger.success("TypeScript type checking passed")
        } else {
            logger.error("TypeScript type checking failed:")

            // Filter the TypeScript errors to only show those related to the files being linted
            const combinedOutput = tscResult.stdout + tscResult.stderr
            const filteredOutput = filterTypeScriptErrors(combinedOutput, rawFiles, projectRoot)

            if (filteredOutput.trim()) {
                logger.plain(filteredOutput)
                hasTypeErrors = true
            } else {
                logger.success("No TypeScript errors found in the specified files")
            }
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
