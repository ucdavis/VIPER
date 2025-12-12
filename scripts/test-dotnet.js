#!/usr/bin/env node

const { execFileSync } = require("node:child_process")
const path = require("node:path")
const fs = require("node:fs")
const { createLogger } = require("./lib/script-utils")
const { needsBuild, markAsBuilt } = require("./lib/build-cache")

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

        markAsBuilt(projectPath, projectName, result)
        logger.success("Build completed")
        return true
    } catch (error) {
        logger.error("Build failed!")
        console.error((error.stdout || "") + (error.stderr || ""))
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
        execFileSync("dotnet", ["test", precommitDll, "--no-build", "--verbosity=normal", "--nologo"], {
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
