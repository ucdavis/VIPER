#!/usr/bin/env node

const { execFileSync } = require("node:child_process")
const path = require("node:path")
const fs = require("node:fs")
const {
    handleCommitDecisionForCategorizedIssues,
    displayCategorizedIssues,
    categorizeIssuesBySeverity,
} = require("./lib/lint-staged-common")
const { createLogger } = require("./lib/script-utils")
const { categorizeRule } = require("./lib/critical-rules")
const { needsBuild, markAsBuilt, getCachedBuildOutput } = require("./lib/build-cache")

// Platform-specific path patterns
// Regex patterns for robust path classification (handle both "web" and "web/file.cs")
const WEB_PATH_REGEX = /(^|[\\/])web([\\/]|$)/
const TEST_PATH_REGEX = /(^|[\\/])test([\\/]|$)/

/**
 * Parse dotnet build output to extract SonarAnalyzer issues
 * @param {string} output - Raw output from dotnet build
 * @returns {Array} - Array of standardized issue objects
 */
function parseDotnetBuildOutput(output) {
    const issues = []

    if (!output || !output.trim()) {
        return issues
    }

    const lines = output.split("\n")

    for (const line of lines) {
        // Parse dotnet build analyzer output: file(line,col): severity RULE: message [project]
        // Example: C:\path\to\file.cs(68,1): warning S125: Remove this commented out code. [project.csproj]
        // Note: May have MSBuild prefix like "     1>" at the beginning
        const match = line.match(
            /^(?:\s*\d+>)?(.+?)\((\d+),(\d+)\):\s+(warning|error)\s+(\w+):\s+(.+?)(?:\s+\[(.+?)\])?\r?$/m,
        )

        if (match) {
            const [, filePath, lineNum, col, severity, rule, message, project] = match

            // Convert absolute paths to relative paths for consistent display
            const relativePath = path.relative(process.cwd(), filePath)

            issues.push({
                file: relativePath,
                line: Number.parseInt(lineNum, 10),
                col: Number.parseInt(col, 10),
                severity: severity,
                rule: rule,
                message: message.trim(),
                project: project,
            })
        }
    }

    return issues
}

/**
 * Parse dotnet format output to extract individual issues
 * @param {string} output - Raw output from dotnet format
 * @returns {Array} - Array of standardized issue objects
 */
function parseDotnetFormatOutput(output) {
    const issues = []

    if (!output || !output.trim()) {
        return issues
    }

    const lines = output.split("\n")

    for (const line of lines) {
        // Parse dotnet format output: file(line,col): severity RULE: message [project] (project part is optional)
        // Example: C:\path\to\file.cs(68,1): warning S125: Remove this commented out code. [project.csproj]
        // Note: May have MSBuild prefix like "     1>" at the beginning
        const match = line.match(
            /^(?:\s*\d+>)?(.+?)\((\d+),(\d+)\):\s+(warning|error)\s+(\w+):\s+(.+?)(?:\s+\[(.+?)\])?\r?$/m,
        )

        if (match) {
            const [, filePath, lineNum, col, severity, rule, message, project] = match

            // Convert absolute paths to relative paths for consistent display
            const relativePath = path.relative(process.cwd(), filePath)

            issues.push({
                file: relativePath,
                line: Number.parseInt(lineNum, 10),
                col: Number.parseInt(col, 10),
                severity: severity,
                rule: rule,
                message: message.trim(),
                project: project,
            })
        }
    }

    return issues
}

// Parse command line arguments using shared utility
const args = process.argv.slice(2)
const fixFlag = args.includes("--fix")
const forceFlag = args.includes("--force")
const rawFiles = args.filter((a) => !["--fix", "--force"].includes(a) && !a.startsWith("--"))

const logger = createLogger(".NET")

if (rawFiles.length === 0) {
    logger.success("No .cs files to check.")
    process.exit(0)
}

// Separate files by project (web vs test) - handle absolute and relative paths
const webFiles = rawFiles.filter((f) => WEB_PATH_REGEX.test(f))
const testFiles = rawFiles.filter((f) => TEST_PATH_REGEX.test(f))

