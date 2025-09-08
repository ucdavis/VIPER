import { useQuasar } from "quasar"

/**
 * Composable for schedule-related dialog confirmations
 */
export function useScheduleDialogs() {
    const $q = useQuasar()

    /**
     * Show conflict confirmation dialog
     */
    function confirmConflictDialog(clinicianName: string, conflictRotationName: string): Promise<boolean> {
        return new Promise<boolean>((resolve) => {
            $q.dialog({
                title: "Other Rotation Assignment",
                message: `Note: ${clinicianName} is also scheduled for ${conflictRotationName} during this week. Do you want to add them to this rotation as well?`,
                persistent: true,
                ok: { label: "Yes, Add", color: "primary" },
                cancel: { label: "Cancel", color: "grey" },
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
        })
    }

    /**
     * Show removal confirmation dialog
     */
    function confirmRemovalDialog(clinicianName: string): Promise<boolean> {
        return new Promise<boolean>((resolve) => {
            $q.dialog({
                title: "Confirm Removal",
                message: `Remove ${clinicianName} from this week's schedule?`,
                cancel: true,
                persistent: true,
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
        })
    }

    /**
     * Show primary evaluator removal confirmation dialog
     */
    function confirmPrimaryRemovalDialog(clinicianName: string): Promise<boolean> {
        return new Promise<boolean>((resolve) => {
            $q.dialog({
                title: "Remove Primary Status",
                message: `Remove primary evaluator status from ${clinicianName}? Another clinician should be designated as primary.`,
                cancel: true,
                persistent: true,
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
        })
    }

    return {
        confirmConflictDialog,
        confirmRemovalDialog,
        confirmPrimaryRemovalDialog,
    }
}
