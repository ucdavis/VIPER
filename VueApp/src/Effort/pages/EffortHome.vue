<template>
    <div class="q-pa-md">
        <template v-if="isLoading">
            <div class="text-grey q-my-md">Loading...</div>
        </template>
        <template v-else>
            <h2 class="text-grey-7">Select your action from the menu</h2>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { termService } from "../services/term-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"

const route = useRoute()
const router = useRouter()
const {
    hasManageTerms,
    hasManageUnits,
    hasManageEffortTypes,
    hasViewAudit,
    hasImportCourse,
    hasEditCourse,
    hasDeleteCourse,
    hasManageRCourseEnrollment,
    hasImportInstructor,
    hasEditInstructor,
    hasDeleteInstructor,
    hasViewDept,
    isAdmin,
} = useEffortPermissions()

const isLoading = ref(true)

// Check if user has access to any menu items besides "My Effort"
const hasOtherMenuAccess = computed(() => {
    return (
        hasImportCourse.value ||
        hasEditCourse.value ||
        hasDeleteCourse.value ||
        hasManageRCourseEnrollment.value ||
        hasImportInstructor.value ||
        hasEditInstructor.value ||
        hasDeleteInstructor.value ||
        hasViewDept.value ||
        hasManageTerms.value ||
        hasManageUnits.value ||
        hasManageEffortTypes.value ||
        hasViewAudit.value ||
        isAdmin.value
    )
})

// Users with dashboard access should land on the dashboard
const hasDashboardAccess = computed(() => isAdmin.value || hasViewDept.value)

onMounted(async () => {
    const termCode = route.params.termCode ? parseInt(route.params.termCode as string, 10) : null

    if (termCode) {
        try {
            await termService.getTerm(termCode)
            // Redirect to dashboard for users with ViewAllDepartments or ViewDept
            if (hasDashboardAccess.value) {
                router.replace({ name: "StaffDashboard", params: { termCode } })
                return
            }
            // If user only has access to "My Effort", redirect them there directly
            if (!hasOtherMenuAccess.value) {
                router.replace({ name: "MyEffort", params: { termCode } })
                return
            }
        } catch {
            router.replace({ name: "TermSelection" })
        } finally {
            isLoading.value = false
        }
    } else {
        router.replace({ name: "TermSelection" })
    }
})
</script>
