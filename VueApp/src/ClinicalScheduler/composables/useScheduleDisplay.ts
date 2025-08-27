import { computed, Ref } from 'vue'
import { useQuasar } from 'quasar'

export interface ScheduleWeek {
    weekId: number
    weekNumber: number
    dateStart: string
    dateEnd: string
    rotation?: any
    clinician?: any
    isPrimaryEvaluator?: boolean
    scheduleId?: number
}

export function useScheduleDisplay(currentYear: Ref<number | null>, currentGradYear: Ref<number>) {
    const $q = useQuasar()
    
    /**
     * Check if a year is in the past
     */
    const isPastYear = computed(() => {
        const year = currentYear.value
        if (year === null) return false
        
        // Compare against the current grad year from the backend
        // which represents the active academic year
        return year < currentGradYear.value
    })

    /**
     * Format date range for display
     */
    function formatDateRange(startDate: string, endDate: string): string {
        const start = new Date(startDate)
        const end = new Date(endDate)
        const options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' }
        
        // If same month, show as "Jan 1-7"
        if (start.getMonth() === end.getMonth()) {
            return `${start.toLocaleDateString('en-US', options)}-${end.getDate()}`
        }
        
        // Otherwise show as "Jan 1 - Feb 7"
        return `${start.toLocaleDateString('en-US', options)} - ${end.toLocaleDateString('en-US', options)}`
    }

    /**
     * Get week display status/color
     */
    function getWeekStatus(week: ScheduleWeek): {
        color: string
        textColor: string
        hasAssignment: boolean
        isPrimary: boolean
    } {
        const hasAssignment = !!(week.rotation || week.clinician)
        const isPrimary = week.isPrimaryEvaluator || false
        
        return {
            color: hasAssignment 
                ? (isPrimary ? 'primary' : 'green-2')
                : 'grey-2',
            textColor: hasAssignment
                ? (isPrimary ? 'white' : 'green-9')
                : 'grey-6',
            hasAssignment,
            isPrimary
        }
    }

    /**
     * Handle week click with proper validation
     */
    async function handleWeekClick(
        week: ScheduleWeek,
        selectedItem: any,
        onSchedule: (week: ScheduleWeek) => Promise<void>
    ) {
        if (isPastYear.value) return // No editing for past years

        if (!selectedItem) {
            $q.notify({
                type: 'warning',
                message: 'Please select an item first'
            })
            return
        }

        await onSchedule(week)
    }

    /**
     * Group weeks by semester (already done by backend)
     */
    function groupWeeksBySemester(weeks: ScheduleWeek[]): Record<string, ScheduleWeek[]> {
        // This is typically already done by the backend
        // but provided here for consistency
        const groupedMap = new Map<string, ScheduleWeek[]>()
        
        weeks.forEach(week => {
            const date = new Date(week.dateStart)
            const month = date.getMonth() + 1
            const year = date.getFullYear()
            
            let semester: string
            if (month >= 7 && month <= 12) {
                semester = `Fall ${year}`
            } else if (month >= 1 && month <= 3) {
                semester = `Winter ${year}`
            } else {
                semester = `Spring ${year}`
            }
            
            if (!groupedMap.has(semester)) {
                groupedMap.set(semester, [])
            }
            groupedMap.get(semester)!.push(week)
        })
        
        return Object.fromEntries(groupedMap)
    }


    /**
     * Get display name for a rotation
     */
    function getRotationDisplayName(rotation: any): string {
        if (!rotation) return ''
        
        // Remove everything after and including the first parenthesis
        const name = rotation.rotationName || rotation.name || ''
        const beforeParenthesis = name.split('(')[0].trim()
        return beforeParenthesis || name
    }

    /**
     * Get display name for a clinician
     */
    function getClinicianDisplayName(clinician: any): string {
        if (!clinician) return ''
        return clinician.fullName || `${clinician.firstName} ${clinician.lastName}`.trim()
    }

    return {
        isPastYear,
        formatDateRange,
        getWeekStatus,
        handleWeekClick,
        groupWeeksBySemester,
        getRotationDisplayName,
        getClinicianDisplayName
    }
}