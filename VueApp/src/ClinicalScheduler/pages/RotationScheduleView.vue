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
            <select 
                v-model="selectedRotationId" 
                @change="onRotationChange"
                class="title-dropdown"
                :disabled="isLoading"
            >
                <option value="">
                    {{ isLoading ? 'Loading rotations...' : 'Select a rotation' }}
                </option>
                <optgroup 
                    v-for="service in groupedRotations" 
                    :key="service.serviceId"
                    :label="service.serviceName"
                >
                    <option 
                        v-for="rotation in service.rotations" 
                        :key="rotation.rotId"
                        :value="rotation.rotId"
                    >
                        {{ getRotationAbbreviation(rotation) }}
                    </option>
                </optgroup>
            </select>
        </h2>
        
        <!-- Error display for rotation loading -->
        <div v-if="error" class="error-message">
            {{ error }}
            <button @click="loadRotations" :disabled="isLoading">Retry</button>
        </div>

        <!-- Instructions (only show when rotation is selected) -->
        <div v-if="selectedRotation" class="instructions">
            <p>This list of clinicians should be contain any clinician scheduled for the rotation in the current or previous year.</p>
            <p>The user can click on a clinician to select them, and then click on any week to schedule them.</p>
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
                <select class="add-clinician-dropdown">
                    <option>Add Clinician</option>
                    <option>Choi, April</option>
                    <option>Chromik, Melissa</option>
                    <option>Keil, Tessa</option>
                    <option>Lui, Clinson</option>
                </select>
            </div>
        </div>


        <!-- Season headers and week grids -->
        <div v-if="selectedRotation">
            <h3>Spring 2025</h3>
            <div class="week-grid">
                <div v-for="week in springWeeks" :key="week.weekId" 
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
                            <span v-if="!assignment.isPrimary" class="remove-btn" title="Remove this clinician from the schedule." @click.stop="removeAssignment()">✖</span>
                            <span v-else class="remove-btn-disabled" title="Cannot remove primary clinician. Make another clinician primary first.">✖</span>
                            <span>{{ assignment.clinicianName }}</span>
                            <span class="primary-star" 
                                  :class="{ filled: assignment.isPrimary }"
                                  :title="assignment.isPrimary ? 'Primary evaluator. To transfer primary status, click the star on another clinician.' : 'Click to make this clinician the primary evaluator.'"
                                  @click.stop="togglePrimary()">{{ assignment.isPrimary ? '★' : '☆' }}</span>
                        </div>
                    </div>
                </div>
            </div>

            <h3>Summer 2025</h3>
            <div class="week-grid">
                <div v-for="week in summerWeeks" :key="week.weekId" 
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
                            <span v-if="!assignment.isPrimary" class="remove-btn" title="Remove this clinician from the schedule." @click.stop="removeAssignment()">✖</span>
                            <span v-else class="remove-btn-disabled" title="Cannot remove primary clinician. Make another clinician primary first.">✖</span>
                            <span>{{ assignment.clinicianName }}</span>
                            <span class="primary-star" 
                                  :class="{ filled: assignment.isPrimary }"
                                  :title="assignment.isPrimary ? 'Primary evaluator. To transfer primary status, click the star on another clinician.' : 'Click to make this clinician the primary evaluator.'"
                                  @click.stop="togglePrimary()">{{ assignment.isPrimary ? '★' : '☆' }}</span>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Legend -->
        <div v-if="selectedRotation" class="legend">
            <h4>Legend</h4>
            <ul>
                <li><span class="icon-demo remove-icon">✖</span> removes the clinician from the schedule</li>
                <li>Primary clinicians (<span class="icon-demo primary-icon">★</span>) cannot be removed until another clinician is made primary first</li>
                <li><span class="icon-demo primary-icon">★</span> marks them as primary (and unmarks the current primary, if any)</li>
                <li><span class="icon-demo alert-icon">⚠️</span> indicates weeks that require a primary evaluator</li>
            </ul>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { RotationService, type RotationWithService, type RotationScheduleData, type WeekItem } from '../services/RotationService'

// Router
const route = useRoute()
const router = useRouter()

// Reactive data
const selectedRotationId = ref<number | string>('')
const selectedRotation = ref<RotationWithService | null>(null)
const selectedClinician = ref<string | null>(null)
const rotations = ref<RotationWithService[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)

