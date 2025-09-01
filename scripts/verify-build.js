#!/usr/bin/env node

// Build verification script for pre-commit hooks
// Runs quick compilation checks without producing output files

const { exec } = require("node:child_process")
const { promisify } = require("node:util")
const path = require("node:path")
const { createLogger } = require("./lib/script-utils")

const execAsync = promisify(exec)
const logger = createLogger("Build Verify")

// Configuration
const TIMEOUT_MS = 60_000 // 1 minute timeout for builds

async function verifyVueTypeScript() {
    logger.info("Checking Vue.js TypeScript compilation...")

    try {
        const vueAppDir = path.resolve(process.cwd(), "VueApp")
        const { stderr } = await execAsync("npx vue-tsc --noEmit --skipLibCheck", {
            cwd: vueAppDir,
            timeout: TIMEOUT_MS,
            env: { ...process.env, NODE_ENV: "development" },
        })

        if (stderr && !stderr.includes("WARNING")) {
            logger.warning("TypeScript warnings:")
            console.log(stderr)
        }

        logger.success("Vue.js TypeScript compilation passed ✓")
        return true
    } catch (error) {
        logger.error("Vue.js TypeScript compilation failed")
        console.error(error.stdout || error.message)
        if (error.stderr) {
            console.error(error.stderr)
        }
        return false
    }
}

async function verifyVueBuild() {
    logger.info("Checking Vue.js build...")

    try {
        const vueAppDir = path.resolve(process.cwd(), "VueApp")
        const { stderr } = await execAsync("npm run build-only-dev", {
            cwd: vueAppDir,
            timeout: TIMEOUT_MS,
            env: { ...process.env, NODE_ENV: "development" },
        })

        if (stderr && !stderr.includes("WARNING")) {
            logger.warning("Vue.js build warnings:")
            console.log(stderr)
        }

        logger.success("Vue.js build passed ✓")
        return true
    } catch (error) {
        logger.error("Vue.js build failed")
        console.error(error.stdout || error.message)
        if (error.stderr) {
            console.error(error.stderr)
        }
        return false
    }
}

async function verifyDotNetBuild() {
    logger.info("Checking .NET compilation...")

    try {
        const { stderr } = await execAsync("dotnet build ./web/Viper.csproj --no-restore --verbosity quiet --nologo", {
            timeout: TIMEOUT_MS,
            env: { ...process.env, DOTNET_USE_COMPILER_SERVER: "1" },
        })

        if (stderr) {
            logger.warning(".NET build warnings:")
            console.log(stderr)
        }

        logger.success(".NET compilation passed ✓")
        return true
    } catch (error) {
        logger.error(".NET compilation failed")
        console.error(error.stdout || error.message)
        if (error.stderr) {
            console.error(error.stderr)
        }
        return false
    }
}

async function main() {
    logger.info("Starting build verification...")

    // Run checks in parallel for speed
    const checks = await Promise.allSettled([verifyVueTypeScript(), verifyVueBuild(), verifyDotNetBuild()])

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
