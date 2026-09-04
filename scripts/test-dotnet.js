#!/usr/bin/env node

import { execFileSync } from "node:child_process"
import path from "node:path"
import fs from "node:fs"
import { createLogger } from "./lib/script-utils.js"
import {
    needsBuild,
    markAsBuilt,
    wasBuildSuccessful,
    getCachedBuildOutput,
    filterBuildErrors,
} from "./lib/build-cache.js"

const { env } = process
const logger = createLogger("TEST")

// Uses --artifacts-path for full isolation from dev server (avoids file lock conflicts)
const MAX_BUILD_BUFFER = 20_971_520 // 20 MB for .NET build output
const artifactsPath = ".artifacts-precommit"
const buildPath = "test"
// Building test/ compiles web/ too, via the ProjectReference in Viper.test.csproj, so the
// cache has to hash both projects. Hashing test/ alone let a web-only change reuse a stale
// assembly and report a false pass. Mirrors the pair build-dotnet.js tracks.
const cachedProjects = [
    { dir: "web", name: "Viper.csproj" },
    { dir: "test", name: "Viper.test.csproj" },
]
const precommitDll = path.join(artifactsPath, "bin", "Viper.test", "debug", "Viper.test.dll")

/**
 * Check if precommit build exists (called from pre-commit hook)
 */
function precommitBuildExists() {
    return fs.existsSync(precommitDll)
}

/**
 * Build to .artifacts-precommit if needed (for standalone runs)
 * @returns {boolean} - Success status
 */
function ensureBuild() {
    // Use filter, not some, so every project logs its cache decision instead of short-circuiting
    const staleProjects = cachedProjects.filter(({ dir, name }) => needsBuild(dir, name))

    // Check if precommit build exists and cache says no rebuild needed
    if (precommitBuildExists() && staleProjects.length === 0) {
        // Check if cached build was successful
        const failed = cachedProjects.find(({ name }) => wasBuildSuccessful(name) === false)
        if (failed) {
            logger.error("Build failed (cached) - fix the error below and try again:")
            console.error(filterBuildErrors(getCachedBuildOutput(failed.name)))
            return false
        }
        logger.success("Using existing precommit build")
        return true
    }

    logger.info(`Building test project → ${artifactsPath}`)

    try {
        const result = execFileSync(
            "dotnet",
            [
                "build",
                `${buildPath}/`,
                "--artifacts-path",
                artifactsPath,
                "--verbosity",
                "quiet",
                "--nologo",
                "-p:WarningLevel=0",
            ],
            {
                encoding: "utf8",
                maxBuffer: MAX_BUILD_BUFFER,
                timeout: 120_000,
                stdio: ["inherit", "pipe", "pipe"],
                env: { ...env, DOTNET_USE_COMPILER_SERVER: "1" },
            },
        )

        for (const { dir, name } of cachedProjects) {
            markAsBuilt(dir, name, result, true)
        }
        logger.success("Build completed")
        return true
    } catch (error) {
        const output = (error.stdout || "") + (error.stderr || "")
        // Cache failure to avoid redundant rebuild attempts
        for (const { dir, name } of cachedProjects) {
            markAsBuilt(dir, name, output, false)
        }
        logger.error("Build failed!")
        console.error(output)
        return false
    }
}

/**
 * Build test app args from the CLI: bare patterns become a class filter
 * (npm run test:backend -- MyTestClass); if any arg starts with "-",
 * all args pass through to the test app verbatim
 * @returns {string[]}
 */
function getTestArgs() {
    const args = process.argv.slice(2)
    if (args.length === 0 || args.some((arg) => arg.startsWith("-"))) {
        return args
    }
    return args.flatMap((pattern) => ["--filter-class", `*${pattern}*`])
}

/**
 * Run dotnet test
 * @param {string[]} extraArgs - Additional args forwarded to `dotnet test`
 * @returns {boolean} - Success status
 */
function runTests(extraArgs) {
    logger.info(extraArgs.length > 0 ? `Running tests: ${extraArgs.join(" ")}` : "Running tests...")
    try {
        execFileSync("dotnet", ["test", "--test-modules", precommitDll, "--", ...extraArgs], {
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
    // To clear cache first, run: npm run clear-cache && npm run test:backend
    if (!ensureBuild()) {
        process.exit(1)
    }

    // Run tests
    const testSuccess = runTests(getTestArgs())
    process.exit(testSuccess ? 0 : 1)
}

// Handle errors
process.on("unhandledRejection", (error) => {
    logger.error(`Unhandled error: ${error.message}`)
    process.exit(1)
})

// Run
main()
