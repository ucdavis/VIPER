import type { Ref } from "vue"
import { useQuasar } from "quasar"

/**
 * Downloads one export, returning false when the server had nothing to export. A report whose
 * server narrows its export to the grid takes the keys of the rows the grid shows, in its order;
 * one that exports everything ignores them.
 */
type DownloadExport = (rowKeys?: string[]) => Promise<boolean>

/** A report either opens its PDF in a new tab or downloads it, never both. */
type PdfExport =
    | {
          /** Opens the PDF in a new tab. */
          openPdf: () => void
          downloadPdf?: never
      }
    | {
          /** Downloads the PDF as the workbook is. */
          downloadPdf: DownloadExport
          openPdf?: never
      }

type ReportExports = PdfExport & {
    downloadExcel: DownloadExport
}

type ReportExportOptions = {
    /**
     * Export an empty grid rather than warning there is nothing to export: the server returns a
     * file with headers and no rows, as the legacy app did. Only for a report whose PDF downloads,
     * since one that opens in a tab would leave the reader with a blank one.
     */
    exportWhenEmpty?: boolean
}

/**
 * Export toolbar handlers for a student report or overview grid. Unless the report exports when
 * empty, an empty grid is caught before the PDF export, so a report that opens its PDF in a new
 * tab never leaves the reader with a blank one.
 */
export function useReportExports(
    rows: Ref<unknown[]>,
    exports: ReportExports,
    { exportWhenEmpty = false }: ReportExportOptions = {},
) {
    const $q = useQuasar()

    async function download(fetchFile: DownloadExport, label: string, rowKeys?: string[]): Promise<void> {
        const success = await fetchFile(rowKeys)
        if (success) {
            $q.notify({ type: "positive", message: `${label} report downloaded.` })
        } else {
            $q.notify({ type: "warning", message: "No data to export." })
        }
    }

    async function handleExcelExport(rowKeys?: string[]): Promise<void> {
        await download(exports.downloadExcel, "Excel", rowKeys)
    }

    // Async only for the download. An opening report reaches openPdf before the first await, so
    // the new tab still opens within the click and popup blockers let it through.
    async function handlePdfExport(rowKeys?: string[]): Promise<void> {
        if (!exportWhenEmpty && rows.value.length === 0) {
            $q.notify({ type: "warning", message: "No data to export." })
            return
        }
        if (exports.downloadPdf) {
            await download(exports.downloadPdf, "PDF", rowKeys)
            return
        }
        exports.openPdf()
    }

    return { handleExcelExport, handlePdfExport }
}
