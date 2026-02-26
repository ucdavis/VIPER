const { spawnSync } = require("node:child_process")
const path = require("node:path")
const fs = require("node:fs")
const { createLogger } = require("./script-utils")

const { env } = process

// Platform-specific constants
const IS_WINDOWS = process.platform === "win32"

/**
 * Categorize issues by severity and rule category
 * @param {Array} issues - Array of issue objects with severity and rule properties
 * @param {Function} categorizeRule - Function to categorize rules (returns 'critical-security', 'critical-accessibility', etc.)
 * @param {string|string[]} criticalCategories - Critical category name(s) (e.g., 'critical-security' or ['critical-security', 'critical-accessibility'])
 * @returns {Object} - Categorized issues: { criticalErrors, nonCriticalErrors, warnings }
 */
function categorizeIssuesBySeverity(issues, categorizeRule, criticalCategories) {
    const criticalErrors = []
    const nonCriticalErrors = []
    const warnings = []

    // Normalize criticalCategories to array for consistent handling
    const criticalCategoriesArray = Array.isArray(criticalCategories) ? criticalCategories : [criticalCategories]

    for (const issue of issues) {
        const category = categorizeRule(issue.rule)

        if (criticalCategoriesArray.includes(category)) {
            criticalErrors.push(issue)
        } else if (issue.severity === "error" || issue.severity === 2) {
            nonCriticalErrors.push(issue)
        } else {
            warnings.push(issue)
        }
    }

    return { criticalErrors, nonCriticalErrors, warnings }
}

/**
 * Create a standardized summary reporter for lint results
 * @param {string} toolName - Name of the linting tool (e.g., 'CSS', 'Vue/JS/TS')
 * @param {Object} logger - Logger instance to use for output
 * @returns {Object} - Reporter functions
 */
function createSummaryReporter(toolName, logger) {
    return {
        /**
         * Log summary of issues found
         * @param {number} totalIssues - Total number of issues
         * @param {number} criticalCount - Number of critical security/accessibility errors
         * @param {number} errorCount - Number of regular errors
         * @param {number} warningCount - Number of warnings
         */
        logSummary(totalIssues, criticalCount, errorCount, warningCount) {
            const parts = []
            if (criticalCount > 0) {
                parts.push(`${criticalCount} critical errors`)
            }
            if (errorCount > 0) {
                parts.push(`${errorCount} errors`)
            }
            if (warningCount > 0) {
                parts.push(`${warningCount} warnings`)
            }

            logger.plain(`\n📊 ${toolName} Summary: ${totalIssues} total issues (${parts.join(", ")})`)
        },

        /**
         * Log commit decision and exit with appropriate code
         * @param {boolean} hasBlockingIssues - Whether there are issues that should block
         * @param {boolean} hasWarnings - Whether there are non-blocking warnings
         * @param {string} blockingReason - Reason for blocking (e.g., 'CRITICAL ERRORS', 'FEDERAL ACCESSIBILITY COMPLIANCE')
         */
        handleCommitDecision(hasBlockingIssues, hasWarnings, blockingReason = "CRITICAL ERRORS") {
            const blockOnWarnings = shouldBlockOnWarnings()
            const shouldBlock = hasBlockingIssues || (blockOnWarnings && hasWarnings)

            if (shouldBlock) {
                if (hasBlockingIssues) {
                    logger.plain(`\n🛑 COMMIT BLOCKED due to ${blockingReason}.`)
                    logger.plain("🔒 Critical issues MUST be fixed before committing.")
                }
                if (blockOnWarnings && hasWarnings && !hasBlockingIssues) {
                    logger.plain("\n⚠️  Warnings detected.")
                    logger.plain(
                        "💡 These warnings would not block commits in normal mode. Fix warnings above or use lint:precommit to ignore warnings.",
                    )
                }
                process.exit(1)
            } else if (hasWarnings) {
                logger.plain("\n✅ COMMIT ALLOWED - Only warnings detected (non-blocking).")
                logger.plain("💡 Consider addressing warnings to improve code quality.")
            } else {
                logger.plain(`\n✅ All ${toolName} checks passed!`)
            }
        },
    }
}

/**
 * Display categorized issues with standardized formatting using consistent logger
 * @param {Object} categorizedIssues - Issues categorized by severity
 * @param {Object} config - Display configuration
 * @param {string} toolName - Name of the linting tool for logger context
 */
