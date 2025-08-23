import { defineStore } from 'pinia'
import { ref, computed, readonly } from 'vue'
import permissionService from '../services/PermissionService'
import type { 
    UserPermissions, 
    ServicePermissionCheck, 
    RotationPermissionCheck, 
    PermissionSummary,
    User,
    Service
} from '../types'

export const usePermissionsStore = defineStore('permissions', () => {
    // State
    const userPermissions = ref<UserPermissions | null>(null)
    const permissionSummary = ref<PermissionSummary | null>(null)
    const isLoading = ref(false)
    const error = ref<string | null>(null)

    // Getters
    const user = computed<User | null>(() => userPermissions.value?.user || null)
    
    const hasManagePermission = computed<boolean>(() => 
        userPermissions.value?.permissions.hasManagePermission || false
    )
    
    const editableServices = computed<Service[]>(() => 
        userPermissions.value?.editableServices || []
    )
    
    const editableServiceCount = computed<number>(() => 
        userPermissions.value?.permissions.editableServiceCount || 0
    )
    
    const servicePermissions = computed<Record<number, boolean>>(() => 
        userPermissions.value?.permissions.servicePermissions || {}
    )

    // Actions
    async function fetchUserPermissions(): Promise<UserPermissions | null> {
        try {
            isLoading.value = true
            error.value = null

            const permissions = await permissionService.getUserPermissions()
            userPermissions.value = permissions
            
            return permissions
        } catch (err) {
            error.value = err instanceof Error ? err.message : 'Failed to fetch user permissions'
            console.error('Error fetching user permissions:', err)
            return null
        } finally {
            isLoading.value = false
        }
    }

    async function fetchPermissionSummary(): Promise<PermissionSummary | null> {
        try {
            isLoading.value = true
            error.value = null

            const summary = await permissionService.getPermissionSummary()
            permissionSummary.value = summary
            
            return summary
        } catch (err) {
            error.value = err instanceof Error ? err.message : 'Failed to fetch permission summary'
            console.error('Error fetching permission summary:', err)
            return null
        } finally {
            isLoading.value = false
        }
    }

    async function checkServicePermission(serviceId: number): Promise<ServicePermissionCheck | null> {
        try {
            isLoading.value = true
            error.value = null

            const result = await permissionService.canEditService(serviceId)
            return result
        } catch (err) {
            error.value = err instanceof Error ? err.message : `Failed to check service ${serviceId} permissions`
            console.error('Error checking service permission:', err)
            return null
        } finally {
            isLoading.value = false
        }
    }

    async function checkRotationPermission(rotationId: number): Promise<RotationPermissionCheck | null> {
        try {
            isLoading.value = true
            error.value = null

            const result = await permissionService.canEditRotation(rotationId)
            return result
        } catch (err) {
            error.value = err instanceof Error ? err.message : `Failed to check rotation ${rotationId} permissions`
            console.error('Error checking rotation permission:', err)
            return null
        } finally {
            isLoading.value = false
        }
    }

    function canEditService(serviceId: number): boolean {
        // Quick synchronous check using user permissions data
        if (userPermissions.value && typeof serviceId === 'number' && serviceId > 0) {
            const servicePerms = userPermissions.value.permissions?.servicePermissions || {}
            const key = serviceId.toString()
            return Object.prototype.hasOwnProperty.call(servicePerms, key) 
                ? Boolean(servicePerms[key as unknown as keyof typeof servicePerms]) 
                : false
        }
        return false
    }

    function isServiceEditable(serviceId: number): boolean {
        return editableServices.value.some(service => service.serviceId === serviceId)
    }

    function getRequiredPermission(serviceId: number): string | null {
        const service = editableServices.value.find(s => s.serviceId === serviceId)
        return service?.scheduleEditPermission || null
    }

    function clearData(): void {
        userPermissions.value = null
        permissionSummary.value = null
        error.value = null
    }

    function clearError(): void {
        error.value = null
    }

    // Initialize permissions on store creation
    const initialize = async (): Promise<void> => {
        await fetchUserPermissions()
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
        fetchUserPermissions,
        fetchPermissionSummary,
        checkServicePermission,
        checkRotationPermission,
        canEditService,
        isServiceEditable,
        getRequiredPermission,
        clearData,
        clearError,
        initialize
    }
})