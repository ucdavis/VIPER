<template>
    <EffortDialogShell
        ref="shell"
        :model-value="modelValue"
        title="Edit Percentage Assignment"
        title-id="percent-assignment-edit-title"
        submit-label="Save"
        :is-saving="isSaving"
        @close="handleClose"
        @submit="savePercentage"
    >
        <PercentAssignmentFormFields
            v-model="form"
            v-bind="fieldProps"
        />

        <!-- Post-save Warning Banner -->
        <StatusBanner
            v-if="warningMessage"
            type="warning"
        >
            <div><strong>Saved with notice:</strong> {{ warningMessage }}</div>
            <template #action>
                <q-btn
                    flat
                    dense
                    label="OK"
                    @click="acknowledgeWarning"
                />
            </template>
        </StatusBanner>

        <!-- Error Banner -->
        <StatusBanner
            v-if="errorMessage"
            type="error"
        >
            {{ errorMessage }}
        </StatusBanner>
    </EffortDialogShell>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import EffortDialogShell from "./EffortDialogShell.vue"
import PercentAssignmentFormFields from "./PercentAssignmentFormFields.vue"
import { percentageService } from "../services/percentage-service"
import { usePercentageDialog } from "../composables/use-percentage-dialog"
import type { PercentageDto, PercentAssignTypeDto, UnitDto, UpdatePercentageRequest } from "../types"

const props = defineProps<{
    modelValue: boolean
    percentage: PercentageDto | null
    percentAssignTypes: PercentAssignTypeDto[]
    units: UnitDto[]
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    saved: [percentage: PercentageDto]
}>()

const percentAssignTypesRef = computed(() => props.percentAssignTypes)
const unitsRef = computed(() => props.units)

const {
    form,
    fieldProps,
    formatToIsoDate,
    parseDateToMonthYear,
    setInitialState,
    confirmClose,
    isSaving,
    errorMessage,
    warningMessage,
    pendingWarningConfirm,
} = usePercentageDialog(percentAssignTypesRef, unitsRef)

const shell = ref<InstanceType<typeof EffortDialogShell> | null>(null)
const savedResult = ref<PercentageDto | null>(null)

// Handle close (X button, Cancel button, or Escape key) with unsaved changes check
async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

// Populate form when dialog opens with percentage data
watch(
    () => props.modelValue,
    (isOpen) => {
        if (isOpen && props.percentage) {
            populateForm()
            errorMessage.value = ""
            warningMessage.value = ""
            pendingWarningConfirm.value = false
            savedResult.value = null
        }
        shell.value?.resetValidation()
    },
)

function populateForm() {
    if (!props.percentage) return

    const start = parseDateToMonthYear(props.percentage.startDate)
    const end = parseDateToMonthYear(props.percentage.endDate)

    form.value = {
        percentAssignTypeId: props.percentage.percentAssignTypeId,
        unitId: props.percentage.unitId,
        modifier: props.percentage.modifier,
        startMonth: start.month,
        startYear: start.year,
        endMonth: end.month,
        endYear: end.year,
        percentageValue: props.percentage.percentageValue,
        comment: props.percentage.comment ?? "",
        compensated: props.percentage.compensated,
    }

    shell.value?.resetValidation()
    setInitialState()
}

async function savePercentage() {
    if (!props.percentage) return
    const valid = await shell.value?.validate()
    if (!valid) return

    isSaving.value = true
    errorMessage.value = ""
    warningMessage.value = ""

    const startDate = formatToIsoDate(form.value.startMonth!, form.value.startYear!)
    const endDate =
        form.value.endMonth && form.value.endYear ? formatToIsoDate(form.value.endMonth, form.value.endYear) : null

    const request: UpdatePercentageRequest = {
        percentAssignTypeId: form.value.percentAssignTypeId!,
        unitId: form.value.unitId!,
        modifier: form.value.modifier,
        comment: form.value.comment || null,
        percentageValue: form.value.percentageValue,
        startDate,
        endDate,
        compensated: form.value.compensated,
    }

    const result = await percentageService.updatePercentage(props.percentage.id, request)
    isSaving.value = false

    if (!result.success) {
        errorMessage.value = result.error ?? "Failed to update percentage assignment"
        return
    }

    // Warnings returned after update are informational - record is already saved
    if (result.warnings && result.warnings.length > 0 && !pendingWarningConfirm.value) {
        warningMessage.value = result.warnings.join("; ")
        pendingWarningConfirm.value = true
        savedResult.value = result.result ?? null
        return
    }

    emit("update:modelValue", false)
    emit("saved", result.result!)
}

function acknowledgeWarning() {
    // Record was already saved, just close and emit the saved result
    if (savedResult.value) {
        emit("update:modelValue", false)
        emit("saved", savedResult.value)
    }
}
</script>
