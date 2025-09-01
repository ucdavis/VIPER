<template>
    <div class="clinical-scheduler-container">
        <SchedulerNavigation />

        <div class="row items-center q-mb-md">
            <h2 class="col-auto q-pr-sm">Schedule Rotations for</h2>
            <div class="col-auto">
                <ClinicianSelector
                    v-model="selectedClinician"
                    :year="currentYear"
                    :include-all-affiliates="includeAllAffiliates"
                    :show-affiliates-toggle="true"
                    @change="onClinicianChange"
                    @update:include-all-affiliates="includeAllAffiliates = $event"
                    @clinicians-loaded="handleClinicianSelectorReady"
                    :is-past-year="isPastYear"
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

        <!-- Helper message for clinicians with no assignments - moved to top -->
        <ScheduleBanner
            v-if="selectedClinician && hasNoAssignments"
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
            v-if="selectedClinician && !isPastYear"
            type="instructions"
            custom-message="This list of rotations should contain any rotations this clinician is scheduled for in the current or previous year. Click on a rotation to select it and then click on any week to schedule the clinician."
        />

        <!-- Loading state -->
        <div
            v-if="loadingSchedule"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading schedule...</div>
        </div>

        <!-- Error state -->
        <ScheduleBanner
            v-else-if="scheduleError"
            type="error"
            :error-message="scheduleError"
        />

        <!-- No clinician selected -->
        <ScheduleBanner
            v-else-if="!selectedClinician"
            type="info"
            custom-message="Please select a clinician to view their schedule."
        />

        <!-- Schedule display -->
        <div v-else-if="clinicianSchedule">
            <!-- Rotation selector section (only show when not past year) -->
            <RecentSelections
                v-if="!isPastYear"
                :items="rotationItems"
                :selected-item="selectedRotation"
                recent-label="Recent Rotations:"
                add-new-label="Add New Rotation:"
                item-type="rotation"
                item-key-field="rotId"
                item-display-field="name"
                selector-spacing="none"
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

            <!-- Schedule by semester -->
            <div
                v-for="semester in schedulesBySemester"
                :key="semester.semester"
            >
                <h3>{{ semester.semester }}</h3>

                <!-- Week grid -->
                <div class="row q-gutter-md q-mb-lg">
                    <WeekScheduleCard
                        v-for="week in semester.weeks"
                        :key="week.weekId"
                        :week="week"
                        :is-past-year="isPastYear"
                        :additional-classes="''"
                        @click="onWeekClick"
                    >
                        <template #assignments="{ week: weekItem }">
                            <div
                                v-if="
                                    weekItem.rotations &&
                                    Array.isArray(weekItem.rotations) &&
                                    weekItem.rotations.length > 0
                                "
                                class="rotation-assignment"
                            >
                                <div
                                    v-for="rotation in weekItem.rotations"
                                    :key="`${rotation.scheduleId || rotation.rotationId}-${rotation.rotationId}`"
                                    class="row items-center q-gutter-xs q-mb-xs"
                                >
                                    <q-icon
                                        v-if="!isPastYear"
                                        name="close"
                                        size="xs"
                                        color="negative"
                                        class="cursor-pointer"
                                        aria-label="Remove rotation from schedule"
                                        @click.stop="
                                            removeRotation({ ...weekItem, rotation, scheduleId: rotation.scheduleId })
                                        "
                                    />
                                    <span class="col text-body2">{{ rotation.name }}</span>
                                    <q-icon
                                        v-if="!isPastYear"
                                        :name="rotation.isPrimaryEvaluator ? 'star' : 'star_outline'"
                                        size="xs"
                                        :color="rotation.isPrimaryEvaluator ? 'amber' : 'grey-5'"
                                        class="cursor-pointer"
                                        :title="
                                            rotation.isPrimaryEvaluator
                                                ? 'Primary evaluator. To transfer primary status, click the star on another clinician.'
                                                : 'Click to make this clinician the primary evaluator.'
                                        "
                                        :aria-label="
                                            rotation.isPrimaryEvaluator
                                                ? 'Primary evaluator - click to transfer to another clinician'
                                                : 'Click to make this clinician the primary evaluator'
                                        "
                                        @click.stop="
                                            togglePrimary({
                                                ...weekItem,
                                                rotation,
                                                scheduleId: rotation.scheduleId,
                                                isPrimaryEvaluator: rotation.isPrimaryEvaluator,
                                            })
                                        "
                                    />
                                    <q-icon
                                        v-else-if="rotation.isPrimaryEvaluator"
                                        name="star"
                                        size="xs"
                                        color="amber"
                                        title="Primary evaluator"
                                        aria-label="Primary evaluator"
                                    />
                                </div>
                            </div>

                            <div
                                v-else
                                class="text-center q-py-sm"
                            >
                                <div
                                    v-if="!isPastYear"
                                    class="text-grey-6 text-caption cursor-pointer"
                                >
                                    Click to add rotation
                                </div>
                                <div
                                    v-else
                                    class="text-grey-5 text-caption"
                                >
                                    No assignment
                                </div>
                            </div>
                        </template>
                    </WeekScheduleCard>
                </div>
            </div>
        </div>

        <!-- Legend -->
        <ScheduleLegend v-if="selectedClinician && schedulesBySemester.length > 0" />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar } from "quasar"
