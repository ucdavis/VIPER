<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 500px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Edit Effort Record</div>
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
                    <!-- Verification Warning -->
                    <q-banner
                        v-if="props.isVerified"
                        class="bg-orange-2"
                        rounded
                    >
                        <template #avatar>
                            <q-icon
                                name="info"
                                color="orange-9"
                            />
                        </template>
                        <span class="text-orange-9">
                            This instructor's effort has been verified. Editing this record will clear the verification
                            status and require re-verification.
                        </span>
                    </q-banner>

                    <!-- Course (read-only) -->
                    <q-input
                        :model-value="courseLabel"
                        label="Course"
                        dense
                        outlined
                        readonly
                        bottom-slots
                    />

                    <!-- Effort Type Selection -->
                    <q-select
                        v-model="selectedEffortType"
                        :options="filteredEffortTypes"
                        label="Effort Type"
                        dense
                        options-dense
                        outlined
                        option-value="id"
                        :option-label="(opt: EffortTypeOptionDto) => `${opt.description} (${opt.id})`"
                        emit-value
                        map-options
                        :loading="isLoadingOptions"
                        :rules="[requiredRule('Effort type')]"
                        lazy-rules="ondemand"
                    />

                    <!-- Role Selection -->
                    <q-select
                        v-model="selectedRole"
                        :options="roles"
                        label="Role"
                        dense
                        options-dense
                        outlined
                        option-value="id"
                        option-label="description"
                        emit-value
                        map-options
                        :loading="isLoadingOptions"
                        :rules="[requiredRule('Role')]"
                        lazy-rules="ondemand"
                    />

                    <!-- Effort Value -->
                    <q-input
                        v-model.number="effortValue"
                        :label="effortLabel"
                        type="number"
                        dense
                        outlined
                        min="0"
                        :rules="effortValueRules"
                        lazy-rules="ondemand"
                    />

                    <!-- Notes (generic R-Course only) -->
                    <q-input
                        v-if="props.record?.course.isGenericRCourse"
                        v-model="notes"
                        label="Notes"
                        type="textarea"
                        dense
                        outlined
                        maxlength="500"
                        counter
                        autogrow
                        :hint="notesHint"
                    />

                    <!-- Warning Message -->
                    <q-banner
                        v-if="warningMessage"
                        class="bg-warning"
                        rounded
                    >
                        <template #avatar>
                            <q-icon
                                name="warning"
                                color="dark"
                            />
                        </template>
                        {{ warningMessage }}
                    </q-banner>

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

            <q-card-actions align="right">
                <q-btn
                    flat
                    label="Cancel"
                    @click="handleClose"
                />
                <q-btn
                    color="primary"
                    label="Save"
                    :loading="isSaving"
                    @click="updateRecord"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { QForm } from "quasar"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { recordService } from "../services/record-service"
import type { EffortTypeOptionDto, RoleOptionDto, InstructorEffortRecordDto } from "../types"
import { effortValueRules, requiredRule, notesMaxHint } from "../validation"
import "../effort-forms.css"

const props = defineProps<{
    modelValue: boolean
    record: InstructorEffortRecordDto | null
    termCode: number
    isVerified?: boolean
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    updated: []
}>()

const formRef = ref<QForm | null>(null)

// Form state
const selectedEffortType = ref<string | null>(null)
const selectedRole = ref<number | null>(null)
const effortValue = ref<number | null>(null)
const notes = ref<string | null>(null)
const notesHint = computed(() => notesMaxHint(notes.value))

// Form data for unsaved changes tracking
const formData = ref({
    selectedEffortType: null as string | null,
    selectedRole: null as number | null,
    effortValue: null as number | null,
    notes: null as string | null,
})

// Unsaved changes tracking
const { setInitialState, confirmClose } = useUnsavedChanges(formData)

// Handle close (X button, Cancel button, or Escape key) with unsaved changes check
async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

// Sync individual refs to formData for unsaved changes tracking
function syncFormData() {
    formData.value = {
        selectedEffortType: selectedEffortType.value,
        selectedRole: selectedRole.value,
        effortValue: effortValue.value,
        notes: notes.value,
    }
}

