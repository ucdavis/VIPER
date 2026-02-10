<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        maximized-on-mobile
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 500px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">{{ enrollmentOnly ? "Edit R-Course Enrollment" : "Edit Course" }}</div>
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

            <q-card-section v-if="course">
                <div class="text-subtitle1 q-mb-md">
                    {{ course.courseCode }}-{{ course.seqNumb }}
                    <span class="text-grey-7">(CRN: {{ course.crn }})</span>
                </div>

                <q-form
                    ref="formRef"
                    class="effort-form"
                    greedy
                >
                    <q-input
                        v-model.number="formData.enrollment"
                        type="number"
                        label="Enrollment"
                        dense
                        outlined
                        lazy-rules="ondemand"
                        :rules="[
                            (v: number) => v >= 0 || 'Enrollment must be non-negative',
                            (v: number) => Number.isInteger(v) || 'Enrollment must be a whole number',
                        ]"
                    />

                    <template v-if="!enrollmentOnly">
                        <q-input
                            v-model.number="formData.units"
                            type="number"
                            label="Units"
                            dense
                            outlined
                            step="0.5"
                            lazy-rules="ondemand"
                            :rules="[(v: number) => v >= 0 || 'Units must be non-negative']"
                        />

                        <q-select
                            v-model="formData.custDept"
                            :options="departments"
                            label="Custodial Department *"
                            dense
                            options-dense
                            outlined
                            lazy-rules="ondemand"
                            :rules="[(v: string) => !!v || 'Department is required']"
                        />
                    </template>
                </q-form>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    label="Cancel"
                    flat
                    @click="handleClose"
                />
                <q-btn
                    label="Save"
                    color="primary"
                    :loading="isSaving"
                    @click="save"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import { useQuasar, QForm } from "quasar"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { courseService } from "../services/course-service"
import type { CourseDto } from "../types"
import "../effort-forms.css"

const props = defineProps<{
    modelValue: boolean
    course: CourseDto | null
    departments: string[]
    enrollmentOnly?: boolean
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    updated: []
}>()

const $q = useQuasar()

const formRef = ref<QForm | null>(null)
const isSaving = ref(false)
const formData = ref({
    enrollment: 0,
    units: 0,
    custDept: "",
})

// Unsaved changes tracking
const { setInitialState, confirmClose } = useUnsavedChanges(formData)

// Handle close (X button, Cancel button, or Escape key) with unsaved changes check
async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

// Reset form when dialog opens with course data
watch(
    () => props.modelValue,
    (open) => {
        if (open && props.course) {
            formData.value = {
                enrollment: props.course.enrollment,
                units: props.course.units,
                custDept: props.course.custDept,
            }
            setInitialState()
        }
    },
)

async function save() {
    if (!props.course) return

    // Validate all fields at once (not just until first error)
    const valid = await formRef.value?.validate(true)
    if (!valid) return

    isSaving.value = true

    try {
        let result: { success: boolean; error?: string }

        if (props.enrollmentOnly) {
            // Use the enrollment-only endpoint for R-course enrollment updates
            result = await courseService.updateCourseEnrollment(props.course.id, formData.value.enrollment)
        } else {
            // Use the full update endpoint
            result = await courseService.updateCourse(props.course.id, {
                enrollment: formData.value.enrollment,
                units: formData.value.units,
                custDept: formData.value.custDept,
            })
        }

        if (result.success) {
            emit("update:modelValue", false)
            emit("updated")
        } else {
            $q.notify({ type: "negative", message: result.error ?? "Failed to update course" })
        }
    } finally {
        isSaving.value = false
    }
}
</script>
