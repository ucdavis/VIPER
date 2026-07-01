import type { Ref, ComputedRef } from "vue"
import { computed, ref } from "vue"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { usePercentageForm } from "./use-percentage-form"
import type { PercentAssignTypeDto, UnitDto } from "../types"

/**
 * Shared state for the percentage Add/Edit dialogs: the percentage form
 * composable, the props bundle for PercentAssignmentFormFields, unsaved-changes
 * tracking, and the saving/error/warning state common to both dialogs.
 */
function usePercentageDialog(
    percentAssignTypes: Ref<PercentAssignTypeDto[]> | ComputedRef<PercentAssignTypeDto[]>,
    units: Ref<UnitDto[]> | ComputedRef<UnitDto[]>,
) {
    const {
        form,
        groupedTypeOptions,
        unitOptions,
        modifierOptions,
        monthOptions,
        yearOptions,
        endMonthRules,
        endYearRules,
        formatToIsoDate,
        parseDateToMonthYear,
        resetForm,
    } = usePercentageForm(percentAssignTypes, units)

    // Props passed straight through to PercentAssignmentFormFields.
    const fieldProps = computed(() => ({
        groupedTypeOptions: groupedTypeOptions.value,
        modifierOptions,
        unitOptions: unitOptions.value,
        monthOptions,
        yearOptions: yearOptions.value,
        endMonthRules: endMonthRules.value,
        endYearRules: endYearRules.value,
    }))

    const { setInitialState, confirmClose } = useUnsavedChanges(form)

    const isSaving = ref(false)
    const errorMessage = ref("")
    const warningMessage = ref("")
    const pendingWarningConfirm = ref(false)

    return {
        form,
        fieldProps,
        formatToIsoDate,
        parseDateToMonthYear,
        resetForm,
        setInitialState,
        confirmClose,
        isSaving,
        errorMessage,
        warningMessage,
        pendingWarningConfirm,
    }
}

export { usePercentageDialog }
