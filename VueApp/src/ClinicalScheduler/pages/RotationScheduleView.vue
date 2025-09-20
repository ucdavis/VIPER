<template>
    <div class="clinical-scheduler-container position-relative">
        <SchedulerNavigation />

        <!-- Loading permissions state - show this until we have permission data -->
        <div
            v-if="permissionsStore.isLoading || !permissionsStore.userPermissions"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading...</div>
        </div>

        <!-- Access denied for users without rotation view permissions -->
        <AccessDeniedCard
            v-else-if="!permissionsStore.canAccessRotationView"
            :message="ACCESS_DENIED_MESSAGES.ROTATION_VIEW"
            :subtitle="ACCESS_DENIED_SUBTITLES.ROTATION_VIEW"
        >
            <template #actions>
                <q-btn
                    color="primary"
                    label="Return to Home"
                    to="/ClinicalScheduler"
                />
            </template>
        </AccessDeniedCard>

        <!-- Access denied for specific rotation -->
        <AccessDeniedCard
            v-else-if="showUnauthorizedRotationError"
            :message="ACCESS_DENIED_MESSAGES.UNAUTHORIZED_ROTATION"
            :subtitle="ACCESS_DENIED_SUBTITLES.UNAUTHORIZED_ROTATION"
        >
            <template #actions>
                <q-btn
                    color="primary"
                    label="Select Available Rotation"
                    @click="clearUnauthorizedRotation"
                />
            </template>
        </AccessDeniedCard>

        <!-- Main content when user has access -->
        <div v-else-if="!showUnauthorizedRotationError">
            <div class="schedule-header">
                <div class="row items-center q-mb-md q-gutter-md mobile-column">
                    <div class="row items-center mobile-column q-gutter-md">
                        <h2 class="q-pr-md q-my-none mobile-full-width">Schedule Clinicians for</h2>
                        <div class="col-auto mobile-full-width">
                            <RotationSelector
                                v-model="selectedRotationId"
                                :year="currentYear"
                                :only-with-scheduled-weeks="isPastYear"
                                @rotation-selected="onRotationSelected"
                                style="min-width: 300px"
                            />
                        </div>
                        <div class="col-auto mobile-no-margin">
                            <YearSelector
                                v-model="currentYear"
                                @year-changed="onYearChange"
                                style="min-width: 120px"
                            />
                        </div>
                    </div>
                </div>
            </div>

            <!-- Error display for rotation loading -->
            <ScheduleBanner
                v-if="error"
                type="error"
                :error-message="error"
            >
                <template #action>
                    <q-btn
                        flat
                        color="white"
                        label="Retry"
                        :loading="isLoading"
                        @click="loadRotations"
                    />
                </template>
            </ScheduleBanner>

            <!-- Read-only notice for past years -->
            <ScheduleBanner
                v-if="isPastYear"
                type="read-only"
            />

            <!-- No rotation selected message -->
            <ScheduleBanner
                v-else-if="!selectedRotation"
                type="info"
                custom-message="Please select a rotation to view its schedule."
            />

            <!-- Instructions (only show when rotation is selected and not past year) -->
            <ScheduleBanner
                v-else-if="selectedRotation && !isPastYear"
                type="instructions"
                custom-message="This list of clinicians should contain any clinician scheduled for the rotation in the current or previous year. The user can click on a clinician to select them, and then click on any week to schedule them."
            />

            <!-- Clinician selector section (only show when rotation is selected and not past year) -->
            <RecentSelections
                v-if="selectedRotation && !isPastYear"
                :items="clinicianItems"
                :local-selected-items="selectedClinicians"
                :multi-select="true"
                recent-label="Recent Clinicians:"
                add-new-label="Add New Clinician:"
                item-type="clinician"
                item-key-field="fullName"
                item-display-field="fullName"
                label-spacing="md"
                selector-spacing="lg"
                :is-loading="isLoadingSchedule"
                empty-state-message="No recent clinicians. Please add a clinician below."
                :selected-weeks-count="selectedWeekIds.length"
                :show-schedule-button="true"
                :is-alt-key-held="scheduleViewRef?.isAltKeyHeld ?? false"
                :is-delete-mode="isInDeleteMode"
                @select-items="selectClinicianItems"
                @clear-selection="clearClinicianSelection"
                @schedule-selected="scheduleBulkCliniciansToWeeks"
            >
                <template #selector>
                    <ClinicianSelector
                        :model-value="null"
                        :year="currentYear"
                        :include-all-affiliates="includeAllAffiliates"
                        @update:include-all-affiliates="includeAllAffiliates = $event"
                        @change="onAddClinicianSelected"
                        :affiliates-toggle-label="'Include all affiliates'"
                        view-context="rotation"
                        class="q-mt-none"
                        style="min-width: 280px"
                    />
                </template>
            </RecentSelections>

            <!-- No weeks scheduled message -->
            <ScheduleBanner
                v-if="selectedRotation && weeksBySemester.length === 0 && !isLoadingSchedule"
                type="no-entries"
                :custom-message="`${selectedRotation.name} has no weeks scheduled for ${currentYear}.`"
            />

            <!-- Unified schedule view -->
            <ScheduleView
                v-else-if="selectedRotation && weeksBySemester.length > 0"
                ref="scheduleViewRef"
                :schedules-by-semester="weeksBySemester"
                view-mode="rotation"
                :is-past-year="isPastYear"
                :is-loading="isLoadingSchedule"
                :error="scheduleError"
                :can-edit="canEditRotation"
                :show-legend="true"
                :show-warning-in-legend="true"
                :show-warning-icon="true"
                :show-primary-toggle="true"
                :requires-primary-for-week="true"
                :enable-week-selection="!isPastYear"
                :get-assignments="
                    (week) =>
                        getWeekAssignments(week.weekId).map((a) => ({
                            id: a.id,
                            displayName: a.clinicianName,
                            isPrimary: a.isPrimary,
                        }))
                "
                :requires-primary-evaluator="(week) => requiresPrimaryEvaluator(week)"
                :get-week-additional-classes="(week) => ({ 'requires-primary-card': requiresPrimaryEvaluator(week) })"
                :loading-week-id="loadingWeekId"
                empty-state-message="Click to add clinician"
                read-only-empty-message="No assignments"
                warning-icon-title="Primary evaluator required for this week"
                primary-evaluator-title="Primary evaluator. To transfer primary status, click the star on another clinician."
                make-primary-title="Click to make this clinician the primary evaluator."
                primary-removal-disabled-message="Cannot remove primary clinician. Make another clinician primary first."
                @week-click="handleWeekClick"
                @remove-assignment="(id, name, isPrimary) => removeAssignment(id, name, isPrimary)"
                @toggle-primary="(id, isPrimary, name) => togglePrimary(id, isPrimary, name)"
                @selected-weeks-change="onSelectedWeeksChange"
            />
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar } from "quasar"
import { RotationService } from "../services/rotation-service"
import type { RotationWithService, RotationScheduleData } from "../types/rotation-types"
import { InstructorScheduleService } from "../services/instructor-schedule-service"
import type { Clinician } from "../services/clinician-service"
import { PageDataService } from "../services/page-data-service"
import { usePermissionsStore } from "../stores/permissions"
import { useScheduleUpdatesWithRollback } from "../composables/use-optimistic-schedule-updates"
import { useBulkDeletionLogic } from "../composables/use-bulk-deletion-logic"
import { useScheduleNormalization } from "../composables/use-schedule-normalization"
import { useDeleteMode } from "../composables/use-delete-mode"
import RotationSelector from "../components/RotationSelector.vue"
import ClinicianSelector from "../components/ClinicianSelector.vue"
import YearSelector from "../components/YearSelector.vue"
import SchedulerNavigation from "../components/SchedulerNavigation.vue"
import type { WeekItem } from "../components/WeekScheduleCard.vue"
import ScheduleBanner from "../components/ScheduleBanner.vue"
import RecentSelections from "../components/RecentSelections.vue"
import AccessDeniedCard from "../components/AccessDeniedCard.vue"
import { ACCESS_DENIED_MESSAGES, ACCESS_DENIED_SUBTITLES } from "../constants/permission-messages"
import { UI_CONFIG } from "../constants/app-constants"
import ScheduleView from "../components/ScheduleView.vue"