// Function to run dotnet build for SonarAnalyzer on a specific project
const runBuild = (projectPath) => {
    const projectName = `${projectPath}.csproj`

    // Check if build is needed (unless forced)
    if (forceFlag) {
        logger.info(`Force flag enabled, skipping cache for ${projectPath}/ project`)
    } else {
        try {
            if (!needsBuild(projectPath, projectName)) {
                // Get cached analyzer output instead of skipping analysis
                const cachedOutput = getCachedBuildOutput(projectName)
                logger.success(`Build not needed for ${projectPath}/ project (using cached results)`)

                // Check for analyzer warnings in cached output
                const hasWarnings =
                    cachedOutput && (cachedOutput.includes("warning ") || cachedOutput.includes(": warning"))
                return { hasErrors: false, hasWarnings: hasWarnings, output: cachedOutput || "" }
            }
        } catch (error) {
            // If cache check fails, continue with fresh build
            logger.info(`Cache check failed, proceeding with fresh build: ${error.message}`)
        }
    }

    const args = ["build", `${projectPath}/`, "--no-incremental", "--verbosity", "normal"]

    try {
        logger.info(`Building ${projectPath}/ project for code analysis...`)
        const result = execFileSync("dotnet", args, {
            encoding: "utf8",
            timeout: 60_000, // Reduce timeout to 1 minute
            stdio: ["inherit", "pipe", "pipe"], // Suppress stdout, capture for parsing
        })

        // Store successful build in cache
        try {
            markAsBuilt(projectName, result)
        } catch (error) {
            logger.warning(`Failed to cache build result: ${error.message}`)
        }

        logger.success(`Build completed for ${projectPath}/ project`)

        // Check for analyzer warnings in build output
        const hasWarnings = result && (result.includes("warning ") || result.includes(": warning"))
        return { hasErrors: false, hasWarnings: hasWarnings, output: result || "" }
    } catch (error) {
        // Handle build failures - still capture analyzer output
        if (error.stdout || error.stderr) {
            const output = (error.stdout || "") + (error.stderr || "")

            // Store build output even for failed builds so analyzers can process it
            try {
                markAsBuilt(projectName, output)
            } catch (error) {
                logger.warning(`Failed to cache build output: ${error.message}`)
            }

            const hasWarnings = output.includes("warning ") || output.includes(": warning")
            const hasErrors = output.includes("error ") && !output.includes("Build FAILED")

            logger.info(`Build completed with issues for ${projectPath}/ project`)
            return { hasErrors: hasErrors, hasWarnings: hasWarnings, output: output }
        }

        logger.error(`DOTNET BUILD ERROR for ${projectPath}/: ${error.message}`)
        return { hasErrors: true, hasWarnings: false, output: "" }
    }
}

// Function to apply formatting fixes to a project
const applyFormatFixes = (projectPath, relativePaths) => {
    logger.info(`🔧 Attempting to fix formatting issues in ${relativePaths.length} file(s)...`)

    const fixArgs = ["format", `${projectPath}/`, "--severity", "warn"]
    for (const p of relativePaths) {
        fixArgs.push("--include", p)
    }

    let fixesApplied = false
    try {
        const fixResult = execFileSync("dotnet", fixArgs, {
            encoding: "utf8",
            timeout: 180_000,
            stdio: ["inherit", "pipe", "pipe"],
        })

        // Check if fixes were actually applied
        if (fixResult && fixResult.trim()) {
            fixesApplied = true
            logger.success(`✅ Applied automatic fixes in ${projectPath}/ project`)
        }
    } catch (error) {
        // Fix command may fail but still apply some fixes
        if (error.stdout && error.stdout.trim()) {
            fixesApplied = true
            logger.success(`✅ Applied some automatic fixes in ${projectPath}/ project`)
        }
    }

    if (!fixesApplied) {
        logger.info(`ℹ️  No automatic fixes available for ${projectPath}/ project`)
    }

    return fixesApplied
}

// Function to verify formatting for a project
const verifyFormatting = (projectPath, relativePaths, afterFix = false) => {
    if (afterFix) {
        logger.info(`🔍 Checking for remaining issues in ${projectPath}/ project...`)
    }

    const verifyArgs = ["format", `${projectPath}/`, "--verify-no-changes", "--severity", "warn"]
    for (const p of relativePaths) {
        verifyArgs.push("--include", p)
    }

    try {
        const result = execFileSync("dotnet", verifyArgs, {
            encoding: "utf8",
            timeout: 180_000,
            stdio: ["inherit", "pipe", "pipe"],
        })

        if (afterFix) {
            logger.success(`All issues have been fixed in ${projectPath}/ files`)
        }
        return { hasErrors: false, hasWarnings: false, output: result || "" }
    } catch (error) {
        if (error.stdout || error.stderr) {
            const output = (error.stdout || "") + (error.stderr || "")
            const hasWarnings = output.includes("warning ") || output.includes(": warning")
            const hasErrors = output.includes("error ") || output.includes(": error")

            if (afterFix) {
                const remainingCount = (output.match(/warning|error/g) || []).length
                logger.warning(`${remainingCount} issue(s) require manual attention in ${projectPath}/ project`)
            }

            return { hasErrors: hasErrors, hasWarnings: hasWarnings, output }
        }
        return { hasErrors: true, hasWarnings: false, output: "" }
    }
}

