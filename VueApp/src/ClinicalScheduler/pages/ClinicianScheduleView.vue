<template>
    <div class="clinical-scheduler-container position-relative">
        <SchedulerNavigation />

        <!-- Loading permissions state - show this until we have permission data -->
        <div
            v-if="isLoadingPermissions"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading...</div>
        </div>

        <!-- Access denied for users with rotation-specific permissions -->
        <AccessDeniedCard
            v-else-if="!canAccessClinicianView"
            :message="ACCESS_DENIED_MESSAGES.CLINICIAN_VIEW"
            :subtitle="ACCESS_DENIED_SUBTITLES.CLINICIAN_VIEW"
        >
            <template #actions>
                <q-btn
                    color="primary"
                    label="Return to Home"
                    @click="goToHome"
                />
            </template>
        </AccessDeniedCard>

        <!-- Main content for users with appropriate permissions -->
        <div v-else>
            <div class="schedule-header">
                <div class="row items-center q-mb-md q-gutter-md mobile-column">
                    <div class="row items-center mobile-column q-gutter-md">
                        <h2 class="q-pr-md q-my-none mobile-full-width">Schedule Rotations for</h2>
                        <div class="col-auto mobile-full-width mobile-extra-spacing">
                            <ClinicianSelector
                                v-model="selectedClinician"
                                :year="currentYear"
                                :include-all-affiliates="includeAllAffiliates"
                                :show-affiliates-toggle="true"
                                :is-own-schedule-only="hasClinicianViewReadOnly"
                                @change="onClinicianChange"
                                @update:include-all-affiliates="includeAllAffiliates = $event"
                                @clinicians-loaded="handleClinicianSelectorReady"
                                :is-past-year="isPastYear"
                                view-context="clinician"
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

            <!-- Helper message for clinicians with no assignments - moved to top -->
            <ScheduleBanner
                v-if="selectedClinician && hasNoAssignments && !loadingSchedule"
                type="no-entries"
                :custom-message="`${selectedClinician?.fullName || 'This clinician'} has no rotation assignments for ${currentYear}.`"
            />

            <!-- Read-only notice for past years -->
            <ScheduleBanner
                v-if="isPastYear"
                type="read-only"
            />

            <!-- No clinician selected -->
            <ScheduleBanner
                v-if="!selectedClinician && !loadingSchedule"
                type="info"
                :custom-message="SCHEDULE_MESSAGES.SELECTION.NO_CLINICIAN"
            />

            <!-- Schedule display -->
            <div v-if="selectedClinician">
                <!-- Rotation selector section (only show when not past year) -->
                <RecentSelections
                    v-if="!isPastYear"
                    :items="rotationItems"
                    :local-selected-items="selectedRotations"
                    :multi-select="true"
                    :recent-label="SCHEDULE_LABELS.RECENT_ROTATIONS"
                    :add-new-label="SCHEDULE_LABELS.ADD_NEW_ROTATION"
                    :item-type="SCHEDULE_LABELS.ITEM_TYPE.ROTATION"
                    item-key-field="rotId"
                    item-display-field="name"
                    selector-spacing="none"
                    :is-loading="loadingSchedule"
                    :empty-state-message="SCHEDULE_MESSAGES.RECENT.EMPTY_ROTATIONS"
                    :selected-weeks-count="selectedWeekIds.length"
                    :show-schedule-button="true"
                    :is-alt-key-held="isAltKeyHeld"
                    :is-delete-mode="isInDeleteMode"
                    @select-items="selectRotations"
                    @clear-selection="clearRotationSelection"
                    @schedule-selected="handleScheduleSelected"
                >
                    <template #before-dropdown>
                        <PrimaryEvaluatorToggle
                            v-model="makePrimaryEvaluator"
                            :selection-count="selectedRotations.length"
                            item-type="rotation"
                        />
                    </template>
                    <template #selector>
                        <RotationSelector
                            v-model="selectedNewRotationId"
                            :exclude-rotation-names="assignedRotationNames"
                            :only-with-scheduled-weeks="true"
                            :year="currentYear"
                            @rotation-selected="onAddRotationSelected"
                            style="min-width: 280px"
                            class="q-mt-none"
                        />
                    </template>
                </RecentSelections>

                <!-- Generic Schedule View Component -->
                <ScheduleView
                    ref="scheduleViewRef"
                    :schedules-by-semester="schedulesBySemester"
                    view-mode="clinician"
                    :is-past-year="isPastYear"
                    :is-loading="loadingSchedule"
                    :error="scheduleError"
                    :can-edit="!isPastYear"
                    :show-legend="true"
                    :show-warning-icon="false"
                    :show-primary-toggle="true"
                    :enable-week-selection="!isPastYear"
                    :no-data-message="`${selectedClinician?.fullName || 'This clinician'} has no rotation assignments for ${currentYear}.`"
                    empty-state-message="Click to add rotation"
                    read-only-empty-message="No assignment"
                    primary-evaluator-title="Primary evaluator. To transfer primary status, click the star on another clinician."
                    make-primary-title="Click to make this clinician the primary evaluator."
                    :get-assignments="getClinicianWeekAssignments"
                    :requires-primary-evaluator="() => false"
                    :loading-week-id="loadingWeekId"
                    @week-click="onWeekClick"
                    @remove-assignment="handleRemoveRotation"
                    @toggle-primary="handleTogglePrimary"
                    @selected-weeks-change="onSelectedWeeksChange"
                />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar } from "quasar"
