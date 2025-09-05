import { ref } from "vue"
import { permissionService } from "../services/permission-service"
import type {
    UserPermissions,
    ServicePermissionCheck,
    RotationPermissionCheck,
    InstructorSchedulePermissionCheck,
    PermissionSummary,
} from "../types"

/**
 * State interface for the permissions store.
 * Defines the reactive state properties used by the permissions store.
 */
interface PermissionsState {
    userPermissions: ReturnType<typeof ref<UserPermissions | null>>
    permissionSummary: ReturnType<typeof ref<PermissionSummary | null>>
    isLoading: ReturnType<typeof ref<boolean>>
    error: ReturnType<typeof ref<string | null>>
}

/**
 * Generic helper for executing permission actions with consistent error handling.
 * Reduces code duplication across all permission action functions.
 * @param action - The async function to execute
 * @param errorPrefix - Prefix for error messages
 * @param state - The permissions store state
 * @returns Promise with the action result or null on error
 */
async function executePermissionAction<T>(
    action: () => Promise<T>,
    errorPrefix: string,
    state: PermissionsState,
): Promise<T | null> {
    const { isLoading, error: errorState } = state

    try {
        isLoading.value = true
        errorState.value = null
        return await action()
    } catch (error) {
        errorState.value = error instanceof Error ? error.message : errorPrefix
        return null
    } finally {
        isLoading.value = false
    }
}

/**
 * Creates permission action functions for the store.
 * Provides async actions for fetching permissions and performing permission checks.
 * @param state - The permissions store state
 * @returns Object containing all permission action methods
 */
export function createPermissionActions(state: PermissionsState) {
    const { userPermissions, permissionSummary } = state

    /**
     * Fetches the current user's permissions from the API.
     * Updates the store state with the retrieved permissions.
     * @returns Promise resolving to user permissions object or null on error
     */
    function fetchUserPermissions(): Promise<UserPermissions | null> {
        return executePermissionAction(
            async () => {
                const permissions = await permissionService.getUserPermissions()
                userPermissions.value = permissions
                return permissions
            },
            "Failed to fetch user permissions",
            state,
        )
    }

    /**
     * Fetches a summary of system-wide permission information.
     * Useful for administrative views and permission management.
     * @returns Promise resolving to permission summary object or null on error
     */
    function fetchPermissionSummary(): Promise<PermissionSummary | null> {
        return executePermissionAction(
            async () => {
                const summary = await permissionService.getPermissionSummary()
                permissionSummary.value = summary
                return summary
            },
            "Failed to fetch permission summary",
            state,
        )
    }

    /**
     * Checks if the current user can edit schedules for a specific service.
     * Used for dynamic permission validation before allowing edit operations.
     * @param serviceId - The ID of the service to check permissions for
     * @returns Promise resolving to service permission check result or null on error
     */
    function checkServicePermission(serviceId: number): Promise<ServicePermissionCheck | null> {
        return executePermissionAction(
            () => permissionService.canEditService(serviceId),
            `Failed to check service ${serviceId} permissions`,
            state,
        )
    }

    /**
     * Checks if the current user can edit a specific rotation's schedule.
     * Used for validating access to rotation-specific scheduling operations.
     * @param rotationId - The ID of the rotation to check permissions for
     * @returns Promise resolving to rotation permission check result or null on error
     */
    function checkRotationPermission(rotationId: number): Promise<RotationPermissionCheck | null> {
        return executePermissionAction(
            () => permissionService.canEditRotation(rotationId),
            `Failed to check rotation ${rotationId} permissions`,
            state,
        )
    }

    /**
     * Checks if the current user can edit their own instructor schedule entry.
     * Used for validating self-service editing capabilities.
     * @param instructorScheduleId - The ID of the instructor schedule to check permissions for
     * @returns Promise resolving to instructor schedule permission check result or null on error
     */
    function checkOwnSchedulePermission(
        instructorScheduleId: number,
    ): Promise<InstructorSchedulePermissionCheck | null> {
        return executePermissionAction(
            () => permissionService.canEditOwnSchedule(instructorScheduleId),
            `Failed to check own schedule ${instructorScheduleId} permissions`,
            state,
        )
    }

    return {
        fetchUserPermissions,
        fetchPermissionSummary,
        checkServicePermission,
        checkRotationPermission,
        checkOwnSchedulePermission,
    }
}

export type { PermissionsState }
