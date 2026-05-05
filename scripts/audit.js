#!/usr/bin/env node

// `npm run audit` — run both fallow and jscpd whole-project audits.

const path = require("node:path")
const { spawnSync } = require("node:child_process")

function runNode(script) {
    const result = spawnSync(process.execPath, [path.join(__dirname, script)], {
        stdio: "inherit",
        windowsHide: true,
    })
    if (result.signal) {
        return 1
    }
    return result.status ?? 1
}

// Run both audits unconditionally so a failure in the first doesn't hide the
// second's findings; aggregate exit codes at the end.
const codes = ["audit-fallow.js", "audit-jscpd.js"].map(runNode)
const max = Math.max(0, ...codes)
if (max !== 0) {
    process.exit(max)
}
