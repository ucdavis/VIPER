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
                :isPastYear="isPastYear"
                class="ms-3"
                style="min-width: 400px;"
            />
            <select
                v-model="currentYear"
                @change="onYearChange"
                class="year-selector ms-3"
            >
                <option v-for="year in availableYears" :key="year" :value="year">
                    {{ year }}
                </option>
            </select>
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
                <q-select
                    v-model="selectedNewRotationObj"
                    :options="filteredRotations"
                    :loading="false"
                    :error="false"
                    placeholder="Add Rotation"
                    emit-value
                    map-options
                    use-input
                    fill-input
                    hide-selected
                    clearable
                    :input-debounce="300"
                    @filter="onFilterRotations"
                    @update:model-value="onAddRotationSelected"
                    option-label="displayText"
                    option-value="rotId"
                    class="rotation-dropdown"
                    style="min-width: 200px;"
                >
                    <template v-slot:prepend>
                        <q-icon name="add_circle" />
                    </template>

                    <template v-slot:no-option>
                        <q-item>
                            <q-item-section class="text-grey">
                                {{ rotationSearchQuery ? 'No available rotations' : 'Loading rotations...' }}
                            </q-item-section>
                        </q-item>
                    </template>

                    <template v-slot:option="scope">
                        <q-item v-bind="scope.itemProps">
                            <q-item-section>
                                <q-item-label>{{ scope.opt.rotationName }}</q-item-label>
                            </q-item-section>
                        </q-item>
                    </template>

                    <template v-slot:selected-item="scope">
                        <span v-if="scope.opt">{{ scope.opt.rotationName }} ({{ scope.opt.serviceName }})</span>
                    </template>
                </q-select>
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
                                <span>{{ week.rotation.abbreviation }}</span>
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
        <div v-if="selectedClinician && schedulesBySemester.length > 0" class="legend">
            <h4>Legend</h4>
            <ul>
                <li><span class="icon-demo remove-icon">✖</span> removes the rotation from the schedule</li>
                <li><span class="icon-demo primary-icon">★</span> makes them the primary evaluator (replacing the current one, if there is one)</li>
                <li>Primary evaluators (<span class="icon-demo primary-icon">★</span>) are responsible for student evaluations</li>
            </ul>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ClinicianSelector from '../components/ClinicianSelector.vue'
import { ClinicianService, type Clinician, type ClinicianScheduleData } from '../services/ClinicianService'
import { RotationService, type RotationWithService } from '../services/RotationService'

const route = useRoute()
const router = useRouter()

const selectedClinician = ref<Clinician | null>(null)
const clinicianSchedule = ref<ClinicianScheduleData | null>(null)
const clinicianRotations = ref<any[]>([])
const selectedRotation = ref<any | null>(null)
const selectedNewRotationObj = ref<any | null>(null)
const loadingSchedule = ref(false)
const scheduleError = ref<string | null>(null)
const allRotations = ref<RotationWithService[]>([])
const currentYear = ref(new Date().getFullYear())
const filteredRotations = ref<any[]>([])
const rotationSearchQuery = ref('')
const includeAllAffiliates = ref(false)

// Computed properties for year selection
const availableYears = computed(() => {
    const thisYear = new Date().getFullYear()
    const years = []
    // Show current year plus 5 years back
    for (let i = 0; i <= 5; i++) {
        years.push(thisYear - i)
    }
    return years
})

const isPastYear = computed(() => {
    return currentYear.value < new Date().getFullYear()
})

// Use schedule data directly from API (no transformation needed)
const schedulesBySemester = computed(() => {
    if (!clinicianSchedule.value || !clinicianSchedule.value.schedulesBySemester) return []
    return clinicianSchedule.value.schedulesBySemester
})

// Computed property for available rotations (filtered to exclude already assigned ones)
const availableRotations = computed(() => {
    if (!allRotations.value || !clinicianRotations.value) return []

    // Get list of rotation IDs already assigned to this clinician
    const assignedRotationIds = new Set(clinicianRotations.value.map((r: any) => r.rotId))

    // Filter out rotations already assigned
    return allRotations.value
        .filter((rotation: RotationWithService) => !assignedRotationIds.has(rotation.rotId))
        .map((rotation: RotationWithService) => ({
            rotId: rotation.rotId,
            rotationName: rotation.name,
            abbreviation: rotation.abbreviation,
            serviceName: rotation.service?.serviceName || 'Unknown Service'
        }))
        .sort((a, b) => a.rotationName.localeCompare(b.rotationName))
})

// Check if clinician has no rotation assignments
const hasNoAssignments = computed(() => {
    if (!schedulesBySemester.value || schedulesBySemester.value.length === 0) return false

    // Check if all weeks have no rotation assignments
    return schedulesBySemester.value.every(semester =>
        semester.weeks.every(week => !week.rotation)
    )
})

const fetchAllRotations = async () => {
    try {
        const result = await RotationService.getRotations({ includeService: true })
        if (result.success) {
            allRotations.value = result.result
        } else {
            // Failed to fetch rotations - handle gracefully
        }
    } catch (error) {
        // Error fetching rotations - handle gracefully
    }
}

