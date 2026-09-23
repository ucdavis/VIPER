import type { Ref } from "vue"
import { onBeforeRouteLeave } from "vue-router"
import { useConfirmDialog } from "@/composables/use-confirm-dialog"

/**
 * Guards a form page against navigating away with unsaved changes, asking the user to confirm
 * first. For a dialog rather than a page, see confirmClose in use-unsaved-changes.
 *
 * @param isDirty Whether the form holds changes that have not been saved.
 */
export function useConfirmLeave(isDirty: Ref<boolean>): void {
    const { confirmAction } = useConfirmDialog()

    onBeforeRouteLeave(async () => {
        if (!isDirty.value) {
            return true
        }
        return await confirmAction({
            title: "Unsaved Changes",
            message: "You have unsaved changes. Are you sure you want to leave?",
            okLabel: "Discard Changes",
            okColor: "negative",
            cancelLabel: "Keep Editing",
        })
    })
}