// Keep formData in sync when individual refs change
watch([selectedEffortType, selectedRole, effortValue, notes], syncFormData)

// Options data
const effortTypes = ref<EffortTypeOptionDto[]>([])
const roles = ref<RoleOptionDto[]>([])

// Loading and error state
const isLoadingOptions = ref(false)
const isSaving = ref(false)
const errorMessage = ref("")
const warningMessage = ref("")

// Computed: Course label (read-only display)
const courseLabel = computed(() => {
    if (!props.record) return ""
    const course = props.record.course
    return `${course.subjCode} ${course.crseNumb.trim()}-${course.seqNumb} (${course.units} units)`
})

// Use course classification flags directly from CourseDto (populated by backend CourseClassificationService)
const courseFlags = computed(() => {
    if (!props.record) return { isDvm: false, is199299: false, isRCourse: false }
    const course = props.record.course
    return {
        isDvm: course.isDvm ?? false,
        is199299: course.is199299 ?? false,
        isRCourse: course.isRCourse ?? false,
    }
})

// Computed: Filter effort types based on course category
const filteredEffortTypes = computed(() => {
    const flags = courseFlags.value
    return effortTypes.value.filter((et) => {
        if (flags.isDvm && !et.allowedOnDvm) return false
        if (flags.is199299 && !et.allowedOn199299) return false
        if (flags.isRCourse && !et.allowedOnRCourses) return false
        return true
    })
})

// Computed: Effort label (Hours vs Weeks)
const effortLabel = computed(() => {
    if (!selectedEffortType.value) return "Hours"
    const effortType = effortTypes.value.find((et) => et.id === selectedEffortType.value)
    if (effortType?.usesWeeks && props.termCode >= 201604) {
        return "Weeks"
    }
    return "Hours"
})

// Reset form when dialog opens with record data
watch(
    () => props.modelValue,
    async (isOpen) => {
        if (isOpen && props.record) {
            errorMessage.value = ""
            warningMessage.value = ""
            await loadOptions()
            populateForm()
        }
    },
)

function populateForm() {
    if (!props.record) return

    selectedEffortType.value = props.record.effortType
    selectedRole.value = props.record.role
    effortValue.value = props.record.effortValue ?? 0
    notes.value = props.record.notes
    syncFormData()
    setInitialState()
}

async function loadOptions() {
    isLoadingOptions.value = true

    try {
        const [effortTypesResult, rolesResult] = await Promise.all([
            recordService.getEffortTypeOptions(),
            recordService.getRoleOptions(),
        ])

        effortTypes.value = effortTypesResult
        roles.value = rolesResult
    } catch {
        errorMessage.value = "Failed to load options"
    } finally {
        isLoadingOptions.value = false
    }
}

async function updateRecord() {
    if (!props.record) return
    const valid = await formRef.value?.validate(true)
    if (!valid) return

    isSaving.value = true
    errorMessage.value = ""
    warningMessage.value = ""

    try {
        const result = await recordService.updateEffortRecord(props.record.id, {
            effortTypeId: selectedEffortType.value!,
            roleId: selectedRole.value!,
            effortValue: effortValue.value!,
            notes: notes.value?.trim() || null,
            originalModifiedDate: props.record.modifiedDate,
        })

        if (result.success) {
            if (result.result?.warning) {
                warningMessage.value = result.result.warning
                // Show warning briefly before closing
                setTimeout(() => {
                    emit("update:modelValue", false)
                    emit("updated")
                }, 2000)
            } else {
                emit("update:modelValue", false)
                emit("updated")
            }
        } else {
            errorMessage.value = result.error || "Failed to update effort record"
            // Close dialog on concurrency conflict so user sees refreshed data
            if (result.isConflict) {
                setTimeout(() => {
                    emit("update:modelValue", false)
                    emit("updated") // Triggers reload
                }, 2000)
            }
        }
    } catch {
        errorMessage.value = "An unexpected error occurred"
    } finally {
        isSaving.value = false
    }
}
</script>
