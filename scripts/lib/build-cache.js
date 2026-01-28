#!/usr/bin/env node

const fs = require("node:fs")
const path = require("node:path")
const crypto = require("node:crypto")
const { execFileSync } = require("node:child_process")
const { createLogger } = require("./script-utils")

const { env } = process
const logger = createLogger("Cache")

// Quiet mode suppresses info logs (only show warnings/errors)
// Enable via QUIET_CACHE=1 environment variable
const quietMode = env.QUIET_CACHE === "1"

/**
 * Build caching utilities using content hashing
 * Tracks file content hashes to determine if rebuilds are needed
 * More reliable than timestamp-based caching - eliminates need for --force flag
 */

// Cache directory
const CACHE_DIR = path.join(__dirname, "..", "..", ".build-cache")
const CACHE_FILE = path.join(CACHE_DIR, "build-hashes.json")

// Hash display length for log messages
const HASH_DISPLAY_LENGTH = 12

// Ensure cache directory exists
function ensureCacheDir() {
    if (!fs.existsSync(CACHE_DIR)) {
        fs.mkdirSync(CACHE_DIR, { recursive: true })
    }
}

/**
 * Recursively find all files matching extensions in a directory
 * @param {string} dir - Directory to search
 * @param {string[]} extensions - File extensions to include (e.g., ['.cs', '.csproj'])
 * @param {string[]} excludeDirs - Directories to exclude (e.g., ['bin', 'obj', 'node_modules'])
 * @returns {string[]} - Array of absolute file paths
 */
function findFiles(dir, extensions = [".cs", ".csproj"], excludeDirs = ["bin", "obj", "node_modules", ".git"]) {
    const files = []

    function scan(currentDir) {
        try {
            const entries = fs.readdirSync(currentDir, { withFileTypes: true })

            for (const entry of entries) {
                const fullPath = path.join(currentDir, entry.name)

                if (entry.isDirectory()) {
                    // Skip excluded directories
                    if (!excludeDirs.includes(entry.name)) {
                        scan(fullPath)
                    }
                } else if (entry.isFile()) {
                    const ext = path.extname(entry.name).toLowerCase()
                    if (extensions.includes(ext)) {
                        files.push(fullPath)
                    }
                }
            }
        } catch (error) {
            logger.warning(`Could not scan directory ${currentDir}: ${error.message}`)
        }
    }

    scan(dir)
    return files.toSorted((a, b) => a.localeCompare(b)) // Sort for consistent ordering
}

/**
 * Compute SHA-256 hash of a single file
 * @param {string} filePath - Path to the file
 * @returns {string} - Hex-encoded hash
 */
function hashFile(filePath) {
    try {
        const hash = crypto.createHash("sha256")
        const content = fs.readFileSync(filePath)
        hash.update(content)
        return hash.digest("hex")
    } catch (error) {
        logger.warning(`Could not hash file ${filePath}: ${error.message}`)
        return "0"
    }
}

/**
 * Compute combined hash of all relevant files in a project
 * @param {string} projectPath - Path to the project directory
 * @param {string[]} extensions - File extensions to check (e.g., ['.cs', '.csproj'])
 * @returns {string} - Hex-encoded hash of all file contents
 */
function computeProjectHash(projectPath, extensions = [".cs", ".csproj"]) {
    try {
        const files = findFiles(projectPath, extensions)

        if (files.length === 0) {
            logger.warning(`No files found in ${projectPath} with extensions ${extensions.join(", ")}`)
            return "0"
        }

        // Hash each file individually, then combine hashes
        const combinedHash = crypto.createHash("sha256")

        for (const file of files) {
            const fileHash = hashFile(file)
            combinedHash.update(fileHash)
        }

        return combinedHash.digest("hex")
    } catch (error) {
        logger.warning(`Could not compute hash for ${projectPath}: ${error.message}`)
        return "0"
    }
}

/**
 * Load build cache from disk
 * @returns {Object} - Cache object with project hashes and build outputs
 */
function loadBuildCache() {
    ensureCacheDir()

    try {
        if (fs.existsSync(CACHE_FILE)) {
            return JSON.parse(fs.readFileSync(CACHE_FILE, "utf8"))
        }
    } catch (error) {
        logger.warning(`Could not load build cache: ${error.message}`)
    }

    return {}
}

/**
 * Save build cache to disk
 * @param {Object} cache - Cache object to save
 */
function saveBuildCache(cache) {
    ensureCacheDir()

    try {
        fs.writeFileSync(CACHE_FILE, JSON.stringify(cache, null, 2))
    } catch (error) {
        logger.warning(`Could not save build cache: ${error.message}`)
    }
}

/**
 * Check if a project needs to be built based on file content hashes
 * @param {string} projectPath - Path to the project directory
 * @param {string} projectName - Name of the project for caching
 * @returns {boolean} - True if build is needed
 */
