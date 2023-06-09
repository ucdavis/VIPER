﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
/*
    Returns a date formatted to yyyy-mm-dd
    If the date is not valid, returns empty string
*/
function formatDateForDateInput(d) {
    var dt = new Date(d)
    return (d && d != "" && dt instanceof Date && !isNaN(dt.valueOf()))
        ? (dt.getFullYear() + "-" + ("" + (dt.getMonth() + 1)).padStart(2, "0") + "-" + ("" + (dt.getDate())).padStart(2, "0"))
        : "";
}

/*
 * Validation error to include the errors object for .NET 400
 */
class ValidationError extends Error {
    constructor(message, errorList) {
        super(message)
        this.errorList = errorList
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
        //return json (unless we got 204 No Content)
        .then(r => r.status == "204" ? r : r.json())
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

        throw new ValidationError(result.errorMessage ? result.errorMessage : response.statusText, result?.errors)
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
            if (typeof error.errorList == Object) {
                for (key in error.errorList) {
                    errorTarget[key] = {
                        error: true,
                        message: error.errorList[key].join("")
                    }
                }
            }
            else {
                for (var i = 0; i < 5 && i < error.errorList.length; i++) {
                    errorTarget.message += " " + error.errorList[i];
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
    this.viperNavMenu = await viperFetch(this, "nav" + qs)
}