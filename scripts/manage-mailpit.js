#!/usr/bin/env node
// Environment variables loaded via --env-file-if-exists=.env.local in package.json

const { env: processEnv } = process
const { spawn, execFileSync } = require("node:child_process")
const fs = require("node:fs")
const path = require("node:path")
const http = require("node:http")
const https = require("node:https")
const net = require("node:net")
const crypto = require("node:crypto")
const yauzl = require("yauzl")
const { killProcess } = require("./lib/script-utils")

// Configuration constants
const GITHUB_API_LATEST = "https://api.github.com/repos/axllent/mailpit/releases/latest"
const ASSET_NAME = "mailpit-windows-amd64.zip"
const INSTALL_DIR = processEnv.MAILPIT_INSTALL_DIR || String.raw`C:\Tools\mailpit`
const MAILPIT_EXE = path.join(INSTALL_DIR, "mailpit.exe")

// Get environment variables with defaults
const { getDevServerEnv, openBrowser, createLogger } = require("./lib/script-utils")
const env = getDevServerEnv()
const SMTP_PORT = env.MAILPIT_SMTP_PORT
const WEB_PORT = env.MAILPIT_WEB_PORT

// Create logger
const logger = createLogger("Mailpit")
const { error: logError, success: logSuccess, info: logInfo, warning: logWarning } = logger

// Timeouts and limits (in milliseconds)
const NETWORK_TIMEOUT = 2000
const STARTUP_DELAY = 3000
const CONNECTION_TIMEOUT = 10_000
const HTTP_STATUS_OK = 200
const HTTP_STATUS_MOVED = 301
const HTTP_STATUS_FOUND = 302
const BYTES_PER_MB = 1_048_576

// Promise-based delay to avoid no-promise-executor-return lint warnings
function delay(ms) {
    return new Promise((resolve) => {
        setTimeout(resolve, ms)
    })
}

// Fetch JSON from a URL (follows redirects)
function fetchJson(url) {
    return new Promise((resolve, reject) => {
        const options = { headers: { "User-Agent": "VIPER-Mailpit-Installer" } }
        https
            .get(url, options, (res) => {
                if (res.statusCode === HTTP_STATUS_MOVED || res.statusCode === HTTP_STATUS_FOUND) {
                    resolve(fetchJson(res.headers.location))
                    return
                }
                if (res.statusCode !== HTTP_STATUS_OK) {
                    reject(new Error(`HTTP ${res.statusCode} fetching ${url}`))
                    return
                }
                let data = ""
                res.on("data", (chunk) => {
                    data += chunk
                })
                res.on("end", () => {
                    try {
                        resolve(JSON.parse(data))
                    } catch (error) {
                        reject(error)
                    }
                })
            })
            .on("error", reject)
    })
}

// Get latest Mailpit release info from GitHub
async function getLatestRelease() {
    logInfo("Checking GitHub for latest Mailpit release...")
    const release = await fetchJson(GITHUB_API_LATEST)
    const version = release.tag_name.replace(/^v/, "")
    const zipUrl = `https://github.com/axllent/mailpit/releases/download/${release.tag_name}/${ASSET_NAME}`

    // Get SHA256 digest from GitHub's release asset metadata
    const asset = release.assets.find((a) => a.name === ASSET_NAME)
    if (!asset) {
        throw new Error(`Asset ${ASSET_NAME} not found in release ${release.tag_name}`)
    }

    // GitHub provides digest as "sha256:<hex>"
    const sha256 = asset.digest.replace(/^sha256:/, "").toLowerCase()

    logInfo(`Latest version: ${version}`)
    return { version, zipUrl, sha256 }
}

// Get currently installed version from the binary
function getInstalledVersion() {
    try {
        const output = execFileSync(MAILPIT_EXE, ["version"], { encoding: "utf8", timeout: 5000 })
        const match = output.match(/v(\d+\.\d+\.\d+)/)
        return match ? match[1] : null
    } catch {
        return null
    }
}

// Check if a port is in use
function checkPort(port) {
    return new Promise((resolve) => {
        const server = net.createServer()
        let resolved = false
        const safeResolve = (value) => {
            if (!resolved) {
                resolved = true
                resolve(value)
            }
        }
        const timeout = setTimeout(() => {
            server.close()
            logWarning(`Port ${port} check timed out`)
            safeResolve(false)
        }, STARTUP_DELAY)

        server.listen(port, () => {
            clearTimeout(timeout)
            server.once("close", () => safeResolve(false))
            server.close()
        })

        server.on("error", () => {
            clearTimeout(timeout)
            safeResolve(true)
        })
    })
}

