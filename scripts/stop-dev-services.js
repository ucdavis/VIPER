#!/usr/bin/env node

// A catch-all script in case there are zombie processes running.
// Cross-platform support via shared utilities.

import { pathToFileURL } from "node:url"
import {
    killProcess,
    killProcessOnPort,
    getDevServerEnv,
    createLogger,
    execAsync,
    DEFAULT_ENV_VARS,
} from "./lib/script-utils.js"

// Create logger with prefix
const logger = createLogger("Dev Stop")
const { error: logError, success: logSuccess, info: logInfo, warning: logWarning } = logger

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

// Find the PID of a process listening on a given port (Windows only)
async function findPidOnPort(port) {
    if (process.platform !== "win32") {
        return null
    }
    const safePort = Number.parseInt(port, 10)
    if (Number.isNaN(safePort) || safePort < 1 || safePort > 65535) {
        return null
    }
    try {
        const { stdout } = await execAsync(`netstat -aon | findstr ":${safePort}" | findstr "LISTENING"`)
        if (!stdout) {
            return null
        }
        const lines = stdout.trim().split(/\r?\n/).filter(Boolean)
        const pids = lines.map((line) => line.trim().split(/\s+/).pop()).filter((pid) => pid && pid !== "0")
        return pids.length > 0 ? pids[0] : null
    } catch {
        return null
    }
}

// Query Windows processes with a WQL filter. Throws if the query itself fails, so
// callers can tell that apart from an empty result.
async function queryProcesses(filter) {
    const { stdout } = await execAsync(
        `powershell -NoProfile -NonInteractive -Command "ConvertTo-Json -Compress -InputObject @(Get-CimInstance Win32_Process -Filter \\"${filter}\\" | Select-Object ProcessId,ParentProcessId,Name)"`,
    )
    return JSON.parse(stdout)
}

// Walk up from the server to the top of its dotnet chain. The chain is
// dotnet watch > dotnet-watch.dll > dotnet run > server, and killing anything
// below the top leaves dotnet watch alive to restart the server.
async function findTopDotnetAncestor(pid) {
    if (process.platform !== "win32") {
        return null
    }
    try {
        const processes = await queryProcesses("Name='dotnet.exe' OR Name='Viper.exe'")
        const processesByPid = new Map(processes.map((entry) => [entry.ProcessId, entry]))
        const maxChainDepth = 5
        let top = null
        let current = processesByPid.get(Number(pid))
        for (let depth = 0; depth < maxChainDepth; depth += 1) {
            const parent = processesByPid.get(current?.ParentProcessId)
            if (parent?.Name !== "dotnet.exe") {
                break
            }
            top = String(parent.ProcessId)
            current = parent
        }
        return top
    } catch {
        return null
    }
}

// An idle dotnet watch has no server to show which checkout owns it, so it can't
// be stopped safely when other instances may be running. Report it instead.
async function warnAboutRemainingDotnetWatch() {
    if (process.platform !== "win32") {
        return
    }
    try {
        const watchers = await queryProcesses("Name='dotnet.exe' AND CommandLine LIKE 'dotnet watch%'")
        for (const { ProcessId: pid } of watchers) {
            logWarning(`dotnet watch (PID ${pid}) is still running, possibly for another checkout.`)
            logWarning(`If it belongs to this one, stop it with: taskkill /F /T /PID ${pid}`)
        }
    } catch (error) {
        logWarning(`Could not check for remaining dotnet watch processes: ${error.message}`)
    }
}

// Find and kill orphaned dotnet watch processes whose parent session is dead
async function killOrphanedDotnetWatch() {
    if (process.platform !== "win32") {
        return 0
    }
    try {
        const watchers = await queryProcesses("Name='dotnet.exe' AND CommandLine LIKE '%watch%'")
        const results = await Promise.all(
            watchers.map(async ({ ProcessId: pid, ParentProcessId: parentPid }) => {
                // If the parent is dead, this dotnet watch is orphaned
                // A failed query throws, so a watcher is only killed when its parent is really gone
                const parents = await queryProcesses(`ProcessId=${parentPid}`)
                if (parents.length === 0) {
                    const wasKilled = await killProcess(String(pid))
                    if (wasKilled) {
                        logSuccess(`Orphaned dotnet watch: Stopped process tree (PID ${pid})`)
                        return true
                    }
                }
                return false
            }),
        )
        return results.filter(Boolean).length
    } catch {
        return 0
    }
}

// Stop .NET processes on ASPNETCORE ports, including parent dotnet watch
async function stopDotnetProcesses(envVars) {
    const dotnetPorts = [envVars.ASPNETCORE_HTTPS_PORT].filter((port) => port && !Number.isNaN(port))

    const stopPromises = dotnetPorts.map(async (port) => {
        // Find the process on the port so we can also kill its parent (dotnet watch)
        const pid = await findPidOnPort(port)
        if (pid) {
            const topPid = await findTopDotnetAncestor(pid)
            if (topPid) {
                // Kill the top of the chain: killProcess uses /T to kill the entire tree
                const killed = await killProcess(topPid)
                if (killed) {
                    logSuccess(
                        `.NET development server: Stopped dotnet watch (PID ${topPid}) and its process tree on port ${port}`,
                    )
                    return true
                }
            }
        }
        // Fallback: kill just the port-bound process
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
        logInfo(".NET development server: Not running on port")
    }

    // Sweep for orphaned dotnet watch processes (parent session died but watch survived)
    const orphansKilled = await killOrphanedDotnetWatch()

    await warnAboutRemainingDotnetWatch()

    return stopped + orphansKilled
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

// Check for a VIPER server or dotnet watch, from dev or dev:build, in any checkout (Windows only)
async function isAnyViperInstanceRunning() {
    if (process.platform !== "win32") {
        return false
    }
    const instances = await queryProcesses(
        "Name='Viper.exe' OR (Name='dotnet.exe' AND (CommandLine LIKE 'dotnet watch%' OR CommandLine LIKE '%Viper.dll%'))",
    )
    return instances.length > 0
}

// Mailpit is shared by every instance on the machine, so only the last one stops it
async function stopSharedMailpit(envVars) {
    try {
        if (await isAnyViperInstanceRunning()) {
            logInfo("Mailpit: Left running for another VIPER instance")
            return 0
        }
    } catch (error) {
        logWarning(`Mailpit: Left running because other instances could not be checked: ${error.message}`)
        return 0
    }

    await stopMailpit()

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
    return mailpitResults.filter(Boolean).length
}

// Main stop function
async function stopDevServices() {
    logInfo("Stopping development services...")

    // Load environment variables
    const envVars = getDevServerEnv()

    // Check if any env vars differ from defaults (via .env.local or shell overrides)
    const hasCustomValues = Object.entries(envVars).some(([key, value]) => DEFAULT_ENV_VARS[key] !== value)

    if (hasCustomValues) {
        logInfo(`Using non-default environment variables`)
    } else {
        logInfo(`Using default ports`)
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
        // Stop .NET processes (port-based for safety)
        const dotnetStopped = await stopDotnetProcesses(envVars)

        // Stop Vite dev server
        const viteStopped = await stopViteDevServer(envVars)

        // Runs after .NET stops, so a server still running belongs to another instance
        const mailpitPortsStopped = await stopSharedMailpit(envVars)

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

// Run the script (ESM equivalent of require.main === module)
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    void (async () => {
        try {
            await stopDevServices()
        } catch (error) {
            logError(`Unexpected error: ${error.message}`)
            process.exit(1)
        }
    })()
}

export { stopDevServices }