// Router and Quasar
const route = useRoute()
const router = useRouter()
const $q = useQuasar()

// Permissions store
const permissionsStore = usePermissionsStore()

// Schedule updates with rollback and queue
const { addScheduleWithRollback, removeScheduleWithRollback, togglePrimaryWithRollback } =
    useScheduleUpdatesWithRollback()

const { executeBulkDeletionLogic } = useBulkDeletionLogic()

// Reactive data
const selectedRotationId = ref<number | null>(null)
const selectedRotation = ref<RotationWithService | null>(null)
const selectedClinician = ref<string | null>(null)
const selectedClinicianData = ref<{ fullName: string; mothraId: string } | null>(null)
const selectedClinicians = ref<{ fullName: string; mothraId: string }[]>([])
const scheduleViewRef = ref<InstanceType<typeof ScheduleView> | null>(null)
const selectedWeekIds = ref<number[]>([])
const rotations = ref<RotationWithService[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)
const unauthorizedRotationId = ref<number | null>(null)

// Real data from database
const scheduleData = ref<RotationScheduleData | null>(null)
const isLoadingSchedule = ref(false)
const scheduleError = ref<string | null>(null)
// YearSelector will initialize with grad year
const currentYear = ref<number | null>(null)
const currentGradYear = ref<number>(new Date().getFullYear())

