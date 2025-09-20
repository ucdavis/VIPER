import { computed } from "vue"
import { usePermissionsStore } from "../stores/permissions"
import { useRouter } from "vue-router"

/**
 * Composable for common permission checking logic across schedule views
 */
export function usePermissionChecks() {
    const permissionsStore = usePermissionsStore()
    const router = useRouter()

    // Common computed properties
    const isLoadingPermissions = computed(() => permissionsStore.isLoading || !permissionsStore.userPermissions)

    const canAccessClinicianView = computed(() => permissionsStore.canAccessClinicianView)

    const canAccessRotationView = computed(() => permissionsStore.canAccessRotationView)

    const hasClinicianViewReadOnly = computed(() => permissionsStore.hasClinicianViewReadOnly)

    const hasOnlyServiceSpecificPermissions = computed(() => permissionsStore.hasOnlyServiceSpecificPermissions)

    // Navigation helpers
    function goToHome() {
        router.push("/ClinicalScheduler")
    }

    function goToClinicianView() {
        router.push("/ClinicalScheduler/clinician")
    }

    function goToRotationView() {
        router.push("/ClinicalScheduler/rotation")
    }

    return {
        // Permission states
        isLoadingPermissions,
        canAccessClinicianView,
        canAccessRotationView,
        hasClinicianViewReadOnly,
        hasOnlyServiceSpecificPermissions,

        // Navigation
        goToHome,
        goToClinicianView,
        goToRotationView,

        // Direct store access for additional properties
        permissionsStore,
    }
}