import ClinicianSelector from "../components/ClinicianSelector.vue"
import PrimaryEvaluatorToggle from "../components/PrimaryEvaluatorToggle.vue"
import YearSelector from "../components/YearSelector.vue"
import RotationSelector from "../components/RotationSelector.vue"
import SchedulerNavigation from "../components/SchedulerNavigation.vue"
import ScheduleView, { type ScheduleAssignment, type ScheduleSemester } from "../components/ScheduleView.vue"
import { type WeekItem } from "../components/WeekScheduleCard.vue"
import { ClinicianService, type Clinician, type ClinicianScheduleData } from "../services/clinician-service"
import { InstructorScheduleService } from "../services/instructor-schedule-service"
import { PageDataService } from "../services/page-data-service"
import { usePermissionsStore } from "../stores/permissions"
import { useScheduleUpdatesWithRollback } from "../composables/use-optimistic-schedule-updates"
import { useBulkDeletionLogic } from "../composables/use-bulk-deletion-logic"
import { useScheduleNormalization } from "../composables/use-schedule-normalization"
import { useDeleteMode } from "../composables/use-delete-mode"
import type { RotationWithService } from "../types/rotation-types"
import ScheduleBanner from "../components/ScheduleBanner.vue"
import RecentSelections from "../components/RecentSelections.vue"
import PermissionInfoBanner from "../components/PermissionInfoBanner.vue"
import AccessDeniedCard from "../components/AccessDeniedCard.vue"
import {
    ACCESS_DENIED_MESSAGES,
    ACCESS_DENIED_SUBTITLES,
    SCHEDULE_OPERATION_ERRORS,
} from "../constants/permission-messages"
import { UI_CONFIG } from "../constants/app-constants"
import { isRotationExcluded } from "../constants/rotation-constants"
import { SCHEDULE_MESSAGES, SCHEDULE_LABELS } from "../constants/schedule-config"
import { usePermissionChecks } from "../composables/use-permission-checks"

// Interface for clinician rotation item
interface ClinicianRotationItem {
    rotId: number
    serviceId?: number
    name?: string
    abbreviation?: string
    serviceName?: string
    serviceShortName?: string
    assignmentCount?: number
    isPrimaryEvaluator?: boolean
}

const route = useRoute()
const router = useRouter()
const $q = useQuasar()

// Permissions composable and store
const { isLoadingPermissions, canAccessClinicianView, hasClinicianViewReadOnly, goToHome } = usePermissionChecks()
const permissionsStore = usePermissionsStore()

// Composables for optimistic updates
const { addScheduleWithRollback, removeScheduleWithRollback, togglePrimaryWithRollback } =
    useScheduleUpdatesWithRollback()

const { executeBulkDeletionLogic } = useBulkDeletionLogic()
const { normalizeClinicianSchedule, getAssignedRotationNames } = useScheduleNormalization()

// Component state
const selectedClinician = ref<Clinician | null>(null)
const clinicianSchedule = ref<ClinicianScheduleData | null>(null)
const clinicianRotations = ref<ClinicianRotationItem[]>([])
const selectedRotation = ref<ClinicianRotationItem | null>(null)
const selectedRotations = ref<RotationWithService[]>([])
const selectedNewRotationId = ref<number | null>(null)
const additionalRotation = ref<RotationWithService | null>(null)
const loadingSchedule = ref(false)
const scheduleError = ref<string | null>(null)
const currentYear = ref<number | null>(null)
const currentGradYear = ref<number>(new Date().getFullYear())
const includeAllAffiliates = ref(false)
const urlMothraId = ref<string | null>(null)

// Loading states for operations
const isAddingRotation = ref(false)
const isRemovingRotation = ref(false)
const isTogglingPrimary = ref(false)
const loadingWeekId = ref<number | null>(null)
const selectedWeekIds = ref<number[]>([])

// Primary evaluator checkbox state
const makePrimaryEvaluator = ref(false)

// Component refs
const scheduleViewRef = ref<any>(null)

const isPastYear = computed(() => {
    return currentYear.value !== null && currentYear.value < currentGradYear.value
})

// Track Alt key state from ScheduleView
const isAltKeyHeld = computed(() => {
    return scheduleViewRef.value?.isAltKeyHeld || false
})

// Computed property for rotation items to show in RecentSelections
const rotationItems = computed(() => {
    const items: RotationWithService[] = []

    // Add the newly selected rotation if it's not in the list
    if (additionalRotation.value) {
        items.push(additionalRotation.value)
    }

    // Add rotations from backend
    for (const rotation of clinicianRotations.value) {
        // Don't add duplicates (check by rotId)
        if (!items.some((item) => item.rotId === rotation.rotId)) {
            items.push(rotation as RotationWithService)
        }
    }

    return items.sort((a, b) => (a.name || "").localeCompare(b.name || ""))
})

