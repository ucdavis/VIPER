#!/usr/bin/env node

// Whole-solution ReSharper inspectcode scan via the dotnet local tool.
// Reports to ./inspect-report/ (html + sarif).
// Build artifacts isolated under .artifacts-resharper/ so the dev server's
// locked Viper.exe doesn't collide with inspectcode's build step.
// For CI regression detection see scripts/audit-resharper-regression.js.

const fs = require("node:fs")
const path = require("node:path")
const { spawnSync } = require("node:child_process")

const { env } = process

const PROJECT_ROOT = path.join(__dirname, "..")
const OUT_DIR = path.join(PROJECT_ROOT, "inspect-report")
const ARTIFACTS_DIR = path.join(PROJECT_ROOT, ".artifacts-resharper")
const SLN = path.join(PROJECT_ROOT, "Viper.sln")

// On Windows, jb inspectcode autodetects MSBuild and picks SSMS's MSBuild
// toolset 18.0 over the .NET SDK MSBuild (verified on a machine with SSMS
// 22 installed: workload SDK references like
// Microsoft.NET.SDK.WorkloadAutoImportPropsLocator fail to resolve under
// the SSMS toolset). Force the dotnet SDK MSBuild to avoid this.
function findDotnetSdkMsbuild() {
    const candidateRoots = [
        env.DOTNET_ROOT && path.join(env.DOTNET_ROOT, "sdk"),
        path.join("C:", "Program Files", "dotnet", "sdk"),
        path.join("C:", "Program Files (x86)", "dotnet", "sdk"),
    ].filter(Boolean)
    for (const sdkRoot of candidateRoots) {
        if (fs.existsSync(sdkRoot)) {
            const versions = fs
                .readdirSync(sdkRoot, { withFileTypes: true })
                .filter((d) => d.isDirectory() && /^\d+\.\d+\.\d+/.test(d.name))
                .map((d) => d.name)
                .toSorted((a, b) => b.localeCompare(a, undefined, { numeric: true }))
            for (const version of versions) {
                const dll = path.join(sdkRoot, version, "MSBuild.dll")
                if (fs.existsSync(dll)) {
                    return { dll, version }
                }
            }
        }
    }
    return null
}

function runInspectCode(format, outFile) {
    const args = [
        "tool",
        "run",
        "jb",
        "inspectcode",
        SLN,
        `--output=${outFile}`,
        `--format=${format}`,
        "--severity=WARNING",
        // Keep build artifacts out of web/bin so the dev server's locked
        // Viper.exe does not break the inspectcode build.
        `--properties:UseArtifactsOutput=true`,
        `--properties:ArtifactsPath=${ARTIFACTS_DIR}`,
    ]

    if (process.platform === "win32") {
        const sdk = findDotnetSdkMsbuild()
        if (sdk) {
            args.push(`--toolset-path=${sdk.dll}`, `--dotnetcoresdk=${sdk.version}`)
        }
    }

    console.log(`\n━━━ inspectcode → ${path.relative(PROJECT_ROOT, outFile)} ━━━`)
    // No shell: passing args directly avoids cmd.exe re-splitting them at
    // spaces inside paths like "C:\Program Files\dotnet\sdk\..\MSBuild.dll".
    const result = spawnSync("dotnet", args, {
        cwd: PROJECT_ROOT,
        stdio: "inherit",
        windowsHide: true,
    })
    if (result.error || result.status !== 0) {
        const reason = result.error?.message ?? `exit ${result.status}`
        console.error(`❌ inspectcode failed: ${reason}`)
        process.exit(1)
    }
}

// --format=sarif|html|both (default: both). The regression gate only needs
// SARIF; skipping HTML roughly halves CI scan time.
function parseFormat(argv) {
    for (const arg of argv) {
        const m = arg.match(/^--format=(?<value>sarif|html|both)$/i)
        if (m?.groups) {
            return m.groups.value.toLowerCase()
        }
    }
    return "both"
}

const format = parseFormat(process.argv.slice(2))

fs.mkdirSync(OUT_DIR, { recursive: true })

// SARIF for parseable CI consumption, HTML for human review.
if (format === "sarif" || format === "both") {
    runInspectCode("Sarif", path.join(OUT_DIR, "inspect.sarif"))
}
if (format === "html" || format === "both") {
    runInspectCode("Html", path.join(OUT_DIR, "inspect.html"))
}

console.log(`\n✅ Reports written to ${path.relative(PROJECT_ROOT, OUT_DIR)}/`)
