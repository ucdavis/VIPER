<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 550px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Edit Percentage Assignment</div>
                <q-space />
                <q-btn
                    icon="close"
                    flat
                    round
                    dense
                    aria-label="Close dialog"
                    @click="handleClose"
                />
            </q-card-section>

            <q-card-section class="q-py-sm">
                <q-form
                    ref="formRef"
                    class="effort-form"
                    greedy
                >
                    <PercentAssignmentFormFields
                        v-model="form"
                        :grouped-type-options="groupedTypeOptions"
                        :modifier-options="modifierOptions"
                        :unit-options="unitOptions"
                        :month-options="monthOptions"
                        :year-options="yearOptions"
                        :end-month-rules="endMonthRules"
                        :end-year-rules="endYearRules"
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
                </q-form>
            </q-card-section>

            <DialogSubmitActions
                submit-label="Save"
                :is-saving="isSaving"
                @cancel="handleClose"
                @submit="savePercentage"
            />
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { QForm } from "quasar"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import StatusBanner from "@/components/StatusBanner.vue"
import DialogSubmitActions from "./DialogSubmitActions.vue"
import PercentAssignmentFormFields from "./PercentAssignmentFormFields.vue"
import { percentageService } from "../services/percentage-service"
import { usePercentageForm } from "../composables/use-percentage-form"
import "../effort-forms.css"
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

// Use shared form composable
const percentAssignTypesRef = computed(() => props.percentAssignTypes)
const unitsRef = computed(() => props.units)

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
} = usePercentageForm(percentAssignTypesRef, unitsRef)

const formRef = ref<QForm | null>(null)

// Unsaved changes tracking
const { setInitialState, confirmClose } = useUnsavedChanges(form)

// Handle close (X button, Cancel button, or Escape key) with unsaved changes check
async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

// Loading and error state
const isSaving = ref(false)
const errorMessage = ref("")
const warningMessage = ref("")
const pendingWarningConfirm = ref(false)
const savedResult = ref<PercentageDto | null>(null)

// Reset form when dialog opens with percentage data
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
        formRef.value?.resetValidation()
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

    formRef.value?.resetValidation()
    setInitialState()
}

async function savePercentage() {
    if (!props.percentage) return
    const valid = await formRef.value?.validate(true)
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