// Function to run dotnet format on a specific project
const runFormat = (projectPath, projectFiles) => {
    if (projectFiles.length === 0) {
        return { hasErrors: false, hasWarnings: false, output: "" }
    }

    // Convert absolute paths to relative paths for dotnet format
    const projectRoot = process.cwd()
    const relativePaths = projectFiles.map((f) => {
        // Use path.relative to get project-root-relative paths reliably
        const resolved = path.resolve(f)
        const relative = path.relative(projectRoot, resolved)

        // Verify the file is within the project (security check)
        if (relative.startsWith("..") || path.isAbsolute(relative)) {
            throw new Error(`File outside project directory: ${f}`)
        }

        return relative
    })

    logger.info(`Checking format for ${projectFiles.length} file(s) in ${projectPath}/ project...`)

    if (fixFlag) {
        // Apply fixes first
        applyFormatFixes(projectPath, relativePaths)

        // Then verify for remaining issues
        return verifyFormatting(projectPath, relativePaths, true)
    }
    // In check mode, just verify
    return verifyFormatting(projectPath, relativePaths, false)
}

// Helper function to check if a file is in a directory
const isFileInDirectory = (issueFile, requestedDir) => {
    try {
        if (!fs.existsSync(requestedDir)) {
            return false
        }

        const stats = fs.statSync(requestedDir)
        if (!stats.isDirectory()) {
            return false
        }

        const relativePath = path.relative(requestedDir, issueFile)
        return !relativePath.startsWith("..") && !path.isAbsolute(relativePath)
    } catch {
        return false
    }
}

// Filter issues to only those relevant to the requested files/directories
function isRelevantIssue(issue, requestedFiles) {
    // If no specific files requested, show all issues
    if (requestedFiles.length === 0) {
        return true
    }

    for (const requestedPath of requestedFiles) {
        const normalizedRequested = path.normalize(requestedPath).replaceAll("\\\\", "/")
        const normalizedIssueFile = path.normalize(issue.file).replaceAll("\\\\", "/")

        // Direct file match
        if (normalizedIssueFile === normalizedRequested) {
            return true
        }

        // Directory match - check if issue file is within the requested directory
        // Handle both "web" and "web/" style arguments
        const dirPath = normalizedRequested.endsWith("/") ? normalizedRequested : `${normalizedRequested}/`
        if (normalizedIssueFile.startsWith(dirPath)) {
            return true
        }

        // Use helper function for more robust directory checking
        if (isFileInDirectory(issue.file, requestedPath)) {
            return true
        }
    }
    return false
}

try {
    let allFormatOutput = ""
    let allBuildOutput = ""

    // Run build for SonarAnalyzer on web project if there are web files
    if (webFiles.length > 0) {
        const buildResult = runBuild("web")
        allBuildOutput += buildResult.output || ""

        // Run format on web project
        const formatResult = runFormat("web", webFiles)
        allFormatOutput += formatResult.output || ""
    }

    // Run build for SonarAnalyzer on test project if there are test files
    if (testFiles.length > 0) {
        const buildResult = runBuild("test")
        allBuildOutput += buildResult.output || ""

        // Run format on test project
        const formatResult = runFormat("test", testFiles)
        allFormatOutput += formatResult.output || ""
    }

    // Parse both build and format outputs to get all issues
    const buildIssues = parseDotnetBuildOutput(allBuildOutput)
    const formatIssues = parseDotnetFormatOutput(allFormatOutput)

    // Filter issues to only those relevant to requested files
    const allIssues = [...buildIssues, ...formatIssues]
    const issues = allIssues.filter((issue) => isRelevantIssue(issue, rawFiles))

    // Categorize issues using shared function (same logic as other scripts)
    const categorizedIssues = categorizeIssuesBySeverity(issues, categorizeRule, "critical-security")

    // Check if there are actual issues to report
    const totalIssues =
        categorizedIssues.criticalErrors.length +
        categorizedIssues.nonCriticalErrors.length +
        categorizedIssues.warnings.length

    // Display issues using shared logic that respects LINT_BLOCK_ON_WARNINGS
    if (totalIssues > 0) {
        const displayConfig = {
            criticalLabel: ".NET Security Violations",
            nonCriticalLabel: "Format Errors",
            warningLabel: "Format Warnings",
            criticalIcon: "🔐",
        }
        displayCategorizedIssues(categorizedIssues, displayConfig, ".NET")
    }

    if (totalIssues > 0) {
        // Issues are already displayed above

        // Use shared commit decision handler with .NET-specific messaging
        const config = {}
        if (categorizedIssues.criticalErrors.length > 0) {
            config.criticalBlockingMessage = ".NET SECURITY VIOLATIONS MUST BE FIXED"
        }
        if (categorizedIssues.nonCriticalErrors.length > 0) {
            config.errorBlockingMessage = ".NET FORMAT ERRORS MUST BE FIXED"
        }

        handleCommitDecisionForCategorizedIssues(categorizedIssues, config, ".NET")
    } else {
        logger.success(`All C# files ${fixFlag ? "have been fixed" : "pass linting checks"}`)
    }
} catch (error) {
    if (error.code === "ETIMEDOUT") {
        logger.error("dotnet format timed out after 3 minutes")
    }
    process.exit(1)
}
