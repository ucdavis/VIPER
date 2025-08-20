<template>
  <div class="clinical-scheduler-container">
    <SchedulerNavigation />

    <div class="row items-center q-mb-md">
      <h2 class="col-auto q-pr-sm">
        Schedule Clinicians for
      </h2>
      <div class="col-auto">
        <RotationSelector
          v-model="selectedRotationId"
          :year="currentYear"
          :only-with-scheduled-weeks="true"
          @rotation-selected="onRotationSelected"
          style="min-width: 300px;"
        />
      </div>
      <div class="col-auto">
        <YearSelector
          v-model="currentYear"
          @year-changed="onYearChange"
          style="min-width: 120px;"
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

    <!-- Instructions (only show when rotation is selected and not past year) -->
    <ScheduleBanner
      v-if="selectedRotation && !isPastYear"
      type="instructions"
      custom-message="This list of clinicians should contain any clinician scheduled for the rotation in the current or previous year. The user can click on a clinician to select them, and then click on any week to schedule them."
    />

    <!-- Clinician selector section (only show when rotation is selected and not past year) -->
    <q-card
      v-if="selectedRotation && !isPastYear"
      class="q-mb-lg"
      flat
      bordered
    >
      <q-card-section>
        <div class="row items-center q-gutter-md">
          <div class="col-auto">
            <div class="text-subtitle2 q-mb-sm">
              Recent Clinicians:
            </div>
            <div class="q-gutter-xs">
              <q-btn
                v-for="clinician in availableClinicians"
                :key="clinician"
                :color="selectedClinician === clinician ? 'positive' : 'grey-4'"
                :text-color="selectedClinician === clinician ? 'white' : 'dark'"
                :outline="selectedClinician === clinician"
                size="sm"
                @click="selectClinician(clinician)"
              >
                {{ clinician }}
              </q-btn>
            </div>
          </div>

          <div class="col-auto q-mt-xl">
            <div class="row items-start q-gutter-sm q-pb-lg">
              <div
                class="text-subtitle2 q-mt-xs"
              >
                Add New Clinician:
              </div>
              <ClinicianSelector
                :model-value="null"
                :year="currentYear"
                :include-all-affiliates="includeAllAffiliates"
                @update:include-all-affiliates="includeAllAffiliates = $event"
                @change="onAddClinicianSelected"
                :affiliates-toggle-label="'Include all affiliates'"
                class="q-mt-none"
              />
            </div>
          </div>
        </div>
      </q-card-section>
    </q-card>


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
                      @click.stop="removeAssignment()"
                    />
                    <q-icon
                      v-else-if="!isPastYear"
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
                      v-else-if="!isPastYear"
                      :name="assignment.isPrimary ? 'star' : 'star_outline'"
                      size="xs"
                      :color="assignment.isPrimary ? 'amber' : 'grey-5'"
                      class="cursor-pointer"
                      :title="assignment.isPrimary ? 'Primary evaluator. To transfer primary status, click the star on another clinician.' : 'Click to make this clinician the primary evaluator.'"
                      :aria-label="assignment.isPrimary ? 'Primary evaluator - click to transfer to another clinician' : 'Click to make this clinician the primary evaluator'"
                      @click.stop="togglePrimary()"
                    />
                  </div>
                </div>
              </div>

              <div
                v-else
                class="text-center q-py-sm"
              >
                <div class="text-grey-6 text-caption">
                  {{ !isPastYear ? 'Click to add clinician' : 'No assignments' }}
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
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { RotationService, type RotationWithService, type RotationScheduleData } from '../services/RotationService'
import type { Clinician } from '../services/ClinicianService'
import RotationSelector from '../components/RotationSelector.vue'
import ClinicianSelector from '../components/ClinicianSelector.vue'
import YearSelector from '../components/YearSelector.vue'
import ScheduleLegend from '../components/ScheduleLegend.vue'
import SchedulerNavigation from '../components/SchedulerNavigation.vue'
import WeekScheduleCard, { type WeekItem } from '../components/WeekScheduleCard.vue'
import ScheduleBanner from '../components/ScheduleBanner.vue'

// Router
const route = useRoute()
const router = useRouter()

// Reactive data
const selectedRotationId = ref<number | null>(null)
const selectedRotation = ref<RotationWithService | null>(null)
const selectedClinician = ref<string | null>(null)
const rotations = ref<RotationWithService[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

// Real data from database
const scheduleData = ref<RotationScheduleData | null>(null)
const isLoadingSchedule = ref(false)
const scheduleError = ref<string | null>(null)
// YearSelector will initialize with grad year
const currentYear = ref<number | null>(null)

// Clinician data for the dropdown
const includeAllAffiliates = ref(false)

// Year selection is now handled by YearSelector component

const isPastYear = computed(() => {
    return currentYear.value !== null && currentYear.value < new Date().getFullYear()
})


// ClinicianSelector component handles the clinician dropdown functionality

// Recent clinicians from API response (includes current and previous year)
const availableClinicians = computed(() => {
    if (!scheduleData.value || !scheduleData.value.recentClinicians) return []

    return scheduleData.value.recentClinicians
        .map(clinician => clinician.fullName)
        .sort()
})

// Use the semester data directly from the backend API
// The backend already groups weeks by semester using TermCodeService.GetTermCodeDescription()
const weeksBySeason = computed(() => {
    if (!scheduleData.value || !scheduleData.value.schedulesBySemester) return []

    // Transform backend semester structure to match frontend expectations
    return scheduleData.value.schedulesBySemester.map(semester => ({
        name: semester.semester.toLowerCase().replace(/\s+/g, '-'), // Convert "Spring Semester 2024" to "spring-semester-2024"
        displayName: semester.semester, // Use exact semester name from TermCodeService
        weeks: semester.weeks
    }))
})


// Watch for includeAllAffiliates changes (ClinicianSelector handles reloading automatically)
watch(() => includeAllAffiliates.value, () => {
    // ClinicianSelector automatically reloads when includeAllAffiliates changes
})

// Methods

async function loadRotations() {
    isLoading.value = true
    error.value = null

    try {
        const result = await RotationService.getRotations({ includeService: true })

        if (result.success) {
            rotations.value = result.result
        } else {
            error.value = result.errors.join(', ') || 'Failed to load rotations'
        }
    } catch (err) {
        error.value = 'An unexpected error occurred while loading rotations'
        console.error('Error loading rotations:', err)
    } finally {
        isLoading.value = false
    }
}


function onRotationSelected(rotation: RotationWithService | null) {
    selectedRotation.value = rotation
    selectedRotationId.value = rotation ? rotation.rotId : null // Update rotation ID for URL
    selectedClinician.value = null // Reset clinician selection when rotation changes

    // Update URL with selected rotation
    updateUrl()

    if (selectedRotation.value) {
        console.log('Selected rotation:', selectedRotation.value)
        loadScheduleData(selectedRotation.value.rotId)
    } else {
        scheduleData.value = null
    }
}

async function onRotationChange() {
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
            name: 'RotationScheduleWithId',
            params: { rotationId: selectedRotationId.value?.toString() ?? '' },
            query: Object.keys(query).length > 0 ? query : undefined
        })
    } else {
        // No rotation selected, go back to base route
        await router.replace({
            name: 'RotationSchedule'
        })
    }
}