import ClinicianSelector from "../components/ClinicianSelector.vue"
import YearSelector from "../components/YearSelector.vue"
import ScheduleLegend from "../components/ScheduleLegend.vue"
import RotationSelector from "../components/RotationSelector.vue"
import SchedulerNavigation from "../components/SchedulerNavigation.vue"
import WeekScheduleCard, { type WeekItem } from "../components/WeekScheduleCard.vue"
import { ClinicianService, type Clinician, type ClinicianScheduleData } from "../services/clinician-service"
import { InstructorScheduleService } from "../services/instructor-schedule-service"
import { PageDataService } from "../services/page-data-service"
import { usePermissionsStore } from "../stores/permissions"
import type { RotationWithService } from "../types/rotation-types"

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

// Extended WeekItem interfaces to avoid type intersection issues
interface WeekItemWithRotation extends WeekItem {
    rotation?: { rotationId: number; name: string }
    scheduleId?: number
}

interface WeekItemWithPrimary extends WeekItem {
    rotation?: { rotationId: number; name: string }
    scheduleId?: number
    isPrimaryEvaluator?: boolean
}

import ScheduleBanner from "../components/ScheduleBanner.vue"
import RecentSelections from "../components/RecentSelections.vue"

const route = useRoute()
const router = useRouter()
const $q = useQuasar()

// Permissions store
const permissionsStore = usePermissionsStore()

const selectedClinician = ref<Clinician | null>(null)
const clinicianSchedule = ref<ClinicianScheduleData | null>(null)
const clinicianRotations = ref<ClinicianRotationItem[]>([])
const selectedRotation = ref<ClinicianRotationItem | null>(null)
const selectedNewRotationId = ref<number | null>(null)
const additionalRotation = ref<RotationWithService | null>(null) // Track newly selected rotation from dropdown
const loadingSchedule = ref(false)
const scheduleError = ref<string | null>(null)
const currentYear = ref<number | null>(null) // YearSelector will initialize with grad year
const currentGradYear = ref<number>(new Date().getFullYear())
// RotationSelector component handles rotation filtering and search
const includeAllAffiliates = ref(false)

// Loading states for operations
const isAddingRotation = ref(false)
const isRemovingRotation = ref(false)
const isTogglingPrimary = ref(false)

const isPastYear = computed(() => {
    // Compare selected year against the current grad year from backend
    return currentYear.value !== null && currentYear.value < currentGradYear.value
})

