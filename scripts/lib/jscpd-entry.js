#!/usr/bin/env node

// Resolve the jscpd CLI entry script from the installed package instead of a
// hardcoded path. jscpd v5 moved its entry from bin/jscpd to run-jscpd.js, so
// reading it from the package manifest keeps our runners working across major
// bumps. Callers invoke it via `node <entry>` (see spawnSync usage), which also
// sidesteps Windows .cmd-shim argument-escaping bugs on backslash paths.

const fs = require("node:fs")
const path = require("node:path")

/**
 * Resolve the absolute path to the jscpd CLI entry script.
 * @returns {string | null} Path to the entry script, or null when jscpd is not
 *   installed / resolvable.
 */
function resolveJscpdEntry() {
    try {
        const manifestPath = require.resolve("jscpd/package.json")
        const { bin } = JSON.parse(fs.readFileSync(manifestPath, "utf8"))
        const binRelative = typeof bin === "string" ? bin : bin?.jscpd
        if (!binRelative) {
            return null
        }
        const entry = path.join(path.dirname(manifestPath), binRelative)
        return fs.existsSync(entry) ? entry : null
    } catch {
        return null
    }
}

module.exports = { resolveJscpdEntry }