// Check if Mailpit web interface is responding
function checkMailpitWeb() {
    return new Promise((resolve) => {
        const req = http.get(`http://localhost:${WEB_PORT}`, { timeout: NETWORK_TIMEOUT }, (res) => {
            resolve(res.statusCode === HTTP_STATUS_OK)
        })

        req.on("error", () => resolve(false))
        req.on("timeout", () => {
            req.destroy()
            resolve(false)
        })
    })
}

// Verify file SHA256 hash
function verifyFileHash(filePath, expectedHash) {
    return new Promise((resolve, reject) => {
        const hash = crypto.createHash("sha256")
        const stream = fs.createReadStream(filePath)

        stream.on("data", (data) => {
            hash.update(data)
        })

        stream.on("end", () => {
            const actualHash = hash.digest("hex").toLowerCase()
            const expectedHashLower = expectedHash.toLowerCase()

            if (actualHash === expectedHashLower) {
                logSuccess(`Hash verification passed: ${actualHash}`)
                resolve(true)
            } else {
                logError(`Hash verification failed!`)
                logError(`Expected: ${expectedHashLower}`)
                logError(`Actual:   ${actualHash}`)
                resolve(false)
            }
        })

        stream.on("error", (err) => {
            logError(`Error reading file for hash verification: ${err.message}`)
            reject(err)
        })
    })
}

// Extract ZIP file using Node.js yauzl library
function extractZip(zipPath, extractPath) {
    return new Promise((resolve, reject) => {
        // Ensure the extract path exists
        if (!fs.existsSync(extractPath)) {
            fs.mkdirSync(extractPath, { recursive: true })
        }

        logInfo(`Extracting ZIP using Node.js yauzl library...`)

        yauzl.open(zipPath, { lazyEntries: true }, (err, zipfile) => {
            if (err) {
                logError(`Failed to open ZIP file: ${err.message}`)
                reject(err)
                return
            }

            const extractedFiles = []

            zipfile.readEntry()

            zipfile.on("entry", (entry) => {
                if (entry.fileName.endsWith("/")) {
                    // Directory entry, create directory and continue
                    const dirPath = path.join(extractPath, entry.fileName)
                    fs.mkdirSync(dirPath, { recursive: true })
                    zipfile.readEntry()
                } else {
                    // File entry, extract file with zip-slip protection
                    const destRoot = path.resolve(extractPath)
                    const targetPath = path.resolve(path.join(destRoot, entry.fileName))

                    // Prevent zip-slip: ensure extraction stays inside install root using path.relative
                    const relativePath = path.relative(destRoot, targetPath)
                    const isWindows = process.platform === "win32"

                    // Normalize for case-insensitive comparison on Windows
                    const normalizedRelative = isWindows ? relativePath.toLowerCase() : relativePath

                    // Check if path escapes the destination directory
                    if (normalizedRelative.startsWith("..") || path.isAbsolute(relativePath)) {
                        logWarning(`Skipping unsafe zip entry: ${entry.fileName}`)
                        zipfile.readEntry()
                        return
                    }

                    const filePath = targetPath
                    const fileDir = path.dirname(filePath)

                    // Ensure directory exists
                    if (!fs.existsSync(fileDir)) {
                        fs.mkdirSync(fileDir, { recursive: true })
                    }

                    zipfile.openReadStream(entry, (streamErr, readStream) => {
                        if (streamErr) {
                            logError(`Failed to read entry ${entry.fileName}: ${streamErr.message}`)
                            reject(streamErr)
                            return
                        }

                        const writeStream = fs.createWriteStream(filePath)

                        // Wait for writeStream close (not readStream end) to ensure
                        // Data is fully flushed to disk before processing next entry
                        writeStream.on("close", () => {
                            extractedFiles.push(entry.fileName)
                            logInfo(`Extracted: ${entry.fileName}`)
                            zipfile.readEntry()
                        })

                        readStream.on("error", (readErr) => {
                            logError(`Error reading ${entry.fileName}: ${readErr.message}`)
                            reject(readErr)
                        })

                        writeStream.on("error", (writeErr) => {
                            logError(`Error writing ${filePath}: ${writeErr.message}`)
                            reject(writeErr)
                        })

                        readStream.pipe(writeStream)
                    })
                }
            })

            zipfile.on("end", () => {
                logSuccess(`ZIP extraction completed! Extracted ${extractedFiles.length} files:`)
                for (const file of extractedFiles) {
                    logInfo(`  - ${file}`)
                }
                resolve()
            })

            zipfile.on("error", (zipErr) => {
                logError(`ZIP extraction error: ${zipErr.message}`)
                reject(zipErr)
            })
        })
    })
}

