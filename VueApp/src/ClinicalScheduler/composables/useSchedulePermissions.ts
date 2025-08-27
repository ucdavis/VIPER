import { computed } from 'vue'
import { useQuasar } from 'quasar'
import { usePermissionsStore } from '../stores/permissions'

export function useSchedulePermissions() {
    const $q = useQuasar()
    const permissionsStore = usePermissionsStore()

    /**
     * Check if user can edit a rotation based on its service
     */
    function canEditRotationByService(
        rotationId: number, 
        rotations: any[], 
        additionalRotation?: any
    ): boolean {
        // Check in provided rotations first
        let rotation = rotations.find(r => r.rotId === rotationId)
        
        // If not found, check the additional rotation
        if (!rotation && additionalRotation && additionalRotation.rotId === rotationId) {
            rotation = additionalRotation
        }
        
        if (!rotation || !rotation.serviceId) return false
        return permissionsStore.canEditService(rotation.serviceId)
    }

    /**
     * Check permission and show error if denied
     */
    function checkPermissionWithNotification(
        hasPermission: boolean,
        errorMessage: string = 'You do not have permission to edit this rotation'
    ): boolean {
        if (!hasPermission) {
            $q.notify({
                type: 'negative',
                message: errorMessage,
                timeout: 3000
            })
            return false
        }
        return true
    }

    /**
     * Get user's editable services
     */
    const editableServices = computed(() => permissionsStore.editableServices)
    
    /**
     * Check if user has manage permission
     */
    const hasManagePermission = computed(() => permissionsStore.hasManagePermission)

    /**
     * Check if a specific service is editable
     */
    function canEditService(serviceId: number): boolean {
        return permissionsStore.canEditService(serviceId)
    }

    /**
     * Get service permissions map
     */
    const servicePermissions = computed(() => permissionsStore.servicePermissions)

    return {
        canEditRotationByService,
        checkPermissionWithNotification,
        editableServices,
        hasManagePermission,
        canEditService,
        servicePermissions
    }
}