function initializeFromUrl() {
    // Get rotation ID from URL path parameters
    const rotationIdParam = route.params.rotationId
    if (rotationIdParam && typeof rotationIdParam === 'string') {
        selectedRotationId.value = Number(rotationIdParam)
    }

    // Get year from URL query parameters
    const yearParam = route.query.year
    if (yearParam && typeof yearParam === 'string') {
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
            console.log('Loaded schedule data:', scheduleData.value)
        } else {
            scheduleError.value = result.errors.join(', ') || 'Failed to load schedule data'
        }
    } catch (err) {
        scheduleError.value = 'An unexpected error occurred while loading schedule data'
        console.error('Error loading schedule data:', err)
    } finally {
        isLoadingSchedule.value = false
    }
}

function selectClinician(clinician: string) {
    selectedClinician.value = selectedClinician.value === clinician ? null : clinician
}

function onAddClinicianSelected(clinician: Clinician | null) {
    if (clinician) {
        // In Phase 7, we'll implement the actual API call to add clinicians to schedules
        alert(`Adding "${clinician.fullName}" to schedule will be implemented in Phase 7 (Edit Functionality)`)
    }
}

// ClinicianSelector component handles filtering internally

function getWeekAssignments(weekId: number) {
    if (!scheduleData.value || !scheduleData.value.schedulesBySemester) return []

    // Find the week in the nested semester structure
    for (const semester of scheduleData.value.schedulesBySemester) {
        const week = semester.weeks.find(w => w.weekId === weekId)
        if (week && week.instructorSchedules) {
            return week.instructorSchedules.map(schedule => ({
                id: schedule.instructorScheduleId,
                clinicianName: schedule.fullName,
                isPrimary: schedule.isPrimaryEvaluator,
                mothraId: schedule.mothraId
            }))
        }
    }

    return []
}

function scheduleClinicianToWeek(week: WeekItem) {
    if (!selectedClinician.value) {
        alert('Please select a clinician first')
        return
    }

    // Check if clinician is already scheduled for this week
    const existingAssignment = getWeekAssignments(week.weekId).find(
        assignment => assignment.clinicianName === selectedClinician.value
    )

    if (existingAssignment) {
        alert('Clinician is already scheduled for this week')
        return
    }

    // In Phase 7, we'll implement the actual API call to add assignments
    alert('Adding/editing schedules will be implemented in Phase 7 (Edit Functionality)')
}

function removeAssignment() {
    // In Phase 7, we'll implement the actual API call to remove assignments
    alert('Removing schedule assignments will be implemented in Phase 7 (Edit Functionality)')
}

function togglePrimary() {
    // In Phase 7, we'll implement the actual API call to toggle primary status
    alert('Changing primary evaluator status will be implemented in Phase 7 (Edit Functionality)')
}

function requiresPrimaryEvaluator(week: WeekItem): boolean {
    // Use business rule from backend to determine if primary evaluator is required
    if (!week.requiresPrimaryEvaluator) return false

    // Check if there's already a primary evaluator assigned
    const assignments = getWeekAssignments(week.weekId)
    const hasPrimaryEvaluator = assignments.some(assignment => assignment.isPrimary)

    // Return true if backend says it's required but none is assigned
    return !hasPrimaryEvaluator
}

// Watch for URL changes (browser back/forward)
watch(() => route.params.rotationId, (newRotationId) => {
    if (newRotationId !== selectedRotationId.value?.toString()) {
        selectedRotationId.value = newRotationId ? Number(newRotationId) : null
        // Trigger rotation change if rotations are already loaded
        if (rotations.value.length > 0) {
            onRotationChange()
        }
    }
})

// Lifecycle
onMounted(async () => {
    // Set page title
    document.title = 'VIPER - Schedule by Rotation'

    // Initialize from URL (this will override YearSelector's default if year param exists)
    initializeFromUrl()

    // Load rotations (ClinicianSelector will handle loading clinicians)
    await loadRotations()

    // If we have a rotation ID from URL, trigger the selection after rotations are loaded
    if (selectedRotationId.value && rotations.value.length > 0) {
        await onRotationChange()
    }
})
</script>

<style scoped>
/* Import shared schedule styles */
@import '../assets/schedule-shared.css';



</style>