// Download a file
function downloadFile(url, destination) {
    return new Promise((resolve, reject) => {
        // Ensure directory exists
        const dir = path.dirname(destination)
        if (!fs.existsSync(dir)) {
            fs.mkdirSync(dir, { recursive: true })
        }

        const file = fs.createWriteStream(destination)

        https
            .get(url, async (response) => {
                if (response.statusCode === HTTP_STATUS_FOUND || response.statusCode === HTTP_STATUS_MOVED) {
                    // Follow redirect
                    try {
                        await downloadFile(response.headers.location, destination)
                        resolve()
                    } catch (error) {
                        reject(error)
                    }
                    return
                }

                if (response.statusCode !== HTTP_STATUS_OK) {
                    reject(new Error(`Download failed: ${response.statusCode}`))
                    return
                }

                response.pipe(file)

                file.on("finish", () => {
                    file.close()
                    resolve()
                })

                file.on("error", reject)
            })
            .on("error", reject)
    })
}

// Check if we can write to the installation directory
function checkInstallPermissions() {
    try {
        // Check if the directory exists or can be created
        if (!fs.existsSync(INSTALL_DIR)) {
            // Try to create the directory
            fs.mkdirSync(INSTALL_DIR, { recursive: true })
            logInfo(`Created installation directory: ${INSTALL_DIR}`)
        }

        // Test write permissions by creating a temporary file
        const testFile = path.join(INSTALL_DIR, ".permission-test")
        fs.writeFileSync(testFile, "test")
        fs.unlinkSync(testFile)

        return true
    } catch (error) {
        if (error.code === "EACCES" || error.code === "EPERM") {
            logError(`Permission denied: Cannot write to ${INSTALL_DIR}`)
            logError("This directory requires administrative privileges.")
            logError("Please run the command prompt as Administrator to install Mailpit.")
        } else {
            logError(`Failed to access installation directory: ${error.message}`)
        }
        return false
    }
}

// Install Mailpit (fetches latest version from GitHub)
async function installMailpit() {
    try {
        logInfo("Installing Mailpit...")

        // Check installation permissions first
        if (!checkInstallPermissions()) {
            return false
        }

        // Fetch latest release info from GitHub
        const { version, zipUrl, sha256 } = await getLatestRelease()

        logInfo(`Downloading v${version} from: ${zipUrl}`)

        const zipPath = path.join(INSTALL_DIR, "mailpit.zip")

        // Download ZIP file
        await downloadFile(zipUrl, zipPath)

        // Verify download integrity against SHA256 from GitHub API
        logInfo("Verifying download integrity...")
        const hashValid = await verifyFileHash(zipPath, sha256)
        if (!hashValid) {
            fs.unlinkSync(zipPath)
            throw new Error(
                "Downloaded file failed hash verification. This could indicate a corrupted download or security issue.",
            )
        }

        logInfo("Extracting Mailpit...")

        // Extract ZIP file
        await extractZip(zipPath, INSTALL_DIR)

        // Check what files were extracted
        logInfo("Checking extracted files...")
        const extractedFiles = fs.readdirSync(INSTALL_DIR)
        logInfo(`Found files: ${extractedFiles.join(", ")}`)

        // Look for the executable (might be named differently)
        let executablePath = MAILPIT_EXE
        const possibleNames = ["mailpit.exe", "mailpit-windows-amd64.exe", "mailpit"]

        for (const name of possibleNames) {
            const testPath = path.join(INSTALL_DIR, name)
            if (fs.existsSync(testPath)) {
                if (name !== "mailpit.exe") {
                    // Rename to expected name
                    fs.renameSync(testPath, MAILPIT_EXE)
                    logInfo(`Renamed ${name} to mailpit.exe`)
                }
                executablePath = MAILPIT_EXE
                break
            }
        }

        // Verify the executable exists
        if (!fs.existsSync(executablePath)) {
            throw new Error(`Installation failed - no executable found. Extracted files: ${extractedFiles.join(", ")}`)
        }

        // Clean up ZIP file
        try {
            fs.unlinkSync(zipPath)
        } catch {
            logWarning(`Could not delete ZIP file: ${zipPath}`)
        }

        logSuccess(`Mailpit v${version} installed to: ${MAILPIT_EXE}`)
        return true
    } catch (error) {
        logError(`Failed to install Mailpit: ${error.message}`)
        return false
    }
}

