import { ref } from "vue"
import { useReportExports } from "../composables/use-report-exports"

/**
 * Tests for useReportExports, the export toolbar handlers shared by the student roster and
 * report pages.
 */

const mockNotify = vi.fn<(...args: unknown[]) => unknown>()

vi.mock("quasar", () => ({
    useQuasar: () => ({ notify: (...args: unknown[]) => mockNotify(...args) }),
}))

function setup(rowCount: number, downloadResult = true) {
    vi.clearAllMocks()
    const rows = ref(Array.from({ length: rowCount }, (_, i) => ({ id: i })))
    const downloadExcel = vi.fn<() => Promise<boolean>>().mockResolvedValue(downloadResult)
    const downloadCsv = vi.fn<() => Promise<boolean>>().mockResolvedValue(downloadResult)
    const openPdf = vi.fn<() => void>()
    return {
        rows,
        downloadExcel,
        downloadCsv,
        openPdf,
        ...useReportExports(rows, { downloadExcel, openPdf, downloadCsv }),
    }
}

/** A report with no CSV endpoint, as the emergency contact pages are today. */
function setupWithoutCsv() {
    vi.clearAllMocks()
    const rows = ref([{ id: 1 }])
    const downloadExcel = vi.fn<() => Promise<boolean>>().mockResolvedValue(true)
    return useReportExports(rows, { downloadExcel, openPdf: vi.fn<() => void>() })
}

describe("excel export", () => {
    it("confirms the download when the workbook arrives", async () => {
        expect.hasAssertions()
        const { handleExcelExport, downloadExcel } = setup(2)

        await handleExcelExport()

        expect(downloadExcel).toHaveBeenCalledWith()
        expect(mockNotify).toHaveBeenCalledWith({ type: "positive", message: "Excel report downloaded." })
    })

    it("warns instead when the server had nothing to export", async () => {
        expect.hasAssertions()
        const { handleExcelExport } = setup(2, false)

        await handleExcelExport()

        expect(mockNotify).toHaveBeenCalledWith({ type: "warning", message: "No data to export." })
    })
})

describe("pdf export", () => {
    it("opens the pdf when there are rows", () => {
        expect.hasAssertions()
        const { handlePdfExport, openPdf } = setup(2)

        handlePdfExport()

        expect(openPdf).toHaveBeenCalledWith()
        expect(mockNotify).not.toHaveBeenCalled()
    })

    it("warns rather than opening a blank tab when the grid is empty", () => {
        expect.hasAssertions()
        // The PDF opens in a new tab, so an empty grid has to be caught before opening it.
        const { handlePdfExport, openPdf } = setup(0)

        handlePdfExport()

        expect(openPdf).not.toHaveBeenCalled()
        expect(mockNotify).toHaveBeenCalledWith({ type: "warning", message: "No data to export." })
    })

    it("follows the grid as its rows change", () => {
        expect.hasAssertions()
        const { rows, handlePdfExport, openPdf } = setup(0)

        rows.value = [{ id: 1 }]
        handlePdfExport()

        expect(openPdf).toHaveBeenCalledWith()
    })
})

describe("csv export", () => {
    it("confirms the download when the file arrives", async () => {
        expect.hasAssertions()
        const { handleCsvExport, downloadCsv } = setup(2)

        await handleCsvExport!()

        expect(downloadCsv).toHaveBeenCalledWith()
        expect(mockNotify).toHaveBeenCalledWith({ type: "positive", message: "CSV report downloaded." })
    })

    it("warns instead when the server had nothing to export", async () => {
        expect.hasAssertions()
        const { handleCsvExport } = setup(2, false)

        await handleCsvExport!()

        expect(mockNotify).toHaveBeenCalledWith({ type: "warning", message: "No data to export." })
    })

    it("hands back no handler for a report that serves no csv, so the toolbar hides the button", () => {
        expect.hasAssertions()
        expect(setupWithoutCsv().handleCsvExport).toBeUndefined()
    })
})
