<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        maximized-on-mobile
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 600px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Add Course Manually</div>
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

            <q-card-section>
                <q-form
                    ref="formRef"
                    class="effort-form"
                    greedy
                >
                    <p class="text-body2 text-grey q-mb-md">
                        Use this form to add a course that is not available in Banner. For Banner courses, use "Import
                        from Banner" instead.
                    </p>

                    <div class="row q-col-gutter-sm">
                        <div class="col-12 col-sm-5">
                            <q-input
                                v-model="formData.subjCode"
                                label="Subject Code *"
                                dense
                                outlined
                                maxlength="3"
                                :rules="[
                                    (v: string) => !!v?.trim() || 'Subject code is required',
                                    (v: string) => !v || v.trim().length <= 3 || 'Max 3 characters',
                                ]"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div class="col-12 col-sm-4">
                            <q-input
                                v-model="formData.crseNumb"
                                label="Course Number *"
                                dense
                                outlined
                                maxlength="5"
                                :rules="[(v: string) => !!v?.trim() || 'Course number is required']"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div class="col-12 col-sm-3">
                            <q-input
                                v-model="formData.seqNumb"
                                label="Section *"
                                dense
                                outlined
                                maxlength="3"
                                :rules="[(v: string) => !!v?.trim() || 'Section is required']"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div class="col-12">
                            <q-input
                                v-model="formData.crn"
                                label="CRN *"
                                dense
                                outlined
                                maxlength="5"
                                :rules="[
                                    (v: string) => !!v?.trim() || 'CRN is required',
                                    (v: string) => !v || /^\d{5}$/.test(v.trim()) || 'CRN must be 5 digits',
                                ]"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div class="col-6">
                            <q-input
                                v-model.number="formData.enrollment"
                                type="number"
                                label="Enrollment"
                                dense
                                outlined
                                :rules="[nonNegativeRule('Enrollment'), wholeNumberRule('Enrollment')]"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div class="col-6">
                            <q-input
                                v-model.number="formData.units"
                                type="number"
                                label="Units"
                                dense
                                outlined
                                step="0.5"
                                :rules="[nonNegativeRule('Units')]"
                                lazy-rules="ondemand"
                            />
                        </div>
                        <div class="col-12">
                            <q-select
                                v-model="formData.custDept"
                                :options="departments"
                                label="Custodial Department *"
                                dense
                                options-dense
                                outlined
                                :rules="[requiredRule('Department')]"
                                lazy-rules="ondemand"
                            />
                        </div>
                    </div>
                </q-form>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    label="Cancel"
                    flat
                    @click="handleClose"
                />
                <q-btn
                    label="Create Course"
                    color="primary"
                    :loading="isCreating"
                    @click="create"
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
import { requiredRule, nonNegativeRule, wholeNumberRule } from "../validation"
import "../effort-forms.css"

const props = defineProps<{
    modelValue: boolean
    termCode: number | null
    departments: string[]
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    created: []
}>()

const $q = useQuasar()

const isCreating = ref(false)
const formRef = ref<QForm | null>(null)
const formData = ref({
    subjCode: "",
    crseNumb: "",
    seqNumb: "",
    crn: "",
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

// Reset form when dialog opens
watch(
    () => props.modelValue,
    (open) => {
        if (open) {
            formData.value = {
                subjCode: "",
                crseNumb: "",
                seqNumb: "",
                crn: "",
                enrollment: 0,
                units: 0,
                custDept: props.departments[0] ?? "",
            }
            formRef.value?.resetValidation()
            setInitialState()
        }
    },
)

async function create() {
    if (!props.termCode) {
        $q.notify({ type: "negative", message: "No term selected" })
        return
    }

    const valid = await formRef.value?.validate(true)
    if (!valid) return

    isCreating.value = true

    try {
        const result = await courseService.createCourse({
            termCode: props.termCode,
            crn: formData.value.crn.trim(),
            subjCode: formData.value.subjCode.trim().toUpperCase(),
            crseNumb: formData.value.crseNumb.trim().toUpperCase(),
            seqNumb: formData.value.seqNumb.trim().toUpperCase(),
            enrollment: formData.value.enrollment,
            units: formData.value.units,
            custDept: formData.value.custDept,
        })

        if (result.success) {
            emit("update:modelValue", false)
            emit("created")
        } else {
            $q.notify({ type: "negative", message: result.error ?? "Failed to create course" })
        }
    } finally {
        isCreating.value = false
    }
}
</script>
