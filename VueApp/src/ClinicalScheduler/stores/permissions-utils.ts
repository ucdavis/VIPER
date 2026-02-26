import type { PermissionsState } from "./permissions-actions"
import type { Service } from "../types"

/**
 * Interface defining the getters used by permission utilities.
 * Provides typed access to computed values from the permissions store.
 */
export interface PermissionsGetters {
    editableServices: {
        value: Service[]
    }
}

/**
 * Creates permission utility functions for the store.
 * Provides helper methods for permission validation and data manipulation.
 * @param state - The permissions store state
 * @param getters - The permissions store getters
 * @returns Object containing all permission utility methods
 */
export function createPermissionUtils(state: PermissionsState, getters: PermissionsGetters) {
    const { userPermissions, error: errorState } = state
    const { editableServices } = getters

    /**
     * Checks if the current user can edit a specific service based on their service permissions.
     * Uses the servicePermissions map to determine access.
     * @param serviceId - The ID of the service to check
     * @returns True if user has explicit permission to edit the service
     */
    function canEditService(serviceId: number): boolean {
        if (userPermissions.value && typeof serviceId === "number" && serviceId > 0) {
            const servicePerms = userPermissions.value.permissions?.servicePermissions || {}
            const key = serviceId.toString()
            return Object.hasOwn(servicePerms, key)
                ? Boolean(servicePerms[key as unknown as keyof typeof servicePerms])
                : false
        }
        return false
    }

    /**
     * Checks if a service is in the user's list of editable services.
     * Uses the editableServices array to determine if the service is accessible.
     * @param serviceId - The ID of the service to check
     * @returns True if the service exists in the user's editable services list
     */
    function isServiceEditable(serviceId: number): boolean {
        return editableServices.value.some((service) => service.serviceId === serviceId)
    }

    /**
     * Gets the required permission string for editing a specific service.
     * Useful for displaying permission requirements to users.
     * @param serviceId - The ID of the service to get permission for
     * @returns The permission string required to edit the service, or null if not found
     */
    function getRequiredPermission(serviceId: number): string | null {
        const service = editableServices.value.find((s) => s.serviceId === serviceId)
        return service?.scheduleEditPermission ?? null
    }

    /**
     * Clears all permission data from the store state.
     * Used for logout scenarios or when switching users.
     */
    function clearData(): void {
        userPermissions.value = null
        errorState.value = null
    }

    /**
     * Clears any error state from the store.
     * Used to reset error state after displaying error messages to users.
     */
    function clearError(): void {
        errorState.value = null
    }

    /**
     * Gets the names of all services the user can edit.
     * Useful for displaying available editing options to users.
     * @returns Array of service names the user can edit
     */
    function getEditableServiceNames(): string[] {
        return editableServices.value.map((service) => service.serviceName)
    }

    /**
     * Gets a human-readable string of all editable service names.
     * Formats the service names for display in UI components with proper grammar.
     * @returns Formatted string of service names (e.g., "Service A and Service B" or "Service A, Service B, and Service C")
     */
    function getEditableServicesDisplay(): string {
        const names = getEditableServiceNames()
        if (names.length === 0) {
            return "None"
        }
        if (names.length === 1) {
            return names[0] || "Unknown Service"
        }
        if (names.length === 2) {
            return names.join(" and ")
        }
        return `${names.slice(0, -1).join(", ")}, and ${names.at(-1)}`
    }

    return {
        canEditService,
        isServiceEditable,
        getRequiredPermission,
        getEditableServiceNames,
        getEditableServicesDisplay,
        clearData,
        clearError,
    }
}
