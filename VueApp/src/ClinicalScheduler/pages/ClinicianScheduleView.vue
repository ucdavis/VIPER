<template>
    <div class="clinical-scheduler-container">
        <div class="navigation-bar">
            <router-link to="/ClinicalScheduler/" class="nav-link">Home</router-link>
            <span class="nav-divider">|</span>
            <router-link to="/ClinicalScheduler/rotation" class="nav-link">Schedule by Rotation</router-link>
            <span class="nav-divider">|</span>
            <span class="nav-link active">Schedule by Clinician</span>
        </div>

        <h2>
            Schedule for
            <ClinicianSelector
                v-model="selectedClinician"
                :year="currentYear"
                :includeAllAffiliates="includeAllAffiliates"
                @update:includeAllAffiliates="includeAllAffiliates = $event"
                @change="onClinicianChange"
                @cliniciansLoaded="handleClinicianSelectorReady"
                :isPastYear="isPastYear"
                class="ms-3 clinician-selector"
            />
            <YearSelector
                v-model="currentYear"
                @yearChanged="onYearChange"
                class="ms-3"
            />
        </h2>

        <!-- Read-only notice for past years -->
        <div v-if="isPastYear" class="read-only-alert">
            <strong>📖 Read-Only Mode:</strong> You are viewing historical schedule data for {{ currentYear }}. Past schedules cannot be edited.
        </div>

        <!-- Instructions -->
        <div v-if="selectedClinician" class="instructions">
            <p>This list of rotations should contain any rotations this clinician is scheduled for in the current or previous year.</p>
            <p v-if="!isPastYear">Click on a rotation to select it and then click on any week to schedule the clinician.</p>
            <p v-else>This is a read-only view of the {{ currentYear }} schedule.</p>
        </div>

        <!-- Loading state -->
        <div v-if="loadingSchedule" class="text-center my-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading schedule...</span>
            </div>
            <p class="mt-2">Loading schedule...</p>
        </div>

        <!-- Error state -->
        <div v-else-if="scheduleError" class="alert alert-danger" role="alert">
            <strong>Error:</strong> {{ scheduleError }}
        </div>

        <!-- No clinician selected -->
        <div v-else-if="!selectedClinician" class="alert alert-info" role="alert">
            Please select a clinician to view their schedule.
        </div>

        <!-- Schedule display -->
        <div v-else-if="clinicianSchedule">
            <!-- Rotation selector section -->
            <div class="rotation-selector-section">
                <div class="rotation-buttons">
                    <button
                        v-for="rotation in clinicianRotations"
                        :key="rotation.rotId"
                        class="rotation-btn"
                        :class="{ selected: selectedRotation?.rotId === rotation.rotId }"
                        @click="selectRotation(rotation)"
                    >
                        {{ rotation.rotationName }}
                    </button>
                </div>
                <div class="add-rotation-wrapper">
                    <q-icon name="add_circle" class="add-rotation-icon" />
                    <RotationSelector
                        v-model="selectedNewRotationId"
                        :excludeRotationNames="assignedRotationNames"
                        @rotationSelected="onAddRotationSelected"
                        class="rotation-dropdown rotation-selector-dropdown"
                    />
                </div>
            </div>

            <!-- Schedule by semester -->
            <div v-for="semester in schedulesBySemester" :key="semester.semester">
                <h3>{{ semester.semester }}</h3>

                <!-- Week grid -->
                <div class="week-grid">
                    <div
                        v-for="week in semester.weeks"
                        :key="week.weekId"
                        class="week-cell"
                        @click="onWeekClick(week)"
                    >
                        <div class="week-header">
                            Week {{ week.weekNumber }}<br>
                            {{ formatDate(week.dateStart) }}
                        </div>
                        <div class="rotation-list">
                            <div v-if="week.rotation" class="rotation-item">
                                <span v-if="!isPastYear" class="remove-btn" @click.stop="removeRotation()">✖</span>
                                <span>{{ week.rotation.rotationName }}</span>
                                <span v-if="!isPastYear"
                                      class="primary-star"
                                      :class="{ filled: week.isPrimaryEvaluator }"
                                      :title="week.isPrimaryEvaluator ? 'Primary evaluator. To transfer primary status, click the star on another clinician.' : 'Click to make this clinician the primary evaluator.'"
                                      @click.stop="togglePrimary()">{{ week.isPrimaryEvaluator ? '★' : '☆' }}</span>
                                <span v-else-if="week.isPrimaryEvaluator"
                                      class="primary-star filled"
                                      title="Primary evaluator">★</span>
                            </div>
                            <div v-else-if="!isPastYear" class="empty-week">
                                <span class="add-rotation-hint">Click to add rotation</span>
                            </div>
                            <div v-else class="empty-week">
                                <span class="no-assignment">No assignment</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Helper message for clinicians with no assignments -->
            <div v-if="hasNoAssignments" class="no-assignments-help">
                <strong>💡 {{ selectedClinician?.fullName || 'This clinician' }} has no rotation assignments for {{ currentYear }}.</strong>
                <span v-if="!isPastYear">Click on any week below to assign a rotation, or use the "Add Rotation" dropdown above.</span>
                <span v-else>This is historical data showing no assignments were made.</span>
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
import { ClinicianService, type Clinician, type ClinicianScheduleData } from '../services/ClinicianService'


