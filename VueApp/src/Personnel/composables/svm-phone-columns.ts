import type { QTableProps } from "quasar"

/**
 * Columns for the frequently called numbers table.
 *
 * Kept out of the component so the page can ask what a search matches without restating the
 * fields, and out of svm-data-fetch so that tests mocking the fetch layer do not have to stub a
 * column definition that has nothing to do with fetching.
 */
function buildFrequentNumberColumns(isEdit: boolean): QTableProps["columns"] {
    const cols: QTableProps["columns"] = [
        { name: "label", label: "Location", field: "label", align: "left", sortable: true },
        { name: "phone", label: "Phone", field: "phone", align: "left" },
    ]
    if (isEdit) {
        cols.push(
            { name: "edit", label: "Edit", field: "edit", align: "left" },
            { name: "delete", label: "Delete", field: "delete", align: "left" },
        )
    }
    return cols
}

export { buildFrequentNumberColumns }
