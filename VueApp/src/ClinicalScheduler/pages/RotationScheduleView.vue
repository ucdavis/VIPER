<template>
    <div class="clinical-scheduler-container">
        <SchedulerNavigation />

        <div class="row items-center q-mb-md">
            <h2 class="col-auto q-pr-sm">Schedule Clinicians for</h2>
            <div class="col-auto">
                <RotationSelector
                    v-model="selectedRotationId"
                    :year="currentYear"
                    :only-with-scheduled-weeks="true"
                    @rotation-selected="onRotationSelected"
                    style="min-width: 300px"
                />
            </div>
            <div class="col-auto">
                <YearSelector
                    v-model="currentYear"
                    @year-changed="onYearChange"
                    style="min-width: 120px"
                />
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
            :selected-item="selectedClinicianItem"
            recent-label="Recent Clinicians:"
            add-new-label="Add New Clinician:"
            item-type="clinician"
            item-key-field="fullName"
            item-display-field="fullName"
            label-spacing="md"
            selector-spacing="lg"
            @select-item="selectClinicianItem"
            @clear-selection="clearClinicianSelection"
        >
            <template #selector>
                <ClinicianSelector
                    :model-value="null"
                    :year="currentYear"
                    :include-all-affiliates="includeAllAffiliates"
                    @update:include-all-affiliates="includeAllAffiliates = $event"
                    @change="onAddClinicianSelected"
                    :affiliates-toggle-label="'Include all affiliates'"
                    class="q-mt-none"
                    style="min-width: 280px"
                />
            </template>
        </RecentSelections>

        <!-- No weeks scheduled message -->
        <ScheduleBanner
            v-if="selectedRotation && weeksBySeason.length === 0 && !isLoadingSchedule"
            type="no-entries"
            :custom-message="`${selectedRotation.name} has no weeks scheduled for ${currentYear}.`"
        />

        <!-- Season headers and week grids -->
        <div v-if="selectedRotation && weeksBySeason.length > 0">
            <div
                v-for="season in weeksBySeason"
                :key="season.name"
            >
                <h3>{{ season.displayName }}</h3>
                <div class="row q-gutter-md q-mb-lg">
                    <WeekScheduleCard
                        v-for="week in season.weeks"
                        :key="week.weekId"
                        :week="week"
                        :is-past-year="isPastYear"
                        :additional-classes="{ 'requires-primary-card': requiresPrimaryEvaluator(week) }"
                        @click="scheduleClinicianToWeek"
                    >
                        <template #week-icon="{ week: weekItem }">
                            <q-icon
                                v-if="requiresPrimaryEvaluator(weekItem)"
                                name="warning"
                                size="xs"
                                color="orange"
                                title="Primary evaluator required for this week"
                            />
                        </template>

                        <template #assignments="{ week: weekItem }">
                            <div
                                v-if="getWeekAssignments(weekItem.weekId).length > 0"
                                class="q-gutter-xs"
                            >
                                <div
                                    v-for="assignment in getWeekAssignments(weekItem.weekId)"
                                    :key="assignment.id"
                                    class="assignment-item"
                                >
                                    <div class="row items-center q-gutter-xs">
                                        <!-- Remove button - only show for current year -->
                                        <q-icon
                                            v-if="!isPastYear && !assignment.isPrimary"
                                            name="close"
                                            size="xs"
                                            color="negative"
                                            class="cursor-pointer"
                                            title="Remove this clinician from the schedule."
                                            aria-label="Remove clinician from schedule"
                                            @click.stop="removeAssignment(assignment.id, assignment.clinicianName)"
                                        />
                                        <q-icon
                                            v-else-if="!isPastYear && canEditRotation"
                                            name="close"
                                            size="xs"
                                            color="grey-4"
                                            title="Cannot remove primary clinician. Make another clinician primary first."
                                            aria-label="Cannot remove primary clinician"
                                        />

                                        <span class="col text-body2">{{ assignment.clinicianName }}</span>

                                        <!-- Primary star - for past years, only show filled stars without click handler -->
                                        <q-icon
                                            v-if="isPastYear && assignment.isPrimary"
                                            name="star"
                                            size="xs"
                                            color="amber"
                                            title="Primary evaluator"
                                            aria-label="Primary evaluator"
                                        />
                                        <q-icon
                                            v-else-if="!isPastYear && canEditRotation"
                                            :name="assignment.isPrimary ? 'star' : 'star_outline'"
                                            size="xs"
                                            :color="assignment.isPrimary ? 'amber' : 'grey-5'"
                                            class="cursor-pointer"
                                            :title="
                                                assignment.isPrimary
                                                    ? 'Primary evaluator. To transfer primary status, click the star on another clinician.'
                                                    : 'Click to make this clinician the primary evaluator.'
                                            "
                                            :aria-label="
                                                assignment.isPrimary
                                                    ? 'Primary evaluator - click to transfer to another clinician'
                                                    : 'Click to make this clinician the primary evaluator'
                                            "
                                            @click.stop="
                                                togglePrimary(
                                                    assignment.id,
                                                    assignment.isPrimary,
                                                    assignment.clinicianName,
                                                )
                                            "
                                        />
                                    </div>
                                </div>
                            </div>

                            <div
                                v-else
                                class="text-center q-py-sm"
                            >
                                <div class="text-grey-6 text-caption">
                                    {{ !isPastYear && canEditRotation ? "Click to add clinician" : "No assignments" }}
                                </div>
                            </div>
                        </template>
                    </WeekScheduleCard>
                </div>
            </div>
        </div>

        <!-- Legend -->
        <ScheduleLegend
            v-if="selectedRotation && weeksBySeason.length > 0"
            :show-warning="true"
        />
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
import { useScheduleStateUpdater } from "../composables/use-schedule-state-updater"
import RotationSelector from "../components/RotationSelector.vue"
import ClinicianSelector from "../components/ClinicianSelector.vue"
import YearSelector from "../components/YearSelector.vue"
import ScheduleLegend from "../components/ScheduleLegend.vue"
import SchedulerNavigation from "../components/SchedulerNavigation.vue"
import WeekScheduleCard, { type WeekItem } from "../components/WeekScheduleCard.vue"
import ScheduleBanner from "../components/ScheduleBanner.vue"
import RecentSelections from "../components/RecentSelections.vue"

