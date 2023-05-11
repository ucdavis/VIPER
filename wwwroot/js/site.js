// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
/*
    Returns a date formatted to yyyy-mm-dd
    If the date is not valid, returns empty string
*/
formatDateForDateInput = function (d) {
    var dt = new Date(d)
    return (d && d != "" && dt instanceof Date && !isNaN(dt.valueOf()))
        ? (dt.getFullYear() + "-" + ("" + (dt.getMonth() + 1)).padStart(2, "0") + "-" + ("" + (dt.getDate())).padStart(2, "0"))
        : "";
}
