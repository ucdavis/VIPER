import { onMounted, onUpdated } from "vue"
import type { Ref } from "vue"
import type { QSelect } from "quasar"

/**
 * Names a QSelect from a visible label rendered beside it, for forms that show the question in
 * full above the field rather than as a floating label.
 *
 * For QSelect, a fall-through `aria-labelledby` never reaches the control.
 * The `label` prop does name the combobox, but only by rendering a
 * floating label, which is shortened when the available width is limited.
 * Setting the attribute on the target directly is what
 * is left. `RichTextEditor` takes the same approach for QEditor's contenteditable region.
 *
 * Quasar rebuilds the focus target when a field moves between readonly and editable, so the
 * attribute is re-applied on every update.
 */
export function useSelectAriaLabel(selectRef: Ref<QSelect | null | undefined>, labelId: string): void {
    function applyAccessibleName(): void {
        const root = selectRef.value?.$el as HTMLElement | undefined | null
        const combobox = root?.querySelector<HTMLElement>('[role="combobox"]')
        if (combobox && combobox.getAttribute("aria-labelledby") !== labelId) {
            combobox.setAttribute("aria-labelledby", labelId)
        }
    }

    onMounted(applyAccessibleName)
    onUpdated(applyAccessibleName)
}