const canEditRotation = (rotationId: number) => {
    // First check if user has EditOwnSchedule permission and is editing their own schedule
    if (permissionsStore.hasEditOwnSchedulePermission && selectedClinician.value) {
        // Check if the current user is editing their own schedule
        const currentUserMothraId = permissionsStore.user?.mothraId
        if (
            currentUserMothraId &&
            currentUserMothraId.toLowerCase() === selectedClinician.value.mothraId.toLowerCase()
        ) {
            return true // User can edit their own schedule
        }
    }

    // Otherwise, check service-level permissions
    let rotation = clinicianRotations.value.find((r) => r.rotId === rotationId)

    if (!rotation && additionalRotation.value && additionalRotation.value.rotId === rotationId) {
        rotation = additionalRotation.value
    }

    if (!rotation || !rotation.serviceId) return false
    return permissionsStore.canEditService(rotation.serviceId)
}

const schedulesBySemester = computed<ScheduleSemester[]>(() => {
    return normalizeClinicianSchedule(clinicianSchedule.value)
})

// Use shared delete mode logic
const { isInDeleteMode } = useDeleteMode({
    selectedItems: selectedRotations,
    selectedWeekIds,
    schedulesBySemester,
    getWeekAssignments: getClinicianWeekAssignments,
    getItemIdentifier: (rotation: ClinicianRotationItem) => rotation.rotId,
    getAssignmentIdentifier: (assignment: ScheduleAssignment) => (assignment as any).rotationId,
})

const assignedRotationNames = computed(() => {
    return getAssignedRotationNames(clinicianRotations.value)
})

const hasNoAssignments = computed(() => {
    if (loadingSchedule.value) return false
    if (!schedulesBySemester.value) return false
    if (schedulesBySemester.value.length === 0) return true

    return schedulesBySemester.value.every((semester: ScheduleSemester) =>
        semester.weeks.every((week: any) => !week.rotations || week.rotations.length === 0),
    )
})

// Helper function to get assignments for the generic ScheduleView
function getClinicianWeekAssignments(week: WeekItem): ScheduleAssignment[] {
    const weekWithRotations = week as any
    if (!weekWithRotations.rotations || !Array.isArray(weekWithRotations.rotations)) return []

    // Filter out excluded rotations (like "vacation")
    return weekWithRotations.rotations
        .filter((rotation: any) => !isRotationExcluded(rotation.name))
        .map((rotation: any) => ({
            id: rotation.scheduleId || 0,
            displayName: rotation.name || "",
            isPrimary: rotation.isPrimaryEvaluator || false,
            rotationId: rotation.rotationId,
            serviceId: rotation.serviceId,
        }))
}

// Alias for consistency with bulk scheduling function
const getWeekAssignments = getClinicianWeekAssignments

const fetchClinicianSchedule = async (mothraId: string) => {
    loadingSchedule.value = true
    scheduleError.value = null
    clinicianSchedule.value = null
    clinicianRotations.value = []
    additionalRotation.value = null

    try {
        // Ensure we have a valid year
        const yearToUse = currentYear.value || currentGradYear.value || new Date().getFullYear()

        const result = await ClinicianService.getClinicianSchedule(mothraId, { year: yearToUse })
        if (result.success) {
            clinicianSchedule.value = result.result

            const rotationMap = new Map<number, ClinicianRotationItem>()
            if (result.result.schedulesBySemester) {
                for (const semester of result.result.schedulesBySemester) {
                    for (const week of semester.weeks) {
                        // Ensure rotations is always an array
                        if (!week.rotations) {
                            week.rotations = []
                        }
                        const rotationsToProcess = week.rotations

                        for (const rotation of rotationsToProcess) {
                            // Skip excluded rotations like "vacation"
                            if (
                                rotation &&
                                !rotationMap.has(rotation.rotationId) &&
                                !isRotationExcluded(rotation.name)
                            ) {
                                rotationMap.set(rotation.rotationId, {
                                    rotId: rotation.rotationId,
                                    name: rotation.name,
                                    abbreviation: rotation.abbreviation,
                                    serviceName: rotation.serviceName,
                                    serviceId: rotation.serviceId,
                                })
                            }
                        }
                    }
                }
            }

            clinicianRotations.value = Array.from(rotationMap.values()).sort((a, b) =>
                (a.name || "").localeCompare(b.name || ""),
            )

            selectedRotation.value = null
        } else {
            scheduleError.value = result.errors.join(", ") || "Failed to load schedule"
        }
    } catch (error) {
        scheduleError.value = error instanceof Error ? error.message : "Failed to load schedule"
    } finally {
        loadingSchedule.value = false
    }
}

const onClinicianChange = (clinician: Clinician | null) => {
    // Clear rotation selections when changing clinicians
    selectedRotation.value = null
    selectedRotations.value = []

    if (clinician) {
        const query: Record<string, string | number> = {}
        if (currentYear.value !== null && currentYear.value !== new Date().getFullYear()) {
            query.year = currentYear.value
        }
        router
            .push({
                name: "ClinicianScheduleWithId",
                params: { mothraId: encodeURIComponent(clinician.mothraId) },
                query,
            })
            .catch(() => {
                /* handle navigation error */
            })
        fetchClinicianSchedule(clinician.mothraId).catch(() => {
            /* handle fetch error */
        })
    } else {
        router.push({ name: "ClinicianSchedule" }).catch(() => {
            /* handle navigation error */
        })
        clinicianSchedule.value = null
        clinicianRotations.value = []
        selectedRotation.value = null
    }
}

