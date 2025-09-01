import { defineStore } from "pinia"
import { ref, computed, readonly } from "vue"
import { permissionService } from "../services/permission-service"
import type {
    UserPermissions,
    ServicePermissionCheck,
    RotationPermissionCheck,
    PermissionSummary,
    User,
    Service,
} from "../types"

// Helper functions extracted to reduce main store function size
function createPermissionsState() {
    return {
        userPermissions: ref<UserPermissions | null>(null),
        permissionSummary: ref<PermissionSummary | null>(null),
        isLoading: ref(false),
        error: ref<string | null>(null),
    }
}

function createPermissionsGetters(state: ReturnType<typeof createPermissionsState>) {
    return {
        user: computed<User | null>(() => state.userPermissions.value?.user || null),
        hasManagePermission: computed<boolean>(
            () => state.userPermissions.value?.permissions.hasManagePermission || false,
        ),
        editableServices: computed<Service[]>(() => state.userPermissions.value?.editableServices || []),
        editableServiceCount: computed<number>(
            () => state.userPermissions.value?.permissions.editableServiceCount || 0,
        ),
        servicePermissions: computed<Record<number, boolean>>(
            () => state.userPermissions.value?.permissions.servicePermissions || {},
        ),
    }
}

// Helper function to create permission actions
function createPermissionActions(state: ReturnType<typeof createPermissionsState>) {
    const { userPermissions, permissionSummary, isLoading, error: errorState } = state

    async function fetchUserPermissions(): Promise<UserPermissions | null> {
        try {
            isLoading.value = true
            errorState.value = null

            const permissions = await permissionService.getUserPermissions()
            userPermissions.value = permissions

            return permissions
        } catch (error) {
            errorState.value = error instanceof Error ? error.message : "Failed to fetch user permissions"
            return null
        } finally {
            isLoading.value = false
        }
    }

    async function fetchPermissionSummary(): Promise<PermissionSummary | null> {
        try {
            isLoading.value = true
            errorState.value = null

            const summary = await permissionService.getPermissionSummary()
            permissionSummary.value = summary

            return summary
        } catch (error) {
            errorState.value = error instanceof Error ? error.message : "Failed to fetch permission summary"
            return null
        } finally {
            isLoading.value = false
        }
    }

    async function checkServicePermission(serviceId: number): Promise<ServicePermissionCheck | null> {
        try {
            isLoading.value = true
            errorState.value = null

            const result = await permissionService.canEditService(serviceId)
            return result
        } catch (error) {
            errorState.value =
                error instanceof Error ? error.message : `Failed to check service ${serviceId} permissions`
            return null
        } finally {
            isLoading.value = false
        }
    }

    async function checkRotationPermission(rotationId: number): Promise<RotationPermissionCheck | null> {
        try {
            isLoading.value = true
            errorState.value = null

            const result = await permissionService.canEditRotation(rotationId)
            return result
        } catch (error) {
            errorState.value =
                error instanceof Error ? error.message : `Failed to check rotation ${rotationId} permissions`
            return null
        } finally {
            isLoading.value = false
        }
    }

    return {
        fetchUserPermissions,
        fetchPermissionSummary,
        checkServicePermission,
        checkRotationPermission,
    }
}

// Helper function for utility methods
function createPermissionUtils(
    state: ReturnType<typeof createPermissionsState>,
    getters: ReturnType<typeof createPermissionsGetters>,
) {
    const { userPermissions, permissionSummary, error: errorState } = state
    const { editableServices } = getters

    function canEditService(serviceId: number): boolean {
        if (userPermissions.value && typeof serviceId === "number" && serviceId > 0) {
            const servicePerms = userPermissions.value.permissions?.servicePermissions || {}
            const key = serviceId.toString()
            return Object.prototype.hasOwnProperty.call(servicePerms, key)
                ? Boolean(servicePerms[key as unknown as keyof typeof servicePerms])
                : false
        }
        return false
    }

    function isServiceEditable(serviceId: number): boolean {
        return editableServices.value.some((service) => service.serviceId === serviceId)
    }

    function getRequiredPermission(serviceId: number): string | null {
        const service = editableServices.value.find((s) => s.serviceId === serviceId)
        return service?.scheduleEditPermission ?? null
    }

    function clearData(): void {
        userPermissions.value = null
        permissionSummary.value = null
        errorState.value = null
    }

    function clearError(): void {
        errorState.value = null
    }

    return {
        canEditService,
        isServiceEditable,
        getRequiredPermission,
        clearData,
        clearError,
    }
}

export const usePermissionsStore = defineStore("permissions", () => {
    // State
    const state = createPermissionsState()
    const { userPermissions, permissionSummary, isLoading, error } = state

    // Getters
    const getters = createPermissionsGetters(state)
    const { user, hasManagePermission, editableServices, editableServiceCount, servicePermissions } = getters

    // Actions
    const actions = createPermissionActions(state)
    const utils = createPermissionUtils(state, getters)

    // Initialize permissions on store creation
    const initialize = async (): Promise<void> => {
        await actions.fetchUserPermissions()
    }

    return {
        // State
        userPermissions: readonly(userPermissions),
        permissionSummary: readonly(permissionSummary),
        isLoading: readonly(isLoading),
        error: readonly(error),

        // Getters
        user,
        hasManagePermission,
        editableServices,
        editableServiceCount,
        servicePermissions,

        // Actions
        ...actions,
        ...utils,
        initialize,
    }
})
