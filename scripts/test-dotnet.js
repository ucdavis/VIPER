#!/usr/bin/env node

const { execFileSync } = require("node:child_process")
const path = require("node:path")
const fs = require("node:fs")
const { createLogger } = require("./lib/script-utils")
const { needsBuild, markAsBuilt } = require("./lib/build-cache")

const logger = createLogger("TEST")

const projectPath = "test"
const projectName = "Viper.test.csproj"
const testBinPath = path.join(projectPath, "bin")

/**
 * Stop dev services before running tests
 */
function stopDevServices() {
    try {
        logger.info("Stopping dev services...")
        execFileSync("npm", ["run", "dev:stop"], {
            encoding: "utf8",
            stdio: "inherit",
            timeout: 30_000,
        })
    } catch {
        // dev:stop may fail if services aren't running, that's okay
        logger.info("Dev services stopped (or were not running)")
    }
}

/**
 * Check if build output exists
 */
function buildOutputExists() {
    try {
        return fs.existsSync(testBinPath) && fs.readdirSync(testBinPath).length > 0
    } catch {
        return false
    }
}

/**
 * Run dotnet build for the test project
 * @returns {boolean} - Success status
 */
function runBuild() {
    const buildArgs = ["build", "--verbosity", "quiet", "--nologo", "/p:WarningLevel=0"]

    try {
        logger.info("Building test project...")
        const result = execFileSync("dotnet", buildArgs, {
            cwd: projectPath,
            encoding: "utf8",
            timeout: 120_000, // 2 minute timeout
            stdio: ["inherit", "pipe", "pipe"],
        })

        // Cache successful build
        try {
            markAsBuilt(projectPath, projectName, result)
            logger.success("Build completed and cached")
        } catch (error) {
            logger.warning(`Failed to cache build result: ${error.message}`)
        }

        return true
    } catch (error) {
        // Capture build output even on failure
        const output = (error.stdout || "") + (error.stderr || "")

        // Still cache the output for debugging
        try {
            markAsBuilt(projectPath, projectName, output)
        } catch (error) {
            logger.warning(`Failed to cache build output: ${error.message}`)
        }

        logger.error("Build failed!")
        if (output) {
            console.error(output)
        }
        return false
    }
}

/**
 * Run dotnet test
 * @returns {boolean} - Success status
 */
function runTests() {
    const testArgs = ["test", "--no-build", "--verbosity=normal", "--nologo"]

    try {
        logger.info("Running tests...")
        execFileSync("dotnet", testArgs, {
            cwd: projectPath,
            encoding: "utf8",
            timeout: 300_000, // 5 minute timeout for tests
            stdio: "inherit",
        })

        logger.success("All tests passed!")
        return true
    } catch {
        logger.error("Tests failed!")
        return false
    }
}

/**
 * Main execution
 */
function main() {
    // Stop dev services
    stopDevServices()

    // Check if build is needed
    try {
        if (needsBuild(projectPath, projectName)) {
            // Build is needed, continue to build step below
        } else if (buildOutputExists()) {
            // Build not needed and output exists, skip to tests
            logger.success("Build not needed (using cached results)")
            const testSuccess = runTests()
            process.exit(testSuccess ? 0 : 1)
        } else {
            // Cache says no build needed, but output is missing
            logger.warning("Cache indicates no build needed, but build output missing. Rebuilding...")
        }
    } catch (error) {
        // If cache check fails, proceed with fresh build
        logger.info(`Cache check failed, proceeding with fresh build: ${error.message}`)
    }

    // Run build
    const buildSuccess = runBuild()
    if (!buildSuccess) {
        process.exit(1)
    }

    // Run tests
    const testSuccess = runTests()
    process.exit(testSuccess ? 0 : 1)
}

// Handle errors
process.on("unhandledRejection", (error) => {
    logger.error(`Unhandled error: ${error.message}`)
    process.exit(1)
})

// Run
main()
