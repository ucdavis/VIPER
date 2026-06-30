import { useFetch } from "../../composables/ViperFetch"
import type { ApiResult } from "./rotation-service"
import type { ViewContext } from "../types"

const { get } = useFetch()

class ClinicianService {
    private static readonly BASE_URL = `${import.meta.env.VITE_API_URL}clinicalscheduler/clinicians`

    /**
     * Build URL with query parameters
     */
    private static buildUrl(baseUrl: string, params: Record<string, string | number | boolean> = {}): string {
        const paramMap = new Map<string, string>()
        for (const [key, value] of Object.entries(params)) {
            if (value !== null) {
                paramMap.set(key, value.toString())
            }
        }
        const queryString = new URLSearchParams([...paramMap]).toString()

        return queryString ? `${baseUrl}?${queryString}` : baseUrl
    }

    /**
     * Get all clinicians
     */
    static async getClinicians(options?: {
        year?: number
        includeAllAffiliates?: boolean
        viewContext?: ViewContext
    }): Promise<ApiResult<Clinician[]>> {
        try {
            const { year, includeAllAffiliates, viewContext } = options || {}

            const params: Record<string, string | number | boolean> = {}
            if (typeof year === "number") {
                params.year = year
            }
            if (typeof includeAllAffiliates === "boolean") {
                params.includeAllAffiliates = includeAllAffiliates
            }
            if (typeof viewContext === "string") {
                params.viewContext = viewContext
            }

            const url = this.buildUrl(this.BASE_URL, params)
            const response = await get(url)

            // Handle both response formats:
            // 1. When includeAllAffiliates=true, backend returns raw array
            // 2. Otherwise, it returns wrapped response with success/result/errors
            if (Array.isArray(response)) {
                // Raw array response - wrap it in ApiResult format
                return {
                    result: response,
                    success: true,
                    errors: [],
                }
            }
            // Already wrapped response
            return response
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }

    /**
     * Get schedule for a specific clinician
     */
    static async getClinicianSchedule(
        mothraId: string,
        options?: { year?: number },
    ): Promise<ApiResult<ClinicianScheduleData>> {
        try {
            const { year } = options || {}

            const params: Record<string, string | number | boolean> = {}
            if (typeof year === "number") {
                params.year = year
            }

            const url = this.buildUrl(`${this.BASE_URL}/${encodeURIComponent(mothraId)}/schedule`, params)
            return await get(url)
        } catch (error) {
            return {
                result: {
                    clinician: null as unknown as ClinicianScheduleData["clinician"],
                    schedulesBySemester: [],
                },
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }
}

interface Clinician {
    mothraId: string
    firstName: string
    lastName: string
    fullName: string
    role?: string | undefined
}

interface ScheduleRotation {
    rotationId: number
    rotationName: string
    name: string
    abbreviation: string
    serviceId: number
    serviceName: string
    scheduleId: number
    isPrimaryEvaluator: boolean
}

interface ScheduleWeek {
    weekId: number
    weekNumber: number
    dateStart: string
    dateEnd: string
    rotations: ScheduleRotation[]
    requiresPrimaryEvaluator?: boolean
}

interface ScheduleBySemester {
    semester: string
    weeks: ScheduleWeek[]
}

interface ClinicianScheduleData {
    clinician:
        | {
              mothraId: string
              firstName: string
              lastName: string
              fullName: string
              role?: string | undefined
          }
        | undefined
    schedulesBySemester: ScheduleBySemester[]
}

export { ClinicianService, type Clinician, type ClinicianScheduleData }