const onYearChange = () => {
    if (selectedClinician.value) {
        const query: Record<string, string | number> = {}
        if (currentYear.value !== null) {
            query.year = currentYear.value
        }
        router
            .push({
                name: "ClinicianScheduleWithId",
                params: { mothraId: encodeURIComponent(selectedClinician.value.mothraId) },
                query,
            })
            .catch(() => {
                /* handle navigation error */
            })
        fetchClinicianSchedule(selectedClinician.value.mothraId).catch(() => {
            /* handle fetch error */
        })
    } else {
        const query: Record<string, string | number> = {}
        if (currentYear.value !== null) {
            query.year = currentYear.value
        }
        router.push({ query }).catch(() => {
            /* handle navigation error */
        })
    }
}

// Initialize from URL parameters
const initializeFromUrl = () => {
    const yearParam = route.query.year
    if (yearParam && typeof yearParam === "string") {
        currentYear.value = Number(yearParam)
    } else if (!currentYear.value) {
        // Default to current grad year if no year specified
        currentYear.value = currentGradYear.value || new Date().getFullYear()
    }
}

const loadClinicianFromUrl = async () => {
    const mothraId = route.params.mothraId as string
    if (mothraId) {
        const decodedMothraId = decodeURIComponent(mothraId)

        // Store for later use by handleClinicianSelectorReady
        urlMothraId.value = decodedMothraId

        try {
            // Ensure year is set before fetching
            if (!currentYear.value) {
                // Year not set when loading clinician from URL, using current year
                currentYear.value = currentGradYear.value || new Date().getFullYear()
            }

            // Fetch the schedule which includes clinician data
            await fetchClinicianSchedule(decodedMothraId)

            // Mark that we've already loaded the schedule to prevent duplicate calls
            if (clinicianSchedule.value?.clinician) {
                // Create a temporary clinician object to prevent the watcher from refetching
                selectedClinician.value = {
                    mothraId: clinicianSchedule.value.clinician.mothraId,
                    firstName: clinicianSchedule.value.clinician.firstName || "",
                    lastName: clinicianSchedule.value.clinician.lastName || "",
                    fullName: clinicianSchedule.value.clinician.fullName || decodedMothraId,
                    role: clinicianSchedule.value.clinician.role || "",
                }
                // Clear urlMothraId since we've handled it
                urlMothraId.value = null
            }
        } catch (error) {
            scheduleError.value = error instanceof Error ? error.message : "Failed to load schedule"
        }
    }
}

// Handle when ClinicianSelector has loaded its clinicians and we have a URL mothraId
const handleClinicianSelectorReady = (clinicians: Clinician[]) => {
    // Only update if we have a URL mothraId and haven't already set the clinician
    if (urlMothraId.value && selectedClinician.value?.mothraId !== urlMothraId.value) {
        // Find the full clinician data from the list
        const clinician = clinicians.find((c) => c.mothraId === urlMothraId.value)
        if (clinician) {
            // Update with the full clinician data from the selector
            selectedClinician.value = clinician
        } else if (selectedClinician.value?.mothraId === urlMothraId.value) {
            // We already have the clinician set from loadClinicianFromUrl
            // Just update with any missing details if available
            const matchingClinician = clinicians.find((c) => c.mothraId === selectedClinician.value?.mothraId)
            if (matchingClinician) {
                selectedClinician.value = matchingClinician
            }
        }
        urlMothraId.value = null
    }
}

const _selectRotation = (rotation: ClinicianRotationItem | RotationWithService) => {
    selectedRotation.value = rotation as ClinicianRotationItem
}

const selectRotations = (rotations: RotationWithService[]) => {
    selectedRotations.value = rotations
    // For backward compatibility, set single selection to the first item
    if (rotations.length > 0) {
        selectedRotation.value = rotations[0] as ClinicianRotationItem
    } else {
        selectedRotation.value = null
    }
}

const clearRotationSelection = () => {
    selectedRotation.value = null
    selectedRotations.value = []
    makePrimaryEvaluator.value = false
    // Also clear week selection when clearing rotation selection
    selectedWeekIds.value = []
    if (scheduleViewRef.value) {
        scheduleViewRef.value.clearSelection()
    }
}

const handleScheduleSelected = async () => {
    // Check if we're in delete mode
    if (isInDeleteMode.value) {
        await deleteBulkAssignments()
    } else {
        // Trigger bulk scheduling when button is clicked
        if (selectedWeekIds.value.length > 0 && selectedRotations.value.length > 0) {
            await scheduleBulkRotationsToWeeks()
        }
    }
}

