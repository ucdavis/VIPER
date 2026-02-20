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
                        :rules="[requiredRule('Type')]"
                        lazy-rules="ondemand"
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
                        :rules="[requiredRule('Unit')]"
                        lazy-rules="ondemand"
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
                                :rules="[requiredRule('Start month')]"
                                lazy-rules="ondemand"
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
                                :rules="[requiredRule('Start year')]"
                                lazy-rules="ondemand"
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
                                :rules="endMonthRules"
                                lazy-rules="ondemand"
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
                                :rules="endYearRules"
                                lazy-rules="ondemand"
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
                        :rules="[percentRule]"
                        lazy-rules="ondemand"
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
                        autogrow
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
                </q-form>
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
                    @click="createPercentage"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { QForm } from "quasar"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { percentageService } from "../services/percentage-service"
import { usePercentageForm } from "../composables/use-percentage-form"
import { requiredRule, percentRule } from "../validation"
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
