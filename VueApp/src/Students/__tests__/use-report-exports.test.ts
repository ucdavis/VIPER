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
    const rows = ref(Array.from({ length: rowCount }, (_, i) => ({ id: i })))
    const downloadExcel = vi.fn<() => Promise<boolean>>().mockResolvedValue(downloadResult)
    const openPdf = vi.fn<() => void>()
    return {
        rows,
        downloadExcel,
        openPdf,
        ...useReportExports(rows, { downloadExcel, openPdf }),
    }
}

describe("excel export", () => {
    it("confirms the download when the workbook arrives", async () => {
        expect.hasAssertions()
        const { handleExcelExport, downloadExcel } = setup(2)

        await handleExcelExport()

        // No keys: a report that does not narrow its exports to the grid exports everything.
        expect(downloadExcel).toHaveBeenCalledWith(undefined)
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
    it("opens the pdf when there are rows", async () => {
        expect.hasAssertions()
        const { handlePdfExport, openPdf } = setup(2)

        await handlePdfExport()

        expect(openPdf).toHaveBeenCalledWith()
        expect(mockNotify).not.toHaveBeenCalled()
    })

    it("warns rather than opening a blank tab when the grid is empty", async () => {
        expect.hasAssertions()
        // The PDF opens in a new tab, so an empty grid has to be caught before opening it.
        const { handlePdfExport, openPdf } = setup(0)

        await handlePdfExport()

        expect(openPdf).not.toHaveBeenCalled()
        expect(mockNotify).toHaveBeenCalledWith({ type: "warning", message: "No data to export." })
    })

    it("follows the grid as its rows change", async () => {
        expect.hasAssertions()
        const { rows, handlePdfExport, openPdf } = setup(0)

        rows.value = [{ id: 1 }]
        await handlePdfExport()

        expect(openPdf).toHaveBeenCalledWith()
    })
})

describe("pdf download", () => {
    /** A report that downloads its PDF rather than opening it, as career selection does. */
    function setupDownload(rowCount: number, downloadResult = true) {
        const rows = ref(Array.from({ length: rowCount }, (_, i) => ({ id: i })))
        const downloadPdf = vi.fn<() => Promise<boolean>>().mockResolvedValue(downloadResult)
        const downloadExcel = vi.fn<() => Promise<boolean>>().mockResolvedValue(true)
        return { downloadPdf, ...useReportExports(rows, { downloadExcel, downloadPdf }) }
    }

    it("confirms the download when the pdf arrives", async () => {
        expect.hasAssertions()
        const { handlePdfExport, downloadPdf } = setupDownload(2)

        await handlePdfExport()

        expect(downloadPdf).toHaveBeenCalledWith(undefined)
        expect(mockNotify).toHaveBeenCalledWith({ type: "positive", message: "PDF report downloaded." })
    })

    it("warns instead when the server had nothing to export", async () => {
        expect.hasAssertions()
        const { handlePdfExport } = setupDownload(2, false)

        await handlePdfExport()

        expect(mockNotify).toHaveBeenCalledWith({ type: "warning", message: "No data to export." })
    })

    it("warns without a request when the grid is empty", async () => {
        expect.hasAssertions()
        const { handlePdfExport, downloadPdf } = setupDownload(0)

        await handlePdfExport()

        expect(downloadPdf).not.toHaveBeenCalled()
        expect(mockNotify).toHaveBeenCalledWith({ type: "warning", message: "No data to export." })
    })
})

describe("grid-narrowed exports", () => {
    /** A report that sends the grid's rows and exports even when empty, as career selection does. */
    function setupGrid(rowCount: number) {
        const rows = ref(Array.from({ length: rowCount }, (_, i) => ({ id: i })))
        const downloadExcel = vi.fn<(rowKeys?: string[]) => Promise<boolean>>().mockResolvedValue(true)
        const downloadPdf = vi.fn<(rowKeys?: string[]) => Promise<boolean>>().mockResolvedValue(true)
        return {
            downloadExcel,
            downloadPdf,
            ...useReportExports(rows, { downloadExcel, downloadPdf }, { exportWhenEmpty: true }),
        }
    }

    it("passes the grid's row keys through to each download", async () => {
        expect.hasAssertions()
        const { handleExcelExport, handlePdfExport, downloadExcel, downloadPdf } = setupGrid(2)

        await handleExcelExport(["2", "1"])
        await handlePdfExport(["2"])

        expect(downloadExcel).toHaveBeenCalledWith(["2", "1"])
        expect(downloadPdf).toHaveBeenCalledWith(["2"])
    })

    it("exports an empty grid rather than warning, for a headers-only file", async () => {
        expect.hasAssertions()
        const { handlePdfExport, downloadPdf } = setupGrid(0)

        await handlePdfExport([])

        expect(downloadPdf).toHaveBeenCalledWith([])
        expect(mockNotify).toHaveBeenCalledWith({ type: "positive", message: "PDF report downloaded." })
    })
})
