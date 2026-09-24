import type { Ref } from "vue"
import { useQuasar } from "quasar"

type ReportExports = {
    /** Downloads the workbook, returning false when the server had nothing to export. */
    downloadExcel: () => Promise<boolean>
    /** Opens the PDF in a new tab. */
    openPdf: () => void
    /** Downloads the CSV. Left out by a report that serves no CSV, which then shows no CSV button. */
    downloadCsv?: () => Promise<boolean>
}

/**
 * Export toolbar handlers for a student report or overview grid. The PDF opens in a new tab, so
 * an empty grid is caught here rather than leaving the reader with a blank tab.
 */
export function useReportExports(rows: Ref<unknown[]>, exports: ReportExports) {
    const $q = useQuasar()

    async function download(fetchFile: () => Promise<boolean>, label: string): Promise<void> {
        const success = await fetchFile()
        if (success) {
            $q.notify({ type: "positive", message: `${label} report downloaded.` })
        } else {
            $q.notify({ type: "warning", message: "No data to export." })
        }
    }

    async function handleExcelExport(): Promise<void> {
        await download(exports.downloadExcel, "Excel")
    }

    const { downloadCsv } = exports
    const handleCsvExport = downloadCsv === undefined ? undefined : () => download(downloadCsv, "CSV")

    function handlePdfExport(): void {
        if (rows.value.length === 0) {
            $q.notify({ type: "warning", message: "No data to export." })
            return
        }
        exports.openPdf()
    }

    return { handleExcelExport, handlePdfExport, handleCsvExport }
}
