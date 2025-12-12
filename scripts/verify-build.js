#!/usr/bin/env node

// Build verification script for pre-commit hooks
// Runs compilation checks with caching to avoid redundant builds

const { spawn } = require("node:child_process")
const path = require("node:path")
const { createLogger } = require("./lib/script-utils")
const {
    needsBuild,
    markAsBuilt,
    wasBuildSuccessful,
    getCachedBuildOutput,
    filterBuildErrors,
} = require("./lib/build-cache")

const logger = createLogger("Build Verify")

// Configuration
const TIMEOUT_MS = 60_000 // 1 minute timeout for builds
const { env } = process

// Helper function to run commands with color output preserved
function runCommand(command, args, options = {}) {
    return new Promise((resolve, reject) => {
        const child = spawn(command, args, {
            stdio: "inherit", // This preserves colors by inheriting parent's stdio
            shell: true,
            ...options,
        })

        let timeout = null
        if (TIMEOUT_MS) {
            timeout = setTimeout(() => {
                child.kill("SIGTERM")
                reject(new Error(`Command timed out after ${TIMEOUT_MS}ms`))
            }, TIMEOUT_MS)
        }

        child.on("exit", (code) => {
            clearTimeout(timeout)
            if (code === 0) {
                resolve()
            } else {
                reject(new Error(`Command failed with exit code ${code}`))
            }
        })

        child.on("error", (error) => {
            clearTimeout(timeout)
            reject(error)
        })
    })
}

async function verifyVueTypeScript() {
    logger.info("Checking Vue.js TypeScript compilation...")

    try {
        const vueAppDir = path.resolve(process.cwd(), "VueApp")
        await runCommand("npx", ["vue-tsc", "--build", "--force"], {
            cwd: vueAppDir,
            env: { ...env, NODE_ENV: "development" },
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
        await runCommand("npm", ["run", "build-only-dev"], {
            cwd: vueAppDir,
            env: { ...env, NODE_ENV: "development" },
        })

        logger.success("Vue.js build passed ✓")
        return true
    } catch {
        logger.error("Vue.js build failed")
        return false
    }
}

async function verifyDotNetBuild() {
    // Check cache - if build-dotnet.js already built both projects, skip
    // Building test/ also builds web/ (via ProjectReference)
    const webCached = !needsBuild("web", "Viper.csproj")
    const testCached = !needsBuild("test", "Viper.test.csproj")

    if (webCached && testCached) {
        // Check if cached build was successful
        if (wasBuildSuccessful("Viper.csproj") === false) {
            logger.error(".NET compilation failed (cached) - fix the error below:")
            console.error(filterBuildErrors(getCachedBuildOutput("Viper.csproj")))
            return false
        }
        logger.success(".NET compilation passed ✓ (cached)")
        return true
    }

    logger.info("Checking .NET compilation...")

    try {
        // Build test project (includes web via ProjectReference)
        await runCommand(
            "dotnet",
            [
                "build",
                "./test/Viper.test.csproj",
                "-o",
                "test/bin/Precommit",
                "--no-restore",
                "--nologo",
                "--verbosity",
                "quiet",
            ],
            {
                env: { ...env, DOTNET_USE_COMPILER_SERVER: "1", DOTNET_CLI_FORCE_UTF8_ENCODING: "true" },
            },
        )

        // Cache success
        markAsBuilt("web", "Viper.csproj", "", true)
        markAsBuilt("test", "Viper.test.csproj", "", true)
        logger.success(".NET compilation passed ✓")
        return true
    } catch {
        // Cache failure
        markAsBuilt("web", "Viper.csproj", "", false)
        markAsBuilt("test", "Viper.test.csproj", "", false)
        logger.error(".NET compilation failed")
        return false
    }
}

async function main() {
    logger.info("Starting build verification...")

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
