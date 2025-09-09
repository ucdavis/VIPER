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

        <!-- Access denied for users with rotation-specific permissions -->
        <AccessDeniedCard
            v-else-if="!permissionsStore.canAccessClinicianView"
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
                                :is-own-schedule-only="permissionsStore.hasClinicianViewReadOnly"
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

            <!-- Instructions (only show when clinician is selected and not past year) -->
            <ScheduleBanner
                v-if="selectedClinician && !isPastYear && !hasNoAssignments"
                type="instructions"
                custom-message="This list of rotations should contain any rotations this clinician is scheduled for in the current or previous year. Click on a rotation to select it and then click on any week to schedule the clinician."
            />

            <!-- No clinician selected -->
            <ScheduleBanner
                v-if="!selectedClinician && !loadingSchedule"
                type="info"
                custom-message="Please select a clinician to view their schedule."
            />

            <!-- Schedule display -->
            <div v-if="selectedClinician">
                <!-- Rotation selector section (only show when not past year) -->
                <RecentSelections
                    v-if="!isPastYear"
                    :items="rotationItems"
                    :selected-item="(selectedRotation as RotationWithService) || null"
                    recent-label="Recent Rotations:"
                    add-new-label="Add New Rotation:"
                    item-type="rotation"
                    item-key-field="rotId"
                    item-display-field="name"
                    selector-spacing="none"
                    :is-loading="loadingSchedule"
                    empty-state-message="No recent rotations. Please add a rotation below."
                    @select-item="selectRotation"
                    @clear-selection="selectedRotation = null"
                >
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
                    :schedules-by-semester="schedulesBySemester"
                    view-mode="clinician"
                    :is-past-year="isPastYear"
                    :is-loading="loadingSchedule"
                    :error="scheduleError"
                    :can-edit="!isPastYear"
                    :show-legend="true"
                    :show-warning-icon="false"
                    :show-primary-toggle="true"
                    :no-data-message="`${selectedClinician?.fullName || 'This clinician'} has no rotation assignments for ${currentYear}.`"
                    empty-state-message="Click to add rotation"
                    read-only-empty-message="No assignment"
                    primary-evaluator-title="Primary evaluator. To transfer primary status, click the star on another clinician."
                    make-primary-title="Click to make this clinician the primary evaluator."
                    :get-assignments="getClinicianWeekAssignments"
                    :requires-primary-evaluator="() => false"
                    @week-click="onWeekClick"
                    @remove-assignment="handleRemoveRotation"
                    @toggle-primary="handleTogglePrimary"
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
import { useScheduleNormalization } from "../composables/use-schedule-normalization"
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

// Permissions store
const permissionsStore = usePermissionsStore()

// Composables for optimistic updates
const { addScheduleWithRollback, removeScheduleWithRollback, togglePrimaryWithRollback } =
    useScheduleUpdatesWithRollback()
const { normalizeClinicianSchedule, getAssignedRotationNames } = useScheduleNormalization()

// Component state
const selectedClinician = ref<Clinician | null>(null)
const clinicianSchedule = ref<ClinicianScheduleData | null>(null)
const clinicianRotations = ref<ClinicianRotationItem[]>([])
const selectedRotation = ref<ClinicianRotationItem | null>(null)
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

const isPastYear = computed(() => {
    return currentYear.value !== null && currentYear.value < currentGradYear.value
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

const selectRotation = (rotation: ClinicianRotationItem | RotationWithService) => {
    selectedRotation.value = rotation as ClinicianRotationItem
}

const scheduleRotationToWeek = async (week: WeekItem) => {
    if (!selectedRotation.value || !selectedClinician.value) return
    if (!clinicianSchedule.value) return

    if (!canEditRotation(selectedRotation.value.rotId)) {
        $q.notify({
            type: "negative",
            message: SCHEDULE_OPERATION_ERRORS.NO_PERMISSION_EDIT_ROTATION,
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
        })
        return
    }

    isAddingRotation.value = true

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
                    isPrimary: false,
                    gradYear: currentYear.value || currentGradYear.value,
                },
            },
            {
                onSuccess: () => {
                    let message = `✓ ${selectedRotation.value!.name} scheduled successfully for Week ${week.weekNumber}`
                    $q.notify({
                        type: "positive",
                        message: message,
                        timeout: 4000,
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
                        timeout: 4000,
                    })
                },
            },
        )
    } catch {
        // Error already handled by onError callback
    } finally {
        isAddingRotation.value = false
    }
}

const onWeekClick = async (week: WeekItem) => {
    if (isPastYear.value) return

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
        })
    }
}

const handleRemoveRotation = async (scheduleId: number, displayName: string) => {
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
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
        })
        return
    }

    const confirmed = await new Promise<boolean>((resolve) => {
        $q.dialog({
            title: "Confirm Removal",
            message: `Remove ${displayName} from this week's schedule?`,
            cancel: true,
            persistent: true,
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
    })

    if (!confirmed) return

    isRemovingRotation.value = true

    try {
        await removeScheduleWithRollback(clinicianSchedule.value, scheduleId, {
            onSuccess: () => {
                $q.notify({
                    type: "positive",
                    message: `${displayName} removed from schedule`,
                })
            },
            onError: (error) => {
                $q.notify({
                    type: "negative",
                    message: error || "Failed to remove rotation",
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
            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
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
                        timeout: UI_CONFIG.NOTIFICATION_TIMEOUT,
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
    }

    selectedNewRotationId.value = null
}

const goToHome = () => {
    router.push("/ClinicalScheduler/").catch(() => {
        /* handle navigation error */
    })
}

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
