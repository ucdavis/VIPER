<template>
    <q-dialog
        v-model="dialogOpen"
        persistent
        maximized-on-mobile
    >
        <q-card style="width: 100%; max-width: 600px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Add Course Manually</div>
                <q-space />
                <q-btn
                    v-close-popup
                    icon="close"
                    flat
                    round
                    dense
                />
            </q-card-section>

            <q-card-section>
                <p class="text-body2 text-grey q-mb-md">
                    Use this form to add a course that is not available in Banner. For Banner courses, use "Import from
                    Banner" instead.
                </p>

                <div class="row q-col-gutter-sm">
                    <div class="col-12 col-sm-5">
                        <q-input
                            v-model="formData.subjCode"
                            label="Subject Code *"
                            dense
                            outlined
                            maxlength="3"
                            :error="!!errors.subjCode"
                            :error-message="errors.subjCode"
                        />
                    </div>
                    <div class="col-12 col-sm-4">
                        <q-input
                            v-model="formData.crseNumb"
                            label="Course Number *"
                            dense
                            outlined
                            maxlength="5"
                            :error="!!errors.crseNumb"
                            :error-message="errors.crseNumb"
                        />
                    </div>
                    <div class="col-12 col-sm-3">
                        <q-input
                            v-model="formData.seqNumb"
                            label="Section *"
                            dense
                            outlined
                            maxlength="3"
                            :error="!!errors.seqNumb"
                            :error-message="errors.seqNumb"
                        />
                    </div>
                    <div class="col-12">
                        <q-input
                            v-model="formData.crn"
                            label="CRN *"
                            dense
                            outlined
                            maxlength="5"
                            :error="!!errors.crn"
                            :error-message="errors.crn"
                        />
                    </div>
                    <div class="col-6">
                        <q-input
                            v-model.number="formData.enrollment"
                            type="number"
                            label="Enrollment"
                            dense
                            outlined
                            :error="!!errors.enrollment"
                            :error-message="errors.enrollment"
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
                            :error="!!errors.units"
                            :error-message="errors.units"
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
                            :error="!!errors.custDept"
                            :error-message="errors.custDept"
                        />
                    </div>
                </div>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    v-close-popup
                    label="Cancel"
                    flat
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
import { ref, reactive, computed, watch } from "vue"
import { useQuasar } from "quasar"
import { effortService } from "../services/effort-service"

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

const dialogOpen = computed({
    get: () => props.modelValue,
    set: (value) => emit("update:modelValue", value),
})

const isCreating = ref(false)
const formData = ref({
    subjCode: "",
    crseNumb: "",
    seqNumb: "",
    crn: "",
    enrollment: 0,
    units: 0,
    custDept: "",
})

const errors = reactive({
    subjCode: "",
    crseNumb: "",
    seqNumb: "",
    crn: "",
    enrollment: "",
    units: "",
    custDept: "",
})

function clearErrors() {
    errors.subjCode = ""
    errors.crseNumb = ""
    errors.seqNumb = ""
    errors.crn = ""
    errors.enrollment = ""
    errors.units = ""
    errors.custDept = ""
}

// Reset form when dialog opens
watch(dialogOpen, (open) => {
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
        clearErrors()
    }
})

function validate(): boolean {
    clearErrors()
    let isValid = true

    // Subject Code
    if (!formData.value.subjCode.trim()) {
        errors.subjCode = "Required"
        isValid = false
    } else if (formData.value.subjCode.trim().length > 3) {
        errors.subjCode = "Max 3 characters"
        isValid = false
    }

    // Course Number
    if (!formData.value.crseNumb.trim()) {
        errors.crseNumb = "Required"
        isValid = false
    }

    // Section
    if (!formData.value.seqNumb.trim()) {
        errors.seqNumb = "Required"
        isValid = false
    }

    // CRN
    if (!formData.value.crn.trim()) {
        errors.crn = "CRN is required"
        isValid = false
    } else if (!/^\d{5}$/.test(formData.value.crn.trim())) {
        errors.crn = "CRN must be 5 digits"
        isValid = false
    }

    // Enrollment
    if (formData.value.enrollment < 0) {
        errors.enrollment = "Must be non-negative"
        isValid = false
    } else if (!Number.isInteger(formData.value.enrollment)) {
        errors.enrollment = "Must be a whole number"
        isValid = false
    }

    // Units
    if (formData.value.units < 0) {
        errors.units = "Must be non-negative"
        isValid = false
    }

    // Custodial Department
    if (!formData.value.custDept) {
        errors.custDept = "Department is required"
        isValid = false
    }

    return isValid
}

async function create() {
    if (!props.termCode) {
        $q.notify({ type: "negative", message: "No term selected" })
        return
    }

    if (!validate()) return

    isCreating.value = true

    try {
        const result = await effortService.createCourse({
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
            dialogOpen.value = false
            emit("created")
        } else {
            $q.notify({ type: "negative", message: result.error ?? "Failed to create course" })
        }
    } finally {
        isCreating.value = false
    }
}
</script>
