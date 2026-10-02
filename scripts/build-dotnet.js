#!/usr/bin/env node

// Build .NET projects for precommit (shared by lint, test, verify)
// Uses --artifacts-path for full isolation from dev server (avoids file lock conflicts)
//
// Only builds test/ project - it references web/ so dotnet builds both.
// Caches both success and failure - if code hasn't changed, result won't change.

import { execFileSync } from "node:child_process"
import { createLogger } from "./lib/script-utils.js"
import {
    needsBuild,
    markAsBuilt,
    wasBuildSuccessful,
    getCachedBuildOutput,
    filterBuildErrors,
} from "./lib/build-cache.js"

const { env } = process
const logger = createLogger("BUILD")

const artifactsPath = ".artifacts-precommit"

// Default execFileSync maxBuffer is 1 MB; full build output with all warnings can exceed
// that, which would surface as a false "build failed" instead of the real result.
const MAX_BUILD_OUTPUT = 10_485_760

// .artifacts-precommit is a separate, isolated output path (deliberately kept apart from
// the normal bin/obj folders to avoid file-lock conflicts with a running dev server), so a
// rebuild here doesn't get the benefit of warm incremental state the way a plain `dotnet
// build` does. A cold build of this solution - more so right after a merge, and with
// SonarAnalyzer's security/vulnerability rule categories enabled - can take a while. 120s
// was too tight and got mistaken for a real compile failure when execFileSync killed the
// process mid-build; give it more headroom.
const BUILD_TIMEOUT_MS = 600_000

// Check if either project needs rebuild
const webNeedsBuild = needsBuild("web", "Viper.csproj")
const testNeedsBuild = needsBuild("test", "Viper.test.csproj")

if (!webNeedsBuild && !testNeedsBuild) {
    // Hash unchanged - return cached result (check both projects)
    const webFailed = wasBuildSuccessful("Viper.csproj") === false
    const testFailed = wasBuildSuccessful("Viper.test.csproj") === false
    if (webFailed || testFailed) {
        logger.error("Build failed (cached) - fix the error(s) below and try again:")
        if (webFailed) {
            console.error(filterBuildErrors(getCachedBuildOutput("Viper.csproj")))
        }
        if (testFailed) {
            console.error(filterBuildErrors(getCachedBuildOutput("Viper.test.csproj")))
        }
        process.exit(1)
    }
    logger.success("Build skipped (cached)")
    process.exit(0)
}

// Build test project (which also builds web via ProjectReference)
logger.info(`Building test/ → ${artifactsPath} (includes web/)`)
try {
    const result = execFileSync(
        "dotnet",
        ["build", "test/", "--artifacts-path", artifactsPath, "--verbosity", "quiet", "--nologo"],
        {
            encoding: "utf8",
            timeout: BUILD_TIMEOUT_MS,
            maxBuffer: MAX_BUILD_OUTPUT,
            stdio: ["inherit", "pipe", "pipe"],
            env: { ...env, DOTNET_USE_COMPILER_SERVER: "1" },
        },
    )

    // Cache success
    markAsBuilt("web", "Viper.csproj", result, true)
    markAsBuilt("test", "Viper.test.csproj", result, true)
    logger.success("Build complete")
} catch (error) {
    const output = (error.stdout || "") + (error.stderr || "")

    if (error.signal) {
        // execFileSync kills the child and sets `signal` (not a normal exit code) when the
        // timeout fires - that's an environment/timing issue, not a deterministic function
        // of the code, so it must NOT be cached as a build failure. Caching it would make
        // every future run replay this same "failure" (via the cached-output branch above)
        // for as long as the file hashes stay unchanged, exactly like a real compile error,
        // even though the code may be fine and just needs another (or longer) attempt.
        logger.error(
            `Build timed out after ${BUILD_TIMEOUT_MS / 1000}s (signal ${error.signal}) - ` +
                "not a compile error, so nothing was cached. Try again; if it keeps timing out, " +
                "BUILD_TIMEOUT_MS in this script may need to go up further.",
        )
        if (output) {
            console.error(filterBuildErrors(output))
        }
        process.exit(1)
    }

    // Cache failure - no point rebuilding if code hasn't changed
    markAsBuilt("web", "Viper.csproj", output, false)
    markAsBuilt("test", "Viper.test.csproj", output, false)
    logger.error("Build failed")
    console.error(filterBuildErrors(output))
    process.exit(1)
}