const fetchClinicianSchedule = async (mothraId: string) => {
    loadingSchedule.value = true
    scheduleError.value = null
    clinicianSchedule.value = null
    clinicianRotations.value = []

    try {
        // Fetch schedule
        const result = await ClinicianService.getClinicianSchedule(mothraId, { year: currentYear.value })
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
                .sort((a, b) => a.rotationName.localeCompare(b.rotationName))

            // Don't auto-select any rotation - let user choose
            selectedRotation.value = null
        } else {
            scheduleError.value = result.errors.join(', ') || 'Failed to load schedule'
        }
    } catch (error) {
        scheduleError.value = error instanceof Error ? error.message : 'Failed to load schedule'
        // Error loading clinician schedule
    } finally {
        loadingSchedule.value = false
    }
}

const onClinicianChange = (clinician: Clinician | null) => {
    if (clinician) {
        // Update URL to include the clinician ID and year if not current
        const query: any = {}
        if (currentYear.value !== new Date().getFullYear()) {
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
        onClinicianChange(selectedClinician.value)
    } else {
        const query: any = {}
        if (currentYear.value !== new Date().getFullYear()) {
            query.year = currentYear.value
        }
        router.push({ query })
    }
}

// Initialize from URL parameters
const initializeFromUrl = () => {
    // Get year from URL parameters
    const yearParam = route.query.year
    if (yearParam && typeof yearParam === 'string') {
        currentYear.value = Number(yearParam)
    }
}

// Load clinician from URL parameter if present
const loadClinicianFromUrl = async () => {
    const mothraId = route.params.mothraId as string
    if (mothraId) {
        const decodedMothraId = decodeURIComponent(mothraId)
        try {
            // First, try to find the clinician in our clinician list to get the correct name
            const cliniciansResult = await ClinicianService.getClinicians({ year: currentYear.value })
            if (cliniciansResult.success) {
                let clinician = cliniciansResult.result.find(c => c.mothraId === decodedMothraId)

                if (clinician) {
                    // Found in the clinician list - use this (it has the correct name)
                    selectedClinician.value = clinician
                } else {
                    // Not found in list, but let's load the schedule and create a clinician from that
                    await fetchClinicianSchedule(decodedMothraId)
                    if (clinicianSchedule.value && clinicianSchedule.value.clinician) {
                        // Create a proper Clinician object with the correct structure
                        selectedClinician.value = {
                            mothraId: clinicianSchedule.value.clinician.mothraId,
                            firstName: clinicianSchedule.value.clinician.firstName,
                            lastName: clinicianSchedule.value.clinician.lastName,
                            fullName: clinicianSchedule.value.clinician.fullName,
                            role: clinicianSchedule.value.clinician.role
                        }
                    }
                    return // Schedule already loaded
                }
            }

            // Load the schedule for the selected clinician
            await fetchClinicianSchedule(decodedMothraId)
        } catch (error) {
            // If the clinician ID in URL is invalid, redirect to base route
            router.push({ name: 'ClinicianSchedule' })
        }
    }
}

const selectRotation = (rotation: any) => {
    selectedRotation.value = rotation
}

const onWeekClick = (week: any) => {
    if (isPastYear.value) return // No editing for past years

    if (week.rotation) {
        // Week has a rotation - functionality to be implemented
        // Future: toggle selection or show options
    } else {
        // Empty week - functionality to be implemented
        // Future: open a rotation picker
    }
}

const removeRotation = () => {
    if (isPastYear.value) return
    // Future: remove the rotation assignment - week parameter will be added when implemented
}

const togglePrimary = () => {
    if (isPastYear.value) return
    // Future: toggle primary evaluator status - week parameter will be added when implemented
}

const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    return `${date.getMonth() + 1}/${date.getDate()}/${date.getFullYear().toString().slice(-2)}`
}

const onAddRotationSelected = (rotId: number | null) => {
    if (rotId) {
        const rotation = availableRotations.value.find(r => r.rotId === rotId)
        if (rotation) {
            // Future: add the rotation to the clinician's schedule
        }

        // Reset dropdown
        selectedNewRotationObj.value = null
    }
}

const filterRotations = (rotations: any[], searchTerm: string) => {
    if (!searchTerm) return rotations
    const search = searchTerm.toLowerCase()
    return rotations.filter(rotation =>
        rotation.rotationName.toLowerCase().includes(search) ||
        rotation.serviceName.toLowerCase().includes(search)
    )
}

const onFilterRotations = (val: string, update: (fn: () => void) => void) => {
    rotationSearchQuery.value = val
    update(() => {
        filteredRotations.value = val === ''
            ? availableRotations.value.map(r => ({ ...r, displayText: `${r.rotationName} (${r.serviceName})` }))
            : filterRotations(availableRotations.value.map(r => ({ ...r, displayText: `${r.rotationName} (${r.serviceName})` })), val)
    })
}

// Watchers
watch(availableRotations, (newRotations) => {
    filteredRotations.value = newRotations.map(r => ({ ...r, displayText: `${r.rotationName} (${r.serviceName})` }))
}, { immediate: true })

// Load clinician from URL on component mount
onMounted(() => {
    // Set page title
    document.title = 'VIPER - Schedule by Clinician'

    // Initialize year from URL first
    initializeFromUrl()
    fetchAllRotations()
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

.year-selector {
    font-size: 16px;
    font-weight: bold;
    color: #8B0000;
    border: 1px solid #ccc;
    padding: 4px 8px;
    background-color: #fff;
    border-radius: 4px;
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