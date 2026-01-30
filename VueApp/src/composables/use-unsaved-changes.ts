import type { Ref } from "vue"
import { ref, computed } from "vue"
import { useQuasar } from "quasar"

/**
 * Composable for tracking unsaved form changes and showing confirmation dialogs.
 *
 * IMPORTANT: formState must be JSON-serializable (no functions, Dates, or circular refs).
 * Uses JSON.stringify for deep equality comparison.
 *
 * Usage:
 * ```typescript
 * const { isDirty, setInitialState, confirmClose, resetDirtyState } = useUnsavedChanges(form)
 *
 * // When dialog opens, capture initial state
 * watch(() => props.modelValue, (isOpen) => {
 *     if (isOpen) {
 *         resetForm()
 *         setInitialState()
 *     }
 * })
 *
 * // When closing dialog
 * async function handleClose() {
 *     if (await confirmClose()) {
 *         emit('update:modelValue', false)
 *     }
 * }
 *
 * // After successful save
 * resetDirtyState()
 * ```
 */
export function useUnsavedChanges<T extends object>(formState: Ref<T>) {
    const $q = useQuasar()
    const initialState = ref<string>("")

    /**
     * Capture the current form state as the "clean" baseline.
     * Call this when the dialog opens or after a successful save.
     */
    function setInitialState() {
        initialState.value = JSON.stringify(formState.value)
    }

    /**
     * Reset dirty tracking (equivalent to setInitialState).
     * Call after successful save to mark current state as clean.
     */
    function resetDirtyState() {
        setInitialState()
    }

    /**
     * Whether the form has unsaved changes compared to initial state.
     */
    const isDirty = computed(() => {
        if (!initialState.value) {
            return false
        }
        return JSON.stringify(formState.value) !== initialState.value
    })

    /**
     * Show confirmation dialog if form is dirty.
     * Returns true if user confirms close (or form is clean), false if user cancels.
     */
    function confirmClose(): Promise<boolean> {
        return new Promise((resolve) => {
            if (!isDirty.value) {
                resolve(true)
                return
            }

            $q.dialog({
                title: "Unsaved Changes",
                message: "You have unsaved changes. Are you sure you want to close?",
                cancel: {
                    label: "Keep Editing",
                    flat: true,
                },
                ok: {
                    label: "Discard Changes",
                    color: "negative",
                },
                persistent: true,
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
                .onDismiss(() => resolve(false))
        })
    }

    return {
        isDirty,
        setInitialState,
        resetDirtyState,
        confirmClose,
    }
}
