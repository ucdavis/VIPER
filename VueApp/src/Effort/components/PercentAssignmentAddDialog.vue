<template>
    <EffortDialogShell
        ref="shell"
        :model-value="modelValue"
        title="Add Percentage Assignment"
        title-id="percent-assignment-add-title"
        submit-label="Add"
        :is-saving="isSaving"
        @close="handleClose"
        @submit="createPercentage"
    >
        <PercentAssignmentFormFields
            v-model="form"
            v-bind="fieldProps"
        />

        <!-- Warning Banner -->
        <StatusBanner
            v-if="warningMessage"
            type="warning"
        >
            {{ warningMessage }}
            <template #action>
                <q-btn
                    flat
                    dense
                    label="Proceed Anyway"
                    @click="saveWithWarning"
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
import type { PercentageDto, PercentAssignTypeDto, UnitDto, CreatePercentageRequest } from "../types"

const props = defineProps<{
    modelValue: boolean
    personId: number
    percentAssignTypes: PercentAssignTypeDto[]
    units: UnitDto[]
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    created: [percentage: PercentageDto]
}>()

const percentAssignTypesRef = computed(() => props.percentAssignTypes)
const unitsRef = computed(() => props.units)

const {
    form,
    fieldProps,
    formatToIsoDate,
    resetForm,
    setInitialState,
    confirmClose,
    isSaving,
    errorMessage,
    warningMessage,
    pendingWarningConfirm,
} = usePercentageDialog(percentAssignTypesRef, unitsRef)

const shell = ref<InstanceType<typeof EffortDialogShell> | null>(null)

// Handle close (X button, Cancel button, or Escape key) with unsaved changes check
async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

// Reset form when dialog opens
watch(
    () => props.modelValue,
    (isOpen) => {
        if (isOpen) {
            resetForm()
            errorMessage.value = ""
            warningMessage.value = ""
            pendingWarningConfirm.value = false
            shell.value?.resetValidation()
            setInitialState()
        }
    },
)

async function createPercentage() {
    const valid = await shell.value?.validate()
    if (!valid) return

    isSaving.value = true
    errorMessage.value = ""
    warningMessage.value = ""

    try {
        const startDate = formatToIsoDate(form.value.startMonth!, form.value.startYear!)
        const endDate =
            form.value.endMonth && form.value.endYear ? formatToIsoDate(form.value.endMonth, form.value.endYear) : null

        const request: CreatePercentageRequest = {
            personId: props.personId,
            percentAssignTypeId: form.value.percentAssignTypeId!,
            unitId: form.value.unitId!,
            modifier: form.value.modifier,
            comment: form.value.comment || null,
            percentageValue: form.value.percentageValue,
            startDate,
            endDate,
            compensated: form.value.compensated,
        }

        // Validate first to show warnings before creating
        if (!pendingWarningConfirm.value) {
            const validation = await percentageService.validatePercentage(request)
            if (!validation.isValid) {
                errorMessage.value = validation.errors.join("; ")
                isSaving.value = false
                return
            }
            if (validation.warnings && validation.warnings.length > 0) {
                warningMessage.value = validation.warnings.join("; ")
                pendingWarningConfirm.value = true
                isSaving.value = false
                return
            }
        }

        const result = await percentageService.createPercentage(request)

        emit("update:modelValue", false)
        emit("created", result.result)
    } catch (err) {
        errorMessage.value = err instanceof Error ? err.message : "Failed to create percentage assignment"
    } finally {
        isSaving.value = false
    }
}

function saveWithWarning() {
    pendingWarningConfirm.value = true
    warningMessage.value = ""
    createPercentage()
}
</script>