// Router and Quasar
const route = useRoute()
const router = useRouter()
const $q = useQuasar()

// Permissions store
const permissionsStore = usePermissionsStore()

// Schedule state updater composable
const { addScheduleToWeek, removeScheduleFromWeek, updateSchedulePrimaryStatus } = useScheduleStateUpdater()

// Reactive data
const selectedRotationId = ref<number | null>(null)
const selectedRotation = ref<RotationWithService | null>(null)
const selectedClinician = ref<string | null>(null)
const selectedClinicianData = ref<{ fullName: string; mothraId: string } | null>(null)
const rotations = ref<RotationWithService[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

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

// Year selection is now handled by YearSelector component

const isPastYear = computed(() => {
    // Compare selected year against the current grad year from backend
    return currentYear.value !== null && currentYear.value < currentGradYear.value
})

const canEditRotation = computed(() => {
    if (!selectedRotation.value || !selectedRotation.value.service) return false
    return permissionsStore.canEditService(selectedRotation.value.service.serviceId)
})

// ClinicianSelector component handles the clinician dropdown functionality

// Recent clinicians from API response (includes current and previous year)

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

const selectedClinicianItem = computed(() => {
    if (!selectedClinician.value) return null
    return clinicianItems.value.find((item) => item.fullName === selectedClinician.value) || null
})

// Use the semester data directly from the backend API
// The backend already groups weeks by semester using TermCodeService.GetTermCodeDescription()
const weeksBySeason = computed(() => {
    if (!scheduleData.value || !scheduleData.value.schedulesBySemester) return []

    // Transform backend semester structure to match frontend expectations
    return scheduleData.value.schedulesBySemester.map((semester) => ({
        name: semester.semester.toLowerCase().replace(/\s+/g, "-"), // Convert "Spring Semester 2024" to "spring-semester-2024"
        displayName: semester.semester, // Use exact semester name from TermCodeService
        weeks: semester.weeks,
    }))
})

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
        const result = await RotationService.getRotations({ includeService: true })

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
    selectedRotation.value = rotations.value.find((r: RotationWithService) => r.rotId === rotationId) || null
    onRotationSelected(selectedRotation.value)
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
function selectClinicianItem(clinicianItem: { fullName: string; mothraId: string }) {
    selectedClinician.value = clinicianItem.fullName
    selectedClinicianData.value = clinicianItem
}

function clearClinicianSelection() {
    selectedClinician.value = null
    selectedClinicianData.value = null
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
                isPrimary: schedule.isPrimaryEvaluator,
                mothraId: schedule.mothraId,
            }))
        }
    }

    return []
}