// Check if Mailpit is installed
function isMailpitInstalled() {
    return fs.existsSync(MAILPIT_EXE)
}

// Check if Mailpit installation is valid
function getMailpitStatus() {
    const installed = isMailpitInstalled()
    if (!installed) {
        return { installed: false }
    }

    try {
        const stats = fs.statSync(MAILPIT_EXE)
        const version = getInstalledVersion()
        logInfo(`Mailpit installed: v${version || "unknown"} (${Math.round(stats.size / BYTES_PER_MB)}MB)`)
        return { installed: true, verified: true, version }
    } catch (error) {
        return { installed: false, error: error.message }
    }
}

// Start Mailpit process
function startMailpit() {
    return new Promise((resolve, _reject) => {
        logInfo("Starting Mailpit...")

        const process = spawn(MAILPIT_EXE, [], {
            detached: true,
            stdio: "ignore",
            windowsHide: true,
        })

        process.unref()

        // Wait a moment and check if it started successfully
        setTimeout(async () => {
            const isRunning = await checkMailpitWeb()
            if (isRunning) {
                logSuccess("Mailpit started successfully!")
                logInfo(`📧 SMTP Server: localhost:${SMTP_PORT}`)
                logInfo(`🌐 Web Interface: http://localhost:${WEB_PORT}`)
                resolve(true)
            } else {
                logError("Mailpit failed to start properly")
                resolve(false)
            }
        }, STARTUP_DELAY)
    })
}

// Check if Mailpit is already running
async function isMailpitRunning() {
    const smtpInUse = await checkPort(SMTP_PORT)
    const webResponding = await checkMailpitWeb()
    return smtpInUse && webResponding
}

// Send a test email via direct SMTP connection
function sendTestEmail() {
    return new Promise((resolve) => {
        const client = new net.Socket()
        let step = 0
        const timestamp = new Date().toISOString()

        const commands = [
            "HELO viper-dev-test\r\n",
            "MAIL FROM: <viper-test@localhost.local>\r\n",
            "RCPT TO: <developer@viper-dev.local>\r\n",
            "DATA\r\n",
            `Subject: =?UTF-8?B?8J+OiSBNYWlscGl0IFRlc3QgRW1haWwgZnJvbSBWSVBFUg==?=\r\n` +
                `From: VIPER Development <viper-test@localhost.local>\r\n` +
                `To: Developer <developer@viper-dev.local>\r\n` +
                `Date: ${new Date().toUTCString()}\r\n` +
                `Message-ID: <viper-test-${Date.now()}@localhost.local>\r\n` +
                `Content-Type: text/html; charset=UTF-8\r\n\r\n` +
                `<h2>🎉 Mailpit Test Email</h2>\r\n` +
                `<p>This test email confirms that Mailpit SMTP integration is working correctly.</p>\r\n` +
                `<div style="background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 15px 0;">\r\n` +
                `  <h3>📋 Test Details</h3>\r\n` +
                `  <ul style="margin: 0;">\r\n` +
                `    <li><strong>Sent:</strong> ${timestamp}</li>\r\n` +
                `    <li><strong>From:</strong> npm run mailpit:status</li>\r\n` +
                `    <li><strong>SMTP Server:</strong> localhost:${SMTP_PORT}</li>\r\n` +
                `    <li><strong>Purpose:</strong> Verify Mailpit connectivity</li>\r\n` +
                `  </ul>\r\n` +
                `</div>\r\n` +
                `<p><strong>✅ Success!</strong> If you can see this email in the Mailpit web interface at <a href="http://localhost:8025">http://localhost:8025</a>, your setup is working correctly!</p>\r\n` +
                `<hr style="margin: 20px 0; border: none; border-top: 1px solid #ddd;">\r\n` +
                `<p><small><em>This email uses the same SMTP configuration that VIPER uses for sending emails.</em></small></p>\r\n` +
                `\r\n.\r\n`,
            "QUIT\r\n",
        ]

        client.connect(SMTP_PORT, "localhost", () => {
            // Connected, wait for greeting
        })

        client.on("data", (data) => {
            const response = data.toString()

            if (
                step < commands.length &&
                (response.startsWith("220") || response.startsWith("250") || response.startsWith("354"))
            ) {
                client.write(commands[step])
                step += 1
            } else if (response.startsWith("221")) {
                client.end()
                logSuccess("Test email sent successfully!")
                resolve(true)
            }
        })

        client.on("error", (err) => {
            logWarning(`Test email failed: ${err.message}`)
            resolve(false)
        })

        client.on("timeout", () => {
            logWarning("SMTP connection timed out")
            client.destroy()
            resolve(false)
        })

        client.setTimeout(CONNECTION_TIMEOUT)
    })
}

