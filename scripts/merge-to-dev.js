#!/usr/bin/env node

/**
 * Merges the current feature branch into Development and pushes.
 *
 * Safety checks:
 * - Must be on a feature branch (not main/Development)
 * - Working directory must be clean (no uncommitted changes)
 * - All commits must be pushed to remote
 * - Development must be up-to-date with remote
 * - Handles push conflicts with automatic rebase
 */

const { execFileSync } = require("node:child_process")

const PROTECTED_BRANCHES = new Set(["main", "Development"])

function execGit(args, options = {}) {
    try {
        return execFileSync("git", args, {
            encoding: "utf8",
            stdio: options.silent ? "pipe" : "inherit",
            ...options,
        })
    } catch (error) {
        if (options.ignoreError) {
            return error.stdout || ""
        }
        throw error
    }
}

function execGitSilent(args) {
    return execGit(args, { silent: true, stdio: "pipe" }).trim()
}

function getCurrentBranch() {
    return execGitSilent(["rev-parse", "--abbrev-ref", "HEAD"])
}

function hasStagedChanges() {
    const status = execGitSilent(["diff", "--cached", "--name-only"])
    return status.length > 0
}

function hasUnpushedCommits(branch) {
    try {
        const result = execGitSilent(["log", `origin/${branch}..${branch}`, "--oneline"])
        return result.length > 0
    } catch {
        // Branch might not have a remote tracking branch yet
        return true
    }
}

function isRemoteBehind(branch) {
    try {
        execGitSilent(["fetch", "origin"])
        const local = execGitSilent(["rev-parse", branch])
        const remote = execGitSilent(["rev-parse", `origin/${branch}`])
        const mergeBase = execGitSilent(["merge-base", branch, `origin/${branch}`])

        // If local and remote are the same, we're up to date
        if (local === remote) {
            return { behind: false, diverged: false }
        }

        // If merge-base equals remote, local is ahead (that's fine for push)
        if (mergeBase === remote) {
            return { behind: false, diverged: false }
        }

        // If merge-base equals local, local is behind
        if (mergeBase === local) {
            return { behind: true, diverged: false }
        }

        // Otherwise, branches have diverged
        return { behind: true, diverged: true }
    } catch {
        return { behind: false, diverged: false }
    }
}

function checkRemoteExists(branch) {
    try {
        execGitSilent(["rev-parse", `origin/${branch}`])
        return true
    } catch {
        return false
    }
}

function error(message) {
    console.error(`\n❌ Error: ${message}\n`)
    process.exit(1)
}

function success(message) {
    console.log(`\n✅ ${message}\n`)
}

function info(message) {
    console.log(`ℹ️  ${message}`)
}

function step(message) {
    console.log(`\n→ ${message}`)
}

const SEPARATOR_LENGTH = 40

function main() {
    console.log(`\n🔀 Merge to Development\n${"=".repeat(SEPARATOR_LENGTH)}`)

    // Step 1: Check current branch
    const currentBranch = getCurrentBranch()
    info(`Current branch: ${currentBranch}`)

    if (PROTECTED_BRANCHES.has(currentBranch)) {
        error(`Cannot run from protected branch '${currentBranch}'. Switch to a feature branch first.`)
    }

    // Step 2: Check for staged but uncommitted changes
    step("Checking for staged changes...")
    if (hasStagedChanges()) {
        console.log("\nStaged changes:")
        execGit(["diff", "--cached", "--name-only"])
        error("You have staged changes that are not committed. Please commit them first.")
    }
    info("No staged changes.")

    // Step 3: Check for unpushed commits
    step("Checking for unpushed commits...")
    if (!checkRemoteExists(currentBranch)) {
        error(
            `Branch '${currentBranch}' has not been pushed to remote. Push it first with: git push -u origin ${currentBranch}`,
        )
    }

    if (hasUnpushedCommits(currentBranch)) {
        error(`You have unpushed commits on '${currentBranch}'. Push them first with: git push`)
    }
    info("All commits are pushed to remote.")

    // Step 4: Fetch and check Development status
    step("Fetching latest from remote...")
    execGit(["fetch", "origin"], { silent: true, stdio: "pipe" })

    step("Checking out Development...")
    try {
        execGit(["checkout", "Development"], { silent: true, stdio: "pipe" })
    } catch {
        error("Failed to checkout Development branch.")
    }

    // Step 5: Check if Development is up-to-date
    step("Checking Development branch status...")
    const devStatus = isRemoteBehind("Development")

    if (devStatus.diverged) {
        execGit(["checkout", currentBranch], { silent: true, stdio: "pipe" })
        error(
            "Local Development branch has diverged from remote. Please resolve manually:\n" +
                "  git checkout Development\n" +
                "  git fetch origin\n" +
                "  git reset --hard origin/Development  # WARNING: This discards local changes",
        )
    }

    // Step 6: Pull latest Development
    step("Pulling latest Development...")
    try {
        execGit(["pull", "--ff-only", "origin", "Development"], { silent: true, stdio: "pipe" })
    } catch {
        execGit(["checkout", currentBranch], { silent: true, stdio: "pipe" })
        error("Failed to pull Development. The branch may have diverged.")
    }
    info("Development is up-to-date.")

    // Step 7: Merge feature branch
    step(`Merging ${currentBranch} into Development...`)
    try {
        execGit(["merge", "--no-edit", currentBranch], { silent: true, stdio: "pipe" })
    } catch {
        console.log("\nMerge conflicts detected:")
        execGit(["status", "--short"])
        execGit(["merge", "--abort"], { ignoreError: true, silent: true, stdio: "pipe" })
        execGit(["checkout", currentBranch], { silent: true, stdio: "pipe" })
        error(`Merge conflicts detected. Please resolve manually:
  git checkout Development
  git merge ${currentBranch}
  # Resolve conflicts, then:
  git add .
  git commit
  git push`)
    }
    info("Merge successful.")

    // Step 8: Push to remote (with rebase retry on conflict)
    step("Pushing to remote...")
    try {
        execGit(["push", "origin", "Development"], { silent: true, stdio: "pipe" })
    } catch {
        info("Push failed. Attempting rebase to incorporate remote changes...")

        try {
            execGit(["fetch", "origin", "Development"], { silent: true, stdio: "pipe" })
            execGit(["rebase", "origin/Development"], { silent: true, stdio: "pipe" })
        } catch {
            execGit(["rebase", "--abort"], { ignoreError: true, silent: true, stdio: "pipe" })
            execGit(["checkout", currentBranch], { silent: true, stdio: "pipe" })
            error(
                "Rebase failed due to conflicts. Please resolve manually:\n" +
                    "  git checkout Development\n" +
                    "  git pull --rebase origin Development\n" +
                    "  # Resolve conflicts if any, then:\n" +
                    "  git push origin Development",
            )
        }

        // Try push again after rebase
        try {
            execGit(["push", "origin", "Development"], { silent: true, stdio: "pipe" })
        } catch {
            execGit(["checkout", currentBranch], { silent: true, stdio: "pipe" })
            error("Push failed after rebase. Please resolve manually.")
        }
    }
    info("Pushed to remote.")

    // Step 9: Return to original branch
    step(`Returning to ${currentBranch}...`)
    execGit(["checkout", currentBranch], { silent: true, stdio: "pipe" })

    success(`Successfully merged '${currentBranch}' into Development and pushed!`)
}

try {
    main()
} catch (error) {
    console.error(error)
    process.exit(1)
}