// Clinician data for the dropdown
const includeAllAffiliates = ref(false)

// Loading states for operations
const isAddingClinician = ref(false)
const isRemovingClinician = ref(false)
const isTogglingPrimary = ref(false)
const loadingWeekId = ref<number | null>(null)

// Year selection is now handled by YearSelector component

const isPastYear = computed(() => {
    // Compare selected year against the current grad year from backend
    return currentYear.value !== null && currentYear.value < currentGradYear.value
})

const canEditRotation = computed(() => {
    if (!selectedRotation.value || !selectedRotation.value.service) return false
    return permissionsStore.canEditService(selectedRotation.value.service.serviceId)
})

const showUnauthorizedRotationError = computed(() => {
    return unauthorizedRotationId.value !== null && !selectedRotation.value
})

// Computed properties for RecentSelections component
const clinicianItems = computed(() => {
    const items: { fullName: string; mothraId: string }[] = []

    // Add the selected clinician if they're not in the recent list
    if (selectedClinicianData.value) {
        items.push(selectedClinicianData.value)
    }

    // Add recent clinicians from backend
    if (scheduleData.value && scheduleData.value.recentClinicians) {
        scheduleData.value.recentClinicians.forEach((clinician) => {
            // Don't add duplicates (check by mothraId)
            if (!items.some((item) => item.mothraId === clinician.mothraId)) {
                items.push({ fullName: clinician.fullName, mothraId: clinician.mothraId })
            }
        })
    }

    return items.sort((a, b) => a.fullName.localeCompare(b.fullName))
})

const _selectedClinicianItem = computed(() => {
    if (!selectedClinician.value) return null
    return clinicianItems.value.find((item) => item.fullName === selectedClinician.value) || null
})

// Use the semester data directly from the backend API
// The backend already groups weeks by semester using TermCodeService.GetTermCodeDescription()
const { normalizeRotationSchedule } = useScheduleNormalization()

// Get all weeks in a flat array for bulk operations
const allWeeks = computed(() => {
    const weeks: WeekItem[] = []
    const semesters = weeksBySemester.value
    if (semesters) {
        for (const semester of semesters) {
            weeks.push(...semester.weeks)
        }
    }
    return weeks
})

const weeksBySemester = computed(() => {
    return normalizeRotationSchedule(scheduleData.value)
})

// Use shared delete mode logic
const { isInDeleteMode } = useDeleteMode({
    selectedItems: selectedClinicians,
    selectedWeekIds,
    schedulesBySemester: weeksBySemester,
    getWeekAssignments: (week: any) => getWeekAssignments(week.weekId),
    getItemIdentifier: (clinician: { fullName: string; mothraId: string }) => clinician.fullName,
    getAssignmentIdentifier: (assignment: any) => assignment.clinicianName,
})

// isSemesterPast is handled inside ScheduleView

// Watch for includeAllAffiliates changes (ClinicianSelector handles reloading automatically)
watch(
    () => includeAllAffiliates.value,
    () => {
        // ClinicianSelector automatically reloads when includeAllAffiliates changes
    },
)

// Methods

async function loadRotations() {
    isLoading.value = true
    error.value = null

    try {
        const result = await RotationService.getRotations()

        if (result.success) {
            rotations.value = result.result
        } else {
            error.value = result.errors.join(", ") || "Failed to load rotations"
        }
    } catch {
        error.value = "An unexpected error occurred while loading rotations"
    } finally {
        isLoading.value = false
    }
}

function onRotationSelected(rotation: RotationWithService | null) {
    selectedRotation.value = rotation
    selectedRotationId.value = rotation ? rotation.rotId : null // Update rotation ID for URL
    selectedClinician.value = null // Reset clinician selection when rotation changes
    selectedClinicianData.value = null
    selectedClinicians.value = []

    // Update URL with selected rotation
    void updateUrl()

    if (selectedRotation.value) {
        void loadScheduleData(selectedRotation.value.rotId)
    } else {
        scheduleData.value = null
    }
}

function onRotationChange() {
    const rotationId = selectedRotationId.value ? Number(selectedRotationId.value) : null
    const foundRotation = rotations.value.find((r: RotationWithService) => r.rotId === rotationId) || null

    // Check if user tried to access a rotation they don't have permission for
    if (rotationId && !foundRotation) {
        // User doesn't have permission for this rotation
        unauthorizedRotationId.value = rotationId
        selectedRotation.value = null
        selectedRotationId.value = null
    } else {
        // Clear any previous unauthorized rotation error
        unauthorizedRotationId.value = null
        selectedRotation.value = foundRotation
        onRotationSelected(selectedRotation.value)
    }
}

