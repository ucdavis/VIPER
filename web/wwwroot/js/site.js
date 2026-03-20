// Shared utilities for VIPER Razor views: fetch wrapper, error handling, date formatting, and session storage.

const MAX_DISPLAY_ERRORS = 5
const HTTP_NO_CONTENT = 204
const HTTP_ACCEPTED = 202

/*
 * Returns a date formatted to yyyy-mm-dd
 * If the date is not valid, returns empty string
 */
function formatDateForDateInput(d) {
    if (!d) {
        return ""
    }
    const [dateStr] = d.split("T")
    const dt = new Date(`${dateStr}T00:00:00`)
    return dateStr && dateStr !== "" && dt instanceof Date && !Number.isNaN(dt.valueOf())
        ? `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, "0")}-${String(dt.getDate()).padStart(2, "0")}`
        : ""
}

/*
 * Returns a date formatted with toLocalDateString if possible
 */
function formatDate(d) {
    if (!d) {
        return ""
    }
    const [dateStr] = d.split("T")
    const dt = new Date(`${dateStr}T00:00:00`)
    return dateStr && dateStr !== "" && dt instanceof Date && !Number.isNaN(dt.valueOf()) ? dt.toLocaleDateString() : ""
}

function formatDateTime(d, options) {
    if (!d) {
        return ""
    }
    const dt = new Date(d)
    return d !== "" && dt instanceof Date && !Number.isNaN(dt.valueOf()) ? dt.toLocaleString("en-US", options) : ""
}

/*
 * Validation error to include the errors object for .NET 400
 */
class ValidationError extends Error {
    constructor(message, errors) {
        super(message)
        this.errors = errors
    }
}
/*
 * Perform a fetch given the url and data. If successful, call any additional functions provided
 */
// oxlint-disable-next-line max-params -- API used across all Razor views
async function viperFetch(VueApp, url, data = {}, additionalFunctions = [], errorTarget = "") {
    if (data.headers === undefined) {
        data.headers = {}
    }
    if (data.headers["X-CSRF-TOKEN"] === undefined) {
        data.headers["X-CSRF-TOKEN"] = csrfToken
    }
    return await fetch(url, data)
        // Handle 4xx and 5xx status codes
        .then((r) => handleViperFetchError(r))
        // Return json (unless we got 204 No Content or 202 Accepted)
        .then((r) => (r.status === HTTP_NO_CONTENT || r.status === HTTP_ACCEPTED ? r : r.json()))
        // Check for success flag and result being defined. Call additional functions
        .then((r) => {
            let result = r
            if (r.success !== undefined) {
                if (!r.success || r.result === undefined) {
                    showViperFetchError(VueApp, r, errorTarget)
                }
                ;({ result } = r)
            }
            while (additionalFunctions.length > 0) {
                additionalFunctions.shift().call(this, result)
            }
            return r.pagination ? { result: result, pagination: r.pagination } : result
        })
        // Catch errors, including those thrown by handleViperFetchError
        .catch((error) => showViperFetchError(VueApp, error, errorTarget))
}

/*
 * If the fetch response ok flag or status code indicates an error
 * generate an error object to be displayed to the user
 */
async function handleViperFetchError(response) {
    if (!response.ok) {
        let result = null
        try {
            result = await response.json()
        } catch {
            throw new Error("An error occurred")
        }

        const message = result.errorMessage ?? result.detail ?? result.statusText
        throw new ValidationError(message, result?.errors)
    }
    return response
}

/*
 * Show an error the client
 */
function populateErrorTarget(errorTarget, error) {
    errorTarget.message = error.message
    if (!error?.errors) {
        return
    }
    if (typeof error.errors === "object") {
        for (const key in error.errors) {
            if (Object.hasOwn(error.errors, key)) {
                errorTarget[key] = {
                    error: true,
                    message: error.errors[key].join(""),
                }
            }
        }
    } else {
        for (let i = 0; i < MAX_DISPLAY_ERRORS && i < error.errors.length; i += 1) {
            errorTarget.message += ` ${error.errors[i]}`
        }
    }
}

function showViperFetchError(VueApp, error, errorTarget) {
    let shownError = false
    try {
        if (errorTarget) {
            populateErrorTarget(errorTarget, error)
            shownError = true
        }
    } catch {
        // Show the error dialog
    }

    if (!shownError) {
        VueApp.showViperError = true
        VueApp.viperErrorMessage = error.message
    }
}

/*
 * Load left nav from the relative URL "nav"
 */
async function loadViperLeftNav() {
    // Build query param list
    // Send in path for some apps?
    let qs = []
    for (const [paramName, val] of this.urlParams) {
        qs.push(`${paramName}=${val}`)
    }
    qs = qs.length > 0 ? `?${qs.join("&")}` : ""
    // Fix nav not loading when the url is host/2 instead of host/2/
    // oxlint-disable-next-line no-magic-numbers -- last 2 chars of URL path
    const navLocation = globalThis.location.href.slice(-2) === "/2" ? "/2/nav" : "nav"
    this.viperNavMenu = await viperFetch(this, navLocation + qs)
}

function getItemFromStorage(key) {
    const val = globalThis.sessionStorage.getItem(key)
    try {
        return val === null ? null : JSON.parse(val)
    } catch {
        /* Parse failure — return null below */
    }
    return null
}

function putItemInStorage(key, val) {
    globalThis.sessionStorage.setItem(key, JSON.stringify(val))
}
