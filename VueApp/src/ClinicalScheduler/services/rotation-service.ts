import { useFetch } from "../../composables/ViperFetch"
import type { RotationWithService, RotationSummary, RotationScheduleData } from "../types/rotation-types"
import type { ApiResult } from "../types/api"
import type { Service } from "../types"

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
        includeService?: boolean
    }): Promise<ApiResult<RotationWithService[]>> {
        try {
            const { serviceId, includeService = true } = options || {}

            const params: Record<string, string | number | boolean> = { includeService }
            if (typeof serviceId === "number") {
                params.serviceId = serviceId
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
        includeService?: boolean
    }): Promise<ApiResult<RotationWithService[]>> {
        try {
            const { year, includeService = true } = options || {}

            const params: Record<string, string | number | boolean> = { includeService }
            if (typeof year === "number") {
                params.year = year
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
     * Get a single rotation by ID
     */
    static async getRotation(
        id: number,
        includeService: boolean = true,
    ): Promise<ApiResult<RotationWithService | null>> {
        try {
            const params = includeService ? "?includeService=true" : "?includeService=false"
            const url = `${this.BASE_URL}/${id}${params}`
            const { get } = useFetch()
            return await get(url)
        } catch (error) {
            return {
                result: null,
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }

    /**
     * Get rotation summary grouped by service
     */
    static async getRotationSummary(): Promise<ApiResult<RotationSummary>> {
        try {
            const url = `${this.BASE_URL}/summary`
            const { get } = useFetch()
            return await get(url)
        } catch (error) {
            return {
                result: {
                    totalRotations: 0,
                    serviceCount: 0,
                    services: [],
                },
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

    /**
     * Get all services (convenience method that extracts services from rotation summary)
     */
    static async getServices(): Promise<ApiResult<Service[]>> {
        try {
            const summaryResult = await this.getRotationSummary()
            if (!summaryResult.success) {
                return {
                    result: [],
                    success: false,
                    errors: summaryResult.errors,
                }
            }

            const services: Service[] = summaryResult.result.services.map((s) => ({
                serviceId: s.serviceId,
                serviceName: s.serviceName,
                shortName: s.shortName,
            }))

            return {
                result: services,
                success: true,
                errors: [],
            }
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }
}

export { RotationService }
export type { ApiResult, RotationScheduleData }
