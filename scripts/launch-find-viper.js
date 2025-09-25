const { spawnSync } = require("node:child_process")
const { getDevServerEnv } = require("./lib/script-utils")

// Cross-platform script for finding Viper.exe process by port

// Get the HTTPS port from environment or use default
function getHttpsPort() {
    const env = getDevServerEnv()
    const port = env.ASPNETCORE_HTTPS_PORT
    return port.toString()
}

// Helper function to check if a PID belongs to Viper.exe
function isViperProcess(pid) {
    try {
        const tasklist = spawnSync("tasklist", [], { encoding: "utf8" })
        const tasklistOutput = tasklist.stdout || ""
        return tasklistOutput.includes("Viper.exe") && tasklistOutput.includes(pid)
    } catch {
        return false
    }
}

// Find process ID listening on the specified port
function findProcessByPort(port) {
    try {
        // Use Windows netstat to find the process listening on the port
        const netstat = spawnSync("netstat", ["-ano"], { encoding: "utf8" })
        const output = netstat.stdout || ""
        const lines = output.split(/\r?\n/).filter((line) => line.includes(`:${port}`) && line.trim())

        for (const line of lines) {
            // Look for LISTENING state
            if (line.includes("LISTENING")) {
                // Extract PID (last column)
                const parts = line.trim().split(/\s+/)
                const pid = parts[parts.length - 1]

                // Validate PID is numeric and verify it's a Viper process
                if (/^\d+$/.test(pid) && isViperProcess(pid)) {
                    return pid
                }
            }
            // Skip non-listening connections
        }
    } catch {
        // If command fails, return null
    }

    return null
}

// Main function
function main() {
    const port = getHttpsPort()
    const pid = findProcessByPort(port)

    if (pid) {
        // Ensure .vscode directory exists
        const vscDir = path.join(__dirname, "..", ".vscode")
        if (!fs.existsSync(vscDir)) {
            fs.mkdirSync(vscDir, { recursive: true })
        }

        // Write PID to file for VS Code to read
        const pidFile = path.join(vscDir, "viper-pid.txt")
        fs.writeFileSync(pidFile, pid.toString())
        console.log(`Found Viper.exe process with PID: ${pid}`)
        process.exit(0)
    } else {
        console.error(`No Viper.exe process found listening on port ${port}`)
        process.exit(1)
    }
}

main()
