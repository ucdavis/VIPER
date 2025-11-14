#!/usr/bin/env node

// Build verification script for pre-commit hooks
// Runs quick compilation checks without producing output files

const { spawn } = require("node:child_process")
const path = require("node:path")
const { createLogger } = require("./lib/script-utils")
const logger = createLogger("Build Verify")

// Configuration
const TIMEOUT_MS = 60_000 // 1 minute timeout for builds

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
            env: { ...process.env, NODE_ENV: "development" },
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
            env: { ...process.env, NODE_ENV: "development" },
        })

        logger.success("Vue.js build passed ✓")
        return true
    } catch {
        logger.error("Vue.js build failed")
        return false
    }
}

async function verifyDotNetBuild() {
    logger.info("Checking .NET compilation...")

    try {
        await runCommand("dotnet", ["build", "./web/Viper.csproj", "--no-restore", "--nologo"], {
            env: { ...process.env, DOTNET_USE_COMPILER_SERVER: "1", DOTNET_CLI_FORCE_UTF8_ENCODING: "true" },
        })

        logger.success(".NET compilation passed ✓")
        return true
    } catch {
        logger.error(".NET compilation failed")
        return false
    }
}

async function verifyDotNetTests() {
    logger.info("Checking .NET test compilation...")

    try {
        await runCommand("dotnet", ["build", "./test/Viper.test.csproj", "--no-restore", "--nologo"], {
            env: { ...process.env, DOTNET_USE_COMPILER_SERVER: "1", DOTNET_CLI_FORCE_UTF8_ENCODING: "true" },
        })

        logger.success(".NET test compilation passed ✓")
        return true
    } catch {
        logger.error(".NET test compilation failed")
        return false
    }
}

async function main() {
    logger.info("Starting build verification...")

    // Run checks in parallel for speed
    const checks = await Promise.allSettled([
        verifyVueTypeScript(),
        verifyVueBuild(),
        verifyDotNetBuild(),
        verifyDotNetTests(),
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
async function runMain() {
    try {
        await main()
    } catch (error) {
        logger.error("Unexpected error during build verification:")
        console.error(error)
        process.exit(1)
    }
}

runMain()
