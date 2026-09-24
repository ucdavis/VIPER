<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { inflect } from "inflection"
import RecordFormDialog from "@/components/RecordFormDialog.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { validateOptionLabel } from "../composables/use-career-option-manager"
import type { CareerOptionTypeConfig } from "../composables/use-career-option-manager"
import type { CareerOptionSaveResult, CareerSelectionOption } from "../types"

const props = defineProps<{
    modelValue: boolean
    titleId: string
    config: CareerOptionTypeConfig
    option: CareerSelectionOption | null // The option being edited, or null to add a new one.
    existingOptions: CareerSelectionOption[]
    saveOption: (id: number | null, label: string) => Promise<CareerOptionSaveResult>
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    saved: [label: string]
}>()

const form = ref({ label: "" })
const saving = ref(false)
const formError = ref("")
const { setInitialState, confirmClose, resetDirtyState } = useUnsavedChanges(form)

const isEdit = computed(() => props.option !== null)
const title = computed(() => `${isEdit.value ? "Edit" : "Add"} ${props.config.singular} Option`)

function labelRule(value: string | null): true | string {
    return (
        validateOptionLabel(value ?? "", {
            existing: props.existingOptions,
            editingId: props.option?.id ?? null,
            maxLength: props.config.maxLength,
        }) ?? true
    )
}

watch(
    () => props.modelValue,
    (isOpen) => {
        if (isOpen) {
            form.value = { label: props.option?.label ?? "" }
            formError.value = ""
            setInitialState()
        }
    },
)

async function submit(): Promise<void> {
    saving.value = true
    formError.value = ""
    const label = form.value.label.trim()
    const result = await props.saveOption(props.option?.id ?? null, label)
    saving.value = false

    if (!result.success) {
        formError.value = result.errors.join(" ") || "Unable to save the option. Please try again."
        return
    }

    resetDirtyState()
    emit("saved", label)
    emit("update:modelValue", false)
}
</script>

<template>
    <RecordFormDialog
        :model-value="modelValue"
        :title-id="titleId"
        :title="title"
        :is-edit="isEdit"
        :saving="saving"
        :form-error="formError"
        submit-label="Add Option"
        :confirm-close="confirmClose"
        @update:model-value="emit('update:modelValue', $event)"
        @submit="submit"
        @hide="formError = ''"
    >
        <StatusBanner
            v-if="isEdit && option && option.usageCount > 0"
            type="warning"
        >
            Selected by {{ option.usageCount }} {{ inflect("student", option.usageCount) }}. Renaming this option
            changes the answer shown for each of them, including in reports and exports.
        </StatusBanner>

        <q-input
            v-model="form.label"
            dense
            outlined
            label="Name"
            :maxlength="config.maxLength"
            counter
            :rules="[labelRule]"
        />
    </RecordFormDialog>
</template>
