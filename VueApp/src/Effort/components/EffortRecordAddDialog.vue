<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 500px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Add Effort Record</div>
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
                            This instructor's effort has been verified. Adding a new record will clear the verification
                            status and require re-verification.
                        </span>
                    </q-banner>

                    <!-- Course Selection -->
                    <q-select
                        v-model="selectedCourse"
                        :options="filteredCourseOptions"
                        label="Course *"
                        dense
                        options-dense
                        outlined
                        emit-value
                        map-options
                        use-input
                        input-debounce="0"
                        :loading="isLoadingCourses"
                        :rules="[requiredRule('Course')]"
                        lazy-rules="ondemand"
                        @filter="filterCourses"
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
                        :options="effortTypeOptionsWithHeaders"
                        label="Effort Type *"
                        dense
                        options-dense
                        outlined
                        option-value="id"
                        :option-label="formatEffortTypeLabel"
                        option-disable="disable"
                        emit-value
                        map-options
                        :loading="isLoadingOptions"
                        :disable="!selectedCourse"
                        :hint="!selectedCourse ? 'Select a course first' : undefined"
                        :rules="[requiredRule('Effort type')]"
                        lazy-rules="ondemand"
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
                                :class="{ 'text-grey-5': scope.opt.alreadyUsed }"
                            >
                                <q-item-section>
                                    <q-item-label class="row items-center">
                                        <span>{{ scope.opt.description }} ({{ scope.opt.id }})</span>
                                        <span
                                            v-if="scope.opt.alreadyUsed"
                                            class="text-grey-6 text-caption q-ml-sm"
                                        >
                                            — Already used in this course
                                        </span>
                                    </q-item-label>
                                </q-item-section>
                            </q-item>
                        </template>
                    </q-select>

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

                    <!-- Notes -->
                    <q-input
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
                    label="Add Effort"
                    :loading="isSaving"
                    @click="createRecord"
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
import type { CourseOptionDto, EffortTypeOptionDto, RoleOptionDto, InstructorEffortRecordDto } from "../types"
import { effortValueRules, requiredRule, notesMaxHint } from "../validation"
import "../effort-forms.css"

const props = defineProps<{
    modelValue: boolean
    personId: number
    termCode: number
    isVerified?: boolean
    preSelectedCourseId?: number | null
    existingRecords?: InstructorEffortRecordDto[]
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    created: []
}>()

const formRef = ref<QForm | null>(null)

// Form state
const selectedCourse = ref<number | null>(null)
const selectedEffortType = ref<string | null>(null)
const selectedRole = ref<number | null>(null)
const effortValue = ref<number | null>(null)
const notes = ref<string | null>(null)
const notesHint = computed(() => notesMaxHint(notes.value))

