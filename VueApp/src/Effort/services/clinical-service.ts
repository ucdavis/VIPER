import { useFetch } from "@/composables/ViperFetch"
import type { ClinicalImportPreviewDto, ClinicalImportMode } from "../types"

const { get } = useFetch()

function getClinicalUrl(termCode: number) {
    return `${import.meta.env.VITE_API_URL}effort/terms/${termCode}/clinical`
}

/**
 * Service for Clinical Import API calls.
 */
export const clinicalService = {
    /**
     * Get a preview of clinical assignments to import based on the selected mode.
     */
    async getPreview(termCode: number, mode: ClinicalImportMode): Promise<ClinicalImportPreviewDto | null> {
        const response = await get(`${getClinicalUrl(termCode)}/preview?mode=${mode}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as ClinicalImportPreviewDto
    },

    /**
     * Get the SSE stream URL for clinical import with real-time progress.
     * Use with EventSource to receive progress updates.
     */
    getStreamUrl(termCode: number, mode: ClinicalImportMode): string {
        return `${getClinicalUrl(termCode)}/stream?mode=${mode}`
    },
}
