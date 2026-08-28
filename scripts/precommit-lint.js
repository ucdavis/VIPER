#!/usr/bin/env node

// Lint staged files (called from pre-commit hook)
// Expects STAGED_FILES environment variable with newline-separated file list
// Delegates to lint-any.js for actual linting (DRY)

import { spawnSync } from "node:child_process"
import path from "node:path"
import { checkPartiallyStaged, createLogger } from "./lib/script-utils.js"

const { env } = process
const logger = createLogger("LINT")
const stagedFiles = (env.STAGED_FILES || "").split("\n").filter(Boolean)

if (stagedFiles.length === 0) {
    logger.success("No files to lint")
    process.exit(0)
}

// Ensure staged files don't have unstaged changes (would lint wrong content)
checkPartiallyStaged(stagedFiles, logger)

logger.info(`Linting ${stagedFiles.length} staged files...`)

// Delegate to lint-any.js which handles all file categorization and routing
const lintAnyPath = path.join(import.meta.dirname, "lint-any.js")
const result = spawnSync("node", [lintAnyPath, ...stagedFiles], {
    stdio: "inherit",
    cwd: process.cwd(),
})

if (result.error) {
    logger.error(`Failed to run lint-any.js: ${result.error.message}`)
    process.exit(1)
}

process.exit(result.status || 0)
