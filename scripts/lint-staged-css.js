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

try {
    // Run Stylelint using shared command runner
    const stylelintArgs = [...(fixFlag ? ["--fix"] : []), "--formatter", "json", "--allow-empty-input", ...files]

    logger.info(`Running Stylelint accessibility and style checks on ${files.length} CSS/Vue files...`)
    const stylelintResult = runCommand("stylelint", stylelintArgs, "Stylelint", projectRoot)

    // Check for fatal errors
    if (stylelintResult.status !== 0 && stylelintResult.status !== 2) {
        logger.error("Stylelint command failed:")
        if (stylelintResult.stdout) {
            logger.error(stylelintResult.stdout)
        }
        if (stylelintResult.stderr) {
            logger.error(stylelintResult.stderr)
        }
        logger.error("🛑 COMMIT BLOCKED - Stylelint execution failed")
        process.exit(1)
    }

    // Status 2 means "violations found" - only warn if no violations were parsed
    if (stylelintResult.status === 2) {
        const jsonToCheck = stylelintResult.stdout.trim() || stylelintResult.stderr.trim()
        const hasValidJson = jsonToCheck && jsonToCheck.startsWith("[")
        if (!hasValidJson) {
            logger.warning("STYLELINT CONFIGURATION WARNING: Status 2 with no parseable violations")
            logger.warning("📋 Consider reviewing stylelint.config.mjs if unexpected behavior occurs")
        }
    }

    // Filter out deprecation warnings
    const cleanStderr = stylelintResult.stderr
        ? stylelintResult.stderr
              .split("\n")
              .filter((line) => !line.includes("DeprecationWarning"))
              .join("\n")
              .trim()
        : ""

    // Parse and categorize Stylelint output
    const issues = parseStylelintOutput(stylelintResult.stdout, cleanStderr)

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