function displayCategorizedIssues(categorizedIssues, config, toolName) {
    const { criticalErrors, nonCriticalErrors, warnings } = categorizedIssues
    const { criticalLabel, nonCriticalLabel, warningLabel, criticalIcon = "🚨" } = config

    const logger = createLogger(toolName)
    const blockOnWarnings = shouldBlockOnWarnings()

    // Display critical errors
    if (criticalErrors.length > 0) {
        logger.plain(`\n${criticalIcon} ${criticalLabel} (${criticalErrors.length}) - MUST FIX:`)
        for (const issue of criticalErrors) {
            logger.plain(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`)
        }
    }

    // Display non-critical errors
    if (nonCriticalErrors.length > 0) {
        logger.error(`${nonCriticalLabel} (${nonCriticalErrors.length}) - MUST FIX:`)
        for (const issue of nonCriticalErrors) {
            logger.plain(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`)
        }
    }

    // Always display warnings so developers can see them
    // LINT_BLOCK_ON_WARNINGS controls whether warnings block commits, not whether they're shown
    if (warnings.length > 0) {
        const blockingStatus = blockOnWarnings ? "" : " - non-blocking"
        logger.warning(`${warningLabel} (${warnings.length})${blockingStatus}:`)
        for (const issue of warnings) {
            logger.plain(`  ${issue.file}:${issue.line}:${issue.col} - ${issue.rule}: ${issue.message}`)
        }
    }

    // Display summary message
    const totalIssues = criticalErrors.length + nonCriticalErrors.length + warnings.length
    const hasErrors = criticalErrors.length > 0 || nonCriticalErrors.length > 0

    if (totalIssues === 0) {
        logger.success(`No issues found`)
    } else if (!blockOnWarnings && !hasErrors) {
        logger.success("No critical violations found")
    }
}

/**
 * Handle commit decision based on categorized issues using consistent logger
 * @param {Object} categorizedIssues - Issues categorized by severity
 * @param {Object} config - Configuration for blocking behavior
 * @param {string} toolName - Name of the linting tool
 * @returns {Object} - Issue counts and blocking status
 */
function handleCommitDecisionForCategorizedIssues(categorizedIssues, config, toolName) {
    const { criticalErrors, nonCriticalErrors, warnings } = categorizedIssues
    const { criticalBlockingMessage, errorBlockingMessage } = config

    const logger = createLogger(toolName)
    const blockOnWarnings = shouldBlockOnWarnings()
    const hasErrors = criticalErrors.length > 0 || nonCriticalErrors.length > 0

    // Calculate totals for summary - always show actual counts found
    const totalIssues = criticalErrors.length + nonCriticalErrors.length + warnings.length
    const criticalCount = criticalErrors.length
    const errorCount = nonCriticalErrors.length
    const warningCount = warnings.length

    // Create and use summary reporter
    const reporter = createSummaryReporter(toolName, logger)

    if (totalIssues > 0 || blockOnWarnings) {
        reporter.logSummary(totalIssues, criticalCount, errorCount, warningCount)
    }

    // Handle blocking with consistent logger
    if (hasErrors) {
        if (criticalErrors.length > 0 && criticalBlockingMessage) {
            logger.error(`COMMIT BLOCKED - ${criticalBlockingMessage}`)
        }
        if (nonCriticalErrors.length > 0 && errorBlockingMessage) {
            logger.error(`COMMIT BLOCKED - ${errorBlockingMessage}`)
        }
        if (!criticalBlockingMessage && !errorBlockingMessage) {
            logger.error("COMMIT BLOCKED due to ERRORS.")
            logger.plain("🔒 All errors MUST be fixed before committing.")
        }
        process.exit(1)
    }

    // Use shared reporter for warning handling
    reporter.handleCommitDecision(false, warnings.length > 0)

    return { totalIssues, criticalCount, errorCount, warningCount, hasErrors }
}

/**
 * Parse command line arguments for lint-staged scripts
 * @returns {Object} { fixFlag: boolean, files: string[] }
 */
function parseArguments() {
    const knownFlags = new Set(["--fix"])
    const args = process.argv.slice(2)
    const fixFlag = args.includes("--fix")
    const rawFiles = args.filter((a) => !knownFlags.has(a) && !a.startsWith("--"))

    return { fixFlag, rawFiles }
}

