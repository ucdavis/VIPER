<template>
    <q-dialog
        v-model="dialogOpen"
        persistent
        maximized-on-mobile
    >
        <q-card style="min-width: 500px; max-width: 600px">
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

                <q-form ref="formRef">
                    <div class="row q-col-gutter-sm q-mb-sm">
                        <div class="col-5">
                            <q-input
                                v-model="formData.subjCode"
                                label="Subject Code"
                                dense
                                outlined
                                maxlength="3"
                                :rules="[
                                    (v: string) => !!v?.trim() || 'Required',
                                    (v: string) => v?.trim().length <= 3 || 'Max 3 characters',
                                ]"
                            />
                        </div>
                        <div class="col-4">
                            <q-input
                                v-model="formData.crseNumb"
                                label="Course Number"
                                dense
                                outlined
                                maxlength="5"
                                :rules="[(v: string) => !!v?.trim() || 'Required']"
                            />
                        </div>
                        <div class="col-3">
                            <q-input
                                v-model="formData.seqNumb"
                                label="Section"
                                dense
                                outlined
                                maxlength="3"
                                :rules="[(v: string) => !!v?.trim() || 'Required']"
                            />
                        </div>
                    </div>

                    <q-input
                        v-model="formData.crn"
                        label="CRN"
                        dense
                        outlined
                        maxlength="5"
                        class="q-mb-sm"
                        :rules="[
                            (v: string) => !!v?.trim() || 'CRN is required',
                            (v: string) => /^\d{5}$/.test(v?.trim()) || 'CRN must be 5 digits',
                        ]"
                    />

                    <div class="row q-col-gutter-sm q-mb-sm">
                        <div class="col-6">
                            <q-input
                                v-model.number="formData.enrollment"
                                type="number"
                                label="Enrollment"
                                dense
                                outlined
                                :rules="[
                                    (v: number) => v >= 0 || 'Must be non-negative',
                                    (v: number) => Number.isInteger(v) || 'Must be a whole number',
                                ]"
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
                                :rules="[(v: number) => v >= 0 || 'Must be non-negative']"
                            />
                        </div>
                    </div>

                    <q-select
                        v-model="formData.custDept"
                        :options="departments"
                        label="Custodial Department"
                        dense
                        options-dense
                        outlined
                        :rules="[(v: string) => !!v || 'Department is required']"
                    />
                </q-form>
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
import { ref, computed, watch } from "vue"
import { useQuasar, QForm } from "quasar"
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

const formRef = ref<QForm | null>(null)
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
    }
})

async function create() {
    if (!props.termCode) {
        $q.notify({ type: "negative", message: "No term selected" })
        return
    }

    const valid = await formRef.value?.validate()
    if (!valid) return

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
