<template>
    <RecordFormDialog
        :model-value="modelValue"
        title-id="svm-add-frequent-number-dialog-title"
        :title="isEdit ? 'Edit Frequent Number' : 'Add Frequent Number'"
        :is-edit="isEdit"
        :saving="saving"
        :form-error="formError"
        submit-label="Upload"
        @update:model-value="emit('update:modelValue', $event)"
        @submit="save"
        @validation-error="onValidationError"
        @hide="resetForm"
    >
        <q-input
            v-model="form.label"
            dense
            outlined
            label="Location"
            maxlength="100"
            :rules="[(v: string | null) => !!v || 'Please enter a location']"
        />

        <q-input
            v-model="form.phone"
            dense
            outlined
            label="Phone Number"
            maxlength="25"
            :rules="[(v: string | null) => !!v || 'Please enter a phone number']"
        />
    </RecordFormDialog>
</template>

<script setup lang="ts">
import { svmFrequentNumberService } from "../services/svm-frequent-number-service.ts"
import { useAddRecordDialog } from "../composables/use-add-record-dialog.ts"
import RecordFormDialog from "@/components/RecordFormDialog.vue"
import type { SVMFrequentNumberRecord } from "../types/svm-phone-types.ts"

const props = defineProps<{
    modelValue: boolean
    editFrequentData?: SVMFrequentNumberRecord | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    saved: [value: boolean]
}>()

type SVMFrequentNumberForm = {
    label: string
    phone: string
    entryId: number
}

function emptyForm(): SVMFrequentNumberForm {
    return {
        label: "",
        phone: "",
        entryId: -1,
    }
}

function formFromEditData(): SVMFrequentNumberForm {
    return {
        label: props.editFrequentData?.label ?? "",
        phone: props.editFrequentData?.phone ?? "",
        entryId: props.editFrequentData?.entryId ?? -1,
    }
}

function buildFormData(form: SVMFrequentNumberForm): SVMFrequentNumberRecord {
    return {
        label: form.label,
        phone: form.phone,
        entryId: form.entryId,
    }
}

const { form, saving, formError, isEdit, save, resetForm, onValidationError } = useAddRecordDialog<
    SVMFrequentNumberForm,
    SVMFrequentNumberRecord
>({
    editData: () => props.editFrequentData,
    emptyForm,
    formFromEditData,
    validate: () => null,
    sendSave: (f, editing) => {
        const dto = buildFormData(f)
        return editing
            ? svmFrequentNumberService.updateFrequentNumber(f.entryId, dto)
            : svmFrequentNumberService.addFrequentNumber(dto)
    },
    onSaved: (result) => emit("saved", result),
    onClose: () => emit("update:modelValue", false),
    recordLabel: "frequent number",
})
</script>
