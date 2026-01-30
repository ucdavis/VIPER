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

            <q-card-section class="q-py-sm percent-form">
                <!-- Type Selection (grouped by class) -->
                <q-select
                    v-model="form.percentAssignTypeId"
                    :options="groupedTypeOptions"
                    label="Type *"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    :error="!!errors.type"
                    :error-message="errors.type"
                >
                    <template #option="scope">
                        <q-item-label
                            v-if="scope.opt.isHeader"
                            header
                            class="text-weight-bold text-primary q-py-xs"
                        >
                            {{ scope.opt.label }}
                        </q-item-label>
                        <q-item
                            v-else
                            v-bind="scope.itemProps"
                        >
                            <q-item-section>
                                <q-item-label>{{ scope.opt.label }}</q-item-label>
                            </q-item-section>
                        </q-item>
                    </template>
                </q-select>

                <!-- Modifier Selection -->
                <q-select
                    v-model="form.modifier"
                    :options="modifierOptions"
                    label="Modifier"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    clearable
                />

                <!-- Unit Selection -->
                <q-select
                    v-model="form.unitId"
                    :options="unitOptions"
                    label="Unit *"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    clearable
                    :error="!!errors.unit"
                    :error-message="errors.unit"
                />

                <!-- Start Date (Month/Year) -->
                <div class="row q-col-gutter-sm">
                    <div class="col">
                        <q-select
                            v-model="form.startMonth"
                            :options="monthOptions"
                            label="Start Month *"
                            dense
                            options-dense
                            outlined
                            emit-value
                            map-options
                            :error="!!errors.startDate"
                        />
                    </div>
                    <div class="col">
                        <q-select
                            v-model="form.startYear"
                            :options="yearOptions"
                            label="Start Year *"
                            dense
                            options-dense
                            outlined
                            emit-value
                            map-options
                            :error="!!errors.startDate"
                            :error-message="errors.startDate"
                        />
                    </div>
                </div>

                <!-- End Date (Month/Year) - Optional -->
                <div class="row q-col-gutter-sm">
                    <div class="col">
                        <q-select
                            v-model="form.endMonth"
                            :options="monthOptions"
                            label="End Month"
                            dense
                            options-dense
                            outlined
                            emit-value
                            map-options
                            clearable
                            :error="!!errors.endDate"
                        />
                    </div>
                    <div class="col">
                        <q-select
                            v-model="form.endYear"
                            :options="yearOptions"
                            label="End Year"
                            dense
                            options-dense
                            outlined
                            emit-value
                            map-options
                            clearable
                            :error="!!errors.endDate"
                            :error-message="errors.endDate"
                        />
                    </div>
                </div>

                <!-- Percent Input -->
                <q-input
                    v-model.number="form.percentageValue"
                    label="Percent *"
                    type="number"
                    dense
                    outlined
                    min="0"
                    max="100"
                    step="0.1"
                    :error="!!errors.percent"
                    :error-message="errors.percent"
                />

                <!-- Comment Input -->
                <q-input
                    v-model="form.comment"
                    label="Comment"
                    type="textarea"
                    dense
                    outlined
                    maxlength="100"
                    counter
                    rows="2"
                />

                <!-- Compensated Checkbox -->
                <q-checkbox
                    v-model="form.compensated"
                    label="Compensated"
                    dense
                />

                <!-- Warning Banner -->
                <q-banner
                    v-if="warningMessage"
                    class="bg-warning q-mb-md"
                    rounded
                >
                    <template #avatar>
                        <q-icon
                            name="warning"
                            color="dark"
                        />
                    </template>
                    {{ warningMessage }}
                    <template #action>
                        <q-btn
                            flat
                            dense
                            label="Proceed Anyway"
                            @click="saveWithWarning"
                        />
                    </template>
                </q-banner>

                <!-- Error Banner -->
                <q-banner
                    v-if="errorMessage"
                    class="bg-negative text-white q-mb-md"
                    rounded
                >
                    <template #avatar>
                        <q-icon
                            name="error"
                            color="white"
                        />
                    </template>
                    {{ errorMessage }}
                </q-banner>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    flat
                    label="Cancel"
                    @click="handleClose"
                />
                <q-btn
                    color="primary"
                    label="Add"
                    :loading="isSaving"
                    :disable="!isFormValid"
                    @click="createPercentage"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { percentageService } from "../services/percentage-service"
import { usePercentageForm } from "../composables/use-percentage-form"
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
    errors,
    groupedTypeOptions,
    unitOptions,
    modifierOptions,
    monthOptions,
    yearOptions,
    isFormValid,
    validateForm,
    formatToIsoDate,
    resetForm,
} = usePercentageForm(percentAssignTypesRef, unitsRef)

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
            setInitialState()
        }
    },
)

async function createPercentage() {
    if (!validateForm()) return

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

<style scoped>
/* Reset Quasar field margins and apply consistent spacing */
.percent-form :deep(.q-field) {
    margin-bottom: 0;
    padding-bottom: 0;
}

.percent-form :deep(.q-field--outlined .q-field__control) {
    padding-bottom: 0;
}

.percent-form > *:not(:last-child) {
    margin-bottom: 1rem;
}
</style>
