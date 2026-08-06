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

const logger = createLogger("CSS")

if (rawFiles.length === 0) {
    logger.success("No .css files to check.")
    process.exit(0)
}

// Project root directory path (for stylelint config)
const projectRoot = path.join(__dirname, "..")

// Sanitize all file paths and filter out null results (missing files)
const files = rawFiles
    .map((filePath) => sanitizeFilePath(filePath, projectRoot, [".css", ".vue"]))
    .filter((file) => file !== null)

/**
 * Parse Stylelint JSON output and convert to standardized issue format
 * @param {string} stdout - Stylelint stdout
 * @param {string} stderr - Stylelint stderr
 * @returns {Array} - Array of standardized issue objects
 */
function parseStylelintOutput(stdout, stderr) {
    const issues = []

    // Use shared JSON parsing utility with text fallback
    const stylelintResults = parseJsonOutput(stdout, stderr, "Stylelint", parseTextOutput)

    // Process each file's results
    for (const fileResult of stylelintResults) {
        if (fileResult.warnings) {
            for (const warning of fileResult.warnings) {
                issues.push({
                    file: fileResult.source,
                    line: warning.line,
                    col: warning.column,
                    severity: warning.severity === "error" ? "error" : "warning",
                    message: warning.text,
                    rule: warning.rule || "unknown",
                })
            }
        }
    }

    return issues
}

/**
 * Fallback function to parse text output when JSON parsing fails
 * @param {string} output - Text output from Stylelint
 * @returns {Array} - Array of standardized issue objects
 */
function parseTextOutput(output) {
    const issues = []
    const lines = output.split("\n")

    for (const line of lines) {
        // Parse stylelint text output format: file:line:col ✖ message [rule]
        const match = line.match(/(.+?):(\d+):(\d+)\s+✖\s+(.+?)\s+\[(.+?)\]/)
        if (match) {
            const [, file, lineNum, col, message, rule] = match
            issues.push({
                file,
                line: Number.parseInt(lineNum, 10),
                col: Number.parseInt(col, 10),
                severity: "error", // Text output typically shows errors
                message,
                rule,
            })
        }
    }

    return issues
}

/**
 * Block the commit, dumping whatever Stylelint emitted so the failure is diagnosable
 * @param {string} reason - What went wrong
 * @param {string} blockedMessage - Short summary for the COMMIT BLOCKED line
 * @param {string} stdout - Stylelint stdout
 * @param {string} stderr - Stylelint stderr, deprecation warnings already filtered
 * @returns {never}
 */
function blockCommit(reason, blockedMessage, stdout, stderr) {
    logger.error(reason)
    if (stdout) {
        logger.error(stdout)
    }
    if (stderr) {
        logger.error(stderr)
    }
    logger.error(`🛑 COMMIT BLOCKED - ${blockedMessage}`)
    process.exit(1)
}

// Stylelint receives each path as an argument and Windows caps a command line at
// ~8191 chars, so a whole-tree run (200+ files) has to be split into batches.
const MAX_BATCH_SIZE = 50

try {
    logger.info(`Running Stylelint accessibility and style checks on ${files.length} CSS/Vue files...`)

    const issues = []

    for (let index = 0; index < files.length; index += MAX_BATCH_SIZE) {
        const batch = files.slice(index, index + MAX_BATCH_SIZE)
        const stylelintArgs = [...(fixFlag ? ["--fix"] : []), "--formatter", "json", "--allow-empty-input", ...batch]

        const stylelintResult = runCommand("stylelint", stylelintArgs, "Stylelint", projectRoot)

        // Filter out deprecation warnings before parsing: stylelint writes its JSON
        // report to stderr, so a leading DeprecationWarning line would make the whole
        // report unparseable.
        const cleanStderr = stylelintResult.stderr
            ? stylelintResult.stderr
                  .split("\n")
                  .filter((line) => !line.includes("DeprecationWarning"))
                  .join("\n")
                  .trim()
            : ""

        // Check for fatal errors
        if (stylelintResult.status !== 0 && stylelintResult.status !== 2) {
            blockCommit("Stylelint command failed:", "Stylelint execution failed", stylelintResult.stdout, cleanStderr)
        }

        // Parse and accumulate this batch's Stylelint output
        const batchIssues = parseStylelintOutput(stylelintResult.stdout, cleanStderr)
        issues.push(...batchIssues)

        // Status 2 means "violations found", so an empty batch means the report was lost in
        // parsing. Fail closed: passing silently here is the blind-stylelint bug this script
        // exists to prevent.
        if (stylelintResult.status === 2 && batchIssues.length === 0) {
            blockCommit(
                "Stylelint reported violations but none could be parsed:",
                "Stylelint output could not be read",
                stylelintResult.stdout,
                cleanStderr,
            )
        }
    }

    // For CSS, we need special handling of accessibility categories
    const criticalAccessibilityIssues = []
    const accessibilityWarnings = []
    const otherIssues = []

    for (const issue of issues) {
        const category = categorizeRule(issue.rule)
        if (category === "critical-accessibility") {
            criticalAccessibilityIssues.push(issue)
        } else if (category === "accessibility-warning") {
            accessibilityWarnings.push(issue)
        } else {
            otherIssues.push(issue)
        }
    }

    // Categorize non-accessibility issues by severity
    const categorizedOtherIssues = categorizeIssuesBySeverity(otherIssues, () => "other", "never-matches")

    // Merge accessibility and other issues for display
    const mergedCategorizedIssues = {
        criticalErrors: [...criticalAccessibilityIssues, ...categorizedOtherIssues.nonCriticalErrors],
        nonCriticalErrors: [],
        warnings: [...accessibilityWarnings, ...categorizedOtherIssues.warnings],
    }

    // Display categorized issues using shared function
    displayCategorizedIssues(
        mergedCategorizedIssues,
        {
            criticalLabel:
                criticalAccessibilityIssues.length > 0
                    ? "WCAG 2.1 AA VIOLATIONS - FEDERAL COMPLIANCE REQUIRED"
                    : "CSS ERRORS",
            nonCriticalLabel: "OTHER ERRORS",
            warningLabel: "ACCESSIBILITY & STYLE WARNINGS",
            criticalIcon: criticalAccessibilityIssues.length > 0 ? "🚨" : "❌",
        },
        "CSS",
    )

    // Use shared commit decision handler with special CSS messaging
    const config = {}
    if (criticalAccessibilityIssues.length > 0) {
        config.criticalBlockingMessage = "FEDERAL ACCESSIBILITY COMPLIANCE REQUIRED"
    }
    if (mergedCategorizedIssues.criticalErrors.length > criticalAccessibilityIssues.length) {
        config.errorBlockingMessage = "CSS ERRORS MUST BE FIXED"
    }

    handleCommitDecisionForCategorizedIssues(mergedCategorizedIssues, config, "CSS")
} catch (error) {
    logger.error("Unexpected error:", error)
    process.exit(1)
}
