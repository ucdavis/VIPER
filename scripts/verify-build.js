#!/usr/bin/env node

// Build verification script for pre-commit hooks
// Runs compilation checks with caching to avoid redundant builds

import { spawn } from "node:child_process"
import path from "node:path"
import { createLogger } from "./lib/script-utils.js"
import {
    needsBuild,
    markAsBuilt,
    wasBuildSuccessful,
    getCachedBuildOutput,
    filterBuildErrors,
    isConfirmedWarningsOnly,
    createSummaryDetailFilter,
    countBuildWarnings,
    clearCacheIfRequested,
} from "./lib/build-cache.js"

const logger = createLogger("Build Verify")

// Clear cache if --clear-cache flag is passed
clearCacheIfRequested()

const { env } = process

// Helper function to run commands with color output preserved
function runCommand(command, args, options = {}) {
    return new Promise((resolve, reject) => {
        const fullCommand = args.length > 0 ? `${command} ${args.join(" ")}` : command
        const child = spawn(fullCommand, {
            stdio: ["ignore", "inherit", "inherit"], // Ignore stdin, inherit stdout/stderr for colors
            shell: true,
            ...options,
        })

        child.on("exit", (code) => {
            if (code === 0) {
                resolve()
            } else {
                reject(new Error(`Command failed with exit code ${code}`))
            }
        })

        child.on("error", (error) => {
            reject(error)
        })
    })
}

// Helper to create error with output property
function createErrorWithOutput(message, output) {
    const error = new Error(message)
    error.output = output
    return error
}

// Buffers the trailing partial line so a filtering decision is never split across two chunks.
function createFilteredWriter(stream, shouldEmit) {
    let pending = ""
    return {
        write(text) {
            pending += text
            const lines = pending.split("\n")
            pending = lines.pop() ?? ""
            for (const line of lines) {
                if (shouldEmit(line)) {
                    stream.write(`${line}\n`)
                }
            }
        },
        flush() {
            if (pending && shouldEmit(pending)) {
                stream.write(pending)
            }
            pending = ""
        },
    }
}

// options.lineFilter builds a per-stream predicate applied to displayed output only; the captured
// output stays complete so caching and error filtering still see everything.
function runCommandWithOutput(command, args, options = {}) {
    const { lineFilter, ...spawnOptions } = options
    return new Promise((resolve, reject) => {
        let stdout = ""
        let stderr = ""

        const fullCommand = args.length > 0 ? `${command} ${args.join(" ")}` : command
        const child = spawn(fullCommand, {
            shell: true,
            ...spawnOptions,
        })

        const passThrough = () => true
        const outWriter = createFilteredWriter(process.stdout, lineFilter ? lineFilter() : passThrough)
        const errWriter = createFilteredWriter(process.stderr, lineFilter ? lineFilter() : passThrough)

        child.stdout.on("data", (data) => {
            stdout += data.toString()
            outWriter.write(data.toString())
        })

        child.stderr.on("data", (data) => {
            stderr += data.toString()
            errWriter.write(data.toString())
        })

        child.on("exit", (code) => {
            outWriter.flush()
            errWriter.flush()
            const output = stdout + stderr
            if (code === 0) {
                resolve(output)
            } else {
                reject(createErrorWithOutput(`Command failed with exit code ${code}`, output))
            }
        })

        child.on("error", (err) => {
            outWriter.flush()
            errWriter.flush()
            reject(createErrorWithOutput(err.message, stdout + stderr))
        })
    })
}

async function verifyVueTypeScript() {
    logger.info("Checking Vue.js TypeScript compilation...")

    try {
        const vueAppDir = path.resolve(process.cwd(), "VueApp")
        await runCommand("npx", ["vue-tsc", "--build", "--force"], {
            cwd: vueAppDir,
            env: { ...env, NODE_ENV: "production" },
        })

        logger.success("Vue.js TypeScript compilation passed ✓")
        return true
    } catch {
        logger.error("Vue.js TypeScript compilation failed")
        return false
    }
}

async function verifyVueBuild() {
    logger.info("Checking Vue.js build...")

    try {
        const vueAppDir = path.resolve(process.cwd(), "VueApp")
        await runCommand("npm", ["run", "build-only"], {
            cwd: vueAppDir,
            env: { ...env, NODE_ENV: "production" },
        })

        logger.success("Vue.js build passed ✓")
        return true
    } catch {
        logger.error("Vue.js build failed")
        return false
    }
}

// Check if .NET has a cached failure (used to fail fast before running Vue builds)
function checkDotNetCacheFailure() {
    const webCached = !needsBuild("web", "Viper.csproj")
    const testCached = !needsBuild("test", "Viper.test.csproj")

    if (webCached && testCached) {
        const webMarkedFailed = wasBuildSuccessful("Viper.csproj") === false
        const testMarkedFailed = wasBuildSuccessful("Viper.test.csproj") === false

        if (webMarkedFailed || testMarkedFailed) {
            return {
                hasCachedFailure: true,
                webFailed: webMarkedFailed,
                testFailed: testMarkedFailed,
            }
        }
    }
    return { hasCachedFailure: false, webFailed: false, testFailed: false }
}

