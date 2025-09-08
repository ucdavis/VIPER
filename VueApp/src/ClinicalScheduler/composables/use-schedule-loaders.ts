import { useQuasar } from "quasar"
import { useScheduleStore } from "../stores/schedule"

/**
 * Composable for loading schedule data with error handling
 */
export function useScheduleLoaders(year: number) {
    const $q = useQuasar()
    const scheduleStore = useScheduleStore()

    /**
     * Load rotation schedule with error handling
     */
    async function loadRotationSchedule(rotationId: number): Promise<boolean> {
        try {
            const result = await scheduleStore.loadRotationSchedule(rotationId, year)
            return result.success
        } catch {
            $q.notify({
                type: "negative",
                message: "Failed to load rotation schedule",
            })
            return false
        }
    }

    /**
     * Load clinician schedule with error handling
     */
    async function loadClinicianSchedule(mothraId: string): Promise<boolean> {
        try {
            const result = await scheduleStore.loadClinicianSchedule(mothraId, year)
            return result.success
        } catch {
            $q.notify({
                type: "negative",
                message: "Failed to load clinician schedule",
            })
            return false
        }
    }

    return {
        loadRotationSchedule,
        loadClinicianSchedule,
    }
}
