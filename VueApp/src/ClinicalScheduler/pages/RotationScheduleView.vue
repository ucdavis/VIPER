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

        <!-- Main content when user has access -->
        <div v-else>
            <div class="schedule-header">
                <div class="row items-center q-mb-md q-gutter-md mobile-column">
                    <div class="row items-center mobile-column q-gutter-md">
                        <h2 class="q-pr-md q-my-none mobile-full-width">Schedule Clinicians for</h2>
                        <div class="col-auto mobile-full-width">
                            <RotationSelector
                                v-model="selectedRotationId"
                                :year="currentYear"
                                :only-with-scheduled-weeks="true"
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

            <!-- Permission info banner -->
            <PermissionInfoBanner />

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
                :is-loading="isLoadingSchedule"
                empty-state-message="No recent clinicians. Please add a clinician below."
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
                empty-state-message="Click to add clinician"
                read-only-empty-message="No assignments"
                warning-icon-title="Primary evaluator required for this week"
                primary-evaluator-title="Primary evaluator. To transfer primary status, click the star on another clinician."
                make-primary-title="Click to make this clinician the primary evaluator."
                primary-removal-disabled-message="Cannot remove primary clinician. Make another clinician primary first."
                @week-click="scheduleClinicianToWeek"
                @remove-assignment="(id, name) => removeAssignment(id, name)"
                @toggle-primary="(id, isPrimary, name) => togglePrimary(id, isPrimary, name)"
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
import { useScheduleNormalization } from "../composables/use-schedule-normalization"
import RotationSelector from "../components/RotationSelector.vue"
import ClinicianSelector from "../components/ClinicianSelector.vue"
import YearSelector from "../components/YearSelector.vue"
import SchedulerNavigation from "../components/SchedulerNavigation.vue"
import type { WeekItem } from "../components/WeekScheduleCard.vue"
import ScheduleBanner from "../components/ScheduleBanner.vue"
import RecentSelections from "../components/RecentSelections.vue"
import PermissionInfoBanner from "../components/PermissionInfoBanner.vue"
import AccessDeniedCard from "../components/AccessDeniedCard.vue"
import { ACCESS_DENIED_MESSAGES, ACCESS_DENIED_SUBTITLES } from "../constants/permission-messages"
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
const { normalizeRotationSchedule } = useScheduleNormalization()

const weeksBySemester = computed(() => {
    return normalizeRotationSchedule(scheduleData.value)
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
                    isPrimary: requiresPrimaryEvaluator(week),
                    gradYear: currentYear.value!,
                },
            },
            {
                onSuccess: () => {
                    $q.notify({
                        type: "positive",
                        message: `✓ ${selectedClinician.value} successfully added to Week ${week.weekNumber}`,
                        timeout: 4000,
                    })
                },
                onError: (error) => {
                    $q.notify({
                        type: "negative",
                        message:
                            error ||
                            "Failed to add clinician to schedule. Please try again or contact support if the problem persists.",
                        timeout: 5000,
                    })
                },
            },
        )
    } catch {
        // Error notification removed - errors are now handled by GenericError component
    } finally {
        isAddingClinician.value = false
    }
}

async function removeAssignment(scheduleId: number, clinicianName: string) {
    if (!selectedRotation.value || !scheduleData.value) return

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

    removeScheduleWithRollback(scheduleData.value, scheduleId, {
        onSuccess: () => {
            $q.notify({
                type: "positive",
                message: `${clinicianName} removed from schedule`,
            })
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