async function updateUrl() {
    if (selectedRotationId.value) {
        const query: Record<string, string> = {}

        if (currentYear.value !== null) {
            query.year = currentYear.value.toString()
        }

        // Update URL with path parameter for rotation ID
        await router.replace({
            name: "RotationScheduleWithId",
            params: { rotationId: selectedRotationId.value?.toString() ?? "" },
            query: Object.keys(query).length > 0 ? query : undefined,
        })
    } else {
        // No rotation selected, go back to base route
        await router.replace({
            name: "RotationSchedule",
        })
    }
}

function initializeFromUrl() {
    // Get rotation ID from URL path parameters
    const rotationIdParam = route.params.rotationId
    if (rotationIdParam && typeof rotationIdParam === "string") {
        selectedRotationId.value = Number(rotationIdParam)
    }

    // Get year from URL query parameters
    const yearParam = route.query.year
    if (yearParam && typeof yearParam === "string") {
        currentYear.value = Number(yearParam)
    }
}

async function onYearChange() {
    // Update URL
    await updateUrl()

    // ClinicianSelector will automatically reload for the new year

    // Reload schedule data if a rotation is selected
    if (selectedRotation.value) {
        await loadScheduleData(selectedRotation.value.rotId)
    }
}

async function loadScheduleData(rotationId: number) {
    isLoadingSchedule.value = true
    scheduleError.value = null

    try {
        const result = await RotationService.getRotationSchedule(rotationId, { year: currentYear.value ?? undefined })

        if (result.success) {
            scheduleData.value = result.result

            // Debug logging for Week 18 issue
        } else {
            scheduleError.value = result.errors.join(", ") || "Failed to load schedule data"
        }
    } catch {
        scheduleError.value = "An unexpected error occurred while loading schedule data"
    } finally {
        isLoadingSchedule.value = false
    }
}

// Methods for RecentSelections component
function _selectClinicianItem(clinicianItem: { fullName: string; mothraId: string }) {
    selectedClinician.value = clinicianItem.fullName
    selectedClinicianData.value = clinicianItem
}

function selectClinicianItems(clinicianItems: { fullName: string; mothraId: string }[]) {
    selectedClinicians.value = clinicianItems
    // For backward compatibility, set single selection to the first item
    if (clinicianItems.length > 0) {
        const firstItem = clinicianItems[0]
        if (firstItem) {
            selectedClinician.value = firstItem.fullName
            selectedClinicianData.value = firstItem
        }
    } else {
        selectedClinician.value = null
        selectedClinicianData.value = null
    }
}

function clearClinicianSelection() {
    selectedClinician.value = null
    selectedClinicianData.value = null
    selectedClinicians.value = []
    // Also clear week selection when clearing clinician selection
    selectedWeekIds.value = []
    if (scheduleViewRef.value) {
        scheduleViewRef.value.clearSelection()
    }
}

function clearUnauthorizedRotation() {
    unauthorizedRotationId.value = null
    selectedRotationId.value = null
    // Clear the rotation ID from the URL
    void router.replace({ name: "RotationSchedule" })
}

function onAddClinicianSelected(clinician: Clinician | null) {
    if (clinician) {
        // Store the full clinician data (including mothraId)
        const clinicianData = {
            fullName: clinician.fullName,
            mothraId: clinician.mothraId,
        }

        // The recent clinicians list comes from the backend and is read-only
        // We can't modify it directly, but we can still select the clinician

        // Select the clinician (same as clicking a recent clinician chip)
        selectedClinician.value = clinician.fullName
        selectedClinicianData.value = clinicianData

        // Also update the multi-select array to auto-select the newly added clinician
        selectedClinicians.value = [clinicianData]

        // If we need to reload schedule data to get updated recent clinicians,
        // that will happen automatically after adding them to a week
    }
}

// ClinicianSelector component handles filtering internally

function getWeekAssignments(weekId: number) {
    if (!scheduleData.value || !scheduleData.value.schedulesBySemester) return []

    // Find the week in the nested semester structure
    for (const semester of scheduleData.value.schedulesBySemester) {
        const week = semester.weeks.find((w) => w.weekId === weekId)
        if (week && week.instructorSchedules) {
            return week.instructorSchedules.map((schedule) => ({
                id: schedule.instructorScheduleId,
                clinicianName: schedule.fullName,
                displayName: schedule.fullName,
                isPrimary: schedule.isPrimaryEvaluator,
                mothraId: schedule.mothraId,
            }))
        }
    }

    return []
}

// Handle week click (either single week or with multiple selected weeks)
async function handleWeekClick(week: WeekItem) {
    // If weeks are selected, schedule to all selected weeks
    if (selectedWeekIds.value.length > 0 && selectedClinicians.value.length > 0) {
        await scheduleCliniciansToBulkWeeks()
        return
    }

    // Otherwise use the original single-week behavior
    await scheduleClinicianToWeek(week)
}

// Handle selected weeks change
function onSelectedWeeksChange(weekIds: number[]) {
    selectedWeekIds.value = weekIds
}

