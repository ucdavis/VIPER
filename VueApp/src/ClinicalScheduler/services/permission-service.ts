import { useFetch } from "../../composables/ViperFetch"
import type {
    UserPermissions,
    ServicePermissionCheck,
    RotationPermissionCheck,
    InstructorSchedulePermissionCheck,
} from "../types"

const { get } = useFetch()

/**
 * Custom error types for permission operations
 */
type PermissionErrorType = "validation" | "api" | "network"

interface PermissionErrorOptions {
    statusCode?: number
    cause?: Error
}

/**
 * Service for managing user permissions and access control in Clinical Scheduler
 */
class PermissionService {
    private static readonly BASE_URL = `${import.meta.env.VITE_API_URL}clinicalscheduler/permissions`

    /**
     * Create a permission error
     */
    private static createError(message: string, type: PermissionErrorType, options?: PermissionErrorOptions): Error {
        const error = new Error(message)
        error.name = "PermissionError"
        Object.assign(error, { errorType: type, ...options })
        return error
    }

    /**
     * Create validation error
     */
    private static validationError(message: string): Error {
        return PermissionService.createError(message, "validation")
    }

    /**
     * Create API error
     */
    private static apiError(message: string, statusCode?: number): Error {
        return PermissionService.createError(message, "api", { statusCode })
    }

    /**
     * Create network error
     */
    private static networkError(message: string, cause?: Error): Error {
        return PermissionService.createError(message, "network", { cause })
    }

    /**
     * Type guard to check if an error is a PermissionError
     */
    private static isPermissionError(error: unknown): error is Error & { errorType: PermissionErrorType } {
        return error instanceof Error && error.name === "PermissionError"
    }

    /**
     * Get the current user's service-level edit permissions
     */
    static async getUserPermissions(): Promise<UserPermissions> {
        try {
            const response = await get(`${PermissionService.BASE_URL}/user`)

            if (!response.success) {
                throw PermissionService.apiError(response.errors?.join(", ") || "Failed to fetch user permissions")
            }

            return response.result
        } catch (error) {
            if (PermissionService.isPermissionError(error)) {
                throw error
            }
            throw PermissionService.networkError("Network error fetching user permissions", error as Error)
        }
    }

    /**
     * Check if current user can edit a specific service
     */
    static async canEditService(serviceId: number): Promise<ServicePermissionCheck> {
        if (!serviceId || serviceId <= 0) {
            throw PermissionService.validationError("Valid service ID is required")
        }

        try {
            const response = await get(`${PermissionService.BASE_URL}/service/${serviceId}/can-edit`)

            if (!response.success) {
                throw PermissionService.apiError(response.errors?.join(", ") || "Failed to check service permissions")
            }

            return response.result
        } catch (error) {
            if (PermissionService.isPermissionError(error)) {
                throw error
            }
            throw PermissionService.networkError("Network error checking service permissions", error as Error)
        }
    }

    /**
     * Check if current user can edit a specific rotation
     */
    static async canEditRotation(rotationId: number): Promise<RotationPermissionCheck> {
        if (!rotationId || rotationId <= 0) {
            throw PermissionService.validationError("Valid rotation ID is required")
        }

        try {
            const response = await get(`${PermissionService.BASE_URL}/rotation/${rotationId}/can-edit`)

            if (!response.success) {
                throw PermissionService.apiError(response.errors?.join(", ") || "Failed to check rotation permissions")
            }

            return response.result
        } catch (error) {
            if (PermissionService.isPermissionError(error)) {
                throw error
            }
            throw PermissionService.networkError("Network error checking rotation permissions", error as Error)
        }
    }

    /**
     * Check if current user can edit their own schedule for a specific instructor schedule entry
     */
    static async canEditOwnSchedule(instructorScheduleId: number): Promise<InstructorSchedulePermissionCheck> {
        if (!instructorScheduleId || instructorScheduleId <= 0) {
            throw PermissionService.validationError("Valid instructor schedule ID is required")
        }

        try {
            const response = await get(
                `${PermissionService.BASE_URL}/instructor-schedule/${instructorScheduleId}/can-edit-own`,
            )

            if (!response.success) {
                throw PermissionService.apiError(
                    response.errors?.join(", ") || "Failed to check own schedule permissions",
                )
            }

            return response.result
        } catch (error) {
            if (PermissionService.isPermissionError(error)) {
                throw error
            }
            throw PermissionService.networkError("Network error checking own schedule permissions", error as Error)
        }
    }
}

// Export singleton instance and class
const permissionService = new PermissionService()

export { permissionService, PermissionService }
