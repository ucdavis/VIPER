#!/usr/bin/env node

/**
 * Run multiple commands in parallel, suppressing output unless a command fails.
 * On failure, shows only the failed command's output.
 *
 * Usage: node run-parallel.js "name1:cmd1" "name2:cmd2" ...
 */

const { spawn } = require("node:child_process")
const { createLogger } = require("./lib/script-utils")

const { env } = process
const logger = createLogger("PARALLEL")
const args = process.argv.slice(2)

// Constants for output formatting
const MS_PER_SECOND = 1000
const SEPARATOR_WIDTH = 60
const DASH_WIDTH = 40

if (args.length === 0) {
    console.error('Usage: node run-parallel.js "name:command" ...')
    process.exit(1)
}

// Parse commands from "NAME:command" format
const tasks = args.map((arg) => {
    const colonIndex = arg.indexOf(":")
    if (colonIndex === -1) {
        return { name: "TASK", command: arg }
    }
    return {
        name: arg.slice(0, colonIndex),
        command: arg.slice(colonIndex + 1),
    }
})

// ANSI color codes for task names
const colors = ["\u001B[33m", "\u001B[34m", "\u001B[32m", "\u001B[35m", "\u001B[36m"] // yellow, blue, green, magenta, cyan
const reset = "\u001B[0m"
const green = "\u001B[32m"
const red = "\u001B[31m"

/**
 * Run a single command and capture its output
 */
function runTask(task, colorIndex) {
    return new Promise((resolve) => {
        const color = colors[colorIndex % colors.length]
        const startTime = Date.now()
        let output = ""

        const child = spawn(task.command, [], {
            shell: true,
            env: { ...env, FORCE_COLOR: "1" },
        })

        child.stdout.on("data", (data) => {
            output += data.toString()
        })

        child.stderr.on("data", (data) => {
            output += data.toString()
        })

        child.on("close", (code) => {
            const duration = ((Date.now() - startTime) / MS_PER_SECOND).toFixed(1)
            resolve({
                name: task.name,
                color,
                code,
                output,
                duration,
            })
        })

        child.on("error", (err) => {
            const duration = ((Date.now() - startTime) / MS_PER_SECOND).toFixed(1)
            resolve({
                name: task.name,
                color,
                code: 1,
                output: `Error: ${err.message}\n`,
                duration,
            })
        })
    })
}

async function main() {
    const startTime = Date.now()

    // Show what we're running
    const taskNames = tasks.map((t, i) => `${colors[i % colors.length]}${t.name}${reset}`).join(", ")
    console.log(`Running: ${taskNames}`)
    console.log("")

    // Run all tasks in parallel
    const results = await Promise.all(tasks.map((task, index) => runTask(task, index)))

    const totalDuration = ((Date.now() - startTime) / MS_PER_SECOND).toFixed(1)

    // Separate failures for output display
    const failures = results.filter((r) => r.code !== 0)

    // Show summary line for each task
    for (const result of results) {
        const status = result.code === 0 ? `${green}✓${reset}` : `${red}✗${reset}`
        console.log(`${status} ${result.color}${result.name}${reset} (${result.duration}s)`)
    }
    console.log("")

    // If any failures, show their output
    if (failures.length > 0) {
        console.log(`${red}${"=".repeat(SEPARATOR_WIDTH)}${reset}`)
        console.log(`${red}Failed tasks output:${reset}`)
        console.log(`${red}${"=".repeat(SEPARATOR_WIDTH)}${reset}`)

        for (const failure of failures) {
            console.log("")
            console.log(`${failure.color}[${failure.name}]${reset} output:`)
            console.log("-".repeat(DASH_WIDTH))
            // Trim and show output
            const trimmedOutput = failure.output.trim()
            if (trimmedOutput) {
                console.log(trimmedOutput)
            } else {
                console.log("(no output)")
            }
        }

        console.log("")
        logger.error(`${failures.length} of ${results.length} tasks failed (${totalDuration}s total)`)
        process.exit(1)
    }

    logger.success(`All ${results.length} tasks passed (${totalDuration}s total)`)
    process.exit(0)
}

main()
