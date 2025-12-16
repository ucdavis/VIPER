import { computed } from "vue"
import { useUserStore } from "@/store/UserStore"

/**
 * Effort permission constants matching backend EffortPermissions.cs
 */
const EffortPermissions = {
    Base: "SVMSecure.Effort",
    ViewDept: "SVMSecure.Effort.ViewDept",
    ViewAllDepartments: "SVMSecure.Effort.ViewAllDepartments",
    EditEffort: "SVMSecure.Effort.EditEffort",
    ManageTerms: "SVMSecure.Effort.ManageTerms",
    VerifyEffort: "SVMSecure.Effort.VerifyEffort",
    ViewAudit: "SVMSecure.Effort.ViewAudit",
} as const

/**
 * Composable for checking Effort-specific permissions.
 * Permissions are loaded into UserStore during router beforeEach guard.
 */
function useEffortPermissions() {
    const userStore = useUserStore()

    /**
     * Check if user has a specific permission.
     */
    function hasPermission(permission: string): boolean {
        return userStore.userInfo.permissions.includes(permission)
    }

    const hasViewAllDepartments = computed(() => hasPermission(EffortPermissions.ViewAllDepartments))
    const hasViewDept = computed(() => hasPermission(EffortPermissions.ViewDept))
    const hasEditEffort = computed(() => hasPermission(EffortPermissions.EditEffort))
    const hasManageTerms = computed(() => hasPermission(EffortPermissions.ManageTerms))
    const hasVerifyEffort = computed(() => hasPermission(EffortPermissions.VerifyEffort))
    const hasViewAudit = computed(() => hasPermission(EffortPermissions.ViewAudit))
    const isAdmin = computed(() => hasViewAllDepartments.value)

    return {
        hasPermission,
        hasViewAllDepartments,
        hasViewDept,
        hasEditEffort,
        hasManageTerms,
        hasVerifyEffort,
        hasViewAudit,
        isAdmin,
        permissions: computed(() => userStore.userInfo.permissions),
    }
}

export { EffortPermissions, useEffortPermissions }
