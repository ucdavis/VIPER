import { useFetch } from "@/composables/ViperFetch"
import type { HarvestPreviewDto, HarvestResultDto } from "../types"

const { get, post } = useFetch()

function getHarvestUrl(termCode: number) {
    return `${import.meta.env.VITE_API_URL}effort/terms/${termCode}/harvest`
}

/**
 * Progress event from SSE stream.
 */
export type HarvestProgressEvent = {
    type: "progress" | "complete" | "error"
    phase: string
    progress: number
    message: string
    detail?: string
    result?: HarvestResultDto
    error?: string
}

/**
 * Service for Harvest API calls.
 */
export const harvestService = {
    /**
     * Get a preview of harvest data without saving.
     */
    async getPreview(termCode: number): Promise<HarvestPreviewDto | null> {
        const response = await get(`${getHarvestUrl(termCode)}/preview`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as HarvestPreviewDto
    },

    /**
     * Commit the harvest - clear existing data and import all phases.
     * @deprecated Use streamHarvest for real-time progress updates.
     */
    async commitHarvest(termCode: number): Promise<HarvestResultDto | null> {
        const response = await post(`${getHarvestUrl(termCode)}/commit`, {})
        if (!response.success || !response.result) {
            return null
        }
        return response.result as HarvestResultDto
    },

    /**
     * Get the SSE stream URL for harvest with real-time progress.
     * Use with EventSource to receive progress updates.
     */
    getStreamUrl(termCode: number): string {
        return `${getHarvestUrl(termCode)}/stream`
    },
}
