import { useFetch } from "../../composables/ViperFetch"
import type { RotationWithService, RotationScheduleData } from "../types/rotation-types"
import type { ApiResult } from "../types/api"

class RotationService {
    private static readonly BASE_URL = `${import.meta.env.VITE_API_URL}clinicalscheduler/rotations`

    /**
     * Build URL with query parameters
     */
    private static buildUrl(baseUrl: string, params: Record<string, string | number | boolean> = {}): string {
        const validParams: Record<string, string> = {}

        for (const [key, value] of Object.entries(params)) {
            if (value !== null && typeof key === "string" && key.length > 0) {
                Object.assign(validParams, { [key]: value.toString() })
            }
        }

        const queryString = new URLSearchParams(validParams).toString()
        return queryString ? `${baseUrl}?${queryString}` : baseUrl
    }

    /**
     * Get all rotations with optional filtering
     */
    static async getRotations(options?: {
        serviceId?: number
        clinicianMothraId?: string
    }): Promise<ApiResult<RotationWithService[]>> {
        try {
            const { serviceId, clinicianMothraId } = options || {}

            const params: Record<string, string | number | boolean> = {}
            if (typeof serviceId === "number") {
                params.serviceId = serviceId
            }
            if (clinicianMothraId) {
                params.clinicianMothraId = clinicianMothraId
            }

            const url = this.buildUrl(this.BASE_URL, params)
            const { get } = useFetch()
            return await get(url)
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }

    /**
     * Get rotations that have scheduled weeks for a specific year
     */
    static async getRotationsWithScheduledWeeks(options?: {
        year?: number
        clinicianMothraId?: string
    }): Promise<ApiResult<RotationWithService[]>> {
        try {
            const { year, clinicianMothraId } = options || {}

            const params: Record<string, string | number | boolean> = {}
            if (typeof year === "number") {
                params.year = year
            }
            if (clinicianMothraId) {
                params.clinicianMothraId = clinicianMothraId
            }

            const url = this.buildUrl(`${this.BASE_URL}/with-scheduled-weeks`, params)
            const { get } = useFetch()
            return await get(url)
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }

    /**
     * Get instructor schedules for a specific rotation
     */
    static async getRotationSchedule(
        rotationId: number,
        options?: { year?: number },
    ): Promise<ApiResult<RotationScheduleData>> {
        try {
            const { year } = options || {}

            const params: Record<string, string | number | boolean> = {}
            if (typeof year === "number") {
                params.year = year
            }

            const url = this.buildUrl(`${this.BASE_URL}/${rotationId}/schedule`, params)
            const { get } = useFetch()
            return await get(url)
        } catch (error) {
            return {
                result: {
                    rotation: null as unknown as RotationScheduleData["rotation"],
                    gradYear: 0,
                    schedulesBySemester: [],
                },
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }
}

export { RotationService }
export type { ApiResult, RotationScheduleData }
