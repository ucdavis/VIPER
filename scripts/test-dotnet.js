#!/usr/bin/env node

const { execFileSync } = require("node:child_process")
const path = require("node:path")
const fs = require("node:fs")
const { createLogger } = require("./lib/script-utils")
const {
    needsBuild,
    markAsBuilt,
    wasBuildSuccessful,
    getCachedBuildOutput,
    filterBuildErrors,
    shouldClearCache,
    clearBuildCache,
} = require("./lib/build-cache")

const { env } = process
const logger = createLogger("TEST")

const projectPath = "test"
const projectName = "Viper.test.csproj"
const precommitBinPath = path.join(projectPath, "bin", "Precommit")
const precommitDll = path.join(precommitBinPath, "Viper.test.dll")

/**
 * Check if precommit build exists (called from pre-commit hook)
 */
function precommitBuildExists() {
    return fs.existsSync(precommitDll)
}

/**
 * Build to bin/Precommit if needed (for standalone runs)
 * @returns {boolean} - Success status
 */
function ensureBuild() {
    // Check if precommit build exists and cache says no rebuild needed
    if (precommitBuildExists() && !needsBuild(projectPath, projectName)) {
        // Check if cached build was successful
        if (wasBuildSuccessful(projectName) === false) {
            logger.error("Build failed (cached) - fix the error below and try again:")
            console.error(filterBuildErrors(getCachedBuildOutput(projectName)))
            return false
        }
        logger.success("Using existing precommit build")
        return true
    }

    logger.info(`Building test project → ${precommitBinPath}`)

    try {
        const result = execFileSync(
            "dotnet",
            [
                "build",
                `${projectPath}/`,
                "-o",
                precommitBinPath,
                "--verbosity",
                "quiet",
                "--nologo",
                "/p:WarningLevel=0",
            ],
            {
                encoding: "utf8",
                timeout: 120_000,
                stdio: ["inherit", "pipe", "pipe"],
                env: { ...env, DOTNET_USE_COMPILER_SERVER: "1" },
            },
        )

        markAsBuilt(projectPath, projectName, result, true)
        logger.success("Build completed")
        return true
    } catch (error) {
        const output = (error.stdout || "") + (error.stderr || "")
        // Cache failure to avoid redundant rebuild attempts
        markAsBuilt(projectPath, projectName, output, false)
        logger.error("Build failed!")
        console.error(output)
        return false
    }
}

/**
 * Run dotnet test
 * @returns {boolean} - Success status
 */
function runTests() {
    logger.info("Running tests...")
    try {
        execFileSync("dotnet", ["test", precommitDll, "--verbosity=normal", "--nologo"], {
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
 * Clear caches and build artifacts when --clear-cache is passed
 */
function clearCacheIfRequested() {
    if (shouldClearCache()) {
        clearBuildCache()
        // Also delete precommit build directory to force fresh build
        if (fs.existsSync(precommitBinPath)) {
            fs.rmSync(precommitBinPath, { recursive: true, force: true })
        }
        // Delete main project bin/obj to ensure test project gets fresh dependencies
        const webBin = path.join("web", "bin")
        const webObj = path.join("web", "obj")
        const testObj = path.join("test", "obj")
        for (const dir of [webBin, webObj, testObj]) {
            if (fs.existsSync(dir)) {
                fs.rmSync(dir, { recursive: true, force: true })
            }
        }
        logger.info("🧹 Cleared test build artifacts")
    }
}

/**
 * Main execution
 */
function main() {
    // Handle --clear-cache flag
    clearCacheIfRequested()

    // Ensure build exists (either from precommit or build now)
    if (!ensureBuild()) {
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