const scheduleRotationToWeek = async (week: WeekItem) => {
    // Prevent multiple clicks while loading
    if (loadingWeekId.value === week.weekId) {
        return
    }

    // Handle multi-select mode
    if (selectedRotations.value.length > 0) {
        await scheduleRotationsToWeek(week)
        return
    }

    // Backward compatibility: single rotation mode
    if (!selectedRotation.value || !selectedClinician.value) return
    if (!clinicianSchedule.value) return

    if (!canEditRotation(selectedRotation.value.rotId)) {
        $q.notify({
            type: "negative",
            message: SCHEDULE_OPERATION_ERRORS.NO_PERMISSION_EDIT_ROTATION,
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
        })
        return
    }

    isAddingRotation.value = true
    loadingWeekId.value = week.weekId

    const currentSelectedRotation = selectedRotation.value
    const currentAdditionalRotation = additionalRotation.value

    try {
        const conflictResult = await InstructorScheduleService.checkConflicts({
            mothraId: selectedClinician.value.mothraId,
            rotationId: selectedRotation.value.rotId,
            weekIds: [week.weekId],
            gradYear: currentYear.value!,
        })

        if (conflictResult.result && conflictResult.result.length > 0) {
            const conflictNames = conflictResult.result.map((c) => c.name).join(", ")
            const proceed = await new Promise<boolean>((resolve) => {
                $q.dialog({
                    title: "Multiple Rotation Assignment",
                    message: `${selectedClinician.value!.fullName} is already scheduled for ${conflictNames} during this week. Do you want to add ${selectedRotation.value!.name} as an additional rotation?`,
                    persistent: true,
                    ok: {
                        label: "Add Rotation",
                        color: "primary",
                    },
                    cancel: {
                        label: "Cancel",
                    },
                })
                    .onOk(() => resolve(true))
                    .onCancel(() => resolve(false))
            })

            if (!proceed) {
                return
            }
        }

        // Use the optimistic update composable
        await addScheduleWithRollback(
            {
                scheduleData: clinicianSchedule.value,
                weekId: week.weekId,
                assignmentData: {
                    rotationId: selectedRotation.value.rotId,
                    rotationName: selectedRotation.value.name,
                    rotationAbbreviation: selectedRotation.value.abbreviation,
                    serviceId: selectedRotation.value.serviceId,
                    serviceName: selectedRotation.value.serviceName,
                    isPrimary: makePrimaryEvaluator.value,
                    gradYear: currentYear.value || currentGradYear.value,
                },
            },
            {
                onSuccess: () => {
                    let message = `✓ ${selectedRotation.value!.name} scheduled successfully for Week ${week.weekNumber}`
                    $q.notify({
                        type: "positive",
                        message: message,
                        timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
                    })

                    // Keep the rotation selected for quick adding to other weeks but clear dropdown
                    selectedRotation.value = currentSelectedRotation
                    additionalRotation.value = currentAdditionalRotation
                    selectedNewRotationId.value = null
                },
                onError: (error) => {
                    let userMessage = "Failed to schedule rotation"
                    if (error.includes("already scheduled") || error.includes("already exists")) {
                        userMessage = `${selectedRotation.value!.name} is already scheduled for this week`
                    } else if (error.includes("permission")) {
                        userMessage = SCHEDULE_OPERATION_ERRORS.NO_PERMISSION_SCHEDULE_ROTATION
                    } else if (error.includes("not found")) {
                        userMessage = "The selected rotation or week was not found"
                    } else {
                        userMessage = error
                    }

                    $q.notify({
                        type: "negative",
                        message: userMessage,
                        timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
                    })
                },
            },
        )
    } catch {
        // Error already handled by onError callback
    } finally {
        isAddingRotation.value = false
        loadingWeekId.value = null
    }
}