// Find and kill existing Mailpit processes (reuses killProcess from script-utils)
async function killExistingMailpit() {
    const stopped = await killProcess("mailpit")
    if (stopped) {
        logSuccess("Stopped existing Mailpit processes")
    } else {
        logInfo("No running Mailpit processes found")
    }
    return stopped
}

// Main management function
async function manageMailpit() {
    try {
        logInfo("Checking Mailpit status...")

        // Check if Mailpit is already running
        if (await isMailpitRunning()) {
            logSuccess("Mailpit is already running")
            logInfo(`📧 SMTP: localhost:${SMTP_PORT} | 🌐 Web: http://localhost:${WEB_PORT}`)
            return true
        }

        // Check if Mailpit is installed
        const status = getMailpitStatus()
        if (status.installed) {
            // Already installed — check for updates
            try {
                const { version: latest } = await getLatestRelease()
                if (status.version && status.version !== latest) {
                    logWarning(`Update available: v${status.version} → v${latest}`)
                    logInfo("Stopping Mailpit for update...")
                    await killExistingMailpit()
                    await delay(NETWORK_TIMEOUT)
                    await installMailpit()
                }
            } catch (error) {
                // GitHub API unavailable — continue with existing install
                logWarning(`Could not check for updates: ${error.message}`)
            }
        } else {
            logWarning("Mailpit not found, installing...")
            const installed = await installMailpit()
            if (!installed) {
                logError("Failed to install Mailpit")
                logInfo("You can try again with: npm run mailpit:start")
                return false
            }
        }

        // Check if ports are blocked by other processes
        const smtpInUse = await checkPort(SMTP_PORT)
        const webInUse = await checkPort(WEB_PORT)

        if (smtpInUse || webInUse) {
            logWarning("Mailpit ports are in use by another process")
            logInfo("Attempting to stop existing Mailpit processes...")
            await killExistingMailpit()

            // Wait a moment for processes to fully terminate
            await delay(NETWORK_TIMEOUT)
        }

        // Start Mailpit
        const started = await startMailpit()
        return started
    } catch (error) {
        logError(`Error managing Mailpit: ${error.message}`)
        return false
    }
}

// Handle command line arguments
async function main() {
    const args = process.argv.slice(2)
    const [command] = args

    switch (command) {
        case "start": {
            // Smart start: auto-install and handle conflicts
            await manageMailpit()
            break
        }

        case "stop": {
            await killExistingMailpit()
            break
        }

        case "status": {
            const running = await isMailpitRunning()
            const status = getMailpitStatus()

            logInfo(`Installed: ${status.installed ? `✅ v${status.version || "unknown"}` : "❌"}`)
            logInfo(`Running: ${running ? "✅" : "❌"}`)

            if (running) {
                logInfo(`📧 SMTP: localhost:${SMTP_PORT}`)
                logInfo(`🌐 Web: http://localhost:${WEB_PORT}`)

                // Send test email if running
                logInfo("Sending test email...")
                const emailSent = await sendTestEmail()
                if (emailSent) {
                    logSuccess("✉️  Test email sent successfully!")

                    // Open browser to Mailpit web interface
                    const webUrl = `http://localhost:${WEB_PORT}`
                    logInfo("Opening Mailpit web interface...")

                    const browserOpened = await openBrowser(webUrl)
                    if (browserOpened) {
                        logInfo("🌐 Mailpit web interface opened in your default browser")
                    } else {
                        logInfo(`🌐 Please manually open: ${webUrl}`)
                    }
                } else {
                    logWarning("Test email failed - SMTP may not be working properly")
                }
            }
            break
        }

        default: {
            // Default to smart start
            await manageMailpit()
            break
        }
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
            await main()
        } catch (error) {
            logError(`Unexpected error: ${error.message}`)
            process.exit(1)
        }
    })()
}

module.exports = { manageMailpit, isMailpitInstalled, isMailpitRunning }
