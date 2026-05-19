#!/usr/bin/env node

// Build .NET projects for precommit (shared by lint, test, verify)
// Uses --artifacts-path for full isolation from dev server (avoids file lock conflicts)
//
// Only builds test/ project - it references web/ so dotnet builds both.
// Caches both success and failure - if code hasn't changed, result won't change.

const { execFileSync } = require("node:child_process")
const { createLogger } = require("./lib/script-utils")
const {
    needsBuild,
    markAsBuilt,
    wasBuildSuccessful,
    getCachedBuildOutput,
    filterBuildErrors,
} = require("./lib/build-cache")

const { env } = process
const logger = createLogger("BUILD")

const artifactsPath = ".artifacts-precommit"

// Default execFileSync maxBuffer is 1 MB; full build output with all warnings can exceed
// that, which would surface as a false "build failed" instead of the real result.
const MAX_BUILD_OUTPUT = 10_485_760

// Check if either project needs rebuild
const webNeedsBuild = needsBuild("web", "Viper.csproj")
const testNeedsBuild = needsBuild("test", "Viper.test.csproj")

if (!webNeedsBuild && !testNeedsBuild) {
    // Hash unchanged - return cached result (check both projects)
    const webFailed = wasBuildSuccessful("Viper.csproj") === false
    const testFailed = wasBuildSuccessful("Viper.test.csproj") === false
    if (webFailed || testFailed) {
        logger.error("Build failed (cached) - fix the error(s) below and try again:")
        if (webFailed) {
            console.error(filterBuildErrors(getCachedBuildOutput("Viper.csproj")))
        }
        if (testFailed) {
            console.error(filterBuildErrors(getCachedBuildOutput("Viper.test.csproj")))
        }
        process.exit(1)
    }
    logger.success("Build skipped (cached)")
    process.exit(0)
}

// Build test project (which also builds web via ProjectReference)
logger.info(`Building test/ → ${artifactsPath} (includes web/)`)
try {
    const result = execFileSync(
        "dotnet",
        ["build", "test/", "--artifacts-path", artifactsPath, "--verbosity", "quiet", "--nologo"],
        {
            encoding: "utf8",
            timeout: 120_000,
            maxBuffer: MAX_BUILD_OUTPUT,
            stdio: ["inherit", "pipe", "pipe"],
            env: { ...env, DOTNET_USE_COMPILER_SERVER: "1" },
        },
    )

    // Cache success
    markAsBuilt("web", "Viper.csproj", result, true)
    markAsBuilt("test", "Viper.test.csproj", result, true)
    logger.success("Build complete")
} catch (error) {
    const output = (error.stdout || "") + (error.stderr || "")
    // Cache failure - no point rebuilding if code hasn't changed
    markAsBuilt("web", "Viper.csproj", output, false)
    markAsBuilt("test", "Viper.test.csproj", output, false)
    logger.error("Build failed")
    console.error(filterBuildErrors(output))
    process.exit(1)
}