// Real data from database
const scheduleData = ref<RotationScheduleData | null>(null)
const isLoadingSchedule = ref(false)
const scheduleError = ref<string | null>(null)
const currentYear = ref(new Date().getFullYear())

// Available clinicians from schedule data
const availableClinicians = computed(() => {
    if (!scheduleData.value) return []
    
    const clinicians = new Set<string>()
    scheduleData.value.instructorSchedules.forEach(schedule => {
        clinicians.add(schedule.fullName)
    })
    
    return Array.from(clinicians).sort()
})

// Week data grouped by season
const springWeeks = computed(() => {
    if (!scheduleData.value) return []
    
    return scheduleData.value.weeks
        .filter(week => {
            const date = new Date(week.dateStart)
            const month = date.getMonth()
            // Spring: March (2) through June (5)
            return month >= 2 && month <= 5
        })
        .sort((a, b) => new Date(a.dateStart).getTime() - new Date(b.dateStart).getTime())
})

const summerWeeks = computed(() => {
    if (!scheduleData.value) return []
    
    return scheduleData.value.weeks
        .filter(week => {
            const date = new Date(week.dateStart)
            const month = date.getMonth()
            // Summer: July (6) through August (7)
            return month >= 6 && month <= 7
        })
        .sort((a, b) => new Date(a.dateStart).getTime() - new Date(b.dateStart).getTime())
})

// Computed
const groupedRotations = computed(() => {
    const groups: Record<number, { serviceId: number, serviceName: string, rotations: RotationWithService[] }> = {}
    
    rotations.value.forEach(rotation => {
        if (rotation.service) {
            const serviceId = rotation.service.serviceId
            if (!groups[serviceId]) {
                groups[serviceId] = {
                    serviceId,
                    serviceName: rotation.service.serviceName,
                    rotations: []
                }
            }
            groups[serviceId].rotations.push(rotation)
        }
    })

    // Sort rotations within each service
    Object.values(groups).forEach(group => {
        group.rotations.sort((a, b) => a.name.localeCompare(b.name))
    })

    // Return sorted by service name
    return Object.values(groups).sort((a, b) => a.serviceName.localeCompare(b.serviceName))
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

function getRotationAbbreviation(rotation: RotationWithService): string {
    // Use abbreviation from database, fallback to rotation name if not available
    return rotation.abbreviation || rotation.name
}

async function onRotationChange() {
    const rotationId = selectedRotationId.value ? Number(selectedRotationId.value) : null
    selectedRotation.value = rotations.value.find(r => r.rotId === rotationId) || null
    selectedClinician.value = null // Reset clinician selection when rotation changes
    
    // Update URL with selected rotation
    await updateUrl()
    
    if (selectedRotation.value) {
        console.log('Selected rotation:', selectedRotation.value)
        await loadScheduleData(selectedRotation.value.rotId)
    } else {
        scheduleData.value = null
    }
}

async function updateUrl() {
    const query: Record<string, string> = {}
    
    if (selectedRotationId.value) {
        query.rotationId = selectedRotationId.value.toString()
    }
    
    // Update URL without triggering a page reload
    await router.replace({ 
        path: route.path, 
        query: Object.keys(query).length > 0 ? query : undefined 
    })
}

function initializeFromUrl() {
    // Get rotation ID from URL parameters
    const rotationIdParam = route.query.rotationId
    if (rotationIdParam && typeof rotationIdParam === 'string') {
        selectedRotationId.value = rotationIdParam
    }
}

async function loadScheduleData(rotationId: number) {
    isLoadingSchedule.value = true
    scheduleError.value = null

    try {
        const result = await RotationService.getRotationSchedule(rotationId, { year: currentYear.value })
        
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
watch(() => route.query.rotationId, (newRotationId) => {
    if (newRotationId !== selectedRotationId.value?.toString()) {
        selectedRotationId.value = newRotationId ? newRotationId.toString() : ''
        // Trigger rotation change if rotations are already loaded
        if (rotations.value.length > 0) {
            onRotationChange()
        }
    }
})

// Lifecycle
onMounted(async () => {
    // Initialize from URL first
    initializeFromUrl()
    
    // Load rotations
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