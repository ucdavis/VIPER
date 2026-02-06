import { useFetch } from "@/composables/ViperFetch"
import type { PercentRolloverPreviewDto } from "../types"

const { get } = useFetch()

function getRolloverUrl(termCode: number) {
    return `${import.meta.env.VITE_API_URL}effort/terms/${termCode}/rollover`
}

/**
 * Service for Percent Rollover API calls.
 */
export const rolloverService = {
    /**
     * Get a preview of percent assignments to rollover.
     */
    async getPreview(termCode: number): Promise<PercentRolloverPreviewDto | null> {
        const response = await get(`${getRolloverUrl(termCode)}/preview`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as PercentRolloverPreviewDto
    },

    /**
     * Get the SSE stream URL for rollover with real-time progress.
     * Use with EventSource to receive progress updates.
     */
    getStreamUrl(termCode: number): string {
        return `${getRolloverUrl(termCode)}/stream`
    },
}
