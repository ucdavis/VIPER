<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 550px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Add Percentage Assignment</div>
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
                </q-form>
            </q-card-section>

            <DialogSubmitActions
                submit-label="Add"
                :is-saving="isSaving"
                @cancel="handleClose"
                @submit="createPercentage"
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
    resetForm,
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

// Reset form when dialog opens
watch(
    () => props.modelValue,
    (isOpen) => {
        if (isOpen) {
            resetForm()
            errorMessage.value = ""
            warningMessage.value = ""
            pendingWarningConfirm.value = false
            formRef.value?.resetValidation()
            setInitialState()
        }
    },
)

async function createPercentage() {
    const valid = await formRef.value?.validate(true)
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
