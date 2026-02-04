function formatDate(d: string) {
    if (d === null || d === "") {
        return ""
    }
    const dateStr = d.split("T")[0] ?? d
    const dt = new Date(`${dateStr}T00:00:00`)
    return dateStr && dateStr !== "" && dt instanceof Date && !Number.isNaN(dt.valueOf()) ? dt.toLocaleDateString() : ""
}

function formatDateTime(d: string, options: object) {
    if (d === null || d === "") {
        return ""
    }
    const dt = new Date(d)
    return d && d !== "" && dt instanceof Date && !Number.isNaN(dt.valueOf()) ? dt.toLocaleString("en-US", options) : ""
}

function formatDateForDateInput(d: string) {
    if (d === null || d === "") {
        return ""
    }
    const dateStr = d.split("T")[0] ?? d
    const dt = new Date(`${dateStr}T00:00:00`)
    return dateStr && dateStr !== "" && dt instanceof Date && !Number.isNaN(dt.valueOf())
        ? `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, "0")}-${String(dt.getDate()).padStart(2, "0")}`
        : ""
}

function useDateFunctions() {
    return { formatDate, formatDateTime, formatDateForDateInput }
}

export { formatDate, formatDateTime, formatDateForDateInput, useDateFunctions }