// Form data for unsaved changes tracking
const formData = ref({
    selectedCourse: null as number | null,
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

// Filtered course options for type-ahead search
const filteredCourseOptions = ref<CourseOption[]>([])

// Filter function for course dropdown
function filterCourses(val: string, update: (fn: () => void) => void) {
    update(() => {
        if (!val) {
            filteredCourseOptions.value = courseOptions.value
        } else {
            const needle = val.toLowerCase()
            filteredCourseOptions.value = courseOptions.value
                .filter((opt) => {
                    // Always keep headers visible if they have matching items below them
                    if (opt.isHeader) return true
                    return opt.label.toLowerCase().includes(needle)
                })
                .filter((opt, index, arr) => {
                    // Remove headers that have no items following them
                    if (!opt.isHeader) return true
                    const nextItem = arr[index + 1]
                    return nextItem && !nextItem.isHeader
                })
        }
    })
}

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

// Computed: Get set of effort types the instructor already has records for (any course)
const existingEffortTypes = computed(() => {
    if (!props.existingRecords) return new Set<string>()
    return new Set(props.existingRecords.map((r) => r.effortType))
})

// Computed: Get set of effort types already used on the selected course
const usedEffortTypesOnCourse = computed(() => {
    if (!selectedCourse.value || !props.existingRecords) return new Set<string>()
    return new Set(props.existingRecords.filter((r) => r.courseId === selectedCourse.value).map((r) => r.effortType))
})

// Extended type for effort type options with sorting/disable info
type EffortTypeOption = EffortTypeOptionDto & {
    disable?: boolean
    alreadyUsed?: boolean
    isHeader?: boolean
    label?: string
}

// Computed: Filter and sort effort types based on selected course category
const filteredEffortTypes = computed<EffortTypeOption[]>(() => {
    // Require course selection before showing effort types
    if (!selectedCourseObj.value) {
        return []
    }

    const course = selectedCourseObj.value
    const filtered = effortTypes.value
        .filter((et) => {
            if (course.isDvm && !et.allowedOnDvm) return false
            if (course.is199299 && !et.allowedOn199299) return false
            if (course.isRCourse && !et.allowedOnRCourses) return false
            return true
        })
        .map((et) => ({
            ...et,
            disable: usedEffortTypesOnCourse.value.has(et.id),
            alreadyUsed: usedEffortTypesOnCourse.value.has(et.id),
        }))

    // Sort alphabetically within each group
    return filtered.sort((a, b) => a.id.localeCompare(b.id))
})

// Computed: Effort type options with section headers
const effortTypeOptionsWithHeaders = computed<EffortTypeOption[]>(() => {
    const filtered = filteredEffortTypes.value
    if (filtered.length === 0) return []

    // Split into recently used and other
    const recentlyUsed = filtered.filter((et) => existingEffortTypes.value.has(et.id))
    const other = filtered.filter((et) => !existingEffortTypes.value.has(et.id))

    const options: EffortTypeOption[] = []

    if (recentlyUsed.length > 0) {
        options.push({ label: "Recently Used", isHeader: true, disable: true } as EffortTypeOption)
        options.push(...recentlyUsed)
    }

    if (other.length > 0) {
        options.push({ label: "Other Effort Types", isHeader: true, disable: true } as EffortTypeOption)
        options.push(...other)
    }

    return options
})

// Format effort type label for display (used for selected value display)
function formatEffortTypeLabel(opt: EffortTypeOption): string {
    if (opt.isHeader) return opt.label ?? ""
    return `${opt.description} (${opt.id})`
}

// Computed: Effort label (Hours vs Weeks) with required asterisk
const effortLabel = computed(() => {
    if (!selectedEffortType.value) return "Hours *"
    const effortType = effortTypes.value.find((et) => et.id === selectedEffortType.value)
    if (effortType?.usesWeeks && props.termCode >= 201604) {
        return "Weeks *"
    }
    return "Hours *"
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
    notes.value = null
    errorMessage.value = ""
    warningMessage.value = ""
    syncFormData()
    setInitialState()
    formRef.value?.resetValidation()
}

// Sync individual refs to formData for unsaved changes tracking
function syncFormData() {
    formData.value = {
        selectedCourse: selectedCourse.value,
        selectedEffortType: selectedEffortType.value,
        selectedRole: selectedRole.value,
        effortValue: effortValue.value,
        notes: notes.value,
    }
}

// Keep formData in sync when individual refs change
watch([selectedCourse, selectedEffortType, selectedRole, effortValue, notes], syncFormData)

async function loadOptions() {
    isLoadingCourses.value = true
    isLoadingOptions.value = true

    try {
        const [coursesResult, effortTypesResult, rolesResult] = await Promise.all([
            recordService.getAvailableCourses(props.personId, props.termCode),
            recordService.getEffortTypeOptions(),
            recordService.getRoleOptions(),
        ])

        existingCourses.value = coursesResult.existingCourses
        allCourses.value = coursesResult.allCourses
        effortTypes.value = effortTypesResult
        roles.value = rolesResult

        // Initialize filtered options with all courses
        filteredCourseOptions.value = courseOptions.value

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
    const valid = await formRef.value?.validate(true)
    if (!valid) return

    isSaving.value = true
    errorMessage.value = ""
    warningMessage.value = ""

    try {
        const result = await recordService.createEffortRecord({
            personId: props.personId,
            termCode: props.termCode,
            courseId: selectedCourse.value!,
            effortTypeId: selectedEffortType.value!,
            roleId: selectedRole.value!,
            effortValue: effortValue.value!,
            notes: notes.value?.trim() || null,
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
