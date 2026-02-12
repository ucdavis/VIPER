import { useFetch } from "@/composables/ViperFetch"
import type { PercentRolloverPreviewDto } from "../types"

const { get } = useFetch()

/** Zero-based month index for June */
const JUNE = 5

function getRolloverUrl(year: number) {
    return `${import.meta.env.VITE_API_URL}effort/rollover/${year}`
}

/**
 * Service for Percent Rollover API calls.
 */
export const rolloverService = {
    /**
     * Auto-detect the boundary year from the current date.
     */
    getDefaultYear(): number {
        const now = new Date()
        const year = now.getFullYear()
        // Before June, the last relevant boundary was July of the previous year
        return now.getMonth() < JUNE ? year - 1 : year
    },

    /**
     * Get a preview of percent assignments to rollover.
     */
    async getPreview(year: number): Promise<PercentRolloverPreviewDto | null> {
        const response = await get(`${getRolloverUrl(year)}/preview`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as PercentRolloverPreviewDto
    },

    /**
     * Get the SSE stream URL for rollover with real-time progress.
     * Use with EventSource to receive progress updates.
     */
    getStreamUrl(year: number): string {
        return `${getRolloverUrl(year)}/stream`
    },
}
