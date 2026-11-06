#!/usr/bin/env node

// Copies the browser builds Razor pages load into web/wwwroot/lib, so Razor and
// the SPAs always run the Vue and Quasar versions pinned in VueApp's lockfile.
// Runs from VueApp's postinstall.

import { cpSync, rmSync } from "node:fs"
import path from "node:path"

const repoRoot = path.join(import.meta.dirname, "..")
const nodeModules = path.join(repoRoot, "VueApp", "node_modules")
const libRoot = path.join(repoRoot, "web", "wwwroot", "lib")

const files = ["vue/dist/vue.global.prod.js", "quasar/dist/quasar.umd.prod.js", "quasar/dist/quasar.prod.css"]

// Clear first so files from an earlier version or restore can't linger and get published.
rmSync(libRoot, { recursive: true, force: true })
for (const file of files) {
    cpSync(path.join(nodeModules, file), path.join(libRoot, file))
}
