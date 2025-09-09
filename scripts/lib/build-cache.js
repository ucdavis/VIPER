#!/usr/bin/env node

const fs = require("node:fs")
const path = require("node:path")
const { execFileSync } = require("node:child_process")
const { createLogger } = require("./script-utils")
const recursiveLastModified = require("recursive-last-modified")

const logger = createLogger("Cache")

/**
 * Build caching utilities to avoid redundant builds during linting
 * Tracks file modifications and build timestamps to determine if rebuilds are needed
 */

// Cache directory
const CACHE_DIR = path.join(__dirname, "..", "..", ".build-cache")
const CACHE_FILE = path.join(CACHE_DIR, "build-timestamps.json")

// Ensure cache directory exists
function ensureCacheDir() {
    if (!fs.existsSync(CACHE_DIR)) {
        fs.mkdirSync(CACHE_DIR, { recursive: true })
    }
}

/**
 * Get the timestamp of the most recent file modification in a directory
 * Uses recursive-last-modified package for reliable directory traversal
 *
 * NOTE: The recursive-last-modified library (v1.0.6) is synchronous and doesn't
 * currently support exclusion lists for directories like node_modules, bin, or obj.
 * This is a known limitation but acceptable for our use case since:
 * 1. We're only scanning for specific extensions (.cs, .csproj) which naturally excludes most large folders
 * 2. The library is synchronous and fast enough for our build caching needs
 * 3. Future versions may add exclusion support per the package roadmap
 *
 * @param {string} projectPath - Path to the project directory
 * @param {string[]} extensions - File extensions to check (e.g., ['.cs', '.csproj'])
 * @returns {number} - Latest modification timestamp (synchronously returned)
 */
function getLatestFileTimestamp(projectPath, extensions = [".cs", ".csproj"]) {
    try {
        const result = recursiveLastModified(projectPath, { extensions })

        // Coerce to number and validate
        const timestamp = Number(result)
        if (!Number.isFinite(timestamp) || timestamp <= 0) {
            logger.warning(`Unexpected timestamp result from recursive-last-modified for ${projectPath}: ${result}`)
            return 0
        }

        return timestamp
    } catch (error) {
        logger.warning(`Could not scan directory ${projectPath}: ${error.message}`)
        return 0
    }
}

/**
 * Load build cache from disk
 * @returns {Object} - Cache object with project timestamps and build outputs
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
 * Check if a project needs to be built based on file timestamps
 * @param {string} projectPath - Path to the project directory
 * @param {string} projectName - Name of the project for caching
 * @returns {boolean} - True if build is needed
 */
function needsBuild(projectPath, projectName) {
    const cache = loadBuildCache()
    const latestFileTime = getLatestFileTimestamp(projectPath)
    const cacheEntry = cache[projectName]

    // Always rebuild if no cache entry exists
    if (!cacheEntry || typeof cacheEntry !== "object") {
        logger.info(`📝 Build needed for ${projectName}: no valid cache entry found`)
        return true
    }

    // Handle both old (timestamp) and new (object) cache formats
    const lastBuildTime = cacheEntry && typeof cacheEntry === "object" ? cacheEntry.timestamp : cacheEntry || 0

    // Check if files have been modified since last build
    // Use a 100ms buffer to account for filesystem timestamp granularity
    // (Windows NTFS and some network filesystems have ~100ms timestamp resolution)
    const TIMESTAMP_BUFFER = 100
    const needsRebuild = latestFileTime > lastBuildTime + TIMESTAMP_BUFFER

    if (needsRebuild) {
        logger.info(
            `📝 Build needed for ${projectName}: files modified since last build (latest: ${new Date(latestFileTime).toISOString()}, cached: ${new Date(lastBuildTime).toISOString()})`,
        )
    } else {
        logger.info(`✅ Skipping build for ${projectName}: no changes since last build`)
    }

    return needsRebuild
}

/**
 * Mark a project as successfully built and cache the build output
 * @param {string} projectName - Name of the project
 * @param {string} buildOutput - Build output to cache
 */
function markAsBuilt(projectName, buildOutput = "") {
    const cache = loadBuildCache()
    cache[projectName] = {
        timestamp: Date.now(),
        output: buildOutput,
    }
    saveBuildCache(cache)
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

        markAsBuilt(projectName, result)
        logger.success(`✅ Build completed for ${projectName}`)

        return { success: true, output: result, wasCached: false }
    } catch (error) {
        // For analyzer tools, we want to capture the output even if build fails
        // The stderr/stdout contains the analyzer warnings/errors we need
        const buildOutput = (error.stdout || "") + (error.stderr || "")

        // Store the build output even for failed builds so analyzers can process it
        markAsBuilt(projectName, buildOutput)

        logger.warning(`⚠️  Build completed with errors for ${projectName} (analyzer output captured)`)

        // Return success=true so analyzers can process the output
        // The analyzer will determine if the errors should block the commit
        return { success: true, output: buildOutput, wasCached: false }
    }
}

module.exports = {
    needsBuild,
    markAsBuilt,
    getCachedBuildOutput,
    clearBuildCache,
    buildIfNeeded,
    getLatestFileTimestamp,
}
