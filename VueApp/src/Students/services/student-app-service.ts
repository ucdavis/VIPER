import { useFetch, postForBlob, downloadBlob } from "@/composables/ViperFetch"

// Every member below is called from the pages, but always through a subclass instance
// (`careerSelectionService.downloadExcel`, `emergencyContactService.getAccessStatus`). fallow
// resolves those against the subclass without linking them back to the declaration on this
// abstract base, so it reports the whole shared surface as unused. That surface is all this
// file holds, which leaves the rule nothing useful to say about it.
// fallow-ignore-file unused-class-member

/** Filenames to save the exports under when the response does not name the file. */
type StudentAppExportFilenames = {
    overviewExcel: string
    excel: string
}

/**
 * The endpoints every student self-service app (emergency contacts, career selection) serves under
 * its own base URL: app-wide student access, and the overview and full-detail exports.
 */
abstract class StudentAppService<TAccessStatus> {
    protected readonly baseUrl: string
    private readonly exportFilenames: StudentAppExportFilenames

    /** @param apiPath The app's path below the API root, e.g. "students/career-selection". */
    constructor(apiPath: string, exportFilenames: StudentAppExportFilenames) {
        this.baseUrl = `${import.meta.env.VITE_API_URL}${apiPath}`
        this.exportFilenames = exportFilenames
    }

    /**
     * Returns null on a failed request. The result is checked against null rather than for
     * truthiness, because an app whose status is a bare boolean reports "closed" as false.
     */
    getAccessStatus = async (): Promise<TAccessStatus | null> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/access/status`)
        if (!response.success || response.result === null || response.result === undefined) {
            return null
        }
        return response.result as TAccessStatus
    }

    /** Returns whether the app is now open, or null on a failed request. */
    toggleAppAccess = async (): Promise<boolean | null> => {
        const { post } = useFetch()
        const response = await post(`${this.baseUrl}/access/toggle-app`)
        if (!response.success || response.result === null || response.result === undefined) {
            return null
        }
        return response.result as boolean
    }

    downloadOverviewExcel = (): Promise<boolean> =>
        this.downloadExportFile("export/overview/excel", this.exportFilenames.overviewExcel)

    openOverviewPdf = (): void => {
        globalThis.open(`${this.baseUrl}/export/overview/pdf`, "_blank", "noopener")
    }

    downloadExcel = (): Promise<boolean> => this.downloadExportFile("export/excel", this.exportFilenames.excel)

    openPdf = (): void => {
        globalThis.open(`${this.baseUrl}/export/pdf`, "_blank", "noopener")
    }

    /** POSTs for an export file and saves it, or returns false when the server had nothing to export. */
    protected async downloadExportFile(path: string, fallbackFilename: string): Promise<boolean> {
        const { blob, filename } = await postForBlob(`${this.baseUrl}/${path}`, {})
        if (blob.size === 0) {
            return false
        }
        downloadBlob(blob, filename ?? fallbackFilename)
        return true
    }
}

export { StudentAppService }