// Academic calendar constants
const ACADEMIC_SEASONS = [
    { name: 'fall', displayName: 'Fall', months: [8, 9, 10, 11] }, // September-December
    { name: 'winter', displayName: 'Winter', months: [0, 1] }, // January-February  
    { name: 'spring', displayName: 'Spring', months: [2, 3, 4, 5] }, // March-June
    { name: 'summer', displayName: 'Summer', months: [6, 7] } // July-August
] as const

// Type definitions for better type safety
interface WeekData {
    weekId: number
    weekNumber: number
    dateStart: string
    dateEnd: string
    rotation?: {
        rotationId: number
        rotationName: string
        abbreviation: string
        serviceName?: string
    }
    isPrimaryEvaluator: boolean
}

interface SemesterData {
    semester: string
    weeks: WeekData[]
}

interface SeasonYearGroup {
    season: typeof ACADEMIC_SEASONS[number]
    year: number
    weeks: WeekData[]
}

const route = useRoute()
const router = useRouter()

const selectedClinician = ref<Clinician | null>(null)
const clinicianSchedule = ref<ClinicianScheduleData | null>(null)
const clinicianRotations = ref<any[]>([])
const selectedRotation = ref<any | null>(null)
const selectedNewRotationId = ref<number | null>(null)
const loadingSchedule = ref(false)
const scheduleError = ref<string | null>(null)
const currentYear = ref<number | null>(null) // YearSelector will initialize with academic year
// RotationSelector component handles rotation filtering and search
const includeAllAffiliates = ref(false)


const isPastYear = computed(() => {
    return currentYear.value !== null && currentYear.value < new Date().getFullYear()
})

