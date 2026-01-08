import { computed } from "vue"
import { useUserStore } from "@/store/UserStore"

/**
 * Effort permission constants matching backend EffortPermissions.cs
 */
const EffortPermissions = {
    Base: "SVMSecure.Effort",
    CreateEffort: "SVMSecure.Effort.CreateEffort",
    DeleteCourse: "SVMSecure.Effort.DeleteCourse",
    DeleteEffort: "SVMSecure.Effort.DeleteEffort",
    DeleteInstructor: "SVMSecure.Effort.DeleteInstructor",
    EditAdHocEval: "SVMSecure.Effort.EditAdHocEval",
    EditCourse: "SVMSecure.Effort.EditCourse",
    EditEffort: "SVMSecure.Effort.EditEffort",
    EditInstructor: "SVMSecure.Effort.EditInstructor",
    EditWhenClosed: "SVMSecure.Effort.EditWhenClosed",
    HarvestCourse: "SVMSecure.Effort.HarvestCourse",
    HarvestTerm: "SVMSecure.Effort.HarvestTerm",
    ImportCourse: "SVMSecure.Effort.ImportCourse",
    ImportInstructor: "SVMSecure.Effort.ImportInstructor",
    LinkCourses: "SVMSecure.Effort.LinkCourses",
    ManageAccess: "SVMSecure.Effort.ManageAccess",
    ManageRCourseEnrollment: "SVMSecure.Effort.ManageRCourseEnrollment",
    ManageTerms: "SVMSecure.Effort.ManageTerms",
    ManageUnits: "SVMSecure.Effort.ManageUnits",
    Reports: "SVMSecure.Effort.Reports",
    SchoolSummary: "SVMSecure.Effort.SchoolSummary",
    VerifyEffort: "SVMSecure.Effort.VerifyEffort",
    ViewAllDepartments: "SVMSecure.Effort.ViewAllDepartments",
    ViewAudit: "SVMSecure.Effort.ViewAudit",
    ViewDept: "SVMSecure.Effort.ViewDept",
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
    const hasImportCourse = computed(() => hasPermission(EffortPermissions.ImportCourse))
    const hasEditCourse = computed(() => hasPermission(EffortPermissions.EditCourse))
    const hasDeleteCourse = computed(() => hasPermission(EffortPermissions.DeleteCourse))
    const hasManageRCourseEnrollment = computed(() => hasPermission(EffortPermissions.ManageRCourseEnrollment))
    const hasLinkCourses = computed(() => hasPermission(EffortPermissions.LinkCourses))
    const hasImportInstructor = computed(() => hasPermission(EffortPermissions.ImportInstructor))
    const hasEditInstructor = computed(() => hasPermission(EffortPermissions.EditInstructor))
    const hasDeleteInstructor = computed(() => hasPermission(EffortPermissions.DeleteInstructor))
    const hasManageUnits = computed(() => hasPermission(EffortPermissions.ManageUnits))
    const isAdmin = computed(() => hasViewAllDepartments.value)

    return {
        hasPermission,
        hasViewAllDepartments,
        hasViewDept,
        hasEditEffort,
        hasManageTerms,
        hasVerifyEffort,
        hasViewAudit,
        hasImportCourse,
        hasEditCourse,
        hasDeleteCourse,
        hasManageRCourseEnrollment,
        hasLinkCourses,
        hasImportInstructor,
        hasEditInstructor,
        hasDeleteInstructor,
        hasManageUnits,
        isAdmin,
        permissions: computed(() => userStore.userInfo.permissions),
    }
}

export { EffortPermissions, useEffortPermissions }