const scheduleBulkRotationsToWeeks = async () => {
    if (!selectedClinician.value || !clinicianSchedule.value) return
    if (selectedRotations.value.length === 0 || selectedWeekIds.value.length === 0) return

    isAddingRotation.value = true

    try {
        // Get all weeks from the schedule
        const allWeeks = schedulesBySemester.value.flatMap((s: ScheduleSemester) => s.weeks)

        // First, check what's already scheduled
        let totalToSchedule = 0
        let totalAlreadyScheduled = 0
        const schedulingPlan: { week: any; rotations: typeof selectedRotations.value }[] = []

        for (const weekId of selectedWeekIds.value) {
            const week = allWeeks.find((w: any) => w.weekId === weekId)
            if (!week) continue

            const existingRotationIds = getClinicianWeekAssignments(week).map(
                (r: ScheduleAssignment) => (r as any).rotationId,
            )

            const toSchedule = selectedRotations.value.filter(
                (rotation) => !existingRotationIds.includes(rotation.rotId),
            )

            const alreadyScheduledForWeek = selectedRotations.value.length - toSchedule.length
            totalAlreadyScheduled += alreadyScheduledForWeek
            totalToSchedule += toSchedule.length

            if (toSchedule.length > 0) {
                schedulingPlan.push({ week, rotations: toSchedule })
            }
        }

        // If everything is already scheduled, show a message and return
        if (totalToSchedule === 0) {
            $q.notify({
                type: "info",
                message: `All selected rotations are already scheduled for the selected weeks`,
                timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
            })
            // Clear selections
            clearRotationSelection()
            selectedWeekIds.value = []
            // Clear the selection in the ScheduleView component
            if (scheduleViewRef.value) {
                scheduleViewRef.value.clearSelection()
            }
            return
        }

        // Show confirmation with accurate counts
        const rotationNames = selectedRotations.value.map((r) => r.name).join(" / ")
        const weekCount = selectedWeekIds.value.length
        let confirmMessage = `Schedule ${selectedRotations.value.length} rotation(s) (${rotationNames}) to ${weekCount} week(s)?`

        if (totalAlreadyScheduled > 0) {
            confirmMessage += ` ${totalToSchedule} new assignment(s) will be created (${totalAlreadyScheduled} already scheduled).`
        } else {
            confirmMessage += ` This will create ${totalToSchedule} assignment(s).`
        }

        const proceed = await new Promise<boolean>((resolve) => {
            $q.dialog({
                title: "Bulk Schedule Confirmation",
                message: confirmMessage,
                persistent: true,
                ok: { label: "Schedule All", color: "primary" },
                cancel: { label: "Cancel", color: "grey" },
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
        })

        if (!proceed) {
            isAddingRotation.value = false
            return
        }

        // Process the scheduling plan
        let totalSuccess = 0
        let totalErrors = 0

        for (const { week, rotations } of schedulingPlan) {
            loadingWeekId.value = week.weekId

            // Schedule rotations for this week
            const schedulePromises: Promise<void>[] = []

            for (const rotation of rotations) {
                const promise = new Promise<void>((resolve) => {
                    addScheduleWithRollback(
                        {
                            scheduleData: clinicianSchedule.value!,
                            weekId: week.weekId,
                            assignmentData: {
                                rotationId: rotation.rotId,
                                rotationName: rotation.name,
                                rotationAbbreviation: rotation.abbreviation,
                                serviceId: rotation.service?.serviceId,
                                serviceName: rotation.service?.serviceName,
                                isPrimary: makePrimaryEvaluator.value,
                                gradYear: currentYear.value || currentGradYear.value,
                            },
                        },
                        {
                            onSuccess: () => {
                                totalSuccess++
                                resolve()
                            },
                            onError: () => {
                                totalErrors++
                                resolve()
                            },
                        },
                    )
                })
                schedulePromises.push(promise)
            }

            await Promise.all(schedulePromises)
        }

        // Show results
        const messages = []
        if (totalSuccess > 0) {
            messages.push(`${totalSuccess} assignment(s) created`)
        }
        if (totalAlreadyScheduled > 0) {
            messages.push(`${totalAlreadyScheduled} already scheduled`)
        }
        if (totalErrors > 0) {
            messages.push(`${totalErrors} failed`)
        }

        $q.notify({
            type: totalErrors > 0 ? "warning" : totalSuccess > 0 ? "positive" : "info",
            message: messages.join(", "),
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
        })

        // Clear selections after bulk operation
        clearRotationSelection()
        selectedWeekIds.value = []
        makePrimaryEvaluator.value = false
        // Clear the selection in the ScheduleView component
        if (scheduleViewRef.value) {
            scheduleViewRef.value.clearSelection()
        }
    } catch {
        $q.notify({
            type: "negative",
            message: "An error occurred during bulk scheduling",
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
        })
    } finally {
        isAddingRotation.value = false
        loadingWeekId.value = null
    }
}

const deleteBulkAssignments = async () => {
    if (!selectedClinician.value || !clinicianSchedule.value) return
    if (selectedRotations.value.length === 0 || selectedWeekIds.value.length === 0) return

    isRemovingRotation.value = true

    try {
        await executeBulkDeletionLogic(
            {
                selectedItems: selectedRotations,
                selectedWeekIds,
                schedulesBySemester,
                getWeekAssignments: getClinicianWeekAssignments,
                matchAssignmentToItem: (assignment, rotation) => assignment.rotationId === rotation.rotId,
                getItemDisplayName: (rotation) => rotation.name || "",
                getItemTypeName: () => "rotation",
                canDeleteAssignment: (_assignment, rotation) => canEditRotation(rotation.rotId),
            },
            {
                scheduleData: clinicianSchedule.value!,
                removeScheduleWithRollback,
                clearSelections: () => {
                    clearRotationSelection()
                    selectedWeekIds.value = []
                    makePrimaryEvaluator.value = false
                    scheduleViewRef.value?.clearSelection()
                },
                showManualConfirmation: true, // Show manual confirmation before bulk deletion
            },
        )
    } finally {
        isRemovingRotation.value = false
    }
}

const scheduleRotationsToWeek = async (week: WeekItem) => {
    // Set loading state for this week
    loadingWeekId.value = week.weekId

    if (!selectedClinician.value || !clinicianSchedule.value) {
        loadingWeekId.value = null
        return
    }

    if (selectedRotations.value.length === 0) {
        $q.notify({
            type: "warning",
            message: "Please select at least one rotation",
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_WARNING,
        })
        loadingWeekId.value = null
        return
    }

    isAddingRotation.value = true

    const existingAssignments = getWeekAssignments(week)
    const alreadyScheduled: string[] = []
    const toSchedule: RotationWithService[] = []

    // Check for existing assignments
    for (const rotation of selectedRotations.value) {
        const exists = existingAssignments.some((assignment) => assignment.rotationId === rotation.rotId)
        if (exists) {
            alreadyScheduled.push(rotation.name || "Unknown rotation")
        } else if (canEditRotation(rotation.rotId)) {
            toSchedule.push(rotation)
        }
    }

    // Show info about already scheduled rotations
    if (alreadyScheduled.length > 0) {
        $q.notify({
            type: "info",
            message: `${alreadyScheduled.join(" / ")} ${alreadyScheduled.length === 1 ? "is" : "are"} already scheduled for this week`,
            timeout: 3000,
        })
    }

    if (toSchedule.length === 0) {
        isAddingRotation.value = false
        loadingWeekId.value = null
        return
    }

    try {
        // Check for conflicts for all rotations
        const conflictPromises = toSchedule.map((rotation) =>
            InstructorScheduleService.checkConflicts({
                mothraId: selectedClinician.value!.mothraId,
                rotationId: rotation.rotId,
                weekIds: [week.weekId],
                gradYear: currentYear.value!,
            }),
        )

        const conflictResults = await Promise.all(conflictPromises)
        const rotationsWithConflicts: string[] = []

        conflictResults.forEach((result, index) => {
            if (result.result && result.result.length > 0) {
                const rotation = toSchedule[index]
                if (rotation) {
                    const conflictNames = result.result.map((c) => c.name).join(", ")
                    if (conflictNames && !conflictNames.includes(rotation.name || "")) {
                        rotationsWithConflicts.push(rotation.name || "Unknown rotation")
                    }
                }
            }
        })

        // If any have conflicts, show a single confirmation dialog
        if (rotationsWithConflicts.length > 0) {
            const proceed = await new Promise<boolean>((resolve) => {
                $q.dialog({
                    title: "Multiple Rotation Assignments",
                    message: `${selectedClinician.value!.fullName} has other rotation assignments during this week. Do you want to add the selected rotations as additional assignments?`,
                    persistent: true,
                    ok: { label: "Yes, Add All", color: "primary" },
                    cancel: { label: "Cancel", color: "grey" },
                })
                    .onOk(() => resolve(true))
                    .onCancel(() => resolve(false))
            })

            if (!proceed) {
                isAddingRotation.value = false
                loadingWeekId.value = null
                return
            }
        }

        // Schedule all rotations
        let successCount = 0
        let errorCount = 0
        const schedulePromises: Promise<void>[] = []

        for (const rotation of toSchedule) {
            const promise = new Promise<void>((resolve) => {
                addScheduleWithRollback(
                    {
                        scheduleData: clinicianSchedule.value!,
                        weekId: week.weekId,
                        assignmentData: {
                            rotationId: rotation.rotId,
                            rotationName: rotation.name,
                            rotationAbbreviation: rotation.abbreviation,
                            serviceId: rotation.service?.serviceId,
                            serviceName: rotation.service?.serviceName,
                            isPrimary: makePrimaryEvaluator.value,
                            gradYear: currentYear.value || currentGradYear.value,
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
                message: `✓ Successfully scheduled ${successCount} rotation${successCount > 1 ? "s" : ""} to Week ${week.weekNumber}`,
                timeout: 4000,
            })
        }
        if (errorCount > 0) {
            $q.notify({
                type: "negative",
                message: `Failed to schedule ${errorCount} rotation${errorCount > 1 ? "s" : ""}`,
                timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
            })
        }
    } catch (_error) {
        $q.notify({
            type: "negative",
            message: "An error occurred while scheduling rotations",
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
        })
    } finally {
        isAddingRotation.value = false
        loadingWeekId.value = null
    }
}

const onSelectedWeeksChange = (weekIds: number[]) => {
    selectedWeekIds.value = weekIds
}

const onWeekClick = async (week: WeekItem) => {
    if (isPastYear.value) return

    // If we have selected weeks and selected rotations, schedule all combinations
    if (selectedWeekIds.value.length > 0 && selectedRotations.value.length > 0) {
        // Check if this week is in the selection
        if (!selectedWeekIds.value.includes(week.weekId)) {
            // If not, this is a regular click - clear selection and handle normally
            selectedWeekIds.value = []
            return
        }

        // Handle bulk operation based on current mode
        if (isInDeleteMode.value) {
            await deleteBulkAssignments()
        } else {
            await scheduleBulkRotationsToWeeks()
        }
        // Clear selections after bulk operation
        selectedWeekIds.value = []
        return
    }

    // Original single-selection behavior
    if (selectedRotation.value && selectedClinician.value) {
        // Check if same rotation is already scheduled
        const rotationAlreadyScheduled = getClinicianWeekAssignments(week).some(
            (r: ScheduleAssignment) => (r as any).rotationId === selectedRotation.value!.rotId,
        )

        if (rotationAlreadyScheduled) {
            $q.notify({
                type: "info",
                message: `${selectedRotation.value.name} is already scheduled for this week`,
            })
            return
        }

        await scheduleRotationToWeek(week)
    } else {
        $q.notify({
            type: "warning",
            message: "Please select a rotation first",
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_WARNING,
        })
    }
}

const handleRemoveRotation = async (scheduleId: number, displayName: string, isPrimary?: boolean) => {
    if (isPastYear.value || !clinicianSchedule.value) return

    // Find the rotation to check permissions
    const weekWithSchedule = schedulesBySemester.value
        .flatMap((s: ScheduleSemester) => s.weeks)
        .find((w: any) => w.rotations?.some((r: any) => r.scheduleId === scheduleId))

    const rotation = (weekWithSchedule as any)?.rotations?.find((r: any) => r.scheduleId === scheduleId)
    if (!rotation) return

    if (!canEditRotation(rotation.rotationId)) {
        $q.notify({
            type: "negative",
            message: SCHEDULE_OPERATION_ERRORS.NO_PERMISSION_EDIT_ROTATION,
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
        })
        return
    }

    // Show confirmation dialog only for primary evaluator removals
    if (isPrimary) {
        const clinicianName = selectedClinician.value?.fullName || "This clinician"
        const confirmed = await new Promise<boolean>((resolve) => {
            $q.dialog({
                title: "Confirm Primary Evaluator Removal",
                message: `${clinicianName} is the primary evaluator for ${displayName}. Are you sure you want to remove them?`,
                cancel: true,
                persistent: true,
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
        })

        if (!confirmed) return
    }

    isRemovingRotation.value = true

    try {
        await removeScheduleWithRollback(clinicianSchedule.value, scheduleId, {
            onSuccess: () => {
                // Show success notification
                $q.notify({
                    type: "positive",
                    message: `${displayName} has been removed from the schedule`,
                    timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
                })
            },
            onError: (error) => {
                $q.notify({
                    type: "negative",
                    message: error || "Failed to remove rotation",
                    timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
                })
            },
            onNotification: (type, message, icon) => {
                // Handle special notifications (e.g., primary evaluator removal warnings)
                $q.notify({
                    type: type as any,
                    message,
                    icon,
                    timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
                })
            },
        })
    } catch {
        // Error already handled by onError callback
    } finally {
        isRemovingRotation.value = false
    }
}

const handleTogglePrimary = async (scheduleId: number, currentIsPrimary: boolean, _displayName: string) => {
    if (isPastYear.value || !clinicianSchedule.value) return

    // Find the rotation to check permissions
    const weekWithSchedule = schedulesBySemester.value
        .flatMap((s: ScheduleSemester) => s.weeks)
        .find((w: any) => w.rotations?.some((r: any) => r.scheduleId === scheduleId))

    const rotation = (weekWithSchedule as any)?.rotations?.find((r: any) => r.scheduleId === scheduleId)
    if (!rotation) return

    if (!canEditRotation(rotation.rotationId)) {
        $q.notify({
            type: "negative",
            message: SCHEDULE_OPERATION_ERRORS.NO_PERMISSION_EDIT_ROTATION,
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
        })
        return
    }

    // If removing primary status, confirm
    if (currentIsPrimary) {
        const confirmed = await new Promise<boolean>((resolve) => {
            $q.dialog({
                title: "Remove Primary Status",
                message: `Remove primary evaluator status from ${selectedClinician.value?.fullName}? Another clinician should be designated as primary.`,
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
        await togglePrimaryWithRollback(
            {
                scheduleData: clinicianSchedule.value,
                scheduleId: scheduleId,
                isPrimary: !currentIsPrimary,
            },
            {
                onSuccess: () => {
                    const action = currentIsPrimary ? "removed as" : "set as"
                    const message = `${selectedClinician.value?.fullName} ${action} primary evaluator`

                    $q.notify({
                        type: "positive",
                        message: message,
                    })
                },
                onError: (error) => {
                    $q.notify({
                        type: "negative",
                        message: error || "Failed to toggle primary evaluator",
                        timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_ERROR,
                    })
                },
            },
        )
    } catch {
        // Error already handled by onError callback
    } finally {
        isTogglingPrimary.value = false
    }
}

const onAddRotationSelected = (rotation: RotationWithService | null) => {
    if (rotation && rotation.rotId && rotation.name) {
        const serviceId = rotation.service?.serviceId || rotation.serviceId

        const rotationData = {
            rotId: rotation.rotId,
            name: rotation.name,
            serviceId: serviceId,
            abbreviation: rotation.abbreviation,
        } as RotationWithService

        additionalRotation.value = rotationData
        selectedRotation.value = rotationData

        // Also update the multi-select array to auto-select the newly added rotation
        selectedRotations.value = [rotationData]
    }

    selectedNewRotationId.value = null
}

// goToHome is now imported from usePermissionChecks composable

// Lifecycle
onMounted(async () => {
    document.title = "VIPER - Schedule by Clinician"

    try {
        currentGradYear.value = await PageDataService.getCurrentGradYear()
    } catch {
        currentGradYear.value = new Date().getFullYear()
    }

    // Initialize year from URL first, before any API calls
    initializeFromUrl()

    await permissionsStore.fetchUserPermissions()

    // Now load clinician with the correct year already set
    loadClinicianFromUrl().catch(() => {
        /* handle load error */
    })
})

// Watch for route changes to handle direct navigation
watch(
    () => route.params.mothraId,
    (newMothraId) => {
        if (newMothraId) {
            loadClinicianFromUrl().catch(() => {
                /* handle load error */
            })
        } else {
            selectedClinician.value = null
            clinicianSchedule.value = null
            clinicianRotations.value = []
            selectedRotation.value = null
        }
    },
)

// Watch for changes to selectedClinician and fetch schedule data

watch(
    selectedClinician,
    (newClinician, oldClinician) => {
        if (newClinician && newClinician.mothraId !== oldClinician?.mothraId) {
            // Only fetch if we don't already have the schedule data
            // (it may have been loaded by loadClinicianFromUrl)
            if (!clinicianSchedule.value || clinicianSchedule.value.clinician?.mothraId !== newClinician.mothraId) {
                fetchClinicianSchedule(newClinician.mothraId).catch(() => {
                    /* handle fetch error */
                })
            }
        } else if (!newClinician) {
            clinicianSchedule.value = null
            clinicianRotations.value = []
            selectedRotation.value = null
        }
    },
    { immediate: false },
)
</script>

<style scoped>
/* Import shared schedule styles */
@import url("../assets/schedule-shared.css");

/* Clinician page specific styling */
@media (width <= 768px) {
    /* Extra spacing for clinician selector to match rotation page */
    .mobile-extra-spacing .clinician-selector-with-toggle {
        margin-bottom: 8px;
    }
}
</style>
