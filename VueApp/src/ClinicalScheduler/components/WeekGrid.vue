<template>
  <div class="week-grid">
    <!-- Grid Header -->
    <div class="week-grid-header mb-3">
      <h5 v-if="selectedRotationName" class="mb-2">
        Schedule for: <strong>{{ selectedRotationName }}</strong>
      </h5>
      <div v-else class="text-muted">
        Select a rotation to view schedule
      </div>
    </div>

    <!-- Week Grid Table -->
    <div v-if="selectedRotationName" class="table-responsive">
      <table class="table table-bordered week-grid-table">
        <thead class="table-light">
          <tr>
            <th scope="col" class="week-header">Week</th>
            <th scope="col" class="dates-header">Dates</th>
            <th scope="col" class="clinicians-header">Assigned Clinicians</th>
            <th scope="col" class="primary-header">Primary Evaluator</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="week in weeks" :key="week.weekNumber" class="week-row">
            <!-- Week Number -->
            <td class="week-cell">
              <div class="week-number">
                Week {{ week.weekNumber }}
              </div>
            </td>
            
            <!-- Week Dates -->
            <td class="dates-cell">
              <div class="week-dates">
                <div class="date-range">
                  {{ formatDate(week.startDate) }} - {{ formatDate(week.endDate) }}
                </div>
                <div class="date-details text-muted small">
                  {{ getDateDetails(week.startDate, week.endDate) }}
                </div>
              </div>
            </td>
            
            <!-- Assigned Clinicians -->
            <td class="clinicians-cell">
              <div class="clinicians-list">
                <div v-if="week.clinicians.length === 0" class="no-clinicians text-muted">
                  <i class="bi bi-person-plus"></i>
                  No clinicians assigned
                </div>
                <div v-else>
                  <div 
                    v-for="clinician in week.clinicians" 
                    :key="clinician.id"
                    class="clinician-badge"
                    :class="{ 'primary-evaluator': clinician.isPrimary }"
                  >
                    {{ clinician.name }}
                    <i v-if="clinician.isPrimary" class="bi bi-star-fill text-warning ms-1" title="Primary Evaluator"></i>
                  </div>
                </div>
              </div>
            </td>
            
            <!-- Primary Evaluator Status -->
            <td class="primary-cell">
              <div class="primary-status">
                <span v-if="week.hasPrimaryEvaluator" class="badge bg-success">
                  <i class="bi bi-check-circle"></i> Assigned
                </span>
                <span v-else class="badge bg-warning text-dark">
                  <i class="bi bi-exclamation-triangle"></i> Missing
                </span>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-state text-center py-5">
      <div class="empty-state-icon mb-3">
        <i class="bi bi-calendar3 text-muted" style="font-size: 3rem;"></i>
      </div>
      <h6 class="text-muted">No Rotation Selected</h6>
      <p class="text-muted">Please select a rotation from the dropdown above to view its schedule.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

// Props
interface Props {
  selectedRotationName?: string | null
  scheduleData?: any[] // Will be expanded in Phase 5
}

const props = withDefaults(defineProps<Props>(), {
  selectedRotationName: null,
  scheduleData: () => []
})

// Sample data structure for static weeks (will be replaced with real data in Phase 5)
interface WeekData {
  weekNumber: number
  startDate: Date
  endDate: Date
  clinicians: {
    id: string
    name: string
    isPrimary: boolean
  }[]
  hasPrimaryEvaluator: boolean
}

// Generate sample weeks for display (static for now)
const weeks = computed<WeekData[]>(() => {
  if (!props.selectedRotationName) return []
  
  // Generate 12 weeks of sample data
  const baseDate = new Date()
  const sampleWeeks: WeekData[] = []
  
  for (let i = 0; i < 12; i++) {
    const startDate = new Date(baseDate)
    startDate.setDate(baseDate.getDate() + (i * 7))
    
    const endDate = new Date(startDate)
    endDate.setDate(startDate.getDate() + 6)
    
    // Add some sample clinicians for demonstration
    const clinicians = i % 3 === 0 ? [
      { id: 'sample1', name: 'Dr. Smith', isPrimary: true },
      { id: 'sample2', name: 'Dr. Johnson', isPrimary: false }
    ] : i % 4 === 0 ? [
      { id: 'sample3', name: 'Dr. Williams', isPrimary: false }
    ] : []
    
    sampleWeeks.push({
      weekNumber: i + 1,
      startDate,
      endDate,
      clinicians,
      hasPrimaryEvaluator: clinicians.some(c => c.isPrimary)
    })
  }
  
  return sampleWeeks
})

// Helper methods
function formatDate(date: Date): string {
  return date.toLocaleDateString('en-US', { 
    month: 'short', 
    day: 'numeric'
  })
}

function getDateDetails(startDate: Date, endDate: Date): string {
  const start = startDate.toLocaleDateString('en-US', { 
    month: 'numeric', 
    day: 'numeric',
    year: 'numeric'
  })
  const end = endDate.toLocaleDateString('en-US', { 
    month: 'numeric', 
    day: 'numeric',
    year: 'numeric'
  })
  return `${start} - ${end}`
}
</script>

<style scoped>
.week-grid {
  max-width: 100%;
}

.week-grid-table {
  font-size: 0.9rem;
}

.week-grid-table th {
  background-color: #f8f9fa;
  font-weight: 600;
  vertical-align: middle;
  text-align: center;
}

.week-header {
  width: 80px;
  min-width: 80px;
}

.dates-header {
  width: 200px;
  min-width: 180px;
}

.clinicians-header {
  width: auto;
  min-width: 250px;
}

.primary-header {
  width: 120px;
  min-width: 120px;
}

.week-row {
  vertical-align: middle;
}

.week-cell {
  text-align: center;
  background-color: #f8f9fa;
}

.week-number {
  font-weight: 600;
  color: #495057;
}

.dates-cell {
  text-align: center;
}

.week-dates {
  padding: 0.25rem 0;
}

.date-range {
  font-weight: 500;
  color: #212529;
}

.date-details {
  margin-top: 0.25rem;
}

.clinicians-cell {
  padding: 0.75rem;
}

.clinicians-list {
  min-height: 2rem;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.clinician-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.5rem;
  background-color: #e9ecef;
  border: 1px solid #dee2e6;
  border-radius: 0.375rem;
  font-size: 0.875rem;
  margin-bottom: 0.25rem;
  max-width: fit-content;
}

.clinician-badge.primary-evaluator {
  background-color: #fff3cd;
  border-color: #ffeaa7;
  font-weight: 500;
}

.no-clinicians {
  display: flex;
  align-items: center;
  justify-content: center;
  font-style: italic;
  min-height: 2rem;
}

.primary-cell {
  text-align: center;
  vertical-align: middle;
}

.primary-status {
  display: flex;
  justify-content: center;
}

.badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
}

.empty-state {
  background-color: #f8f9fa;
  border-radius: 0.5rem;
  margin: 2rem 0;
}

.empty-state-icon {
  opacity: 0.5;
}

/* Responsive adjustments */
@media (max-width: 768px) {
  .week-grid-table {
    font-size: 0.8rem;
  }
  
  .clinician-badge {
    font-size: 0.75rem;
    padding: 0.2rem 0.4rem;
  }
  
  .dates-header,
  .primary-header {
    min-width: 100px;
  }
}
</style>