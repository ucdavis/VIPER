#!/usr/bin/env node

// Script to run the backend with proper environment variable defaults
const { spawn } = require("node:child_process")
const fs = require("node:fs")
const path = require("node:path")
const dotenv = require("dotenv")

// Load .env.local to get ASPNETCORE_URLS (getDevServerEnv only returns port numbers)
const projectRoot = path.resolve(__dirname, "..")
const envPath = path.join(projectRoot, ".env.local")
const envConfig = fs.existsSync(envPath) ? dotenv.config({ path: envPath }).parsed || {} : {}

// Use ASPNETCORE_URLS from .env.local, process.env, or default
const aspnetCoreUrls =
    envConfig.ASPNETCORE_URLS || process.env.ASPNETCORE_URLS || "https://localhost:7157;http://localhost:5000"

// Set up the environment for the backend
const backendEnv = {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: "Development",
    ASPNETCORE_URLS: aspnetCoreUrls,
}

console.log(`Starting backend with URLs: ${aspnetCoreUrls}`)

// Change to the dist/dev directory and run dotnet
process.chdir("dist/dev")
const backend = spawn("dotnet", ["Viper.dll"], {
    env: backendEnv,
    stdio: "inherit",
})

// Handle process exit
backend.on("exit", (code) => {
    process.exit(code || 0)
})

backend.on("error", (error) => {
    console.error("Failed to start backend:", error)
    process.exit(1)
})
