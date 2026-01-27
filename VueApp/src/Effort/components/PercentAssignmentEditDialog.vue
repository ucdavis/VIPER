<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @update:model-value="emit('update:modelValue', $event)"
    >
        <q-card style="width: 100%; max-width: 550px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Edit Percentage Assignment</div>
                <q-space />
                <q-btn
                    v-close-popup
                    icon="close"
                    flat
                    round
                    dense
                    aria-label="Close dialog"
                />
            </q-card-section>

            <q-card-section>
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
                    class="q-mb-sm"
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
                    class="q-mb-sm"
                />

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
                    class="q-mb-sm"
                />

                <!-- Start Date (Month/Year) -->
                <div class="row q-col-gutter-sm q-mb-sm">
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
                <div class="row q-col-gutter-sm q-mb-sm">
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
                    class="q-mb-sm"
                />

                <!-- Comment Input -->
                <q-input
                    v-model="form.comment"
                    label="Comment"
                    type="textarea"
                    dense
                    outlined
                    maxlength="500"
                    counter
                    rows="2"
                    class="q-mb-sm"
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
                    v-close-popup
                    flat
                    label="Cancel"
                />
                <q-btn
                    color="primary"
                    label="Save"
                    :loading="isSaving"
                    :disable="!isFormValid"
                    @click="savePercentage"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { percentageService } from "../services/percentage-service"
import { usePercentageForm } from "../composables/use-percentage-form"
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
    errors,
    groupedTypeOptions,
    unitOptions,
    modifierOptions,
    monthOptions,
    yearOptions,
    isFormValid,
    validateForm,
    formatToIsoDate,
    parseDateToMonthYear,
    resetErrors,
} = usePercentageForm(percentAssignTypesRef, unitsRef)

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

    resetErrors()
}

async function savePercentage() {
    if (!validateForm() || !props.percentage) return

    isSaving.value = true
    errorMessage.value = ""
    warningMessage.value = ""

    try {
        const startDate = formatToIsoDate(form.value.startMonth!, form.value.startYear!)
        const endDate =
            form.value.endMonth && form.value.endYear ? formatToIsoDate(form.value.endMonth, form.value.endYear) : null

        const request: UpdatePercentageRequest = {
            percentAssignTypeId: form.value.percentAssignTypeId!,
            unitId: form.value.unitId,
            modifier: form.value.modifier,
            comment: form.value.comment || null,
            percentageValue: form.value.percentageValue,
            startDate,
            endDate,
            compensated: form.value.compensated,
        }

        const result = await percentageService.updatePercentage(props.percentage.id, request)

        // Warnings returned after update are informational - record is already saved
        if (result.warnings && result.warnings.length > 0 && !pendingWarningConfirm.value) {
            warningMessage.value = result.warnings.join("; ")
            pendingWarningConfirm.value = true
            savedResult.value = result.result
            isSaving.value = false
            return
        }

        emit("update:modelValue", false)
        emit("saved", result.result)
    } catch (err) {
        errorMessage.value = err instanceof Error ? err.message : "Failed to update percentage assignment"
    } finally {
        isSaving.value = false
    }
}

function saveWithWarning() {
    // Record was already saved, just close and emit the saved result
    if (savedResult.value) {
        emit("update:modelValue", false)
        emit("saved", savedResult.value)
    }
}
</script>
