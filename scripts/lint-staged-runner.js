#!/usr/bin/env node

// Lint staged files only - runs lint-any.js on files staged for commit
// Usage: npm run lint:staged

const { execFileSync } = require("node:child_process")
const { checkPartiallyStaged, createLogger } = require("./lib/script-utils")

const { env } = process
const logger = createLogger("LINT:STAGED")

// Get staged files (excluding deleted files)
function getStagedFiles() {
    try {
        return execFileSync("git", ["diff", "--cached", "--name-only", "--diff-filter=d"], {
            encoding: "utf8",
        })
            .trim()
            .split("\n")
            .filter(Boolean)
    } catch (error) {
        logger.error("Failed to get staged files from git")
        console.error(error.message)
        process.exit(1)
    }
}

const stagedFiles = getStagedFiles()

if (stagedFiles.length === 0) {
    logger.success("No staged files to lint")
    process.exit(0)
}

// Ensure staged files don't have unstaged changes (would lint wrong content)
checkPartiallyStaged(stagedFiles, logger)

logger.info(`Found ${stagedFiles.length} staged file(s)`)

// Run lint-any.js with the staged files
try {
    execFileSync("node", ["scripts/lint-any.js", ...stagedFiles], {
        encoding: "utf8",
        stdio: "inherit",
        env: { ...env, LINT_BLOCK_ON_WARNINGS: "true", DOTNET_USE_COMPILER_SERVER: "1" },
    })
} catch (error) {
    // lint-any.js will handle its own exit codes
    process.exit(error.status || 1)
}