async function scheduleClinicianToWeek(week: WeekItem) {
    // Check permissions first
    if (!canEditRotation.value) {
        return // Silently ignore if no permission
    }

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

        // Add the instructor to the schedule
        const result = await InstructorScheduleService.addInstructor({
            MothraId: clinician.mothraId,
            RotationId: selectedRotation.value.rotId,
            WeekIds: [week.weekId],
            GradYear: currentYear.value!,
            IsPrimaryEvaluator: requiresPrimaryEvaluator(week), // Set as primary if required and none exists
        })

        if (result.success) {
            // Show success message with optional warning
            let message = `✓ ${selectedClinician.value} successfully added to Week ${week.weekNumber}`

            // Append warning message if instructor is scheduled for other rotations
            if (result.result?.warningMessage) {
                message += `\n\n${result.result.warningMessage}`
            }

            $q.notify({
                type: result.result?.warningMessage ? "warning" : "positive",
                message: message,
                timeout: result.result?.warningMessage ? 6000 : 4000,
                multiLine: result.result?.warningMessage ? true : false,
                actions: [
                    {
                        icon: "close",
                        color: "white",
                        handler: () => {
                            /* dismiss */
                        },
                    },
                ],
            })

            // Update local state instead of refetching
            if (scheduleData.value) {
                addScheduleToWeek(scheduleData.value, week.weekId, {
                    instructorScheduleId:
                        result.result?.schedules?.[0]?.instructorScheduleId || result.result?.scheduleIds?.[0] || 0,
                    mothraId: selectedClinicianData.value?.mothraId || "",
                    clinicianName: selectedClinician.value || "",
                    isPrimaryEvaluator: requiresPrimaryEvaluator(week),
                })
            }
        } else {
            throw new Error(result.errors.join(", "))
        }
    } catch {
        // Error notification removed - errors are now handled by GenericError component
    } finally {
        isAddingClinician.value = false
    }
}

async function removeAssignment(scheduleId: number, clinicianName: string) {
    if (!selectedRotation.value) return

    // Check permissions
    if (selectedRotation.value.service && !permissionsStore.canEditService(selectedRotation.value.service.serviceId)) {
        // Error notification removed - errors are now handled by GenericError component

        return
    }

    // Confirm removal
    const confirmed = await new Promise<boolean>((resolve) => {
        $q.dialog({
            title: "Confirm Removal",
            message: `Remove ${clinicianName} from this week's schedule?`,
            cancel: true,
            persistent: true,
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
    })

    if (!confirmed) return

    isRemovingClinician.value = true

    try {
        const result = await InstructorScheduleService.removeInstructor(scheduleId)

        if (result.success) {
            $q.notify({
                type: "positive",
                message: `${clinicianName} removed from schedule`,
            })

            // Update local state instead of refetching
            if (scheduleData.value) {
                removeScheduleFromWeek(scheduleData.value, scheduleId)
            }
        } else {
            const errorMessage =
                result.errors && result.errors.length > 0 ? result.errors.join(", ") : "Unknown error occurred"
            throw new Error(errorMessage)
        }
    } catch {
        // Error notification removed - errors are now handled by GenericError component
    } finally {
        isRemovingClinician.value = false
    }
}

async function togglePrimary(scheduleId: number, currentIsPrimary: boolean, clinicianName: string) {
    if (!selectedRotation.value) return

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

    try {
        const result = await InstructorScheduleService.setPrimaryEvaluator(scheduleId, !currentIsPrimary)

        if (result.success) {
            const action = currentIsPrimary ? "removed as" : "set as"
            $q.notify({
                type: "positive",
                message: `${clinicianName} ${action} primary evaluator`,
            })

            // Update local state instead of refetching
            if (scheduleData.value) {
                updateSchedulePrimaryStatus(scheduleData.value, scheduleId, !currentIsPrimary)
            }
        } else {
            const errorMessage =
                result.errors && result.errors.length > 0 ? result.errors.join(", ") : "Unknown error occurred"
            throw new Error(errorMessage)
        }
    } catch {
        // Error notification removed - errors are now handled by GenericError component
    } finally {
        isTogglingPrimary.value = false
    }
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
</style>
