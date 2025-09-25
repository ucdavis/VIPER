import { useFetch } from "../../composables/ViperFetch"
import type {
    UserPermissions,
    ServicePermissionCheck,
    RotationPermissionCheck,
    InstructorSchedulePermissionCheck,
    PermissionSummary,
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
    async getUserPermissions(): Promise<UserPermissions> {
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
    async canEditService(serviceId: number): Promise<ServicePermissionCheck> {
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
    async canEditRotation(rotationId: number): Promise<RotationPermissionCheck> {
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
    async canEditOwnSchedule(instructorScheduleId: number): Promise<InstructorSchedulePermissionCheck> {
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

    /**
     * Get summary of permission system configuration
     */
    async getPermissionSummary(): Promise<PermissionSummary> {
        try {
            const response = await get(`${PermissionService.BASE_URL}/summary`)

            if (!response.success) {
                throw PermissionService.apiError(response.errors?.join(", ") || "Failed to fetch permission summary")
            }

            return response.result
        } catch (error) {
            if (PermissionService.isPermissionError(error)) {
                throw error
            }
            throw PermissionService.networkError("Network error fetching permission summary", error as Error)
        }
    }

    /**
     * Check multiple services for edit permissions in batch
     */
    async checkMultipleServices(serviceIds: number[]): Promise<Record<number, boolean>> {
        try {
            const userPermissions = await this.getUserPermissions()
            const resultMap = new Map<number, boolean>()

            // Validate input and create safe mapping
            const validServiceIds = serviceIds.filter((id) => Number.isInteger(id) && id > 0)
            const servicePerms = userPermissions.permissions?.servicePermissions || {}

            for (const serviceId of validServiceIds) {
                // Safe property access using hasOwnProperty check
                const key = serviceId.toString()
                const hasPermission = Object.hasOwn(servicePerms, key)
                    ? Boolean(servicePerms[key as unknown as keyof typeof servicePerms])
                    : false
                resultMap.set(serviceId, hasPermission)
            }

            // Convert Map to Record for return
            return Object.fromEntries(resultMap)
        } catch {
            // Return safe default permissions
            const resultMap = new Map<number, boolean>()
            const validServiceIds = serviceIds.filter((id) => Number.isInteger(id) && id > 0)
            for (const serviceId of validServiceIds) {
                resultMap.set(serviceId, false)
            }
            return Object.fromEntries(resultMap)
        }
    }

    /**
     * Check multiple rotations for edit permissions in batch
     */
    async checkMultipleRotations(rotationIds: number[]): Promise<Record<number, boolean>> {
        try {
            // For now, check each rotation individually
            // This could be optimized with a batch endpoint if needed
            const resultsMap = new Map<number, boolean>()
            const validRotationIds = rotationIds.filter((id) => Number.isInteger(id) && id > 0)

            const checks = validRotationIds.map(async (rotationId) => {
                try {
                    const result = await this.canEditRotation(rotationId)
                    return { rotationId, canEdit: Boolean(result?.canEdit) }
                } catch {
                    return { rotationId, canEdit: false }
                }
            })

            const checkResults = await Promise.all(checks)

            // Safely assign results using Map
            for (const { rotationId, canEdit } of checkResults) {
                resultsMap.set(rotationId, canEdit)
            }

            return Object.fromEntries(resultsMap)
        } catch {
            // Return safe default permissions using Map
            const resultMap = new Map<number, boolean>()
            const validRotationIds = rotationIds.filter((id) => Number.isInteger(id) && id > 0)
            for (const rotationId of validRotationIds) {
                resultMap.set(rotationId, false)
            }
            return Object.fromEntries(resultMap)
        }
    }

    /**
     * Helper method to check if user has general manage permission
     */
    async hasManagePermission(): Promise<boolean> {
        try {
            const userPermissions = await this.getUserPermissions()
            return userPermissions.permissions.hasManagePermission
        } catch {
            return false
        }
    }

    /**
     * Helper method to get list of services user can edit
     */
    async getEditableServices() {
        try {
            const userPermissions = await this.getUserPermissions()
            return userPermissions.editableServices
        } catch {
            return []
        }
    }
}

// Export singleton instance and class
const permissionService = new PermissionService()

export { permissionService, PermissionService, type PermissionErrorType }
