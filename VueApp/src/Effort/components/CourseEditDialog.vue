<template>
    <q-dialog
        v-model="dialogOpen"
        persistent
        maximized-on-mobile
    >
        <q-card style="min-width: 400px; max-width: 500px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">{{ enrollmentOnly ? "Edit R-Course Enrollment" : "Edit Course" }}</div>
                <q-space />
                <q-btn
                    v-close-popup
                    icon="close"
                    flat
                    round
                    dense
                />
            </q-card-section>

            <q-card-section v-if="course">
                <div class="text-subtitle1 q-mb-md">
                    {{ course.courseCode }}-{{ course.seqNumb }}
                    <span class="text-grey-7">(CRN: {{ course.crn }})</span>
                </div>

                <q-form
                    ref="formRef"
                    class="q-gutter-md"
                >
                    <q-input
                        v-model.number="formData.enrollment"
                        type="number"
                        label="Enrollment"
                        dense
                        outlined
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
                            :rules="[(v: number) => v >= 0 || 'Units must be non-negative']"
                        />

                        <q-select
                            v-model="formData.custDept"
                            :options="departments"
                            label="Custodial Department"
                            dense
                            options-dense
                            outlined
                            :rules="[(v: string) => !!v || 'Department is required']"
                        />
                    </template>
                </q-form>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    v-close-popup
                    label="Cancel"
                    flat
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
import { ref, computed, watch } from "vue"
import { useQuasar, QForm } from "quasar"
import { effortService } from "../services/effort-service"
import type { CourseDto } from "../types"

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

const dialogOpen = computed({
    get: () => props.modelValue,
    set: (value) => emit("update:modelValue", value),
})

const formRef = ref<QForm | null>(null)
const isSaving = ref(false)
const formData = ref({
    enrollment: 0,
    units: 0,
    custDept: "",
})

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
        }
    },
)

async function save() {
    if (!props.course) return

    const valid = await formRef.value?.validate()
    if (!valid) return

    isSaving.value = true

    try {
        let result: { success: boolean; error?: string }

        if (props.enrollmentOnly) {
            // Use the enrollment-only endpoint for R-course enrollment updates
            result = await effortService.updateCourseEnrollment(props.course.id, formData.value.enrollment)
        } else {
            // Use the full update endpoint
            result = await effortService.updateCourse(props.course.id, {
                enrollment: formData.value.enrollment,
                units: formData.value.units,
                custDept: formData.value.custDept,
            })
        }

        if (result.success) {
            dialogOpen.value = false
            emit("updated")
        } else {
            $q.notify({ type: "negative", message: result.error ?? "Failed to update course" })
        }
    } finally {
        isSaving.value = false
    }
}
</script>
