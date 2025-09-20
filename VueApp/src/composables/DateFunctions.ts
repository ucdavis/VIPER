export function useDateFunctions() {
    function formatDate(d: string) {
        if (d == null || d === "") {
            return ""
        }
        d = d.split("T")[0] ?? d
        const dt = new Date(d + "T00:00:00")
        return d && d !== "" && dt instanceof Date && !Number.isNaN(dt.valueOf()) ? dt.toLocaleDateString() : ""
    }

    function formatDateTime(d: string, options: Object) {
        if (d == null || d === "") {
            return ""
        }
        const dt = new Date(d)
        return d && d !== "" && dt instanceof Date && !Number.isNaN(dt.valueOf())
            ? dt.toLocaleString("en-US", options)
            : ""
    }

    function formatDateForDateInput(d: string) {
        if (d == null || d === "") {
            return ""
        }
        d = d.split("T")[0] ?? d
        const dt = new Date(d + "T00:00:00")
        return d && d !== "" && dt instanceof Date && !Number.isNaN(dt.valueOf())
            ? dt.getFullYear() +
                  "-" +
                  ("" + (dt.getMonth() + 1)).padStart(2, "0") +
                  "-" +
                  ("" + dt.getDate()).padStart(2, "0")
            : ""
    }
    return { formatDate, formatDateTime, formatDateForDateInput }
}
