﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
/*
    Returns a date formatted to yyyy-mm-dd
    If the date is not valid, returns empty string
*/
function formatDateForDateInput(d) {
    if (d == null || d == "") {
        return ""
    }
    d = d.split("T")[0]
    var dt = new Date(d + "T00:00:00")
    return (d && d != "" && dt instanceof Date && !isNaN(dt.valueOf()))
        ? (dt.getFullYear() + "-" + ("" + (dt.getMonth() + 1)).padStart(2, "0") + "-" + ("" + (dt.getDate())).padStart(2, "0"))
        : "";
}

/*
    Returns a date formatted with toLocalDateString if possible
*/
function formatDate(d) {
    if (d == null || d == "") {
        return ""
    }
    d = d.split("T")[0]
    var dt = new Date(d + "T00:00:00")
    return (d && d != "" && dt instanceof Date && !isNaN(dt.valueOf())) ? dt.toLocaleDateString() : ""
}

function formatDateTime(d, options) {
    if (d == null || d == "") {
        return ""
    }
    var dt = new Date(d)
    return (d && d != "" && dt instanceof Date && !isNaN(dt.valueOf())) ? dt.toLocaleString("en-US", options) : ""
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
async function viperFetch(VueApp, url, data = {}, additionalFunctions = [], errorTarget = "") {
    if (data.headers === undefined) {
        data.headers = {}
    }
    if (data.headers["X-CSRF-TOKEN"] === undefined) {
        data.headers["X-CSRF-TOKEN"] = csrfToken
    }
    return await fetch(url, data)
        //handle 4xx and 5xx status codes
        .then(r => handleViperFetchError(r))
        //return json (unless we got 204 No Content or 202 Accepted)
        .then(r => (r.status == "204" || r.status == "202") ? r : r.json())
        //check for success flag and result being defined. call additional functions
        .then(r => {
            var result = r
            if (r.success !== undefined) {
                if (!r.success || typeof (r.result) == "undefined") {
                    showViperFetchError(VueApp, r, errorTarget)
                }
                result = r.result
            }
            while (additionalFunctions.length) {
                additionalFunctions.shift().call(this, result)
            }
            return r.pagination ? { result: result, pagination: r.pagination } : result
        })
        //catch errors, including those thrown by handleViperFetchError
        .catch(e => showViperFetchError(VueApp, e, errorTarget))
}

/*
 * If the fetch response ok flag or status code indicates an error
 * generate an error object to be displayed to the user
 */
async function handleViperFetchError(response) {
    if (!response.ok) {
        try {
            var result = await response.json()
        }
        catch (e) {
            throw Error("An error occurred")
        }

        var message = result.errorMessage != null ? result.errorMessage
            : result.detail != null ? result.detail
            : result.statusText
        throw new ValidationError(message, result?.errors)
    }
    return response
}

/*
 * Show an error the client
 */
function showViperFetchError(VueApp, error, errorTarget) {
    var shownError = false
    try {
        if (errorTarget) {
            errorTarget.message = error.message
            if (error?.errors != null) {
                if (typeof error.errors == "object") {
                    for (key in error.errors) {
                        errorTarget[key] = {
                            error: true,
                            message: error.errors[key].join("")
                        }
                    }
                }
                else {
                    for (var i = 0; i < 5 && i < error.errors.length; i++) {
                        errorTarget.message += " " + error.errors[i];
                    }
                }
            }
            shownError = true
        }
    }
    catch (e) {
        //show the error dialog
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
    //build query param list
    //send in path for some apps?
    var qs = [];
    this.urlParams.forEach((val, paramName) => qs.push(paramName + "=" + val))
    qs = qs.length ? ("?" + qs.join("&")) : ""
    //fix nav not loading when the url is host/2 instead of host/2/
    navLocation = window.location.href.substring(window.location.href.length - 2, window.location.href.length) == "/2"
        ? "/2/nav"
        : "nav"
    this.viperNavMenu = await viperFetch(this, navLocation + qs)
}

function getItemFromStorage(key) {
    var val = window.sessionStorage.getItem(key)
    try {
        return val != null ? JSON.parse(val) : null
    }
    catch { }
    return null
}

function putItemInStorage(key, val) {
    window.sessionStorage.setItem(key, JSON.stringify(val))
}