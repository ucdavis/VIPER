import { useFetch } from '../../composables/ViperFetch'
import type { ApiResult } from './RotationService'

const { get } = useFetch()

export interface Clinician {
    mothraId: string
    firstName: string
    lastName: string
    fullName: string
    role?: string | null
}

export interface ClinicianRotationItem {
    instructorScheduleId: number
    rotId: number
    rotationName: string
    rotationAbbreviation: string
    serviceName: string
    serviceShortName: string
    evaluator: boolean
    dateStart: string
    dateEnd: string
    week: {
        weekId: number
        dateStart: string
        dateEnd: string
        termCode: number
        weekNumber: number
    }
}

export interface ScheduleRotation {
    rotationId: number
    rotationName: string
    abbreviation: string
    serviceId: number
    serviceName: string
    scheduleId: number
    isPrimaryEvaluator: boolean
}

export interface ScheduleWeek {
    weekId: number
    weekNumber: number
    dateStart: string
    dateEnd: string
    rotations: ScheduleRotation[]
}

export interface ScheduleBySemester {
    semester: string
    weeks: ScheduleWeek[]
}

export interface ClinicianScheduleData {
    clinician: {
        mothraId: string
        firstName: string
        lastName: string
        fullName: string
        role?: string | null
    } | null
    schedulesBySemester: ScheduleBySemester[]
}

export interface ClinicianRotationSummary {
    mothraId: string
    rotations: {
        rotId: number
        name: string
        abbreviation: string
        serviceName: string
        serviceShortName: string
        assignmentCount: number
        isPrimaryEvaluator: boolean
    }[]
}

export class ClinicianService {
    private static readonly BASE_URL = import.meta.env.VITE_API_URL + 'clinicalscheduler/clinicians'

    /**
     * Build URL with query parameters
     */
    private static buildUrl(baseUrl: string, params: Record<string, any> = {}): string {
        const paramMap = new Map<string, string>()
        Object.entries(params).forEach(([key, value]) => {
            if (value !== undefined && value !== null) {
                paramMap.set(key, value.toString())
            }
        })
        const queryString = new URLSearchParams(Array.from(paramMap)).toString()

        return queryString ? `${baseUrl}?${queryString}` : baseUrl
    }

    /**
     * Get all clinicians
     */
    static async getClinicians(options?: { year?: number; includeAllAffiliates?: boolean }): Promise<ApiResult<Clinician[]>> {
        try {
            const { year, includeAllAffiliates } = options || {}

            const params: Record<string, any> = {}
            if (year !== undefined) {
                params.year = year
            }
            if (includeAllAffiliates !== undefined) {
                params.includeAllAffiliates = includeAllAffiliates
            }

            const url = this.buildUrl(this.BASE_URL, params)
            return await get(url)
        } catch (error) {
            console.error('Error fetching clinicians:', error)
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
            }
        }
    }

    /**
     * Get schedule for a specific clinician
     */
    static async getClinicianSchedule(mothraId: string, options?: { year?: number }): Promise<ApiResult<ClinicianScheduleData>> {
        try {
            const { year } = options || {}

            const params: Record<string, any> = {}
            if (year !== undefined) {
                params.year = year
            }

            const url = this.buildUrl(`${this.BASE_URL}/${encodeURIComponent(mothraId)}/schedule`, params)
            return await get(url)
        } catch (error) {
            console.error(`Error fetching clinician schedule for ${mothraId}:`, error)
            return {
                result: {
                    clinician: null,
                    schedulesBySemester: []
                },
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
            }
        }
    }

    /**
     * Get rotations for a specific clinician
     */
    static async getClinicianRotations(mothraId: string): Promise<ApiResult<ClinicianRotationSummary>> {
        try {
            const url = `${this.BASE_URL}/${encodeURIComponent(mothraId)}/rotations`
            return await get(url)
        } catch (error) {
            console.error(`Error fetching clinician rotations for ${mothraId}:`, error)
            return {
                result: {
                    mothraId: mothraId,
                    rotations: []
                },
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
            }
        }
    }

    /**
     * Search clinicians by name
     */
    static async searchClinicians(query: string, options?: { includeAllAffiliates?: boolean }): Promise<ApiResult<Clinician[]>> {
        try {
            const cliniciansResult = await this.getClinicians(options)
            if (!cliniciansResult.success) {
                return cliniciansResult
            }

            const searchTerm = query.toLowerCase().trim()
            if (!searchTerm) {
                return cliniciansResult
            }

            const filtered = cliniciansResult.result.filter(clinician =>
                clinician.fullName.toLowerCase().includes(searchTerm) ||
                clinician.firstName.toLowerCase().includes(searchTerm) ||
                clinician.lastName.toLowerCase().includes(searchTerm) ||
                clinician.mothraId.toLowerCase().includes(searchTerm)
            )

            return {
                result: filtered,
                success: true,
                errors: []
            }
        } catch (error) {
            console.error('Error searching clinicians:', error)
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
            }
        }
    }
}