function needsBuild(projectPath, projectName) {
    const cache = loadBuildCache()
    const currentHash = computeProjectHash(projectPath)
    const cacheEntry = cache[projectName]

    // Always rebuild if no cache entry exists
    if (!cacheEntry || typeof cacheEntry !== "object") {
        if (!quietMode) {
            logger.info(`📝 Build needed for ${projectName}: no valid cache entry found`)
        }
        return true
    }

    const cachedHash = cacheEntry.hash || "0"

    // Compare content hashes
    const needsRebuild = currentHash !== cachedHash

    if (!quietMode) {
        if (needsRebuild) {
            logger.info(
                `📝 Build needed for ${projectName}: file content has changed (hash: ${currentHash.slice(0, HASH_DISPLAY_LENGTH)}... != cached: ${cachedHash.slice(0, HASH_DISPLAY_LENGTH)}...)`,
            )
        } else {
            logger.info(
                `✅ Skipping build for ${projectName}: no changes detected (hash: ${currentHash.slice(0, HASH_DISPLAY_LENGTH)}...)`,
            )
        }
    }

    return needsRebuild
}

/**
 * Mark a project as built and cache the build output
 * @param {string} projectPath - Path to the project directory
 * @param {string} projectName - Name of the project
 * @param {string} buildOutput - Build output to cache
 * @param {boolean} success - Whether the build succeeded (default: true)
 */
function markAsBuilt(projectPath, projectName, buildOutput = "", success = true) {
    const cache = loadBuildCache()
    const currentHash = computeProjectHash(projectPath)

    cache[projectName] = {
        hash: currentHash,
        timestamp: Date.now(),
        output: buildOutput,
        success,
    }
    saveBuildCache(cache)
}

/**
 * Check if a cached build was successful
 * @param {string} projectName - Name of the project
 * @returns {boolean|null} - true if successful, false if failed, null if not cached
 */
function wasBuildSuccessful(projectName) {
    const cache = loadBuildCache()
    const cacheEntry = cache[projectName]

    if (!cacheEntry || typeof cacheEntry !== "object") {
        return null
    }

    // Legacy cache entries without success field are assumed successful
    return cacheEntry.success !== false
}

/**
 * Get cached build output for a project
 * @param {string} projectName - Name of the project
 * @returns {string|null} - Cached build output or null if not cached or cache invalid
 */
function getCachedBuildOutput(projectName) {
    const cache = loadBuildCache()
    const cacheEntry = cache[projectName]

    if (!cacheEntry || typeof cacheEntry !== "object") {
        return null
    }

    return cacheEntry.output || ""
}

/**
 * Clear the build cache for a specific project or all projects
 * @param {string} [projectName] - Optional project name to clear, or undefined to clear all
 */
function clearBuildCache(projectName) {
    if (projectName) {
        const cache = loadBuildCache()
        delete cache[projectName]
        saveBuildCache(cache)
        logger.info(`🧹 Cleared build cache for ${projectName}`)
    } else {
        try {
            if (fs.existsSync(CACHE_FILE)) {
                fs.unlinkSync(CACHE_FILE)
            }
            logger.info("🧹 Cleared all build cache")
        } catch (error) {
            logger.warning(`Could not clear build cache: ${error.message}`)
        }
    }
}

/**
 * Run a build command only if needed
 * @param {string} projectPath - Path to the project directory
 * @param {string} projectName - Name of the project for caching
 * @param {string[]} buildArgs - Arguments to pass to dotnet build
 * @param {Object} options - Options for execFileSync
 * @returns {Object} - { success: boolean, output: string, wasCached: boolean }
 */
function buildIfNeeded(projectPath, projectName, buildArgs = ["build"], options = {}) {
    if (!needsBuild(projectPath, projectName)) {
        // Return cached build output if available
        const cache = loadBuildCache()
        const cacheEntry = cache[projectName]
        const cachedOutput = cacheEntry && typeof cacheEntry === "object" ? cacheEntry.output : "Build skipped (cached)"

        return { success: true, output: cachedOutput, wasCached: true }
    }

    try {
        logger.info(`🔨 Building ${projectName}...`)
        const result = execFileSync("dotnet", buildArgs, {
            cwd: projectPath,
            encoding: "utf8",
            timeout: 180_000,
            ...options,
        })

        markAsBuilt(projectPath, projectName, result)
        logger.success(`✅ Build completed for ${projectName}`)

        return { success: true, output: result, wasCached: false }
    } catch (error) {
        // For analyzer tools, we want to capture the output even if build fails
        // The stderr/stdout contains the analyzer warnings/errors we need
        const buildOutput = (error.stdout || "") + (error.stderr || "")

        // Store the build output even for failed builds so analyzers can process it
        markAsBuilt(projectPath, projectName, buildOutput)

        logger.warning(`⚠️  Build completed with errors for ${projectName} (analyzer output captured)`)

        // Return success=true so analyzers can process the output
        // The analyzer will determine if the errors should block the commit
        return { success: true, output: buildOutput, wasCached: false }
    }
}

/**
 * Check if format check is needed for specific files based on content hashes
 * @param {string[]} filePaths - Array of file paths to check
 * @param {string} cacheKey - Unique key for this format check (e.g., "web-format")
 * @returns {boolean} - True if format check is needed
 */
