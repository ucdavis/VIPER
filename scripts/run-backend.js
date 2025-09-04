#!/usr/bin/env node

// Script to run the backend with proper environment variable defaults
const { spawn } = require("node:child_process")
const { getDevServerEnv } = require("./lib/script-utils")

// Get environment variables from .env.local if it exists
const env = getDevServerEnv()

// Set default ASPNETCORE_URLS if not already set
const aspnetCoreUrls = env.ASPNETCORE_URLS || "https://localhost:7157;http://localhost:5000"

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