// Group weeks by academic season using actual calendar dates (same logic as RotationScheduleView)
const schedulesBySemester = computed(() => {
    if (!clinicianSchedule.value || !clinicianSchedule.value.schedulesBySemester) return []

    // Extract all weeks from all semesters with proper typing
    const allWeeks: WeekData[] = []
    clinicianSchedule.value.schedulesBySemester.forEach((semester: SemesterData) => {
        semester.weeks.forEach((week: WeekData) => {
            allWeeks.push(week)
        })
    })

    if (allWeeks.length === 0) return []

    // Group weeks by both season and year to handle multi-year academic periods
    const seasonYearGroups = new Map<string, SeasonYearGroup>()

    allWeeks.forEach(week => {
        const date = new Date(week.dateStart)
        const month = date.getMonth()
        const year = date.getFullYear()

        // Find which season this week belongs to
        const season = ACADEMIC_SEASONS.find(s => s.months.includes(month))
        if (season) {
            const key = `${season.name}-${year}`
            if (!seasonYearGroups.has(key)) {
                seasonYearGroups.set(key, {
                    season: season,
                    year: year,
                    weeks: []
                })
            }
            seasonYearGroups.get(key).weeks.push(week)
        }
    })

    // Convert to result array
    const result = []
    for (const [, group] of seasonYearGroups) {
        // Sort weeks within each season-year group by date
        group.weeks.sort((a: any, b: any) => new Date(a.dateStart).getTime() - new Date(b.dateStart).getTime())

        result.push({
            semester: `${group.season.displayName} ${group.year}`,
            weeks: group.weeks
        })
    }

    // Sort by chronological order based on actual calendar dates
    return result.sort((a: any, b: any) => {
        const firstDateA = new Date(a.weeks[0].dateStart)
        const firstDateB = new Date(b.weeks[0].dateStart)
        return firstDateA.getTime() - firstDateB.getTime()
    })
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
    if (!schedulesBySemester.value || schedulesBySemester.value.length === 0) return false

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
        if (currentYear.value !== null && currentYear.value !== new Date().getFullYear()) {
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
        if (currentYear.value !== null && currentYear.value !== new Date().getFullYear()) {
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

const onWeekClick = (week: any) => {
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

const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    return `${date.getMonth() + 1}/${date.getDate()}/${date.getFullYear().toString().slice(-2)}`
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
.clinical-scheduler-container {
    padding: 20px;
    font-family: Arial, sans-serif;
}

.navigation-bar {
    margin-bottom: 20px;
    padding: 10px;
    background-color: #f0f0f0;
    border-radius: 5px;
}

.nav-link {
    color: #0066cc;
    text-decoration: none;
    padding: 5px 10px;
}

.nav-link:hover {
    text-decoration: underline;
}

.nav-link.active {
    font-weight: bold;
    color: #8B0000;
}

.nav-divider {
    margin: 0 10px;
    color: #999;
}

h2 {
    color: #8B0000;
    font-size: 20px;
    margin-bottom: 15px;
    display: flex;
    align-items: center;
    gap: 10px;
}

.instructions {
    background-color: #f5f5f5;
    padding: 10px;
    margin-bottom: 15px;
    font-size: 13px;
}

.instructions p {
    margin: 5px 0;
}

.rotation-selector-section {
    margin-bottom: 20px;
    padding: 15px;
    background-color: #f9f9f9;
    border: 1px solid #ddd;
}

.rotation-buttons {
    display: inline-block;
    margin-right: 20px;
}

.rotation-btn {
    padding: 5px 10px;
    margin: 2px;
    background-color: #fff;
    border: 1px solid #ccc;
    cursor: pointer;
    font-size: 13px;
}

.rotation-btn:hover {
    background-color: #e0e0e0;
}

.rotation-btn.selected {
    background-color: #4CAF50;
    color: white;
    border-color: #4CAF50;
}

.rotation-dropdown {
    padding: 5px;
    min-width: 150px;
    vertical-align: middle;
}

.add-rotation-wrapper {
    display: flex;
    align-items: center;
    gap: 8px;
}

.add-rotation-icon {
    color: #1976d2;
    font-size: 20px;
}

.action-notes {
    font-size: 12px;
    color: #666;
    margin-bottom: 15px;
}

.action-notes p {
    margin: 3px 0;
}

h3 {
    color: #333;
    font-size: 16px;
    margin-top: 20px;
    margin-bottom: 10px;
}

.week-grid {
    display: grid;
    grid-template-columns: repeat(6, 1fr);
    gap: 10px;
    margin-bottom: 20px;
}

.week-cell {
    border: 1px solid #ccc;
    min-height: 120px;
    background-color: #fff;
    cursor: pointer;
    position: relative;
}

.week-cell:hover {
    background-color: #f9f9f9;
}

.week-header {
    background-color: #e0e0e0;
    padding: 5px;
    font-weight: bold;
    font-size: 12px;
    text-align: center;
    border-bottom: 1px solid #ccc;
}

.rotation-list {
    padding: 5px;
}

.rotation-item {
    display: flex;
    align-items: center;
    padding: 3px 5px;
    margin: 2px 0;
    font-size: 13px;
    border: 1px solid transparent;
}

.remove-btn {
    color: #ff0000;
    font-weight: bold;
    margin-right: 5px;
    cursor: pointer;
    font-size: 12px;
}

.primary-star {
    margin-left: auto;
    color: #000;
    font-size: 16px;
    cursor: pointer;
}

.primary-star.filled {
    color: #ffd700;
}

.schedule-note {
    margin-top: 20px;
    font-size: 11px;
    color: #999;
    font-style: italic;
}

.spinner-border {
    width: 3rem;
    height: 3rem;
}

/* Clinician selection styles */
.clinician-selector {
    min-width: 400px;
}

.rotation-selector-dropdown {
    min-width: 200px;
}

/* Legend styles */
.legend {
    margin-top: 30px;
    padding: 15px;
    background-color: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 5px;
}

.legend h4 {
    color: #333;
    font-size: 16px;
    margin-bottom: 10px;
}

.legend ul {
    list-style: none;
    padding-left: 0;
    margin: 0;
}

.legend li {
    margin-bottom: 8px;
    font-size: 13px;
    display: flex;
    align-items: center;
}

.icon-demo {
    display: inline-block;
    margin-right: 8px;
    font-size: 14px;
    width: 16px;
    text-align: center;
}

.icon-demo.remove-icon {
    color: #ff0000;
    font-weight: bold;
}

.icon-demo.primary-icon {
    color: #ffd700;
    font-size: 16px;
}


.read-only-alert {
    background-color: #e3f2fd;
    border: 1px solid #2196f3;
    color: #1565c0;
    padding: 12px;
    border-radius: 4px;
    margin-bottom: 15px;
    font-size: 14px;
}

.read-only-alert strong {
    font-weight: 600;
}

.no-schedule-message {
    text-align: center;
    padding: 40px 20px;
    background-color: #f8f9fa;
    border: 2px dashed #dee2e6;
    border-radius: 8px;
    margin: 20px 0;
}

.no-schedule-icon {
    font-size: 48px;
    margin-bottom: 16px;
}

.no-schedule-message h4 {
    color: #495057;
    margin-bottom: 12px;
    font-size: 18px;
}

.no-schedule-message p {
    color: #6c757d;
    margin-bottom: 8px;
    font-size: 14px;
}

.no-schedule-message .suggestion {
    background-color: #e8f5e8;
    padding: 12px;
    border-radius: 4px;
    border-left: 4px solid #28a745;
    margin: 16px 0;
    color: #155724;
}

.no-schedule-message .historical-note {
    font-style: italic;
    color: #868e96;
}

.no-assignments-help {
    background-color: #fff3cd;
    border: 1px solid #ffeaa7;
    color: #856404;
    padding: 12px;
    border-radius: 4px;
    margin: 16px 0;
    font-size: 14px;
    text-align: center;
}

.no-assignments-help strong {
    display: block;
    margin-bottom: 6px;
}

.empty-week {
    padding: 8px 5px;
    text-align: center;
    font-size: 12px;
    min-height: 20px;
}

.add-rotation-hint {
    color: #6c757d;
    font-style: italic;
    cursor: pointer;
}

.add-rotation-hint:hover {
    color: #495057;
    text-decoration: underline;
}

.no-assignment {
    color: #adb5bd;
    font-style: italic;
}

.week-cell:hover .add-rotation-hint {
    color: #007bff;
}
</style>