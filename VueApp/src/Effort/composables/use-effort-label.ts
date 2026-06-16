import type { Ref } from "vue"
import { computed } from "vue"
import type { EffortTypeOptionDto } from "../types"

// Effort types flagged usesWeeks switch the value label to "Weeks" from this
// term onward; everything before it stays "Hours".
const WEEKS_TERM_THRESHOLD = 201604

/**
 * Label ("Hours"/"Weeks") for an effort value input. Add dialogs mark the field
 * required with a trailing asterisk; the edit dialog passes required=false.
 */
function useEffortLabel(
    selectedEffortType: Ref<string | null>,
    effortTypes: Ref<EffortTypeOptionDto[]>,
    options: { termCode: () => number; required?: boolean },
) {
    const usesWeeks = computed(() => {
        const effortType = effortTypes.value.find((et) => et.id === selectedEffortType.value)
        return Boolean(effortType?.usesWeeks) && options.termCode() >= WEEKS_TERM_THRESHOLD
    })

    return computed(() => {
        const unit = usesWeeks.value ? "Weeks" : "Hours"
        return options.required === false ? unit : `${unit} *`
    })
}

export { useEffortLabel }
