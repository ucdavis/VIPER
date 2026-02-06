<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 600px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Edit Instructor</div>
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
                <!-- Read-only instructor info -->
                <div class="q-mb-md q-pa-sm bg-grey-2 rounded-borders">
                    <div class="text-subtitle2">{{ instructor?.fullName }}</div>
                    <div class="text-caption text-grey-7">Person ID: {{ instructor?.personId }}</div>
                </div>

                <!-- Editable fields -->
                <q-select
                    v-model="form.effortDept"
                    :options="groupedDepartments"
                    label="Department *"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    class="q-mb-sm"
                >
                    <template #option="scope">
                        <q-item v-bind="scope.itemProps">
                            <q-item-section>
                                <q-item-label>{{ scope.opt.label }}</q-item-label>
                            </q-item-section>
                        </q-item>
                    </template>
                </q-select>

                <q-select
                    v-model="form.effortTitleCode"
                    :options="filteredTitleCodes"
                    label="Title Code"
                    option-value="value"
                    option-label="label"
                    emit-value
                    map-options
                    use-input
                    input-debounce="0"
                    dense
                    options-dense
                    outlined
                    clearable
                    class="q-mb-sm"
                    @filter="filterTitleCodes"
                >
                    <template #before-options>
                        <q-banner
                            v-if="isOrphanedTitleCode"
                            class="bg-warning text-white q-mb-sm"
                            dense
                        >
                            Current value "{{ form.effortTitleCode }}" is not in the standard list
                        </q-banner>
                    </template>
                    <template #no-option>
                        <q-item>
                            <q-item-section class="text-grey"> No results found </q-item-section>
                        </q-item>
                    </template>
                </q-select>

                <q-select
                    v-model="form.jobGroupId"
                    :options="jobGroupOptions"
                    label="Job Group"
                    option-value="value"
                    option-label="label"
                    emit-value
                    map-options
                    dense
                    options-dense
                    outlined
                    clearable
                    class="q-mb-sm"
                >
                    <template #before-options>
                        <q-banner
                            v-if="isOrphanedJobGroup"
                            class="bg-warning text-white q-mb-sm"
                            dense
                        >
                            Current value "{{ form.jobGroupId }}" is not in the standard list
                        </q-banner>
                    </template>
                </q-select>

                <q-select
                    v-model="form.reportUnits"
                    :options="reportUnitOptions"
                    label="Report Units"
                    dense
                    options-dense
                    outlined
                    multiple
                    use-chips
                    emit-value
                    map-options
                    option-label="label"
                    option-value="value"
                    class="q-mb-sm"
                />

                <q-checkbox
                    v-model="form.volunteerWos"
                    class="q-mb-sm"
                >
                    Volunteer / WOS
                    <span class="text-caption text-grey-7">
                        - This will prevent the instructor from showing up in the M&amp;P reports.
                    </span>
                </q-checkbox>

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
                    flat
                    label="Cancel"
                    @click="handleClose"
                />
                <q-btn
                    color="primary"
                    label="Update"
                    :loading="isSaving"
                    @click="updateInstructor"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { instructorService } from "../services/instructor-service"
import type { PersonDto, DepartmentDto, ReportUnitDto, TitleCodeDto, JobGroupDto } from "../types"

const props = defineProps<{
    modelValue: boolean
    instructor: PersonDto | null
    termCode: number | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    updated: []
}>()

// Form state
const form = ref({
    effortDept: "",
    effortTitleCode: "",
    jobGroupId: "",
    reportUnits: [] as string[],
    volunteerWos: false,
})

// Unsaved changes tracking
const { setInitialState, confirmClose } = useUnsavedChanges(form)

// Handle close (X button, Cancel button, or Escape key) with unsaved changes check
async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

const departments = ref<DepartmentDto[]>([])
const reportUnits = ref<ReportUnitDto[]>([])
const titleCodes = ref<TitleCodeDto[]>([])
const jobGroups = ref<JobGroupDto[]>([])
const filteredTitleCodes = ref<{ label: string; value: string }[]>([])
const isSaving = ref(false)
const errorMessage = ref("")

