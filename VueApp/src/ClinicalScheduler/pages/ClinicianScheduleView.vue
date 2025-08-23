<template>
  <div class="clinical-scheduler-container">
    <SchedulerNavigation />

    <div class="row items-center q-mb-md">
      <h2 class="col-auto q-pr-sm">
        Schedule Rotations for
      </h2>
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
      <div class="q-mt-md text-body1">
        Loading schedule...
      </div>
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
      <q-card
        v-if="!isPastYear"
        class="q-mb-lg"
        flat
        bordered
      >
        <q-card-section>
          <div class="row items-center q-gutter-md">
            <div class="col-auto">
              <div class="text-subtitle2 q-mb-sm">
                Available Rotations:
              </div>
              <div class="q-gutter-xs">
                <q-btn
                  v-for="rotation in clinicianRotations"
                  :key="rotation.rotId"
                  :color="selectedRotation?.rotId === rotation.rotId ? 'positive' : 'grey-4'"
                  :text-color="selectedRotation?.rotId === rotation.rotId ? 'white' : 'dark'"
                  :outline="selectedRotation?.rotId === rotation.rotId"
                  size="sm"
                  @click="selectRotation(rotation)"
                >
                  {{ rotation.rotationName }}
                </q-btn>
              </div>
            </div>

            <div class="col-auto">
              <div class="row items-center q-gutter-sm">
                <div class="text-subtitle2">
                  Add New Rotation:
                </div>
                <RotationSelector
                  v-model="selectedNewRotationId"
                  :exclude-rotation-names="assignedRotationNames"
                  :only-with-scheduled-weeks="true"
                  :year="currentYear"
                  @rotation-selected="onAddRotationSelected"
                  style="min-width: 200px"
                  class="q-mt-none"
                />
              </div>
            </div>
          </div>
        </q-card-section>
      </q-card>

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
                v-if="weekItem.rotation"
                class="rotation-assignment"
              >
                <div class="row items-center q-gutter-xs">
                  <q-icon
                    v-if="!isPastYear"
                    name="close"
                    size="xs"
                    color="negative"
                    class="cursor-pointer"
                    aria-label="Remove rotation from schedule"
                    @click.stop="removeRotation()"
                  />
                  <span class="col text-body2">{{ weekItem.rotation.rotationName }}</span>
                  <q-icon
                    v-if="!isPastYear"
                    :name="weekItem.isPrimaryEvaluator ? 'star' : 'star_outline'"
                    size="xs"
                    :color="weekItem.isPrimaryEvaluator ? 'amber' : 'grey-5'"
                    class="cursor-pointer"
                    :title="weekItem.isPrimaryEvaluator ? 'Primary evaluator. To transfer primary status, click the star on another clinician.' : 'Click to make this clinician the primary evaluator.'"
                    :aria-label="weekItem.isPrimaryEvaluator ? 'Primary evaluator - click to transfer to another clinician' : 'Click to make this clinician the primary evaluator'"
                    @click.stop="togglePrimary()"
                  />
                  <q-icon
                    v-else-if="weekItem.isPrimaryEvaluator"
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
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ClinicianSelector from '../components/ClinicianSelector.vue'
import YearSelector from '../components/YearSelector.vue'
import ScheduleLegend from '../components/ScheduleLegend.vue'
import RotationSelector from '../components/RotationSelector.vue'
import SchedulerNavigation from '../components/SchedulerNavigation.vue'
import WeekScheduleCard, { type WeekItem } from '../components/WeekScheduleCard.vue'
import { ClinicianService, type Clinician, type ClinicianScheduleData } from '../services/ClinicianService'



import ScheduleBanner from '../components/ScheduleBanner.vue'



const route = useRoute()
const router = useRouter()

const selectedClinician = ref<Clinician | null>(null)
const clinicianSchedule = ref<ClinicianScheduleData | null>(null)
const clinicianRotations = ref<any[]>([])
const selectedRotation = ref<any | null>(null)
const selectedNewRotationId = ref<number | null>(null)
const loadingSchedule = ref(false)
const scheduleError = ref<string | null>(null)
const currentYear = ref<number | null>(null) // YearSelector will initialize with grad year
// RotationSelector component handles rotation filtering and search
const includeAllAffiliates = ref(false)


const isPastYear = computed(() => {
    return currentYear.value !== null && currentYear.value < new Date().getFullYear()
})

// Use the semester data directly from the backend API
// The backend already groups weeks by semester using TermCodeService.GetTermCodeDescription()
const schedulesBySemester = computed(() => {
    if (!clinicianSchedule.value || !clinicianSchedule.value.schedulesBySemester) return []

    // Return the backend data directly - it already has correct semester names and grouping
    return clinicianSchedule.value.schedulesBySemester
})

// Computed property for rotation names already assigned to this clinician
const assignedRotationNames = computed(() => {
    if (!clinicianRotations.value) return []


    return clinicianRotations.value
        .map((r: any) => r.rotationName)
        .filter((name: string) => name) // Filter out any null/undefined names
})

// Check if clinician has no rotation assignments
const hasNoAssignments = computed(() => {
    // Don't show "no assignments" message while still loading
    if (loadingSchedule.value) return false

    if (!schedulesBySemester.value) return false

    // If no semesters/weeks at all, that means no assignments
    if (schedulesBySemester.value.length === 0) return true

    // Check if all weeks have no rotation assignments
    return schedulesBySemester.value.every(semester =>
        semester.weeks.every(week => !week.rotation)
    )
})


