<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @keydown.escape="handleClose"
    >
        <q-card class="dialog-card-sm">
            <q-card-section class="row items-center q-pb-none">
                <div>
                    <div class="text-h6">{{ isEditMode ? "Edit" : "Add" }} Evaluation Data</div>
                    <div class="text-subtitle2 text-grey-7">{{ instructorName }} &mdash; {{ courseName }}</div>
                </div>
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

            <q-separator />

            <q-card-section>
                <q-form
                    ref="formRef"
                    class="effort-form"
                    greedy
                >
                    <div class="text-subtitle2 q-mb-sm">Rating Distribution</div>

                    <q-input
                        v-model.number="form.count5"
                        label="Rating 5 (High) *"
                        type="number"
                        dense
                        outlined
                        :rules="countRules('Rating 5')"
                        lazy-rules="ondemand"
                    />

                    <q-input
                        v-model.number="form.count4"
                        label="Rating 4 *"
                        type="number"
                        dense
                        outlined
                        :rules="countRules('Rating 4')"
                        lazy-rules="ondemand"
                    />

                    <q-input
                        v-model.number="form.count3"
                        label="Rating 3 *"
                        type="number"
                        dense
                        outlined
                        :rules="countRules('Rating 3')"
                        lazy-rules="ondemand"
                    />

                    <q-input
                        v-model.number="form.count2"
                        label="Rating 2 *"
                        type="number"
                        dense
                        outlined
                        :rules="countRules('Rating 2')"
                        lazy-rules="ondemand"
                    />

                    <q-input
                        v-model.number="form.count1"
                        label="Rating 1 (Low) *"
                        type="number"
                        dense
                        outlined
                        :rules="countRules('Rating 1')"
                        lazy-rules="ondemand"
                    />

                    <q-separator class="q-my-sm" />
                    <div class="text-subtitle2 q-mb-sm">Summary Statistics</div>

                    <q-input
                        v-model.number="form.mean"
                        label="Mean *"
                        type="number"
                        step="0.01"
                        dense
                        outlined
                        :rules="meanRules"
                        lazy-rules="ondemand"
                    />

                    <q-input
                        v-model.number="form.standardDeviation"
                        label="Standard Deviation *"
                        type="number"
                        step="0.01"
                        dense
                        outlined
                        :rules="sdRules"
                        lazy-rules="ondemand"
                    />

                    <q-input
                        v-model.number="form.respondents"
                        label="Total Respondents (N) *"
                        type="number"
                        dense
                        outlined
                        :rules="respondentsRules"
                        lazy-rules="ondemand"
                    />

                    <!-- Error Message -->
                    <q-banner
                        v-if="errorMessage"
                        class="bg-negative text-white"
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

            <q-separator />

            <q-card-actions align="right">
                <q-btn
                    flat
                    label="Cancel"
                    @click="handleClose"
                />
                <q-btn
                    color="primary"
                    :label="isEditMode ? 'Save' : 'Add'"
                    :loading="isSaving"
                    @click="onSave"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { QForm } from "quasar"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { courseService } from "../services/course-service"
import type { CourseEvalEntryDto } from "../types"
import { requiredRule } from "../validation"
import "../effort-dialogs.css"
import "../effort-forms.css"

const props = defineProps<{
    modelValue: boolean
    courseId: number
    instructorName: string
    courseName: string
    crn: string
    mothraId: string
    existingData: CourseEvalEntryDto | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    created: []
    updated: []
}>()

const formRef = ref<QForm | null>(null)

const isEditMode = computed(() => props.existingData !== null && props.existingData.status === "AdHoc")

// Form state
const form = ref({
    count1: null as number | null,
    count2: null as number | null,
    count3: null as number | null,
    count4: null as number | null,
    count5: null as number | null,
    mean: null as number | null,
    standardDeviation: null as number | null,
    respondents: null as number | null,
})

// Unsaved changes tracking
const { setInitialState, confirmClose } = useUnsavedChanges(form)

// Saving and error state
const isSaving = ref(false)
const errorMessage = ref("")

// Validation rules
const countRules = (label: string) => [
    requiredRule(label),
    (v: number | null) =>
        v === null || (Number.isInteger(v) && v >= 0) || `${label} must be a non-negative whole number`,
]

const meanRules = [
    requiredRule("Mean"),
    (v: number | null) => v === null || (Number.isFinite(v) && v >= 1 && v <= 5) || "Mean must be between 1 and 5",
]

const sdRules = [
    requiredRule("Standard Deviation"),
    (v: number | null) =>
        v === null || (Number.isFinite(v) && v >= 0 && v <= 5) || "Standard Deviation must be between 0 and 5",
]

const respondentsRules = [
    requiredRule("Total Respondents"),
    (v: number | null) => v === null || (Number.isInteger(v) && v >= 1) || "Total Respondents must be at least 1",
]

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
        }
    },
)

function resetForm() {
    errorMessage.value = ""

    if (isEditMode.value && props.existingData) {
        form.value = {
            count1: props.existingData.count1,
            count2: props.existingData.count2,
            count3: props.existingData.count3,
            count4: props.existingData.count4,
            count5: props.existingData.count5,
            mean: props.existingData.mean,
            standardDeviation: props.existingData.standardDeviation,
            respondents: props.existingData.respondents,
        }
    } else {
        form.value = {
            count1: null,
            count2: null,
            count3: null,
            count4: null,
            count5: null,
            mean: null,
            standardDeviation: null,
            respondents: null,
        }
    }

    formRef.value?.resetValidation()
    setInitialState()
}

async function onSave() {
    const valid = await formRef.value?.validate(true)
    if (!valid) return

    isSaving.value = true
    errorMessage.value = ""

    try {
        if (isEditMode.value && props.existingData?.quantId) {
            const result = await courseService.updateEvaluation(props.courseId, props.existingData.quantId, {
                count1: form.value.count1!,
                count2: form.value.count2!,
                count3: form.value.count3!,
                count4: form.value.count4!,
                count5: form.value.count5!,
                mean: form.value.mean!,
                standardDeviation: form.value.standardDeviation!,
                respondents: form.value.respondents!,
            })

            if (result.success) {
                emit("update:modelValue", false)
                emit("updated")
            } else {
                errorMessage.value = result.error || "Failed to update evaluation"
            }
        } else {
            const result = await courseService.createEvaluation(props.courseId, {
                courseId: props.courseId,
                mothraId: props.mothraId,
                count1: form.value.count1!,
                count2: form.value.count2!,
                count3: form.value.count3!,
                count4: form.value.count4!,
                count5: form.value.count5!,
                mean: form.value.mean!,
                standardDeviation: form.value.standardDeviation!,
                respondents: form.value.respondents!,
            })

            if (result.success) {
                emit("update:modelValue", false)
                emit("created")
            } else {
                errorMessage.value = result.error || "Failed to create evaluation"
            }
        }
    } catch {
        errorMessage.value = "An unexpected error occurred"
    } finally {
        isSaving.value = false
    }
}
</script>
