<template>
    <q-drawer
        v-model="localDrawerOpen"
        show-if-above
        elevated
        side="left"
        :mini="!localDrawerOpen"
        no-mini-animation
        :width="300"
        id="mainLeftDrawer"
        v-cloak
        class="no-print"
    >
        <div
            class="q-pa-sm"
            id="leftNavMenu"
        >
            <q-btn
                dense
                round
                unelevated
                color="secondary"
                icon="close"
                aria-label="Close navigation menu"
                class="float-right lt-md"
                @click="localDrawerOpen = false"
            />
            <h2>Effort 3.0</h2>

            <q-list
                dense
                separator
            >
                <!-- Current Term - clickable with pencil icon -->
                <q-item
                    v-if="currentTerm"
                    clickable
                    v-ripple
                    :to="{ name: 'TermSelection' }"
                    class="leftNavHeader"
                >
                    <q-item-section>
                        <q-item-label lines="1">
                            {{ currentTerm.termName }}
                            <q-icon
                                name="edit"
                                size="xs"
                                class="q-ml-xs"
                            />
                        </q-item-label>
                    </q-item-section>
                </q-item>
                <q-item
                    v-else
                    clickable
                    v-ripple
                    :to="{ name: 'TermSelection' }"
                    class="leftNavHeader"
                >
                    <q-item-section>
                        <q-item-label lines="1">Select a Term</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- Instructors - for users with instructor permissions -->
                <q-item
                    v-if="canViewInstructors && currentTerm"
                    clickable
                    v-ripple
                    :to="{ name: 'InstructorList', params: { termCode: currentTerm.termCode } }"
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">Instructors</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- Courses - for users with course permissions -->
                <q-item
                    v-if="canViewCourses && currentTerm"
                    clickable
                    v-ripple
                    :to="{ name: 'CourseList', params: { termCode: currentTerm.termCode } }"
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">Courses</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- My Effort - can view/verify their own effort -->
                <q-item
                    v-if="currentTerm"
                    clickable
                    v-ripple
                    :to="{ name: 'MyEffort', params: { termCode: currentTerm.termCode } }"
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">My Effort</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- Manage Terms - only for ManageTerms users -->
                <q-item
                    v-if="hasManageTerms"
                    clickable
                    v-ripple
                    :to="
                        currentTerm
                            ? { name: 'TermManagement', query: { termCode: currentTerm.termCode } }
                            : { name: 'TermManagement' }
                    "
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">Manage Terms</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- Manage Units - only for ManageUnits users -->
                <q-item
                    v-if="hasManageUnits"
                    clickable
                    v-ripple
                    :to="
                        currentTerm
                            ? { name: 'UnitListWithTerm', params: { termCode: currentTerm.termCode } }
                            : { name: 'UnitList' }
                    "
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">Manage Units</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- Manage Effort Types - only for ManageEffortTypes users -->
                <q-item
                    v-if="hasManageEffortTypes"
                    clickable
                    v-ripple
                    :to="
                        currentTerm
                            ? { name: 'EffortTypeListWithTerm', params: { termCode: currentTerm.termCode } }
                            : { name: 'EffortTypeList' }
                    "
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">Manage Effort Types</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- Percent Assignment Types - view-only list for admins -->
                <q-item
                    v-if="isAdmin"
                    clickable
                    v-ripple
                    :to="
                        currentTerm
                            ? { name: 'PercentAssignTypeListWithTerm', params: { termCode: currentTerm.termCode } }
                            : { name: 'PercentAssignTypeList' }
                    "
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">Percent Assignment Types</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- Audit - for ViewAudit or ViewDeptAudit users (term optional) -->
                <q-item
                    v-if="hasAnyAuditAccess"
                    clickable
                    v-ripple
                    :to="
                        currentTerm
                            ? { name: 'EffortAuditWithTerm', params: { termCode: currentTerm.termCode } }
                            : { name: 'EffortAudit' }
                    "
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">Audit Trail</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- Help -->
                <q-item
                    clickable
                    v-ripple
                    href="https://ucdsvm.knowledgeowl.com/help/effort-system-overview"
                    target="_blank"
                    rel="noopener"
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label
                            lines="1"
                            class="help-label"
                        >
                            <q-icon
                                name="help_outline"
                                size="xs"
                            />
                            <span>Help</span>
                        </q-item-label>
                    </q-item-section>
                </q-item>
            </q-list>
        </div>
    </q-drawer>
</template>

<script setup lang="ts">
import { ref, watch, computed } from "vue"
import { useRoute } from "vue-router"
import { termService } from "../services/term-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import type { TermDto } from "../types"

const props = defineProps<{
    drawerOpen: boolean
    onDrawerChange: (value: boolean) => void
}>()

const route = useRoute()
const {
    hasManageTerms,
    hasManageUnits,
    hasManageEffortTypes,
    hasAnyAuditAccess,
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

// Users who can add/edit/delete courses or manage R-course enrollment should see the Courses link
const canViewCourses = computed(
    () =>
        hasImportCourse.value ||
        hasEditCourse.value ||
        hasDeleteCourse.value ||
        hasManageRCourseEnrollment.value ||
        isAdmin.value,
)

// Users who can add/edit/delete instructors or have view access should see the Instructors link
const canViewInstructors = computed(
    () =>
        hasImportInstructor.value ||
        hasEditInstructor.value ||
        hasDeleteInstructor.value ||
        hasViewDept.value ||
        isAdmin.value,
)

const localDrawerOpen = ref(props.drawerOpen)
const currentTerm = ref<TermDto | null>(null)

// Sync local drawer state with parent
watch(
    () => props.drawerOpen,
    (newValue) => {
        localDrawerOpen.value = newValue
    },
)

watch(localDrawerOpen, (newValue) => {
    props.onDrawerChange(newValue)
})

// Load term when termCode changes in route
async function loadCurrentTerm(termCode: number | null) {
    if (termCode) {
        currentTerm.value = await termService.getTerm(termCode)
    } else {
        // No term selected - don't show any term until user picks one
        currentTerm.value = null
    }
}

// Watch both route params and query params for termCode
watch(
    () => route.params.termCode || route.query.termCode,
    (newTermCode) => {
        const termCode = newTermCode ? parseInt(newTermCode as string, 10) : null
        loadCurrentTerm(termCode)
    },
    { immediate: true },
)
</script>

<style scoped>
.help-label {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
}
</style>
