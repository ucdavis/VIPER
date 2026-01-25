<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @update:model-value="emit('update:modelValue', $event)"
    >
        <q-card style="width: 100%; max-width: 500px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Add Effort Record</div>
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
                        This instructor's effort has been verified. Adding a new record will clear the verification
                        status and require re-verification.
                    </span>
                </q-banner>

                <!-- Course Selection -->
                <q-select
                    v-model="selectedCourse"
                    :options="courseOptions"
                    label="Course *"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    :loading="isLoadingCourses"
                    class="q-mb-md"
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

                <!-- Effort Type Selection -->
                <q-select
                    v-model="selectedEffortType"
                    :options="filteredEffortTypes"
                    label="Effort Type *"
                    dense
                    options-dense
                    outlined
                    option-value="id"
                    option-label="description"
                    emit-value
                    map-options
                    :loading="isLoadingOptions"
                    :disable="!selectedCourse"
                    :hint="!selectedCourse ? 'Select a course first' : undefined"
                    class="q-mb-md"
                />

                <!-- Role Selection -->
                <q-select
                    v-model="selectedRole"
                    :options="roles"
                    label="Role *"
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
                    label="Add Effort"
                    :loading="isSaving"
                    :disable="!isFormValid"
                    @click="createRecord"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { effortService } from "../services/effort-service"
import type { CourseOptionDto, EffortTypeOptionDto, RoleOptionDto } from "../types"

const props = defineProps<{
    modelValue: boolean
    personId: number
    termCode: number
    isVerified?: boolean
    preSelectedCourseId?: number | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    created: []
}>()

// Form state
const selectedCourse = ref<number | null>(null)
const selectedEffortType = ref<string | null>(null)
const selectedRole = ref<number | null>(null)
const effortValue = ref<number | null>(null)

// Options data
const existingCourses = ref<CourseOptionDto[]>([])
const allCourses = ref<CourseOptionDto[]>([])
const effortTypes = ref<EffortTypeOptionDto[]>([])
const roles = ref<RoleOptionDto[]>([])

// Loading and error state
const isLoadingCourses = ref(false)
const isLoadingOptions = ref(false)
const isSaving = ref(false)
const errorMessage = ref("")
const warningMessage = ref("")

// Type for course option with optional header flag
type CourseOption = {
    label: string
    value?: number
    isHeader?: boolean
    disable?: boolean
    isDvm?: boolean
    is199299?: boolean
    isRCourse?: boolean
}

// Computed: Course options with grouping
const courseOptions = computed<CourseOption[]>(() => {
    const options: CourseOption[] = []

    if (existingCourses.value.length > 0) {
        options.push({ label: "Existing Courses", isHeader: true, disable: true })
        for (const course of existingCourses.value) {
            options.push({
                label: course.label,
                value: course.id,
                isDvm: course.isDvm,
                is199299: course.is199299,
                isRCourse: course.isRCourse,
            })
        }
    }

    if (allCourses.value.length > 0) {
        options.push({ label: "All Courses", isHeader: true, disable: true })
        for (const course of allCourses.value) {
            options.push({
                label: course.label,
                value: course.id,
                isDvm: course.isDvm,
                is199299: course.is199299,
                isRCourse: course.isRCourse,
            })
        }
    }

    return options
})

// Computed: Get selected course object from options (includes flags for filtering)
const selectedCourseObj = computed(() => {
    if (!selectedCourse.value) return null
    const option = courseOptions.value.find((o) => o.value === selectedCourse.value)
    if (!option) return null
    return {
        id: option.value!,
        isDvm: option.isDvm ?? false,
        is199299: option.is199299 ?? false,
        isRCourse: option.isRCourse ?? false,
    }
})

// Computed: Filter effort types based on selected course category
const filteredEffortTypes = computed(() => {
    // Require course selection before showing effort types
    if (!selectedCourseObj.value) {
        return []
    }

    const course = selectedCourseObj.value
    return effortTypes.value.filter((et) => {
        if (course.isDvm && !et.allowedOnDvm) return false
        if (course.is199299 && !et.allowedOn199299) return false
        if (course.isRCourse && !et.allowedOnRCourses) return false
        return true
    })
})

// Computed: Effort label (Hours vs Weeks) with required asterisk
const effortLabel = computed(() => {
    if (!selectedEffortType.value) return "Hours *"
    const effortType = effortTypes.value.find((et) => et.id === selectedEffortType.value)
    if (effortType?.usesWeeks && props.termCode >= 201604) {
        return "Weeks *"
    }
    return "Hours *"
})

// Computed: Form validation
const isFormValid = computed(() => {
    return (
        selectedCourse.value !== null &&
        selectedEffortType.value !== null &&
        selectedRole.value !== null &&
        effortValue.value !== null &&
        effortValue.value >= 0
    )
})

// Reset form when dialog opens
watch(
    () => props.modelValue,
    async (isOpen) => {
        if (isOpen) {
            resetForm()
            await loadOptions()
        }
    },
)

// Clear effort type if it becomes invalid for the selected course
watch(selectedCourseObj, () => {
    if (selectedEffortType.value) {
        const stillValid = filteredEffortTypes.value.some((et) => et.id === selectedEffortType.value)
        if (!stillValid) {
            selectedEffortType.value = null
        }
    }
})

function resetForm() {
    selectedCourse.value = null
    selectedEffortType.value = null
    selectedRole.value = null
    effortValue.value = null
    errorMessage.value = ""
    warningMessage.value = ""
}

async function loadOptions() {
    isLoadingCourses.value = true
    isLoadingOptions.value = true

    try {
        const [coursesResult, effortTypesResult, rolesResult] = await Promise.all([
            effortService.getAvailableCourses(props.personId, props.termCode),
            effortService.getEffortTypeOptions(),
            effortService.getRoleOptions(),
        ])

        existingCourses.value = coursesResult.existingCourses
        allCourses.value = coursesResult.allCourses
        effortTypes.value = effortTypesResult
        roles.value = rolesResult

        // Default role to "Instructor" (ID 2) if available
        const instructorRole = rolesResult.find((r) => r.id === 2)
        if (instructorRole) {
            selectedRole.value = instructorRole.id
        }

        // Pre-select course if provided (e.g., after importing a course)
        if (props.preSelectedCourseId) {
            const allAvailableCourses = [...coursesResult.existingCourses, ...coursesResult.allCourses]
            const courseExists = allAvailableCourses.some((c) => c.id === props.preSelectedCourseId)
            if (courseExists) {
                selectedCourse.value = props.preSelectedCourseId
            }
        }
    } catch {
        errorMessage.value = "Failed to load options"
    } finally {
        isLoadingCourses.value = false
        isLoadingOptions.value = false
    }
}

async function createRecord() {
    if (!isFormValid.value) return

    isSaving.value = true
    errorMessage.value = ""
    warningMessage.value = ""

    try {
        const result = await effortService.createEffortRecord({
            personId: props.personId,
            termCode: props.termCode,
            courseId: selectedCourse.value!,
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
                    emit("created")
                }, 2000)
            } else {
                emit("update:modelValue", false)
                emit("created")
            }
        } else {
            errorMessage.value = result.error || "Failed to create effort record"
        }
    } catch {
        errorMessage.value = "An unexpected error occurred"
    } finally {
        isSaving.value = false
    }
}
</script>
