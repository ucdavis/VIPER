import { useQuasar } from "quasar"

interface ConfirmOptions {
    title: string
    message: string
    /** Label for the confirming button, e.g. "Delete". */
    okLabel: string
    /** Quasar colour for the confirming button; "negative" for destructive actions. */
    okColor?: string
    cancelLabel?: string
}

/**
 * The app's confirm-before-acting dialog, as an awaitable boolean.
 *
 * Quasar's dialog is callback-based, so every caller otherwise hand-rolls the same Promise
 * wrapper - and has to remember that a dismiss (Esc, backdrop) must resolve false like a
 * cancel, not hang forever. Centralising it also keeps the buttons consistent: flat cancel,
 * unelevated ok, `persistent` so a stray backdrop click can't dismiss a destructive prompt.
 *
 * ```typescript
 * const { confirmAction } = useConfirmDialog()
 * const ok = await confirmAction({
 *     title: "Delete File",
 *     message: `Delete "${name}"?`,
 *     okLabel: "Delete",
 *     okColor: "negative",
 * })
 * ```
 */
export function useConfirmDialog() {
    const $q = useQuasar()

    function confirmAction({
        title,
        message,
        okLabel,
        okColor = "primary",
        cancelLabel = "Cancel",
    }: ConfirmOptions): Promise<boolean> {
        // The one place the callback-based QDialog is wrapped up into a promise, so callers await a boolean.
        // eslint-disable-next-line avoid-new -- centralising this wrapper is the point of the composable
        return new Promise<boolean>((resolve) => {
            $q.dialog({
                title,
                message,
                cancel: { label: cancelLabel, flat: true },
                persistent: true,
                ok: { label: okLabel, color: okColor, unelevated: true },
            })
                .onOk(() => resolve(true))
                .onCancel(() => resolve(false))
                .onDismiss(() => resolve(false))
        })
    }

    return { confirmAction }
}
