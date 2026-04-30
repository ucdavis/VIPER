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
    if (result.status !== 0) {
        process.exit(result.status || 1)
    }
}

runNode("audit-fallow.js")
runNode("audit-jscpd.js")
runNode("audit-resharper.js")