// Computed property for rotation items to show in RecentSelections
const rotationItems = computed(() => {
    const items = []

    // Add the newly selected rotation if it's not in the list
    if (additionalRotation.value) {
        items.push(additionalRotation.value)
    }

    // Add rotations from backend
    for (const rotation of clinicianRotations.value) {
        // Don't add duplicates (check by rotId)
        if (!items.some((item) => item.rotId === rotation.rotId)) {
            items.push(rotation)
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

const schedulesBySemester = computed(() => {
    if (!clinicianSchedule.value || !clinicianSchedule.value.schedulesBySemester) return []
    return clinicianSchedule.value.schedulesBySemester
})

const assignedRotationNames = computed(() => {
    if (!clinicianRotations.value) return []

    return clinicianRotations.value
        .map((r: ClinicianRotationItem) => r.name)
        .filter((name: string | undefined): name is string => Boolean(name)) // Filter out any null/undefined names
})

// Check if clinician has no rotation assignments
const hasNoAssignments = computed(() => {
    // Don't show "no assignments" message while still loading
    if (loadingSchedule.value) return false

    if (!schedulesBySemester.value) return false

    // If no semesters/weeks at all, that means no assignments
    if (schedulesBySemester.value.length === 0) return true

    // Check if all weeks have no rotation assignments
    return schedulesBySemester.value.every((semester) =>
        semester.weeks.every((week) => !week.rotations || week.rotations.length === 0),
    )
})

const fetchClinicianSchedule = async (mothraId: string) => {
    loadingSchedule.value = true
    scheduleError.value = null
    clinicianSchedule.value = null
    clinicianRotations.value = []
    additionalRotation.value = null

    try {
        const result = await ClinicianService.getClinicianSchedule(mothraId, { year: currentYear.value ?? undefined })
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
                            if (rotation && !rotationMap.has(rotation.rotationId)) {
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
        void router.push({
            name: "ClinicianScheduleWithId",
            params: { mothraId: encodeURIComponent(clinician.mothraId) },
            query,
        })
        void fetchClinicianSchedule(clinician.mothraId)
    } else {
        void router.push({ name: "ClinicianSchedule" })
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
        void router.push({
            name: "ClinicianScheduleWithId",
            params: { mothraId: encodeURIComponent(selectedClinician.value.mothraId) },
            query,
        })
        void fetchClinicianSchedule(selectedClinician.value.mothraId)
    } else {
        const query: Record<string, string | number> = {}
        if (currentYear.value !== null) {
            query.year = currentYear.value
        }
        void router.push({ query })
    }
}

// Initialize from URL parameters
const initializeFromUrl = () => {
    // Get year from URL parameters - this overrides YearSelector's default
    const yearParam = route.query.year
    if (yearParam && typeof yearParam === "string") {
        currentYear.value = Number(yearParam)
    }
}

const urlMothraId = ref<string | null>(null)

const loadClinicianFromUrl = async () => {
    const mothraId = route.params.mothraId as string
    if (mothraId) {
        const decodedMothraId = decodeURIComponent(mothraId)
        urlMothraId.value = decodedMothraId

        // Load the schedule immediately to show data
        try {
            await fetchClinicianSchedule(decodedMothraId)

            // If we have schedule data but no selectedClinician, create one from schedule data
            if (clinicianSchedule.value && clinicianSchedule.value.clinician && !selectedClinician.value) {
                selectedClinician.value = {
                    mothraId: clinicianSchedule.value.clinician.mothraId,
                    firstName: clinicianSchedule.value.clinician.firstName,
                    lastName: clinicianSchedule.value.clinician.lastName,
                    fullName: clinicianSchedule.value.clinician.fullName,
                    role: clinicianSchedule.value.clinician.role,
                }
            }
        } catch (error) {
            scheduleError.value = error instanceof Error ? error.message : "Failed to load schedule"
        }
    }
}

// Handle when ClinicianSelector has loaded its clinicians and we have a URL mothraId
const handleClinicianSelectorReady = (clinicians: Clinician[]) => {
    if (urlMothraId.value && !selectedClinician.value) {
        // Try to find the clinician in the loaded list
        const clinician = clinicians.find((c) => c.mothraId === urlMothraId.value)
        if (clinician) {
            selectedClinician.value = clinician
            // Clear the URL mothraId since we found the clinician
            urlMothraId.value = null
        }
    }
}

const selectRotation = (rotation: ClinicianRotationItem) => {
    selectedRotation.value = rotation
}

const scheduleRotationToWeek = async (week: WeekItem) => {
    if (!selectedRotation.value || !selectedClinician.value) return

    if (!canEditRotation(selectedRotation.value.rotId)) {
        $q.notify({
            type: "negative",
            message: "You do not have permission to edit this rotation",
            timeout: 3000,
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

        const result = await InstructorScheduleService.addInstructor({
            MothraId: selectedClinician.value.mothraId,
            RotationId: selectedRotation.value.rotId,
            WeekIds: [week.weekId],
            GradYear: currentYear.value!,
            IsPrimaryEvaluator: false, // Can be set later if needed
        })

        if (result.success) {
            let message = `✓ ${selectedRotation.value.name} scheduled successfully for Week ${week.weekNumber}`

            if (result.result?.warningMessage) {
                message += `\n\n${result.result.warningMessage}`
            }

            $q.notify({
                type: result.result?.warningMessage ? "warning" : "positive",
                message: message,
                timeout: result.result?.warningMessage ? 6000 : 4000,
                multiLine: result.result?.warningMessage ? true : false,
            })

            // Instead of a full refresh, update the local data directly
            if (clinicianSchedule.value && clinicianSchedule.value.schedulesBySemester) {
                for (const semester of clinicianSchedule.value.schedulesBySemester) {
                    const weekToUpdate = semester.weeks.find((w) => w.weekId === week.weekId)
                    if (weekToUpdate) {
                        const newRotation = {
                            rotationId: selectedRotation.value.rotId,
                            rotationName: selectedRotation.value.name || "",
                            name: selectedRotation.value.name || "",
                            abbreviation: selectedRotation.value.abbreviation || "",
                            serviceName: selectedRotation.value.serviceName || "",
                            serviceId: selectedRotation.value.serviceId || 0,
                            scheduleId: result.result?.createdSchedules?.[0]?.instructorScheduleId || 0,
                            isPrimaryEvaluator: false,
                        }

                        // Initialize rotations array if it doesn't exist
                        if (!weekToUpdate.rotations) {
                            weekToUpdate.rotations = []
                        }

                        // Add new rotation to the array
                        weekToUpdate.rotations.push(newRotation)

                        // Add rotation to list if it's new
                        if (!clinicianRotations.value.some((r) => r.rotId === selectedRotation.value!.rotId)) {
                            clinicianRotations.value.push({
                                rotId: selectedRotation.value.rotId,
                                name: selectedRotation.value.name,
                                abbreviation: selectedRotation.value.abbreviation || "",
                                serviceName: selectedRotation.value.serviceName || "",
                                serviceId: selectedRotation.value.serviceId,
                            })
                            clinicianRotations.value.sort((a, b) =>
                                (a.name || "").localeCompare(b.name || ""),
                            )
                        }

                        break
                    }
                }
            }

            // Keep the rotation selected for quick adding to other weeks but clear dropdown
            selectedRotation.value = currentSelectedRotation
            additionalRotation.value = currentAdditionalRotation
            // Clear the dropdown to fix the visual issue
            selectedNewRotationId.value = null
        } else {
            throw new Error(result.errors.join(", "))
        }
    } catch (error) {
        let userMessage = "Failed to schedule rotation"
        if (error instanceof Error) {
            if (error.message.includes("already scheduled") || error.message.includes("already exists")) {
                userMessage = `${selectedRotation.value.name} is already scheduled for this week`
            } else if (error.message.includes("permission")) {
                userMessage = "You do not have permission to schedule this rotation"
            } else if (error.message.includes("not found")) {
                userMessage = "The selected rotation or week was not found"
            } else {
                userMessage = error.message
            }
        }

        $q.notify({
            type: "negative",
            message: userMessage,
            timeout: 4000,
        })
    } finally {
        isAddingRotation.value = false
    }
}

const onWeekClick = async (week: WeekItem) => {
    if (isPastYear.value) return // No editing for past years

    if (selectedRotation.value && selectedClinician.value) {
        // Check if same rotation is already scheduled
        const rotationAlreadyScheduled =
            Array.isArray(week.rotations) && week.rotations.some((r: Rotation) => r.rotationId === selectedRotation.value!.rotId)

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

const removeRotation = async (weekItem: WeekItemWithRotation) => {
    if (isPastYear.value || !weekItem.rotation || !weekItem.scheduleId) return

    if (!canEditRotation(weekItem.rotation.rotationId)) {
        $q.notify({
            type: "negative",
            message: "You do not have permission to edit this rotation",
            timeout: 3000,
        })
        return
    }

    const confirmed = await new Promise<boolean>((resolve) => {
        $q.dialog({
            title: "Confirm Removal",
            message: `Remove ${weekItem.rotation?.name} from this week's schedule?`,
            cancel: true,
            persistent: true,
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
    })

    if (!confirmed) return

    isRemovingRotation.value = true

    try {
        const result = await InstructorScheduleService.removeInstructor(weekItem.scheduleId)

        if (result.success) {
            $q.notify({
                type: "positive",
                message: `${weekItem.rotation.name} removed from schedule`,
            })

            // Update the local state instead of refetching
            if (clinicianSchedule.value && clinicianSchedule.value.schedulesBySemester) {
                for (const semester of clinicianSchedule.value.schedulesBySemester) {
                    const week = semester.weeks.find((w) => w.weekId === weekItem.weekId)
                    if (week && week.rotations) {
                        // Remove the rotation from the week's rotations array
                        const rotationIndex = week.rotations.findIndex((r) => r.scheduleId === weekItem.scheduleId)
                        if (rotationIndex !== -1) {
                            week.rotations.splice(rotationIndex, 1)
                        }

                        // Check if this rotation is still assigned to any other weeks
                        const stillAssigned = clinicianSchedule.value.schedulesBySemester.some((sem) =>
                            sem.weeks.some(
                                (w) =>
                                    w.rotations &&
                                    w.rotations.some((r) => r.rotationId === weekItem.rotation!.rotationId),
                            ),
                        )

                        // If not assigned anywhere else, remove from clinicianRotations list
                        if (!stillAssigned) {
                            const rotationListIndex = clinicianRotations.value.findIndex(
                                (r) => r.rotId === weekItem.rotation!.rotationId,
                            )
                            if (rotationListIndex !== -1) {
                                clinicianRotations.value.splice(rotationListIndex, 1)
                            }
                        }

                        break
                    }
                }
            }
        } else {
            throw new Error(result.errors.join(", "))
        }
    } catch {
        $q.notify({
            type: "negative",
            message: "Failed to remove rotation",
            timeout: 3000,
        })
    } finally {
        isRemovingRotation.value = false
    }
}

const togglePrimary = async (weekItem: WeekItemWithPrimary) => {
    if (isPastYear.value || !weekItem.rotation || !weekItem.scheduleId) return

    if (!canEditRotation(weekItem.rotation.rotationId)) {
        $q.notify({
            type: "negative",
            message: "You do not have permission to edit this rotation",
            timeout: 3000,
        })
        return
    }

    const currentIsPrimary = weekItem.isPrimaryEvaluator || false

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
        const result = await InstructorScheduleService.setPrimaryEvaluator(weekItem.scheduleId, !currentIsPrimary)

        if (result.success) {
            const action = currentIsPrimary ? "removed as" : "set as"
            let message = `${selectedClinician.value?.fullName} ${action} primary evaluator`

            // Add previous primary evaluator info if available and setting as primary
            if (!currentIsPrimary && result.result?.previousPrimaryName) {
                message += ` (replaced ${result.result.previousPrimaryName})`
            }

            $q.notify({
                type: "positive",
                message: message,
            })

            // Update the local state instead of refetching
            if (clinicianSchedule.value && clinicianSchedule.value.schedulesBySemester) {
                for (const semester of clinicianSchedule.value.schedulesBySemester) {
                    const week = semester.weeks.find((w) => w.weekId === weekItem.weekId)
                    if (week && week.rotations) {
                        const rotation = week.rotations.find((r) => r.scheduleId === weekItem.scheduleId)
                        if (rotation) {
                            rotation.isPrimaryEvaluator = !currentIsPrimary
                            break
                        }
                    }
                }
            }
        } else {
            const errorMessage =
                result.errors && result.errors.length > 0 ? result.errors.join(", ") : "Unknown error occurred"
            throw new Error(errorMessage)
        }
    } catch {
        $q.notify({
            type: "negative",
            message: "Failed to toggle primary evaluator",
            timeout: 3000,
        })
    } finally {
        isTogglingPrimary.value = false
    }
}

const onAddRotationSelected = (rotation: RotationWithService | null) => {
    if (rotation && rotation.rotId && rotation.name) {
        const serviceId = rotation.service?.serviceId || rotation.serviceId

        if (!serviceId) {
            // Rotation missing serviceId
        }

        const rotationData = {
            rotId: rotation.rotId,
            name: rotation.name,
            serviceId: serviceId,
            abbreviation: rotation.abbreviation,
        }

        additionalRotation.value = rotationData
        selectedRotation.value = rotationData
    }

    selectedNewRotationId.value = null
}

// Watchers

// Load clinician from URL on component mount
onMounted(async () => {
    // Set page title
    document.title = "VIPER - Schedule by Clinician"

    // Fetch current grad year from backend
    try {
        currentGradYear.value = await PageDataService.getCurrentGradYear()
    } catch {
        // Fallback to current calendar year if fetch fails
        currentGradYear.value = new Date().getFullYear()
    }

    // Initialize permissions
    await permissionsStore.fetchUserPermissions()

    // Initialize from URL (this will override YearSelector's default if year param exists)
    void initializeFromUrl()

    void loadClinicianFromUrl()
})

// Watch for route changes to handle direct navigation
watch(
    () => route.params.mothraId,
    (newMothraId) => {
        if (newMothraId) {
            void loadClinicianFromUrl()
        } else {
            // Clear selection if no mothraId in URL
            selectedClinician.value = null
            clinicianSchedule.value = null
            clinicianRotations.value = []
            selectedRotation.value = null
        }
    },
)
</script>

<style scoped>
/* Import shared schedule styles */
@import url("../assets/schedule-shared.css");
</style>
