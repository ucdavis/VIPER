import { useQuasar, exportFile } from "quasar"
import type { QTableProps } from "quasar"

function exportTable(columns: QTableProps["columns"], rows: any[], excludeFromExport: string[] | null = null) {
    if (columns === undefined) {
        return
    }
    // naive encoding to csv format
    const columnsMinusExcludes = columns.filter(
        (c) => excludeFromExport === null || !excludeFromExport.some((e) => e === c.name),
    )
    const content = [
        columnsMinusExcludes.map((col) => wrapCsvValue(col.label)),
        ...rows.map((row) =>
            columnsMinusExcludes
                .map((col) =>
                    wrapCsvValue(
                        typeof col.field === "function"
                            ? col.field(row)
                            : row[col.field === undefined ? col.name : col.field],
                        col.format,
                        row,
                    ),
                )
                .join(","),
        ),
    ].join("\r\n")

    //const { useQuasar, exportFile } = Quasar
    const $q = useQuasar()

    const status = exportFile("table-export.csv", content, "text/csv")

    if (status !== true) {
        $q.notify({
            message: "Browser denied file download...",
            color: "negative",
            icon: "warning",
        })
    }
}

//Escape double quotes (" -> "") and new lines in the content
function wrapCsvValue(val: string, formatFn: any | null = null, row: any = null) {
    let formatted = formatFn !== null && formatFn !== undefined ? formatFn(val, row) : val

    formatted = formatted === undefined || formatted === null ? "" : String(formatted)

    formatted = formatted
        .split('"')
        .join('""')
        /**
         * Excel accepts \n and \r in strings, but some other CSV parsers do not
         * Uncomment the next two lines to escape new lines
         */
        .split("\n")
        .join(String.raw`\n`)
        .split("\r")
        .join(String.raw`\r`)

    return `"${formatted}"`
}

export { exportTable }
