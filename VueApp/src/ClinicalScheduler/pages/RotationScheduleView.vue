<template>
    <div class="clinical-scheduler-container">
        <div class="navigation-bar">
            <router-link to="/ClinicalScheduler/" class="nav-link">Home</router-link>
            <span class="nav-divider">|</span>
            <span class="nav-link active">Schedule by Rotation</span>
            <span class="nav-divider">|</span>
            <router-link to="/ClinicalScheduler/clinician" class="nav-link">Schedule by Clinician</router-link>
        </div>

        <h2>
            Clinician Schedule for
            <RotationSelector
                v-model="selectedRotationId"
                :year="currentYear"
                :only-with-scheduled-weeks="true"
                @rotationSelected="onRotationSelected"
                class="d-inline-block ms-2"
                style="min-width: 300px;"
            />
            <YearSelector
                v-model="currentYear"
                @yearChanged="onYearChange"
                class="ms-3"
            />
        </h2>


        <!-- Error display for rotation loading -->
        <div v-if="error" class="error-message">
            {{ error }}
            <button @click="loadRotations" :disabled="isLoading">Retry</button>
        </div>

        <!-- Read-only notice for past years -->
        <div v-if="isPastYear" class="read-only-alert">
            <strong>📖 Read-Only Mode:</strong> You are viewing historical schedule data for {{ currentYear }}. Past schedules cannot be edited.
        </div>

        <!-- Instructions (only show when rotation is selected) -->
        <div v-if="selectedRotation" class="instructions">
            <p>This list of clinicians should be contain any clinician scheduled for the rotation in the current or previous year.</p>
            <p v-if="!isPastYear">The user can click on a clinician to select them, and then click on any week to schedule them.</p>
            <p v-else>This is a read-only view of the {{ currentYear }} schedule.</p>
        </div>

        <!-- Clinician selector section (only show when rotation is selected) -->
        <div v-if="selectedRotation" class="clinician-selector-section">
            <div class="clinician-buttons">
                <button
                    v-for="clinician in availableClinicians"
                    :key="clinician"
                    class="clinician-btn"
                    :class="{ selected: selectedClinician === clinician }"
                    @click="selectClinician(clinician)"
                >
                    {{ clinician }}
                </button>
                <ClinicianSelector
                    v-if="!isPastYear"
                    :modelValue="null"
                    :year="currentYear"
                    :includeAllAffiliates="includeAllAffiliates"
                    @update:includeAllAffiliates="includeAllAffiliates = $event"
                    @change="onAddClinicianSelected"
                    :affiliatesToggleLabel="'Include all affiliates'"
                    style="min-width: 200px;"
                />
            </div>
        </div>


        <!-- Season headers and week grids -->
        <div v-if="selectedRotation">
            <div v-for="season in weeksBySeason" :key="season.name">
                <h3>{{ season.displayName }}</h3>
                <div class="week-grid">
                    <div v-for="week in season.weeks" :key="week.weekId"
                         class="week-cell"
                         :class="{ 'requires-primary': requiresPrimaryEvaluator(week) }"
                         @click="scheduleClinicianToWeek(week)">
                        <div class="week-header">
                            <span v-if="requiresPrimaryEvaluator(week)" class="alert-icon" title="Primary evaluator required for this week">⚠️</span>
                            Week {{ week.weekNumber }}<br>
                            {{ formatDate(week.dateStart) }}
                        </div>
                        <div class="rotation-list">
                            <div v-for="assignment in getWeekAssignments(week.weekId)" :key="assignment.id" class="rotation-item">
                                <!-- Remove button - only show for current year -->
                                <span v-if="!isPastYear && !assignment.isPrimary"
                                      class="remove-btn"
                                      title="Remove this clinician from the schedule."
                                      @click.stop="removeAssignment()">✖</span>
                                <span v-else-if="!isPastYear"
                                      class="remove-btn-disabled"
                                      title="Cannot remove primary clinician. Make another clinician primary first.">✖</span>

                                <span>{{ assignment.clinicianName }}</span>

                                <!-- Primary star - for past years, only show filled stars without click handler -->
                                <span v-if="isPastYear && assignment.isPrimary"
                                      class="primary-star filled"
                                      title="Primary evaluator">★</span>
                                <span v-else-if="!isPastYear"
                                      class="primary-star"
                                      :class="{ filled: assignment.isPrimary }"
                                      :title="assignment.isPrimary ? 'Primary evaluator. To transfer primary status, click the star on another clinician.' : 'Click to make this clinician the primary evaluator.'"
                                      @click.stop="togglePrimary()">{{ assignment.isPrimary ? '★' : '☆' }}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Legend -->
        <ScheduleLegend v-if="selectedRotation" :show-warning="true" />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { RotationService, type RotationWithService, type RotationScheduleData, type WeekItem } from '../services/RotationService'
