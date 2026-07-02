#!/usr/bin/env node
/* oxlint-disable no-await-in-loop -- Polling intentionally awaits each cycle sequentially */

// Jenkins deploy build status checker. Reports the current/last build for the
// deploy job and, with --watch, polls an in-progress build until it finishes.
// Ignores the internal Jenkins self-signed cert (rejectUnauthorized: false) —
// interim until the CA is trusted. Reads JENKINS_URL/USER/API_TOKEN from the
// environment (loaded via --env-file-if-exists=.env.local) or from .env.local.
//
// Usage:
//   node scripts/jenkins-status.js           # one-shot status
//   node scripts/jenkins-status.js --watch    # poll until the active build ends

const fs = require("node:fs")
const path = require("node:path")
const https = require("node:https")

const POLL_INTERVAL_MS = 10_000
const MS_PER_SEC = 1000
const PERCENT_SCALE = 100
const PROGRESS_CAP = 99
const HTTP_OK = 200
const HTTP_REDIRECT_MIN = 300
const HTTP_REDIRECT_MAX = 400
const MAX_REDIRECTS = 5
const REQUEST_TIMEOUT_MS = 15_000

const ENV_FILE = path.join(__dirname, "..", ".env.local")
const BUILD_FIELDS = "number,building,result,timestamp,estimatedDuration,duration"
const TREE = `inQueue,queueItem[why],lastBuild[${BUILD_FIELDS}]`

const sleep = (ms) =>
    new Promise((resolve) => {
        setTimeout(resolve, ms)
    })

// Prefer process.env (npm --env-file), fall back to parsing .env.local directly
// so the script also works when invoked as `node scripts/jenkins-status.js`.
function loadJenkinsEnv() {
    const { env: baseEnv } = process
    const env = { ...baseEnv }
    if (fs.existsSync(ENV_FILE)) {
        for (const line of fs.readFileSync(ENV_FILE, "utf8").split(/\r?\n/u)) {
            const match = line.match(/^\s*(?<key>JENKINS_[A-Z_]+)\s*=\s*(?<value>.*?)\s*$/u)
            if (match && !env[match.groups.key]) {
                env[match.groups.key] = match.groups.value.replaceAll(/^["']|["']$/gu, "")
            }
        }
    }
    const url = env.JENKINS_URL
    const user = env.JENKINS_USER
    const token = env.JENKINS_API_TOKEN
    if (!url || !user || !token) {
        console.error("❌ Missing JENKINS_URL / JENKINS_USER / JENKINS_API_TOKEN (set them in .env.local).")
        process.exit(1)
    }
    return { url, user, token }
}

// Job root is the trigger URL minus its trailing /build or /buildWithParameters action.
function jobUrlFromTrigger(triggerUrl) {
    return triggerUrl.replace(/\/(?:buildWithParameters|build)\/?$/u, "")
}

function getJson(url, auth, redirects = 0) {
    return new Promise((resolve, reject) => {
        const request = https.request(
            new URL(url),
            {
                method: "GET",
                timeout: REQUEST_TIMEOUT_MS,
                rejectUnauthorized: false, // Internal Jenkins uses a self-signed cert
                headers: { Authorization: `Basic ${Buffer.from(auth).toString("base64")}` },
            },
            (res) => {
                if (res.statusCode >= HTTP_REDIRECT_MIN && res.statusCode < HTTP_REDIRECT_MAX && res.headers.location) {
                    res.resume()
                    if (redirects >= MAX_REDIRECTS) {
                        reject(new Error(`Too many redirects (> ${MAX_REDIRECTS}) from Jenkins`))
                        return
                    }
                    resolve(getJson(new URL(res.headers.location, url).toString(), auth, redirects + 1))
                    return
                }
                let body = ""
                res.on("data", (chunk) => {
                    body += chunk
                })
                res.on("end", () => {
                    if (res.statusCode !== HTTP_OK) {
                        reject(new Error(`HTTP ${res.statusCode} from Jenkins`))
                        return
                    }
                    try {
                        resolve(JSON.parse(body))
                    } catch {
                        reject(new Error("Non-JSON response from Jenkins"))
                    }
                })
            },
        )
        request.on("timeout", () => {
            request.destroy(new Error(`Jenkins request timed out after ${REQUEST_TIMEOUT_MS / MS_PER_SEC}s`))
        })
        request.on("error", reject)
        request.end()
    })
}

// Turn the job JSON into a one-line status plus a done flag for the watch loop.
function describe(job) {
    const build = job.lastBuild || {}
    if (job.inQueue) {
        const why = job.queueItem && job.queueItem.why ? ` (${job.queueItem.why})` : ""
        return { done: false, text: `⏳ Queued${why}` }
    }
    if (build.building) {
        const elapsed = Date.now() - build.timestamp
        const estimated = build.estimatedDuration > 0 ? build.estimatedDuration : 0
        const pct = estimated ? Math.min(PROGRESS_CAP, Math.round((elapsed / estimated) * PERCENT_SCALE)) : "?"
        const estText = estimated ? `${Math.round(estimated / MS_PER_SEC)}s` : "unknown"
        const elapsedText = `${Math.round(elapsed / MS_PER_SEC)}s`
        return { done: false, text: `🔨 Building #${build.number} — ~${pct}% (${elapsedText} / est ${estText})` }
    }
    if (build.number) {
        const icon = build.result === "SUCCESS" ? "✅" : "❌"
        return {
            done: true,
            text: `${icon} Last build #${build.number}: ${build.result} (${Math.round((build.duration || 0) / MS_PER_SEC)}s)`,
        }
    }
    return { done: true, text: "No builds found for this job." }
}

async function checkOnce(jobApi, auth) {
    const job = await getJson(jobApi, auth)
    const status = describe(job)
    console.log(`${new Date().toLocaleTimeString()}  ${status.text}`)
    return status.done
}

async function main() {
    const watch = process.argv.includes("--watch")
    const { url, user, token } = loadJenkinsEnv()
    const jobApi = `${jobUrlFromTrigger(url)}/api/json?tree=${TREE}`
    const auth = `${user}:${token}`

    let done = await checkOnce(jobApi, auth)
    if (watch) {
        while (!done) {
            await sleep(POLL_INTERVAL_MS)
            done = await checkOnce(jobApi, auth)
        }
    }
}

// oxlint-disable-next-line promise/prefer-await-to-then -- Top-level entry point
main().catch((error) => {
    console.error(`❌ ${error.message}`)
    process.exit(1)
})
