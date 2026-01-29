<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @update:model-value="emit('update:modelValue', $event)"
    >
        <q-card style="width: 100%; max-width: 500px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Edit Effort Record</div>
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
                <!-- Verification Warning -->
                <q-banner
                    v-if="props.isVerified"
                    class="bg-orange-2 q-mb-md"
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
                    class="q-mb-md"
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
                    option-label="description"
                    emit-value
                    map-options
                    :loading="isLoadingOptions"
                    class="q-mb-md"
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
                    class="q-mb-md"
                />

                <!-- Effort Value -->
                <q-input
                    v-model.number="effortValue"
                    :label="effortLabel"
                    type="number"
                    dense
                    outlined
                    min="0"
                    class="q-mb-md"
                />

                <!-- Warning Message -->
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
                </q-banner>

                <!-- Error Message -->
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
                    @click="updateRecord"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { effortService } from "../services/effort-service"
import type { EffortTypeOptionDto, RoleOptionDto, InstructorEffortRecordDto } from "../types"

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

// Form state
const selectedEffortType = ref<string | null>(null)
const selectedRole = ref<number | null>(null)
const effortValue = ref<number | null>(null)

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

// Computed: Course category flags for filtering effort types
const courseFlags = computed(() => {
    if (!props.record) return { isDvm: false, is199299: false, isRCourse: false }
    const course = props.record.course
    const crseNumb = course.crseNumb.trim()
    return {
        isDvm: course.custDept?.toUpperCase() === "DVM",
        is199299: crseNumb.startsWith("199") || crseNumb.startsWith("299"),
        isRCourse: crseNumb.toUpperCase().endsWith("R"),
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

// Computed: Form validation
const isFormValid = computed(() => {
    return (
        selectedEffortType.value !== null &&
        selectedRole.value !== null &&
        effortValue.value !== null &&
        effortValue.value >= 0
    )
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
}

async function loadOptions() {
    isLoadingOptions.value = true

    try {
        const [effortTypesResult, rolesResult] = await Promise.all([
            effortService.getEffortTypeOptions(),
            effortService.getRoleOptions(),
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
    if (!isFormValid.value || !props.record) return

    isSaving.value = true
    errorMessage.value = ""
    warningMessage.value = ""

    try {
        const result = await effortService.updateEffortRecord(props.record.id, {
            effortTypeId: selectedEffortType.value!,
            roleId: selectedRole.value!,
            effortValue: effortValue.value!,
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
        }
    } catch {
        errorMessage.value = "An unexpected error occurred"
    } finally {
        isSaving.value = false
    }
}
</script>