/**
 * Parse JSON output from linting tools (ESLint, Stylelint) with fallback to text parsing
 * @param {string} stdout - Tool stdout
 * @param {string} stderr - Tool stderr (may contain valid JSON in some cases)
 * @param {string} toolName - Name of the linting tool for logging
 * @param {Function} textFallbackParser - Function to parse text output if JSON parsing fails
 * @returns {Array} - Array of parsed JSON results (empty array if no valid JSON)
 */
function parseJsonOutput(stdout, stderr, toolName, textFallbackParser) {
    const logger = createLogger(toolName)

    // Parse JSON output (should be in stdout with --format json)
    const jsonOutput = stdout.trim()

    try {
        if (jsonOutput) {
            const results = JSON.parse(jsonOutput)
            if (!Array.isArray(results)) {
                logger.warning(`${toolName} JSON output is not an array, analyzing text output`)
                return textFallbackParser ? textFallbackParser(stdout + stderr) : []
            }
            return results
        }
        return []
    } catch {
        logger.warning(`Failed to parse ${toolName} JSON output, analyzing text output`)
        return textFallbackParser ? textFallbackParser(stdout + stderr) : []
    }
}

/**
 * Securely run a command with local binary resolution
 * @param {string} command - Command name (e.g., 'stylelint', 'eslint')
 * @param {string[]} args - Command arguments
 * @param {string} description - Description for logging
 * @param {string} cwd - Working directory
 * @returns {Object} - { success: boolean, stdout: string, stderr: string, status: number }
 */
function runCommand(command, args, description, cwd) {
    const logger = createLogger("Command")
    logger.plain(`Running ${description}...`)

    // Use local binary - requires proper npm install
    const binName = IS_WINDOWS ? `${command}.cmd` : command
    const localBin = path.join(cwd, "node_modules", ".bin", binName)

    if (!fs.existsSync(localBin)) {
        throw new Error(
            `Command '${command}' not found in node_modules/.bin/. Please run 'npm install' to install dependencies.`,
        )
    }

    // Windows .cmd files need to be run via cmd.exe
    // Use ComSpec directly instead of shell: true to avoid DEP0190 deprecation warning
    // And ensure proper argument escaping
    const useCmd = IS_WINDOWS && localBin.endsWith(".cmd")
    const execPath = useCmd ? env.ComSpec : localBin
    const finalArgs = useCmd ? ["/c", localBin, ...args] : args

    const result = spawnSync(execPath, finalArgs, {
        stdio: ["inherit", "pipe", "pipe"],
        cwd: cwd,
        encoding: "utf8",
        windowsHide: true,
    })

    if (result.error) {
        logger.plainError(`Failed to run ${description}: ${result.error}`)
        return {
            success: false,
            stdout: result.stdout || "",
            stderr: result.stderr || "",
            status: -1,
        }
    }

    return {
        success: result.status === 0,
        stdout: result.stdout || "",
        stderr: result.stderr || "",
        status: result.status,
    }
}

/**
 * Sanitize and validate file paths with security checks
 * @param {string} filePath - Raw file path from lint-staged
 * @param {string} baseDir - Base directory to validate against
 * @param {string[]} allowedExtensions - Array of allowed file extensions (e.g., ['.css', '.vue'])
 * @param {number} maxFileSizeMB - Maximum file size in MB (default: 5)
 * @returns {string|null} - Relative path for linting tools, or null if file should be skipped
 */
