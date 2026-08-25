import { computed, ref, watch } from "vue"
import { useQuasar } from "quasar"
import type { Ref } from "vue"

interface SaveOutcome {
    success: boolean
    result?: any
    errors: string[] | null
}

interface UseAddRecordDialogOptions<TForm, TEditData> {
    editData: () => TEditData | null | undefined
    /** Watched to reset the form when the enclosing context (e.g. unit/section) changes. Omit if there is none. */
    resetOn?: () => unknown
    emptyForm: () => TForm
    formFromEditData: () => TForm
    validate: (form: TForm) => string | null
    sendSave: (form: TForm, isEdit: boolean) => Promise<SaveOutcome>
    onSaved: (result: any) => void
    onClose: () => void
    /** Used in the default save-failure message: "Failed to save/upload {recordLabel}". */
    recordLabel?: string
}

/**
 * Shared submit lifecycle for the phone-area "add/edit record" dialogs (PhoneList, SVM,
 * SVM frequent numbers).
 */
export function useAddRecordDialog<TForm, TEditData>({
    editData,
    resetOn,
    emptyForm,
    formFromEditData,
    validate,
    sendSave,
    onSaved,
    onClose,
    recordLabel = "phone record",
}: UseAddRecordDialogOptions<TForm, TEditData>) {
    const $q = useQuasar()

    const saving = ref(false)
    const formError = ref("")
    const isEdit = computed(() => editData() !== undefined && editData() !== null)

    const form = ref(isEdit.value ? formFromEditData() : emptyForm()) as Ref<TForm>

    watch(editData, () => {
        form.value = formFromEditData()
    })
    if (resetOn) {
        watch(resetOn, () => {
            form.value = emptyForm()
        })
    }

    function resetForm() {
        form.value = emptyForm()
        formError.value = ""
    }

    function onValidationError() {
        formError.value = "Please complete the required fields before saving."
    }

    function reportSaveError(res: { errors: string[] | null }) {
        formError.value = res.errors?.[0] ?? `Failed to ${isEdit.value ? "save" : "upload"} ${recordLabel}`
    }

    async function save() {
        // The saving flag flips synchronously before the first await, so a second submit - a
        // double click, or Enter while the button is already spinning - returns here instead
        // of sending the record twice.
        if (saving.value) {
            return
        }
        formError.value = ""

        const validationError = validate(form.value)
        if (validationError) {
            reportSaveError({ errors: [validationError] })
            return
        }

        saving.value = true
        const res = await sendSave(form.value, isEdit.value)
        saving.value = false

        if (!res.success) {
            reportSaveError(res)
            return
        }

        $q.notify({ type: "positive", message: isEdit.value ? "Phone record updated" : "Phone record created" })
        onSaved(res.result)
        onClose()
    }

    return {
        form,
        saving,
        formError,
        isEdit,
        save,
        resetForm,
        onValidationError,
    }
}

export type { SaveOutcome }
