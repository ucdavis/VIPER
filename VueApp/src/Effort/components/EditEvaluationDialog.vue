<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @keydown.escape="handleClose"
    >
        <q-card class="dialog-card-md">
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
                    <div class="text-subtitle2">Overall Teaching Effectiveness</div>
                    <div class="text-caption text-grey-7 q-mb-sm">Enter number of responses for each rating level</div>

                    <div class="eval-grid">
                        <!-- Column headers -->
                        <div />
                        <div class="eval-header">5 = High</div>
                        <div class="eval-header">4</div>
                        <div class="eval-header">3</div>
                        <div class="eval-header">2</div>
                        <div class="eval-header">1 = Low</div>

                        <!-- Input row -->
                        <div class="eval-row-label">Number of<br />Responses</div>
                        <div>
                            <q-input
                                v-model.number="form.count5"
                                type="number"
                                dense
                                outlined
                                no-error-icon
                                aria-label="Rating 5 (High) response count"
                                :rules="countRules"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div>
                            <q-input
                                v-model.number="form.count4"
                                type="number"
                                dense
                                outlined
                                no-error-icon
                                aria-label="Rating 4 response count"
                                :rules="countRules"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div>
                            <q-input
                                v-model.number="form.count3"
                                type="number"
                                dense
                                outlined
                                no-error-icon
                                aria-label="Rating 3 response count"
                                :rules="countRules"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div>
                            <q-input
                                v-model.number="form.count2"
                                type="number"
                                dense
                                outlined
                                no-error-icon
                                aria-label="Rating 2 response count"
                                :rules="countRules"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div>
                            <q-input
                                v-model.number="form.count1"
                                type="number"
                                dense
                                outlined
                                no-error-icon
                                aria-label="Rating 1 (Low) response count"
                                :rules="countRules"
                                lazy-rules="ondemand"
                            />
                        </div>
                    </div>

                    <q-separator class="q-my-sm" />
                    <div class="text-subtitle2 q-mb-xs">Computed Statistics</div>
                    <div
                        v-if="totalRespondents > 0"
                        class="text-body2 text-grey-8"
                        aria-live="polite"
                    >
                        Mean: <strong>{{ computedMean }}</strong> &nbsp;&middot;&nbsp; SD:
                        <strong>{{ computedSd }}</strong> &nbsp;&middot;&nbsp; N:
                        <strong>{{ totalRespondents }}</strong>
                    </div>
                    <div
                        v-else
                        class="text-body2 text-grey-5"
                    >
                        Enter rating counts to see computed statistics
                    </div>

                    <!-- Error Message -->
                    <q-banner
                        v-if="errorMessage"
                        class="bg-negative text-white q-mt-sm"
                        rounded
                        role="alert"
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

// Form state — only rating counts; mean, SD, respondents are auto-computed
const form = ref({
    count1: null as number | null,
    count2: null as number | null,
    count3: null as number | null,
    count4: null as number | null,
    count5: null as number | null,
})

// Unsaved changes tracking
const { setInitialState, confirmClose } = useUnsavedChanges(form)

// Saving and error state
const isSaving = ref(false)
const errorMessage = ref("")

// Validation rules
const countRules = [
    (v: number | null) => (v !== null && v !== undefined) || "Required",
    (v: number | null) => v === null || (Number.isInteger(v) && v >= 0) || "Must be 0 or more",
]

// Computed summary statistics derived from rating counts
const totalRespondents = computed(() => {
    const { count1, count2, count3, count4, count5 } = form.value
    const c1 = count1 ?? 0
    const c2 = count2 ?? 0
    const c3 = count3 ?? 0
    const c4 = count4 ?? 0
    const c5 = count5 ?? 0
    return c1 + c2 + c3 + c4 + c5
})

const computedMean = computed(() => {
    const n = totalRespondents.value
    if (n === 0) return "—"
    const { count1, count2, count3, count4, count5 } = form.value
    const mean = (1 * (count1 ?? 0) + 2 * (count2 ?? 0) + 3 * (count3 ?? 0) + 4 * (count4 ?? 0) + 5 * (count5 ?? 0)) / n
    return mean.toFixed(2)
})

const computedSd = computed(() => {
    const n = totalRespondents.value
    if (n === 0) return "—"
    const { count1, count2, count3, count4, count5 } = form.value
    const c1 = count1 ?? 0
    const c2 = count2 ?? 0
    const c3 = count3 ?? 0
    const c4 = count4 ?? 0
    const c5 = count5 ?? 0
    const mean = (1 * c1 + 2 * c2 + 3 * c3 + 4 * c4 + 5 * c5) / n
    const variance =
        ((1 - mean) ** 2 * c1 +
            (2 - mean) ** 2 * c2 +
            (3 - mean) ** 2 * c3 +
            (4 - mean) ** 2 * c4 +
            (5 - mean) ** 2 * c5) /
        n
    return Math.sqrt(variance).toFixed(2)
})

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
        }
    } else {
        form.value = {
            count1: null,
            count2: null,
            count3: null,
            count4: null,
            count5: null,
        }
    }

    formRef.value?.resetValidation()
    setInitialState()
}

async function onSave() {
    const valid = await formRef.value?.validate(true)
    if (!valid) {
        errorMessage.value = "All rating fields are required and must be non-negative whole numbers"
        return
    }

    if (totalRespondents.value === 0) {
        errorMessage.value = "At least one rating count must be greater than zero"
        return
    }

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

<style scoped>
.eval-grid {
    display: grid;
    grid-template-columns: auto repeat(5, 1fr);
    gap: 0 0.5rem;
    align-items: center;
}

.eval-header {
    font-weight: 500;
    font-size: 0.875rem;
    text-align: center;
    padding-bottom: 0.25rem;
}

.eval-row-label {
    font-weight: 500;
    font-size: 0.875rem;
    line-height: 1.3;
    padding-right: 0.5rem;
}

/* Hide per-field error text; red borders remain as visual indicators.
   A consolidated message is shown below the grid instead. */
.eval-grid :deep(.q-field__bottom) {
    display: none;
}
</style>