// Show cached .NET build errors
function showCachedDotNetErrors(webFailed, testFailed) {
    logger.error(".NET compilation failed (cached) - fix the error(s) below:")
    const webOutput = webFailed ? filterBuildErrors(getCachedBuildOutput("Viper.csproj")) : ""
    const testOutput = testFailed ? filterBuildErrors(getCachedBuildOutput("Viper.test.csproj")) : ""
    if (webOutput) {
        console.error(`\n${webOutput}`)
    }
    if (testOutput && testOutput !== webOutput) {
        console.error(`\n${testOutput}`)
    }
}

async function verifyDotNetBuild() {
    logger.info("Checking .NET compilation...")

    // Check cache - if build-dotnet.js already built both projects, skip
    // Building test/ also builds web/ (via ProjectReference)
    const webCached = !needsBuild("web", "Viper.csproj")
    const testCached = !needsBuild("test", "Viper.test.csproj")

    if (webCached && testCached) {
        // Check if cached build was successful (check both projects)
        const { hasCachedFailure, webFailed, testFailed } = checkDotNetCacheFailure()
        if (hasCachedFailure) {
            showCachedDotNetErrors(webFailed, testFailed)
            return false
        }
        // A cached run prints no build output, so surface the warning count the cache already holds
        const warningCount = countBuildWarnings(
            getCachedBuildOutput("Viper.test.csproj") ?? getCachedBuildOutput("Viper.csproj"),
        )
        logger.success(
            warningCount > 0
                ? `.NET compilation passed ✓ (cached, ${warningCount} warning(s))`
                : ".NET compilation passed ✓ (cached)",
        )
        return true
    }

    try {
        // Build test project (includes web via ProjectReference)
        // Use --artifacts-path to fully isolate build artifacts from dev server
        const output = await runCommandWithOutput(
            "dotnet",
            [
                "build",
                "./test/Viper.test.csproj",
                "--artifacts-path",
                ".artifacts-precommit",
                "--nologo",
                "--verbosity",
                "quiet",
            ],
            {
                env: { ...env, DOTNET_USE_COMPILER_SERVER: "1", DOTNET_CLI_FORCE_UTF8_ENCODING: "true" },
                lineFilter: createSummaryDetailFilter,
            },
        )

        // Cache success with output
        markAsBuilt("web", "Viper.csproj", output, true)
        markAsBuilt("test", "Viper.test.csproj", output, true)
        logger.success(".NET compilation passed ✓")
        return true
    } catch (error) {
        // Capture build output
        let output = error.output || ""
        if (!output.trim()) {
            output = error.message || "Build failed with unknown error"
        }

        // Fail by default on non-zero exit code.
        // Only treat as success if we can positively confirm it's warnings-only
        // (i.e., output contains "0 Error(s)" or "Build succeeded.")
        const isWarningsOnly = isConfirmedWarningsOnly(output)

        if (isWarningsOnly) {
            // Confirmed warnings-only - treat as success
            markAsBuilt("web", "Viper.csproj", output, true)
            markAsBuilt("test", "Viper.test.csproj", output, true)
            logger.success(".NET compilation passed ✓ (warnings present)")
            return true
        }

        // Could not confirm success - treat as failure
        markAsBuilt("web", "Viper.csproj", output, false)
        markAsBuilt("test", "Viper.test.csproj", output, false)
        logger.error(".NET compilation failed")
        return false
    }
}

async function main() {
    logger.info("Starting build verification...")

    // Check for cached .NET failure first - fail fast without running Vue builds
    const { hasCachedFailure, webFailed, testFailed } = checkDotNetCacheFailure()
    if (hasCachedFailure) {
        logger.info("Checking .NET compilation...")
        showCachedDotNetErrors(webFailed, testFailed)
        logger.error("Build verification failed! ❌")
        logger.plain("")
        logger.plain("Please fix the .NET compilation errors above before committing.")
        logger.plain("You can re-run this verification with: npm run verify:build")
        process.exit(1)
    }

    // Run checks in parallel (.NET uses cache - skips if already built by build-dotnet.js)
    const checks = await Promise.allSettled([
        verifyVueTypeScript(),
        verifyVueBuild(),
        verifyDotNetBuild(), // Builds test/ which includes web/ via ProjectReference
    ])

    const results = checks.map((result) => (result.status === "fulfilled" ? result.value : false))

    const allPassed = results.every((result) => result === true)

    if (allPassed) {
        logger.success("All build verifications passed! ✅")
        process.exit(0)
    } else {
        logger.error("Build verification failed! ❌")
        logger.plain("")
        logger.plain("Please fix the compilation errors above before committing.")
        logger.plain("You can re-run this verification with: npm run verify:build")
        process.exit(1)
    }
}

// Handle process termination gracefully
process.on("SIGINT", () => {
    logger.warning("Build verification interrupted by user")
    process.exit(1)
})

process.on("SIGTERM", () => {
    logger.warning("Build verification terminated")
    process.exit(1)
})

// Run the verification
void (async () => {
    try {
        await main()
    } catch (error) {
        logger.error("Unexpected error during build verification:")
        console.error(error)
        process.exit(1)
    }
})()
