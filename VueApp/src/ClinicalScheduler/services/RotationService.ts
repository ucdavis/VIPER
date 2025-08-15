import { useFetch } from '../../composables/ViperFetch'
import type { Rotation, Service } from '../types'

const { get } = useFetch()

export interface RotationWithService extends Rotation {
    service?: Service
}

export interface RotationSummary {
    totalRotations: number
    serviceCount: number
    services: {
        serviceId: number
        serviceName: string
        shortName: string
        rotationCount: number
        rotations: {
            rotId: number
            name: string
            abbreviation: string
        }[]
    }[]
}

export interface ApiResult<T> {
    result: T
    success: boolean
    errors: string[]
}

export interface InstructorScheduleItem {
    instructorScheduleId: number
    firstName: string
    lastName: string
    fullName: string
    mothraId: string
    evaluator: boolean
    dateStart: string
    dateEnd: string
    week: {
        weekId: number
        dateStart: string
        dateEnd: string
        termCode: number
    }
}

export interface WeekItem {
    weekId: number
    dateStart: string
    dateEnd: string
    termCode: number
    weekNumber: number
    requiresPrimaryEvaluator: boolean
}

export interface RotationScheduleData {
    rotation: {
        rotId: number
        name: string
        abbreviation: string
        service: {
            serviceId: number
            serviceName: string
            shortName: string
        }
    } | null
    year: number
    weeks: WeekItem[]
    instructorSchedules: InstructorScheduleItem[]
}

export class RotationService {
    private static readonly BASE_URL = '/api/clinicalscheduler/rotations'

    /**
     * Build URL with query parameters
     */
    private static buildUrl(baseUrl: string, params: Record<string, any> = {}): string {
        const queryString = new URLSearchParams(
            Object.entries(params).reduce((acc, [key, value]) => {
                if (value !== undefined && value !== null) {
                    acc[key] = value.toString()
                }
                return acc
            }, {} as Record<string, string>)
        ).toString()

        return queryString ? `${baseUrl}?${queryString}` : baseUrl
    }

    /**
     * Get all rotations with optional filtering
     */
    static async getRotations(options?: { serviceId?: number; includeService?: boolean }): Promise<ApiResult<RotationWithService[]>> {
        try {
            const { serviceId, includeService = true } = options || {}

            const params: Record<string, any> = { includeService }
            if (serviceId !== undefined) {
                params.serviceId = serviceId
            }

            const url = this.buildUrl(this.BASE_URL, params)
            return await get(url)
        } catch (error) {
            console.error('Error fetching rotations:', error)
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
            }
        }
    }

    /**
     * Get rotations that have scheduled weeks for a specific year
     */
    static async getRotationsWithScheduledWeeks(options?: { year?: number; includeService?: boolean }): Promise<ApiResult<RotationWithService[]>> {
        try {
            const { year, includeService = true } = options || {}

            const params: Record<string, any> = { includeService }
            if (year !== undefined) {
                params.year = year
            }

            const url = this.buildUrl(`${this.BASE_URL}/with-scheduled-weeks`, params)
            return await get(url)
        } catch (error) {
            console.error('Error fetching rotations with scheduled weeks:', error)
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
            }
        }
    }

    /**
     * Get a single rotation by ID
     */
    static async getRotation(id: number, includeService: boolean = true): Promise<ApiResult<RotationWithService | null>> {
        try {
            const params = includeService ? '?includeService=true' : '?includeService=false'
            const url = `${this.BASE_URL}/${id}${params}`
            return await get(url)
        } catch (error) {
            console.error(`Error fetching rotation ${id}:`, error)
            return {
                result: null,
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
            }
        }
    }

    /**
     * Get rotation summary grouped by service
     */
    static async getRotationSummary(): Promise<ApiResult<RotationSummary>> {
        try {
            const url = `${this.BASE_URL}/summary`
            return await get(url)
        } catch (error) {
            console.error('Error fetching rotation summary:', error)
            return {
                result: {
                    totalRotations: 0,
                    serviceCount: 0,
                    services: []
                },
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
            }
        }
    }

    /**
     * Get instructor schedules for a specific rotation
     */
    static async getRotationSchedule(rotationId: number, options?: { year?: number }): Promise<ApiResult<RotationScheduleData>> {
        try {
            const { year } = options || {}

            const params: Record<string, any> = {}
            if (year !== undefined) {
                params.year = year
            }

            const url = this.buildUrl(`${this.BASE_URL}/${rotationId}/schedule`, params)
            return await get(url)
        } catch (error) {
            console.error(`Error fetching rotation schedule for rotation ${rotationId}:`, error)
            return {
                result: {
                    rotation: null,
                    year: 0,
                    weeks: [],
                    instructorSchedules: []
                },
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
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
                    errors: summaryResult.errors
                }
            }

            const services: Service[] = summaryResult.result.services.map(s => ({
                serviceId: s.serviceId,
                serviceName: s.serviceName,
                shortName: s.shortName
            }))

            return {
                result: services,
                success: true,
                errors: []
            }
        } catch (error) {
            console.error('Error extracting services:', error)
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : 'Unknown error occurred']
            }
        }
    }
}