import type { Clinician } from '../services/ClinicianService'
import RotationSelector from '../components/RotationSelector.vue'
import ClinicianSelector from '../components/ClinicianSelector.vue'
import YearSelector from '../components/YearSelector.vue'
import ScheduleLegend from '../components/ScheduleLegend.vue'

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
// YearSelector will initialize with academic year
const currentYear = ref<number | null>(null)

// Clinician data for the dropdown
const includeAllAffiliates = ref(false)

// Year selection is now handled by YearSelector component

const isPastYear = computed(() => {
    return currentYear.value !== null && currentYear.value < new Date().getFullYear()
})


// ClinicianSelector component handles the clinician dropdown functionality

// Available clinicians from schedule data
const availableClinicians = computed(() => {
    if (!scheduleData.value) return []

    const clinicians = new Set<string>()
    scheduleData.value.instructorSchedules.forEach(schedule => {
        clinicians.add(schedule.fullName)
    })

    return Array.from(clinicians).sort()
})

// Week data grouped by academic season using actual calendar dates
const weeksBySeason = computed(() => {
    if (!scheduleData.value) return []

    // Define academic seasons with their months (no year offsets - use actual calendar dates)
    const seasons = [
        { name: 'fall', displayName: 'Fall', months: [8, 9, 10, 11] }, // Sept-Dec
        { name: 'winter', displayName: 'Winter', months: [0, 1] }, // Jan-Feb
        { name: 'spring', displayName: 'Spring', months: [2, 3, 4, 5] }, // Mar-Jun
        { name: 'summer', displayName: 'Summer', months: [6, 7] } // Jul-Aug
    ]

    // Group weeks by both season and year to handle multi-year academic periods
    const seasonYearGroups = new Map()

    scheduleData.value.weeks.forEach(week => {
        const date = new Date(week.dateStart)
        const month = date.getMonth()
        const year = date.getFullYear()

        // Find which season this week belongs to
        const season = seasons.find(s => s.months.includes(month))
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
        // Sort weeks within each season-year group
        group.weeks.sort((a: any, b: any) => new Date(a.dateStart).getTime() - new Date(b.dateStart).getTime())

        result.push({
            name: `${group.season.name}-${group.year}`,
            displayName: `${group.season.displayName} ${group.year}`,
            weeks: group.weeks,
            seasonName: group.season.name,
            year: group.year
        })
    }

    // Sort by chronological order based on actual calendar dates
    return result.sort((a: any, b: any) => {
        const firstDateA = new Date(a.weeks[0].dateStart)
        const firstDateB = new Date(b.weeks[0].dateStart)
        return firstDateA.getTime() - firstDateB.getTime()
    })
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

        if (currentYear.value !== null && currentYear.value !== new Date().getFullYear()) {
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
    if (!scheduleData.value) return []

    return scheduleData.value.instructorSchedules.filter(schedule =>
        schedule.week.weekId === weekId
    ).map(schedule => ({
        id: schedule.instructorScheduleId,
        clinicianName: schedule.fullName,
        isPrimary: schedule.evaluator,
        mothraId: schedule.mothraId
    }))
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

function formatDate(date: string | Date | undefined): string {
    if (!date) return 'N/A'
    const d = typeof date === 'string' ? new Date(date) : date
    if (isNaN(d.getTime())) return 'Invalid Date'
    return d.toLocaleDateString('en-US', { month: 'numeric', day: 'numeric', year: '2-digit' })
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

.title-dropdown {
    font-size: 18px;
    font-weight: bold;
    color: #8B0000;
    border: 1px solid #ccc;
    padding: 2px 5px;
    background-color: #fff;
}


.error-message {
    color: #d32f2f;
    background-color: #ffebee;
    padding: 10px;
    border-radius: 4px;
    margin-bottom: 15px;
}

.error-message button {
    margin-left: 10px;
    padding: 4px 8px;
    background-color: #d32f2f;
    color: white;
    border: none;
    border-radius: 3px;
    cursor: pointer;
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

.instructions {
    background-color: #f5f5f5;
    padding: 10px;
    margin-bottom: 15px;
    font-size: 13px;
}

.instructions p {
    margin: 5px 0;
}

.clinician-selector-section {
    margin-bottom: 20px;
    padding: 15px;
    background-color: #f9f9f9;
    border: 1px solid #ddd;
}

.clinician-buttons {
    display: inline-block;
    margin-right: 20px;
}

.clinician-btn {
    padding: 5px 10px;
    margin: 2px;
    background-color: #fff;
    border: 1px solid #ccc;
    cursor: pointer;
    font-size: 13px;
}

.clinician-btn:hover {
    background-color: #e0e0e0;
}

.clinician-btn.selected {
    background-color: #4CAF50;
    color: white;
    border-color: #4CAF50;
}


.add-clinician-dropdown {
    padding: 5px;
    min-width: 150px;
    vertical-align: middle;
}

.legend {
    font-size: 12px;
    color: #666;
    margin-top: 20px;
    padding: 15px;
    background-color: #f9f9f9;
    border: 1px solid #ddd;
    border-radius: 4px;
}

.legend h4 {
    margin: 0 0 10px 0;
    font-size: 14px;
    color: #333;
    font-weight: bold;
}

.legend ul {
    margin: 0;
    padding-left: 20px;
}

.legend li {
    margin: 5px 0;
}

.icon-demo {
    font-weight: bold;
    font-size: 14px;
    padding: 1px 2px;
}

.remove-icon {
    color: #ff0000;
}

.primary-icon {
    color: #ffd700;
}

h3 {
    color: #333;
    font-size: 16px;
    margin-top: 20px;
    margin-bottom: 10px;
}

.week-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
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

.remove-btn-disabled {
    color: #ccc;
    font-weight: bold;
    margin-right: 5px;
    cursor: not-allowed;
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

.requires-primary {
    border-color: #ff4444 !important;
    border-width: 2px !important;
    background-color: #fff5f5;
}

.requires-primary .week-header {
    background-color: #ffebeb;
}

.alert-icon {
    color: #ff4444;
    font-weight: bold;
    font-size: 14px;
    margin-right: 5px;
}

/* Mobile Responsiveness */
@media (max-width: 768px) {
    .clinical-scheduler-container {
        padding: 10px;
    }

    h2 {
        font-size: 18px;
        flex-direction: column;
        align-items: flex-start;
        gap: 10px;
    }

    .title-dropdown {
        font-size: 16px;
        min-width: 200px;
        max-width: 100%;
    }

    .week-grid {
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        gap: 8px;
    }

    .week-cell {
        min-height: 100px;
    }

    .week-header {
        padding: 3px;
        font-size: 11px;
    }

    .clinician-buttons {
        flex-wrap: wrap;
    }

    .clinician-btn {
        font-size: 12px;
        padding: 4px 8px;
        margin: 1px;
    }

    .add-clinician-dropdown {
        width: 100%;
        margin-top: 5px;
    }

    .rotation-item {
        font-size: 12px;
        padding: 2px 3px;
    }

    .instructions, .action-notes {
        font-size: 12px;
        padding: 8px;
        margin-bottom: 10px;
    }
}

@media (max-width: 480px) {
    .week-grid {
        grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
    }

    .week-cell {
        min-height: 80px;
    }

    h2 {
        font-size: 16px;
    }

    .title-dropdown {
        font-size: 14px;
    }

    .rotation-item {
        font-size: 11px;
    }

    .clinician-btn {
        font-size: 11px;
        padding: 3px 6px;
    }
}

</style>