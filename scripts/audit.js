#!/usr/bin/env node

// `npm run audit` — run all whole-project audits (fallow, jscpd, ReSharper).
// Note: ReSharper inspectcode adds several minutes to the run.

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

// Run all audits unconditionally so a failure in one doesn't hide the
// others' findings; aggregate exit codes at the end.
const codes = ["audit-fallow.js", "audit-jscpd.js", "audit-resharper.js"].map(runNode)
const max = Math.max(0, ...codes)
if (max !== 0) {
    process.exit(max)
}