// Computed
const groupedDepartments = computed(() => {
    const groups: Record<string, { label: string; value: string }[]> = {}

    for (const dept of departments.value) {
        const groupArray = groups[dept.group] ?? (groups[dept.group] = [])
        groupArray.push({
            label: `${dept.code} - ${dept.name}`,
            value: dept.code,
        })
    }

    const result: { label: string; value: string; disable?: boolean }[] = []
    for (const [groupName, items] of Object.entries(groups)) {
        result.push({ label: groupName, value: "", disable: true })
        result.push(...items)
    }

    return result
})

const reportUnitOptions = computed(() => {
    return reportUnits.value.map((u) => ({
        label: `${u.abbrev} - ${u.unit}`,
        value: u.abbrev,
    }))
})

const titleCodeOptions = computed(() => {
    return titleCodes.value.map((t) => ({
        label: t.name ? `${t.code} - ${t.name}` : t.code,
        value: t.code,
    }))
})

const jobGroupOptions = computed(() => {
    return jobGroups.value.map((j) => ({
        label: j.name ? `${j.code} - ${j.name}` : j.code,
        value: j.code,
    }))
})

// Check if the current title code value is not in the dropdown options (orphaned)
const isOrphanedTitleCode = computed(() => {
    if (!form.value.effortTitleCode) return false
    return !titleCodes.value.some((t) => t.code === form.value.effortTitleCode)
})

// Check if the current job group value is not in the dropdown options (orphaned)
const isOrphanedJobGroup = computed(() => {
    if (!form.value.jobGroupId) return false
    return !jobGroups.value.some((j) => j.code === form.value.jobGroupId)
})

// Filter function for title code dropdown - searches by code OR name
function filterTitleCodes(val: string, update: (fn: () => void) => void) {
    update(() => {
        const needle = val.toLowerCase()
        filteredTitleCodes.value = titleCodeOptions.value.filter(
            (opt) => opt.label.toLowerCase().includes(needle) || opt.value.toLowerCase().includes(needle),
        )
    })
}

// Load lookup data
onMounted(async () => {
    const [depts, units, titles, groups] = await Promise.all([
        instructorService.getInstructorDepartments(),
        instructorService.getReportUnits(),
        instructorService.getTitleCodes(),
        instructorService.getJobGroups(),
    ])
    departments.value = depts
    reportUnits.value = units
    titleCodes.value = titles
    jobGroups.value = groups
    // Initialize filtered title codes with all options
    filteredTitleCodes.value = titleCodeOptions.value
})

// Reset form when dialog opens with instructor
watch(
    () => [props.modelValue, props.instructor],
    ([isOpen, instructor]) => {
        if (isOpen && instructor) {
            const inst = instructor as PersonDto
            form.value = {
                effortDept: inst.effortDept || "",
                effortTitleCode: inst.effortTitleCode || "",
                jobGroupId: inst.jobGroupId || "",
                reportUnits: inst.reportUnit ? inst.reportUnit.split(",").map((s) => s.trim()) : [],
                volunteerWos: inst.volunteerWos ?? false,
            }
            errorMessage.value = ""
            setInitialState()
        }
    },
    { immediate: true },
)

async function updateInstructor() {
    if (!props.instructor || !props.termCode) return

    isSaving.value = true
    errorMessage.value = ""

    try {
        const result = await instructorService.updateInstructor(props.instructor.personId, props.termCode, {
            effortDept: form.value.effortDept,
            effortTitleCode: form.value.effortTitleCode,
            jobGroupId: form.value.jobGroupId || null,
            reportUnits: form.value.reportUnits.length > 0 ? form.value.reportUnits : null,
            volunteerWos: form.value.volunteerWos,
        })

        if (result.success) {
            emit("update:modelValue", false)
            emit("updated")
        } else {
            errorMessage.value = result.error || "Failed to update instructor"
        }
    } catch {
        errorMessage.value = "An unexpected error occurred"
    } finally {
        isSaving.value = false
    }
}
</script>