const fetchClinicianSchedule = async (mothraId: string) => {
    loadingSchedule.value = true
    scheduleError.value = null
    clinicianSchedule.value = null
    clinicianRotations.value = []

    try {
        // Fetch schedule
        const result = await ClinicianService.getClinicianSchedule(mothraId, { year: currentYear.value ?? undefined })
        if (result.success) {
            clinicianSchedule.value = result.result

            // Extract unique rotations from schedule
            const rotationMap = new Map<number, any>()
            if (result.result.schedulesBySemester) {
                result.result.schedulesBySemester.forEach(semester => {
                    semester.weeks.forEach(week => {
                        if (week.rotation && !rotationMap.has(week.rotation.rotationId)) {
                            rotationMap.set(week.rotation.rotationId, {
                                rotId: week.rotation.rotationId,
                                rotationName: week.rotation.rotationName,
                                abbreviation: week.rotation.abbreviation,
                                serviceName: week.rotation.serviceName
                            })
                        }
                    })
                })
            }

            clinicianRotations.value = Array.from(rotationMap.values())
                .sort((a, b) => (a.rotationName || '').localeCompare(b.rotationName || ''))

            // Don't auto-select any rotation - let user choose
            selectedRotation.value = null
        } else {
            scheduleError.value = result.errors.join(', ') || 'Failed to load schedule'
        }
    } catch (error) {
        scheduleError.value = error instanceof Error ? error.message : 'Failed to load schedule'
        console.error('Error loading clinician schedule:', error)
    } finally {
        loadingSchedule.value = false
    }
}

const onClinicianChange = (clinician: Clinician | null) => {
    if (clinician) {
        // Update URL to include the clinician ID and year if not current
        const query: any = {}
        if (currentYear.value !== null && currentYear.value !== new Date().getFullYear()) {
            query.year = currentYear.value
        }
        router.push({
            name: 'ClinicianScheduleWithId',
            params: { mothraId: encodeURIComponent(clinician.mothraId) },
            query
        })
        fetchClinicianSchedule(clinician.mothraId)
    } else {
        // Navigate back to base clinician route
        router.push({ name: 'ClinicianSchedule' })
        clinicianSchedule.value = null
        clinicianRotations.value = []
        selectedRotation.value = null
    }
}

const onYearChange = () => {
    // Update URL with new year
    if (selectedClinician.value) {
        // When clinician is selected, update URL and refetch their schedule for the new year
        const query: any = {}
        if (currentYear.value !== null) {
            query.year = currentYear.value
        }
        router.push({
            name: 'ClinicianScheduleWithId',
            params: { mothraId: encodeURIComponent(selectedClinician.value.mothraId) },
            query
        })
        fetchClinicianSchedule(selectedClinician.value.mothraId)
    } else {
        // No clinician selected, just update the year in URL
        const query: any = {}
        if (currentYear.value !== null) {
            query.year = currentYear.value
        }
        router.push({ query })
    }
}

// Initialize from URL parameters
const initializeFromUrl = () => {
    // Get year from URL parameters - this overrides YearSelector's default
    const yearParam = route.query.year
    if (yearParam && typeof yearParam === 'string') {
        currentYear.value = Number(yearParam)
    }
}

// Store the mothraId from URL for initialization
const urlMothraId = ref<string | null>(null)

// Load clinician from URL parameter if present
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
                    role: clinicianSchedule.value.clinician.role
                }
            }
        } catch (error) {
            console.error('Error loading clinician schedule from URL:', error)
            // Don't redirect immediately - the ClinicianSelector might still be able to find the clinician
        }
    }
}

// Handle when ClinicianSelector has loaded its clinicians and we have a URL mothraId
const handleClinicianSelectorReady = (clinicians: Clinician[]) => {
    if (urlMothraId.value && !selectedClinician.value) {
        // Try to find the clinician in the loaded list
        const clinician = clinicians.find(c => c.mothraId === urlMothraId.value)
        if (clinician) {
            selectedClinician.value = clinician
            // Clear the URL mothraId since we found the clinician
            urlMothraId.value = null
        }
    }
}

const selectRotation = (rotation: any) => {
    selectedRotation.value = rotation
}

const onWeekClick = (week: WeekItem) => {
    if (isPastYear.value) return // No editing for past years

    if (week.rotation) {
        // Week has rotation - functionality for editing will be added here
    } else {
        // Empty week - rotation assignment functionality will be added here
    }
}

const removeRotation = () => {
    if (isPastYear.value) return
    // Remove rotation assignment functionality will be implemented here
}

const togglePrimary = () => {
    if (isPastYear.value) return
    // Toggle primary evaluator functionality will be implemented here
}


const onAddRotationSelected = (rotation: any) => {
    if (rotation) {
        // Rotation assignment functionality will be implemented here
        console.log('Selected rotation to add:', rotation)
    }

    // Reset dropdown
    selectedNewRotationId.value = null
}


// Watchers

// Load clinician from URL on component mount
onMounted(() => {
    // Set page title
    document.title = 'VIPER - Schedule by Clinician'

    // Initialize from URL (this will override YearSelector's default if year param exists)
    initializeFromUrl()

    loadClinicianFromUrl()
})

// Watch for route changes to handle direct navigation
watch(() => route.params.mothraId, (newMothraId) => {
    if (newMothraId) {
        loadClinicianFromUrl()
    } else {
        // Clear selection if no mothraId in URL
        selectedClinician.value = null
        clinicianSchedule.value = null
        clinicianRotations.value = []
        selectedRotation.value = null
    }
})
</script>

<style scoped>
/* Import shared schedule styles */
@import '../assets/schedule-shared.css';
</style>