const DEFAULT_MAX_FILE_SIZE_MB = 5
function sanitizeFilePath(filePath, baseDir, allowedExtensions, maxFileSizeMB = DEFAULT_MAX_FILE_SIZE_MB) {
    const logger = createLogger("FilePath")

    // Handle Windows paths passed by lint-staged - convert forward slashes to proper format
    let normalizedPath = filePath

    // On Windows, lint-staged may pass paths like C:/path/to/file
    // Convert these to proper Windows format first
    if (IS_WINDOWS && /^[A-Za-z]:[\\\\/]/.test(filePath)) {
        normalizedPath = filePath.replaceAll("/", path.sep)
    }

    // Normalize the path to prevent directory traversal
    normalizedPath = path.normalize(normalizedPath)

    // Resolve to absolute path for security checks
    const resolvedPath = path.resolve(normalizedPath)
    const baseAbsPath = path.resolve(baseDir)

    // Ensure file is within base directory using path.relative (more secure than startsWith)
    const relativeToBase = path.relative(baseAbsPath, resolvedPath)
    if (relativeToBase.startsWith("..") || path.isAbsolute(relativeToBase)) {
        logger.plainWarning(`[lint-skip] Outside base directory: ${filePath}`)
        return null
    }

    // Ensure it's an allowed file type (skip check if no extensions specified)
    if (allowedExtensions.length > 0) {
        const ext = path.extname(resolvedPath).toLowerCase()
        if (!allowedExtensions.includes(ext)) {
            logger.plainWarning(`[lint-skip] Disallowed extension: ${filePath}`)
            return null
        }
    }

    // If the file was removed between staging and lint run, treat as non-existent.
    if (!fs.existsSync(resolvedPath)) {
        return null
    }

    // Check file size to prevent DoS attacks
    const KB_TO_BYTES = 1024
    const MB_TO_KB = 1024
    const BYTES_PER_MB = KB_TO_BYTES * MB_TO_KB
    const MAX_FILE_SIZE_BYTES = maxFileSizeMB * BYTES_PER_MB
    let stats = null
    try {
        stats = fs.statSync(resolvedPath)
    } catch {
        // Treat unexpected stat failures as non-existent to avoid blocking commits.
        return null
    }

    if (stats.size > MAX_FILE_SIZE_BYTES) {
        throw new Error(
            `File too large (${Math.round(stats.size / BYTES_PER_MB)}MB > ${maxFileSizeMB}MB): ${filePath}. Large files may cause performance issues during linting. Consider excluding from commit or increasing maxFileSizeMB.`,
        )
    }

    // Return relative path for linting tools (relative to base directory)
    // Use forward slashes for cross-platform compatibility with linting tools
    const relativePath = path.posix.normalize(path.relative(baseAbsPath, resolvedPath))
    return relativePath
}

/**
 * Check if linting should block based on environment variable
 * @returns {boolean} - True if warnings should block commits
 */
function shouldBlockOnWarnings() {
    return env.LINT_BLOCK_ON_WARNINGS === "true"
}

/**
 * Filter TypeScript compiler output to only show errors related to target files/directories
 * @param {string} tscOutput - Raw TypeScript compiler output (stdout + stderr)
 * @param {string[]} targetFiles - Array of target file paths or directories to filter for
 * @param {string} projectRoot - Project root directory for path resolution
 * @returns {string} - Filtered output containing only relevant errors
 */
function filterTypeScriptErrors(tscOutput, targetFiles, projectRoot) {
    if (!tscOutput || !tscOutput.trim()) {
        return tscOutput
    }

    const lines = tscOutput.split("\n")
    const filteredLines = []

    // Convert target files to absolute paths for matching
    const absoluteTargets = targetFiles.map((target) => {
        const resolved = path.resolve(projectRoot, target)
        // Handle both files and directories
        return {
            original: target,
            resolved: resolved,
            isDirectory: fs.existsSync(resolved) && fs.statSync(resolved).isDirectory(),
        }
    })

    for (const line of lines) {
        // TypeScript error format: filepath(line,col): error TSxxxx: message
        const errorMatch = line.match(/^(.+?)\((\d+),(\d+)\):\s+(error|warning)\s+TS\d+:/)

        if (errorMatch) {
            const [, errorFilePath] = errorMatch
            const absoluteErrorPath = path.resolve(projectRoot, errorFilePath)

            // Check if this error belongs to any of our target files/directories
            const isRelevant = absoluteTargets.some((target) => {
                if (target.isDirectory) {
                    // For directories, check if error file is within the directory
                    const relativePath = path.relative(target.resolved, absoluteErrorPath)
                    return !relativePath.startsWith("..") && !path.isAbsolute(relativePath)
                }
                // For files, check exact match
                return absoluteErrorPath === target.resolved
            })

            if (isRelevant) {
                filteredLines.push(line)
            }
        } else {
            // Keep non-error lines (like summary messages) if we have any relevant errors
            // Or if this might be a continuation of an error message
            const hasRelevantErrors = filteredLines.some((line) => line.includes("error TS"))
            if (hasRelevantErrors || line.trim() === "" || line.includes("Found ")) {
                filteredLines.push(line)
            }
        }
    }

    return filteredLines.join("\n")
}

module.exports = {
    IS_WINDOWS,
    categorizeIssuesBySeverity,
    createSummaryReporter,
    displayCategorizedIssues,
    handleCommitDecisionForCategorizedIssues,
    parseArguments,
    parseJsonOutput,
    runCommand,
    sanitizeFilePath,
    shouldBlockOnWarnings,
    filterTypeScriptErrors,
}
