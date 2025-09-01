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
} = require("./lib/lint-staged-common")
const { categorizeRule } = require("./lib/critical-rules")
const { createLogger } = require("./lib/script-utils")

// Parse command line arguments
const { fixFlag, rawFiles } = parseArguments()
const logger = createLogger("CSHTML")

if (rawFiles.length === 0) {
    logger.success("No .cshtml files to check.")
    process.exit(0)
}

// Project root directory path
const projectRoot = path.join(__dirname, "..")

// Sanitize all file paths and filter out null results (missing files)
const files = rawFiles
    .map((filePath) => sanitizeFilePath(filePath, projectRoot, [".cshtml"]))
    .filter((file) => file !== null)

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

try {
    if (files.length === 0) {
        logger.success("No .cshtml files to check")
        process.exit(0)
    }

    // Run ESLint on .cshtml files using root config
    const eslintArgs = [...(fixFlag ? ["--fix"] : []), "--format", "json", ...files]

    logger.info(`Running ESLint security and quality checks on ${files.length} .cshtml files...`)

    const eslintResult = runCommand("eslint", eslintArgs, "ESLint (CSHTML)", projectRoot)

    // Check if ESLint command had a fatal error (only if no JSON output produced)
    if (eslintResult.status !== 0 && eslintResult.status !== 1) {
        // Exit codes 0 = no issues, 1 = linting errors found, >1 = fatal error
        logger.plainError(`\n❌ ESLint command failed with exit code: ${eslintResult.status}`)
        if (eslintResult.stdout) {
            logger.plainError(`STDOUT: ${eslintResult.stdout}`)
        }
        if (eslintResult.stderr) {
            logger.plainError(`STDERR: ${eslintResult.stderr}`)
        }
        logger.plainError("\n🛑 COMMIT BLOCKED - ESLint execution failed")
        process.exit(1)
    }

    // Parse and categorize ESLint output
    const issues = parseESLintOutput(eslintResult.stdout, eslintResult.stderr)
    const categorizedIssues = categorizeIssuesBySeverity(issues, categorizeRule, "critical-security")

    // Display categorized issues using shared function
    displayCategorizedIssues(
        categorizedIssues,
        {
            criticalLabel: "SECURITY ERRORS",
            nonCriticalLabel: "ERRORS",
            warningLabel: "CODE QUALITY WARNINGS",
        },
        "CSHTML",
    )

    // Use shared commit decision handler
    handleCommitDecisionForCategorizedIssues(
        categorizedIssues,
        {
            criticalBlockingMessage: "SECURITY ERRORS MUST BE FIXED",
        },
        "CSHTML",
    )
} catch (error) {
    logger.plainError(`Unexpected error: ${error}`)
    process.exit(1)
}
