import type { QVueGlobals } from "quasar"

/**
 * Utility functions for showing confirmation dialogs using Quasar.
 */

/**
 * Show a confirmation dialog using Quasar's dialog component
 * Returns a promise that resolves to true if confirmed, false if cancelled
 */
function showConfirmationDialog($q: QVueGlobals, title: string, message: string): Promise<boolean> {
    return new Promise((resolve) => {
        $q.dialog({
            title,
            message,
            cancel: true,
            persistent: true,
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
    })
}

/**
 * Show a bulk delete confirmation dialog.
 * @param $q Quasar dialog instance
 * @param message Confirmation message
 * @returns Promise<boolean> - True if confirmed, false if cancelled
 */
function showBulkDeleteConfirmation($q: QVueGlobals, message: string): Promise<boolean> {
    return showConfirmationDialog($q, "Bulk Delete Confirmation", message)
}

export { showConfirmationDialog, showBulkDeleteConfirmation }
