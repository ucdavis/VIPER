<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @keydown.escape="handleClose"
    >
        <q-card class="dialog-card-sm">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Add Instructor Effort</div>
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
                    <!-- Instructor Selection -->
                    <q-select
                        v-model="selectedInstructor"
                        :options="filteredInstructorOptions"
                        label="Instructor *"
                        dense
                        options-dense
                        outlined
                        emit-value
                        map-options
                        use-input
                        input-debounce="0"
                        hint="Type to search all instructors"
                        :loading="isLoadingInstructors"
                        :rules="[requiredRule('Instructor')]"
                        lazy-rules="ondemand"
                        @filter="filterInstructors"
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
                        :options="effortTypes"
                        label="Session Type *"
                        dense
                        options-dense
                        outlined
                        option-value="id"
                        :option-label="(opt: EffortTypeOptionDto) => `${opt.description} (${opt.id})`"
                        emit-value
                        map-options
                        :loading="isLoadingOptions"
                        :rules="[requiredRule('Session type')]"
                        lazy-rules="ondemand"
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

                    <!-- Notes (only for resident courses) -->
                    <q-input
                        v-if="isResidentCourse"
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
                        role="alert"
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
                >
                    <template #loading>
                        <q-spinner
                            size="1em"
                            class="q-mr-sm"
                        />
                        Add Effort
                    </template>
                </q-btn>
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { QForm } from "quasar"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { courseService } from "../services/course-service"
import { recordService } from "../services/record-service"
import type { CourseInstructorOptionDto, EffortTypeOptionDto, RoleOptionDto } from "../types"
import { effortValueRules, requiredRule, notesMaxHint } from "../validation"
import "../effort-dialogs.css"
import "../effort-forms.css"

const props = defineProps<{
    modelValue: boolean
    courseId: number
    termCode: number
    isResidentCourse?: boolean
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    created: []
}>()

const formRef = ref<QForm | null>(null)

// Form state
const selectedInstructor = ref<number | null>(null)
const selectedEffortType = ref<string | null>(null)
const selectedRole = ref<number | null>(null)
const effortValue = ref<number | null>(null)
const notes = ref<string | null>(null)
const notesHint = computed(() => notesMaxHint(notes.value))

// Form data for unsaved changes tracking
const formData = ref({
    selectedInstructor: null as number | null,
    selectedEffortType: null as string | null,
    selectedRole: null as number | null,
    effortValue: null as number | null,
    notes: null as string | null,
})

// Unsaved changes tracking
const { setInitialState, confirmClose } = useUnsavedChanges(formData)

async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

// Options data
const existingInstructors = ref<CourseInstructorOptionDto[]>([])
const allInstructors = ref<CourseInstructorOptionDto[]>([])
const effortTypes = ref<EffortTypeOptionDto[]>([])
const roles = ref<RoleOptionDto[]>([])

// Loading and error state
const isLoadingInstructors = ref(false)
const isLoadingOptions = ref(false)
const isSaving = ref(false)
const errorMessage = ref("")
const warningMessage = ref("")

// Instructor dropdown option type
type InstructorOption = {
    label: string
    value?: number
    isHeader?: boolean
    disable?: boolean
}

// Computed: All instructor options with grouping (used for search)
const instructorOptions = computed<InstructorOption[]>(() => {
    const options: InstructorOption[] = []

    if (existingInstructors.value.length > 0) {
        options.push({ label: "Course Instructors", isHeader: true, disable: true })
        for (const inst of existingInstructors.value) {
            options.push({
                label: inst.fullName,
                value: inst.personId,
            })
        }
    }

    if (allInstructors.value.length > 0) {
        options.push({ label: "Other Instructors", isHeader: true, disable: true })
        for (const inst of allInstructors.value) {
            options.push({
                label: inst.fullName,
                value: inst.personId,
            })
        }
    }

    return options
})

// Computed: Only course instructors (shown by default before searching)
const existingInstructorOptions = computed<InstructorOption[]>(() => {
    if (existingInstructors.value.length === 0) return []
    const options: InstructorOption[] = [{ label: "Course Instructors", isHeader: true, disable: true }]
    for (const inst of existingInstructors.value) {
        options.push({ label: inst.fullName, value: inst.personId })
    }
    return options
})

// Filtered instructor options for type-ahead search
const filteredInstructorOptions = ref<InstructorOption[]>([])

function filterInstructors(val: string, update: (fn: () => void) => void) {
    update(() => {
        if (!val) {
            // Show only course instructors by default
            filteredInstructorOptions.value = existingInstructorOptions.value
        } else {
            // Search across all instructors
            const needle = val.toLowerCase()
            filteredInstructorOptions.value = instructorOptions.value
                .filter((opt) => {
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

function resetForm() {
    selectedInstructor.value = null
    selectedEffortType.value = null
    selectedRole.value = null
    effortValue.value = null
    notes.value = null
    errorMessage.value = ""
    warningMessage.value = ""
    syncFormData()
    formRef.value?.resetValidation()
}

function syncFormData() {
    formData.value = {
        selectedInstructor: selectedInstructor.value,
        selectedEffortType: selectedEffortType.value,
        selectedRole: selectedRole.value,
        effortValue: effortValue.value,
        notes: notes.value,
    }
}

// Keep formData in sync when individual refs change
watch([selectedInstructor, selectedEffortType, selectedRole, effortValue, notes], syncFormData)

async function loadOptions() {
    isLoadingInstructors.value = true
    isLoadingOptions.value = true

    try {
        const [instructorsResult, effortTypesResult, rolesResult] = await Promise.all([
            courseService.getPossibleInstructors(props.courseId),
            recordService.getEffortTypeOptions(),
            recordService.getRoleOptions(),
        ])

        existingInstructors.value = instructorsResult.existingInstructors
        allInstructors.value = instructorsResult.otherInstructors
        effortTypes.value = effortTypesResult
        roles.value = rolesResult

        // Initialize filtered options with course instructors only
        filteredInstructorOptions.value = existingInstructorOptions.value

        // Default role to "Instructor" (ID 2) if available
        const instructorRole = rolesResult.find((r) => r.id === 2)
        if (instructorRole) {
            selectedRole.value = instructorRole.id
        }

        // Capture baseline after async defaults are applied
        syncFormData()
        setInitialState()
    } catch {
        errorMessage.value = "Failed to load options"
    } finally {
        isLoadingInstructors.value = false
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
            personId: selectedInstructor.value!,
            termCode: props.termCode,
            courseId: props.courseId,
            effortTypeId: selectedEffortType.value!,
            roleId: selectedRole.value!,
            effortValue: effortValue.value!,
            notes: notes.value?.trim() || null,
        })

        if (result.success) {
            if (result.result?.warning) {
                warningMessage.value = result.result.warning
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