function needsFormatCheck(filePaths, cacheKey) {
    const cache = loadBuildCache()
    const cacheEntry = cache[cacheKey]

    // Compute combined hash of all specified files
    const currentHash = crypto.createHash("sha256")
    const sortedPaths = [...filePaths].toSorted((a, b) => a.localeCompare(b)) // Ensure consistent ordering

    for (const filePath of sortedPaths) {
        try {
            const fileHash = hashFile(filePath)
            currentHash.update(fileHash)
        } catch (error) {
            logger.warning(`Could not hash file ${filePath}: ${error.message}`)
            return true // If we can't hash, assume format check is needed
        }
    }

    const currentHashDigest = currentHash.digest("hex")

    // Check if cache entry exists and hash matches
    if (!cacheEntry || typeof cacheEntry !== "object") {
        if (!quietMode) {
            logger.info(`📝 Format check needed for ${cacheKey}: no valid cache entry found`)
        }
        return true
    }

    const cachedHash = cacheEntry.hash || "0"
    const needsCheck = currentHashDigest !== cachedHash

    if (!quietMode) {
        if (needsCheck) {
            logger.info(
                `📝 Format check needed for ${cacheKey}: file content changed (hash: ${currentHashDigest.slice(0, HASH_DISPLAY_LENGTH)}... != cached: ${cachedHash.slice(0, HASH_DISPLAY_LENGTH)}...)`,
            )
        } else {
            logger.info(
                `✅ Skipping format check for ${cacheKey}: no changes detected (hash: ${currentHashDigest.slice(0, HASH_DISPLAY_LENGTH)}...)`,
            )
        }
    }

    return needsCheck
}

/**
 * Mark format check as complete and cache the results
 * @param {string[]} filePaths - Array of file paths that were checked
 * @param {string} cacheKey - Unique key for this format check
 * @param {string} formatOutput - Format check output to cache
 */
function markFormatChecked(filePaths, cacheKey, formatOutput = "") {
    const cache = loadBuildCache()

    // Compute hash of all specified files
    const currentHash = crypto.createHash("sha256")
    const sortedPaths = [...filePaths].toSorted((a, b) => a.localeCompare(b))

    for (const filePath of sortedPaths) {
        try {
            const fileHash = hashFile(filePath)
            currentHash.update(fileHash)
        } catch (error) {
            logger.warning(`Could not hash file ${filePath}: ${error.message}`)
            return
        }
    }

    const currentHashDigest = currentHash.digest("hex")

    cache[cacheKey] = {
        hash: currentHashDigest,
        timestamp: Date.now(),
        output: formatOutput,
        files: sortedPaths, // Store file list for debugging
    }

    saveBuildCache(cache)
}

/**
 * Get cached format check output
 * @param {string} cacheKey - Unique key for this format check
 * @returns {string|null} - Cached format output or null if not cached
 */
function getCachedFormatOutput(cacheKey) {
    const cache = loadBuildCache()
    const cacheEntry = cache[cacheKey]

    if (!cacheEntry || typeof cacheEntry !== "object") {
        return null
    }

    return cacheEntry.output || ""
}

// Matches any MSBuild error line with an error code (e.g., ": error CS1234", ": error NETSDK1045")
const ERROR_PATTERN = /:\s*error\s+[A-Z]+\d+/i

/**
 * Filter build output to show only error lines (not warnings)
 * @param {string} output - Full build output
 * @returns {string} - Filtered output with only errors, or empty string if no errors found
 */
function filterBuildErrors(output) {
    if (!output || !output.trim()) {
        return ""
    }
    const errorLines = output
        .split("\n")
        .filter(
            (line) =>
                // Any MSBuild error with error code (CS, MSB, NETSDK, NU, SDK, VBC, etc.)
                ERROR_PATTERN.test(line) ||
                // Build failure indicators
                line.includes("Build FAILED") ||
                // File locking errors
                line.includes("is being used by another process") ||
                line.includes("Could not copy"),
        )
        .join("\n")
    return errorLines.trim()
}

/**
 * Check if build output contains actual compilation errors (not just warnings)
 * @param {string} output - Full build output
 * @returns {boolean} - True if there are actual errors
 */
function hasActualErrors(output) {
    if (!output || !output.trim()) {
        return false
    }
    // Check for actual error patterns (not warnings)
    // Uses same ERROR_PATTERN as filterBuildErrors for consistency
    return (
        ERROR_PATTERN.test(output) ||
        output.includes("Build FAILED") ||
        output.includes("is being used by another process") ||
        output.includes("Could not copy")
    )
}

module.exports = {
    needsBuild,
    markAsBuilt,
    wasBuildSuccessful,
    getCachedBuildOutput,
    filterBuildErrors,
    hasActualErrors,
    clearBuildCache,
    buildIfNeeded,
    computeProjectHash,
    needsFormatCheck,
    markFormatChecked,
    getCachedFormatOutput,
}