// Schedule multiple clinicians to multiple weeks
async function scheduleCliniciansToBulkWeeks() {
    if (!canEditRotation.value || selectedClinicians.value.length === 0 || selectedWeekIds.value.length === 0) {
        return
    }

    const totalOperations = selectedClinicians.value.length * selectedWeekIds.value.length

    // Show confirmation dialog for large operations
    if (totalOperations > 10) {
        const proceed = await new Promise<boolean>((resolve) => {
            $q.dialog({
                title: "Bulk Schedule Confirmation",
                message: `This will create ${totalOperations} assignments (${selectedClinicians.value.length} clinician${selectedClinicians.value.length !== 1 ? "s" : ""} × ${selectedWeekIds.value.length} week${selectedWeekIds.value.length !== 1 ? "s" : ""}). Continue?`,
                persistent: true,
                ok: { label: "Yes, Schedule All", color: "primary" },
                cancel: { label: "Cancel", color: "grey" },
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
        })

        if (!proceed) return
    }

    // Temporarily disable selection during bulk operation
    const originalSelectedClinicians = [...selectedClinicians.value]

    try {
        let successCount = 0
        let alreadyScheduledCount = 0

        // Process each week
        for (const weekId of selectedWeekIds.value) {
            const week = allWeeks.value.find((w) => w.weekId === weekId)
            if (!week) continue

            loadingWeekId.value = weekId

            // Schedule each clinician to this week
            for (const clinician of originalSelectedClinicians) {
                // Check if already scheduled
                const existingAssignment = getWeekAssignments(weekId).find(
                    (assignment) => assignment.clinicianName === clinician.fullName,
                )

                if (!existingAssignment) {
                    // Add the schedule
                    const result = await InstructorScheduleService.addInstructor({
                        MothraId: clinician.mothraId,
                        RotationId: selectedRotation.value!.rotId,
                        WeekIds: [weekId],
                        GradYear: currentYear.value!,
                    })

                    // Update local state if successful
                    if (result.success && result.result?.scheduleIds?.[0] && scheduleData.value?.schedulesBySemester) {
                        const scheduleId = result.result.scheduleIds[0]
                        // Find the week in the schedule data and add the new assignment
                        for (const semester of scheduleData.value.schedulesBySemester) {
                            const targetWeek = semester.weeks.find((w) => w.weekId === weekId)
                            if (targetWeek) {
                                if (!targetWeek.instructorSchedules) {
                                    targetWeek.instructorSchedules = []
                                }
                                targetWeek.instructorSchedules.push({
                                    instructorScheduleId: scheduleId,
                                    mothraId: clinician.mothraId,
                                    fullName: clinician.fullName,
                                    firstName: (clinician as any).firstName || "",
                                    lastName: (clinician as any).lastName || "",
                                    evaluator: true,
                                    isPrimaryEvaluator: false,
                                })
                                break
                            }
                        }
                    }

                    successCount++
                } else {
                    alreadyScheduledCount++
                }
            }
        }

        // Show results
        if (successCount > 0) {
            $q.notify({
                type: "positive",
                message: `Successfully scheduled ${successCount} assignment${successCount !== 1 ? "s" : ""}`,
            })
        }

        if (alreadyScheduledCount > 0) {
            $q.notify({
                type: "info",
                message: `${alreadyScheduledCount} assignment${alreadyScheduledCount !== 1 ? "s were" : " was"} already scheduled`,
            })
        }

        // Clear selections after successful bulk operation
        clearClinicianSelection()
        scheduleViewRef.value?.clearSelection()

        // No need to reload - local state has been updated
    } catch {
        $q.notify({
            type: "negative",
            message: "Failed to complete bulk scheduling. Some assignments may have been created.",
        })
    } finally {
        loadingWeekId.value = null
    }
}

// Alias for bulk scheduling from RecentSelections component
async function scheduleBulkCliniciansToWeeks() {
    // Check if we're in delete mode
    if (isInDeleteMode.value) {
        await deleteBulkAssignments()
    } else {
        await scheduleCliniciansToBulkWeeks()
    }
}

async function deleteBulkAssignments() {
    if (!selectedRotation.value || selectedClinicians.value.length === 0 || selectedWeekIds.value.length === 0) {
        return
    }

    await executeBulkDeletionLogic(
        {
            selectedItems: selectedClinicians,
            selectedWeekIds,
            schedulesBySemester: weeksBySemester,
            getWeekAssignments: (week: any) => getWeekAssignments(week.weekId),
            matchAssignmentToItem: (assignment, clinician) => assignment.clinicianName === clinician.fullName,
            getItemDisplayName: (clinician) => clinician.fullName,
            getItemTypeName: () => "clinician",
        },
        {
            scheduleData: scheduleData.value!,
            removeScheduleWithRollback,
            clearSelections: () => {
                clearClinicianSelection()
                scheduleViewRef.value?.clearSelection()
            },
            showManualConfirmation: true,
        },
    )
}

async function scheduleClinicianToWeek(week: WeekItem) {
    // Check permissions first
    if (!canEditRotation.value) {
        return // Silently ignore if no permission
    }

    // Prevent multiple clicks while loading
    if (loadingWeekId.value === week.weekId) {
        return
    }

    // Handle multi-select mode
    if (selectedClinicians.value.length > 0) {
        await scheduleCliniciansToWeek(week)
        return
    }

    // Backward compatibility: single clinician mode
    if (!selectedClinician.value) {
        $q.notify({
            type: "warning",
            message: "Please select a clinician first",
        })
        return
    }

    if (!selectedRotation.value) {
        $q.notify({
            type: "warning",
            message: "Please select a rotation first",
        })
        return
    }

    // Check if clinician is already scheduled for this week
    const existingAssignment = getWeekAssignments(week.weekId).find(
        (assignment) => assignment.clinicianName === selectedClinician.value,
    )

    if (existingAssignment) {
        $q.notify({
            type: "warning",
            message: `${selectedClinician.value} is already scheduled for this week`,
        })
        return
    }

    // Check permissions
    if (selectedRotation.value.service && !permissionsStore.canEditService(selectedRotation.value.service.serviceId)) {
        // Error notification removed - errors are now handled by GenericError component

        return
    }

    isAddingClinician.value = true
    loadingWeekId.value = week.weekId

    try {
        // Use the stored clinician data
        if (!selectedClinicianData.value) {
            throw new Error("Clinician data not found")
        }

        const clinician = selectedClinicianData.value

        // Check for other rotation assignments (informational)
        const conflictResult = await InstructorScheduleService.checkConflicts({
            mothraId: clinician.mothraId,
            rotationId: selectedRotation.value.rotId,
            weekIds: [week.weekId],
            gradYear: currentYear.value!,
        })

        if (conflictResult.result && conflictResult.result.length > 0) {
            const conflict = conflictResult.result[0]
            if (conflict) {
                const proceed = await new Promise<boolean>((resolve) => {
                    $q.dialog({
                        title: "Other Rotation Assignment",
                        message: `Note: ${selectedClinician.value} is also scheduled for ${conflict.name} during this week. Do you want to add them to this rotation as well?`,
                        persistent: true,
                        ok: { label: "Yes, Add", color: "primary" },
                        cancel: { label: "Cancel", color: "grey" },
                    })
                        .onOk(() => resolve(true))
                        .onCancel(() => resolve(false))
                })

                if (!proceed) {
                    return
                }
            }
        }

        // Add the instructor using the queue-based composable
        if (!scheduleData.value) {
            throw new Error("Schedule data not available")
        }

        addScheduleWithRollback(
            {
                scheduleData: scheduleData.value,
                weekId: week.weekId,
                assignmentData: {
                    clinicianMothraId: clinician.mothraId,
                    clinicianName: selectedClinician.value || "",
                    gradYear: currentYear.value!,
                },
            },
            {
                onSuccess: () => {
                    $q.notify({
                        type: "positive",
                        message: `✓ ${selectedClinician.value} successfully added to Week ${week.weekNumber}`,
                        timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
                    })
                },
                onError: (error) => {
                    $q.notify({
                        type: "negative",
                        message:
                            error ||
                            "Failed to add clinician to schedule. Please try again or contact support if the problem persists.",
                        timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
                    })
                },
            },
        )
    } catch {
        // Error notification removed - errors are now handled by GenericError component
    } finally {
        isAddingClinician.value = false
        loadingWeekId.value = null
    }
}

async function scheduleCliniciansToWeek(week: WeekItem) {
    // Set loading state for this week
    loadingWeekId.value = week.weekId

    if (!selectedRotation.value) {
        $q.notify({
            type: "warning",
            message: "Please select a rotation first",
        })
        loadingWeekId.value = null
        return
    }

    if (selectedClinicians.value.length === 0) {
        $q.notify({
            type: "warning",
            message: "Please select at least one clinician",
        })
        loadingWeekId.value = null
        return
    }

    // Check permissions
    if (selectedRotation.value.service && !permissionsStore.canEditService(selectedRotation.value.service.serviceId)) {
        loadingWeekId.value = null
        return
    }

    isAddingClinician.value = true

    const existingAssignments = getWeekAssignments(week.weekId)
    const alreadyScheduled: string[] = []
    const toSchedule: { fullName: string; mothraId: string }[] = []

    // Check for existing assignments
    for (const clinician of selectedClinicians.value) {
        const exists = existingAssignments.find((assignment) => assignment.clinicianName === clinician.fullName)
        if (exists) {
            alreadyScheduled.push(clinician.fullName)
        } else {
            toSchedule.push(clinician)
        }
    }

    // Show info about already scheduled clinicians
    if (alreadyScheduled.length > 0) {
        $q.notify({
            type: "info",
            message: `${alreadyScheduled.join(" / ")} ${alreadyScheduled.length === 1 ? "is" : "are"} already scheduled for this week`,
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
        })
    }

    if (toSchedule.length === 0) {
        isAddingClinician.value = false
        loadingWeekId.value = null
        return
    }

    try {
        // Check for conflicts for all clinicians
        const conflictPromises = toSchedule.map((clinician) =>
            InstructorScheduleService.checkConflicts({
                mothraId: clinician.mothraId,
                rotationId: selectedRotation.value!.rotId,
                weekIds: [week.weekId],
                gradYear: currentYear.value!,
            }),
        )

        const conflictResults = await Promise.all(conflictPromises)
        const cliniciansWithConflicts: string[] = []

        conflictResults.forEach((result, index) => {
            if (result.result && result.result.length > 0 && result.result[0]) {
                const clinician = toSchedule[index]
                if (clinician) {
                    cliniciansWithConflicts.push(clinician.fullName)
                }
            }
        })

        // If any have conflicts, show a single confirmation dialog
        if (cliniciansWithConflicts.length > 0) {
            const proceed = await new Promise<boolean>((resolve) => {
                $q.dialog({
                    title: "Other Rotation Assignments",
                    message: `Note: ${cliniciansWithConflicts.join(" / ")} ${cliniciansWithConflicts.length === 1 ? "has" : "have"} other rotation assignments during this week. Do you want to add them to this rotation as well?`,
                    persistent: true,
                    ok: { label: "Yes, Add All", color: "primary" },
                    cancel: { label: "Cancel", color: "grey" },
                })
                    .onOk(() => resolve(true))
                    .onCancel(() => resolve(false))
            })

            if (!proceed) {
                isAddingClinician.value = false
                loadingWeekId.value = null
                return
            }
        }

        if (!scheduleData.value) {
            throw new Error("Schedule data not available")
        }

        // Schedule all clinicians
        let successCount = 0
        let errorCount = 0
        const schedulePromises: Promise<void>[] = []

        for (const clinician of toSchedule) {
            const promise = new Promise<void>((resolve) => {
                addScheduleWithRollback(
                    {
                        scheduleData: scheduleData.value!,
                        weekId: week.weekId,
                        assignmentData: {
                            clinicianMothraId: clinician.mothraId,
                            clinicianName: clinician.fullName,
                            gradYear: currentYear.value!,
                        },
                    },
                    {
                        onSuccess: () => {
                            successCount++
                            resolve()
                        },
                        onError: () => {
                            errorCount++
                            resolve()
                        },
                    },
                )
            })

            schedulePromises.push(promise)
        }

        // Wait for all operations to complete
        await Promise.all(schedulePromises)

        // Show summary notification
        if (successCount > 0) {
            $q.notify({
                type: "positive",
                message: `✓ Successfully scheduled ${successCount} clinician${successCount > 1 ? "s" : ""} to Week ${week.weekNumber}`,
                timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
            })
        }
        if (errorCount > 0) {
            $q.notify({
                type: "negative",
                message: `Failed to schedule ${errorCount} clinician${errorCount > 1 ? "s" : ""}`,
                timeout: 5000,
            })
        }
    } catch (_error) {
        $q.notify({
            type: "negative",
            message: "An error occurred while scheduling clinicians",
            timeout: 5000,
        })
    } finally {
        isAddingClinician.value = false
        loadingWeekId.value = null
    }
}

async function removeAssignment(scheduleId: number, clinicianName: string, isPrimary?: boolean) {
    if (!selectedRotation.value || !scheduleData.value) return

    // Check permissions
    if (selectedRotation.value.service && !permissionsStore.canEditService(selectedRotation.value.service.serviceId)) {
        // Error notification removed - errors are now handled by GenericError component
        return
    }

    // Only show confirmation dialog for primary evaluators
    if (isPrimary) {
        const confirmed = await new Promise<boolean>((resolve) => {
            $q.dialog({
                title: "Confirm Primary Evaluator Removal",
                message: `${clinicianName} is the primary evaluator for this week. Are you sure you want to remove them?`,
                cancel: true,
                persistent: true,
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
        })

        if (!confirmed) return
    }

    isRemovingClinician.value = true

    removeScheduleWithRollback(scheduleData.value, scheduleId, {
        onSuccess: (wasPrimary, instructorName) => {
            // Show context-appropriate success notification
            $q.notify({
                type: "positive",
                message: `${clinicianName} has been removed from the schedule`,
                timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
            })

            // Show primary evaluator warning if applicable
            if (wasPrimary) {
                $q.notify({
                    type: "warning",
                    message: `Primary evaluator ${instructorName} has been removed. This week may need a new primary evaluator.`,
                    icon: "star_outline",
                    timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_WARNING,
                })
            }

            isRemovingClinician.value = false
        },
        onError: (error) => {
            $q.notify({
                type: "negative",
                message: error || "Failed to remove clinician from schedule. Please try again.",
                timeout: 5000,
            })
            isRemovingClinician.value = false
        },
    })
}

async function togglePrimary(scheduleId: number, currentIsPrimary: boolean, clinicianName: string) {
    if (!selectedRotation.value || !scheduleData.value) return

    // Check permissions
    if (selectedRotation.value.service && !permissionsStore.canEditService(selectedRotation.value.service.serviceId)) {
        // Error notification removed - errors are now handled by GenericError component

        return
    }

    // If removing primary status, confirm
    if (currentIsPrimary) {
        const confirmed = await new Promise<boolean>((resolve) => {
            $q.dialog({
                title: "Remove Primary Status",
                message: `Remove primary evaluator status from ${clinicianName}? Another clinician should be designated as primary.`,
                cancel: true,
                persistent: true,
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
        })

        if (!confirmed) return
    }

    isTogglingPrimary.value = true

    togglePrimaryWithRollback(
        {
            scheduleData: scheduleData.value,
            scheduleId: scheduleId,
            isPrimary: !currentIsPrimary,
            requiresPrimaryEvaluator: getRequiresPrimaryEvaluatorForSchedule(scheduleId),
        },
        {
            onSuccess: () => {
                const action = currentIsPrimary ? "removed as" : "set as"
                $q.notify({
                    type: "positive",
                    message: `${clinicianName} ${action} primary evaluator`,
                })
                isTogglingPrimary.value = false
            },
            onError: (error) => {
                $q.notify({
                    type: "negative",
                    message: error || "Failed to update primary evaluator status. Please try again.",
                    timeout: 5000,
                })
                isTogglingPrimary.value = false
            },
        },
    )
}

function requiresPrimaryEvaluator(week: WeekItem): boolean {
    // Use business rule from backend to determine if primary evaluator is required
    if (!week.requiresPrimaryEvaluator) return false

    // Check if there's already a primary evaluator assigned
    const assignments = getWeekAssignments(week.weekId)
    const hasPrimaryEvaluator = assignments.some((assignment) => assignment.isPrimary)

    // Return true if backend says it's required but none is assigned
    return !hasPrimaryEvaluator
}

function getRequiresPrimaryEvaluatorForSchedule(scheduleId: number): boolean {
    if (!scheduleData.value || !scheduleData.value.schedulesBySemester) return false

    // Find the week that contains this schedule assignment
    for (const semester of scheduleData.value.schedulesBySemester) {
        for (const week of semester.weeks) {
            if (week.instructorSchedules) {
                const assignment = week.instructorSchedules.find(
                    (schedule) => schedule.instructorScheduleId === scheduleId,
                )
                if (assignment) {
                    // Return true if this week requires a primary evaluator
                    return week.requiresPrimaryEvaluator ?? false
                }
            }
        }
    }
    return false
}

// Watch for URL changes (browser back/forward)
watch(
    () => route.params.rotationId,
    (newRotationId) => {
        if (newRotationId !== selectedRotationId.value?.toString()) {
            selectedRotationId.value = newRotationId ? Number(newRotationId) : null
            // Trigger rotation change if rotations are already loaded
            if (rotations.value.length > 0) {
                onRotationChange()
            }
        }
    },
)

// Lifecycle
onMounted(async () => {
    // Set page title
    document.title = "VIPER - Schedule by Rotation"

    // Initialize from URL (this will override YearSelector's default if year param exists)
    initializeFromUrl()

    // Fetch current grad year from backend
    try {
        currentGradYear.value = await PageDataService.getCurrentGradYear()
    } catch {
        // Fallback to current calendar year if fetch fails
        currentGradYear.value = new Date().getFullYear()
    }

    // Initialize permissions
    try {
        await permissionsStore.fetchUserPermissions()
    } catch {
        // Permission loading failed, continue with limited access
    }

    // Load rotations (ClinicianSelector will handle loading clinicians)
    await loadRotations()

    // If we have a rotation ID from URL, trigger the selection after rotations are loaded
    if (selectedRotationId.value && rotations.value.length > 0) {
        onRotationChange()
    }
})
</script>

<style scoped>
/* Import shared schedule styles */
@import url("../assets/schedule-shared.css");

/* No access section */
.no-access-container {
    display: flex;
    justify-content: center;
    margin-top: 40px;
}

.no-access-card {
    max-width: 400px;
    padding: 20px;
}

.no-access-card h3 {
    color: var(--ucdavis-black-80);
    margin-bottom: 16px;
    font-size: 20px;
}

.no-access-card p {
    margin-bottom: 12px;
    color: var(--ucdavis-black-60);
}
</style>
