#!/usr/bin/env node

// A catch-all script in case there are zombie processes running.
// Cross-platform support via shared utilities.

const { killProcess, killProcessOnPort, getDevServerEnv, createLogger } = require("./lib/script-utils")

// Create logger with prefix
const logger = createLogger("Dev Stop")
const { error: logError, success: logSuccess, info: logInfo } = logger

// Stop Mailpit specifically (cross-platform)
async function stopMailpit() {
    const stopped = await killProcess("mailpit")
    if (stopped) {
        logSuccess("Mailpit: Stopped")
    } else {
        logInfo("Mailpit: Not running")
    }
    return stopped
}

// Stop .NET processes on ASPNETCORE ports (safer than killing by name)
async function stopDotnetProcesses(envVars) {
    const dotnetPorts = [envVars.ASPNETCORE_HTTPS_PORT].filter((port) => port && !Number.isNaN(port))

    const stopPromises = dotnetPorts.map(async (port) => {
        const portStopped = await killProcessOnPort(port)
        if (portStopped) {
            logSuccess(`.NET development server: Stopped process on port ${port}`)
            return true
        }
        return false
    })
    const results = await Promise.all(stopPromises)
    const stopped = results.filter(Boolean).length

    if (stopped === 0) {
        logInfo(".NET development server: Not running")
    }

    return stopped
}

// Check and stop Vite dev server processes
async function stopViteDevServer(envVars) {
    const vitePorts = [envVars.VITE_PORT, envVars.VITE_HMR_PORT].filter((port) => port && !Number.isNaN(port))

    if (vitePorts.length === 0) {
        logInfo("Vite dev server: No Vite ports configured")
        return 0
    }

    const stopPromises = vitePorts.map(async (port) => {
        const portStopped = await killProcessOnPort(port)
        if (portStopped) {
            logSuccess(`Vite dev server: Stopped process on port ${port}`)
            return true
        }
        return false
    })
    const results = await Promise.all(stopPromises)
    const stopped = results.filter(Boolean).length

    if (stopped === 0) {
        logInfo("Vite dev server: Not running")
    }

    return stopped
}

// Main stop function
async function stopDevServices() {
    logInfo("Stopping development services...")

    // Load environment variables
    const envVars = getDevServerEnv()

    // Check if .env.local was loaded
    const hasCustomValues = Object.entries(envVars).some(
        ([key, value]) => process.env[key] && Number.parseInt(process.env[key], 10) !== value,
    )

    if (hasCustomValues) {
        logInfo(`Loaded environment variables from .env.local`)
    } else {
        logInfo(`Using default ports (no .env.local found)`)
    }

    // Show which ports we'll be checking
    const allPorts = [
        envVars.VITE_PORT,
        envVars.VITE_HMR_PORT,
        envVars.ASPNETCORE_HTTPS_PORT,
        envVars.MAILPIT_SMTP_PORT,
        envVars.MAILPIT_WEB_PORT,
    ].filter((port) => port && !Number.isNaN(port))

    logInfo(`Checking ports: ${allPorts.join(", ")}`)

    try {
        // Stop Mailpit first
        await stopMailpit()

        // Stop .NET processes (port-based for safety)
        const dotnetStopped = await stopDotnetProcesses(envVars)

        // Stop Vite dev server
        const viteStopped = await stopViteDevServer(envVars)

        // Stop any remaining processes on Mailpit ports (in case Mailpit wasn't caught by name)
        const mailpitPorts = [envVars.MAILPIT_SMTP_PORT, envVars.MAILPIT_WEB_PORT]
        const mailpitStopPromises = mailpitPorts.map(async (port) => {
            const portStopped = await killProcessOnPort(port)
            if (portStopped) {
                logSuccess(`Stopped remaining process on Mailpit port ${port}`)
                return true
            }
            return false
        })
        const mailpitResults = await Promise.all(mailpitStopPromises)
        const mailpitPortsStopped = mailpitResults.filter(Boolean).length

        // Summary
        const totalStopped = dotnetStopped + viteStopped + mailpitPortsStopped
        if (totalStopped > 0) {
            logSuccess(`✨ Summary: ${totalStopped} development service(s) stopped`)
        } else {
            logInfo("✨ Summary: All development services were already stopped")
        }
    } catch (error) {
        logError(`Error stopping services: ${error.message}`)
        process.exit(1)
    }
}

// Handle process termination
process.on("SIGINT", () => {
    logInfo("Received SIGINT, exiting...")
    process.exit(0)
})

process.on("SIGTERM", () => {
    logInfo("Received SIGTERM, exiting...")
    process.exit(0)
})

// Run the script
if (require.main === module) {
    ;(async () => {
        try {
            await stopDevServices()
        } catch (error) {
            logError(`Unexpected error: ${error.message}`)
            process.exit(1)
        }
    })()
}

module.exports = { stopDevServices }
