<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        maximized-on-mobile
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 1100px; max-height: 90vh">
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

            <q-card-section
                class="scroll"
                style="max-height: calc(90vh - 120px)"
            >
                <!-- Read-only instructor info header -->
                <div class="q-mb-md q-pa-sm bg-grey-2 rounded-borders">
                    <div class="text-subtitle2">{{ instructor?.fullName }}</div>
                    <div class="text-caption text-grey-7">
                        Person ID: {{ instructor?.personId }} | Dept: {{ instructor?.effortDept }}
                    </div>
                </div>

                <!-- Instructor Settings Section -->
                <div
                    v-if="canEditInstructor"
                    class="q-mb-md"
                >
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
                        v-if="settingsErrorMessage"
                        class="bg-negative text-white q-mb-md"
                        rounded
                    >
                        <template #avatar>
                            <q-icon
                                name="error"
                                color="white"
                            />
                        </template>
                        {{ settingsErrorMessage }}
                    </q-banner>

                    <div class="row justify-end">
                        <q-btn
                            color="primary"
                            label="Update Instructor"
                            :loading="isSavingSettings"
                            @click="updateInstructor"
                        />
                    </div>
                </div>

                <!-- Percent Assignments Section -->
                <div class="q-mt-md">
                    <div class="text-subtitle1 q-mb-sm">
                        <q-icon
                            name="pie_chart"
                            class="q-mr-sm"
                        />
                        Percent Assignments
                    </div>
                    <PercentAssignmentTable
                        :percentages="percentages"
                        :can-edit="canEdit && canEditInstructor"
                        :loading="loadingPercentages"
                        @add="showAddPercentDialog = true"
                        @edit="openEditPercentDialog"
                        @delete="confirmDeletePercent"
                    />
                </div>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    flat
                    label="Close"
                    @click="handleClose"
                />
            </q-card-actions>
        </q-card>

        <!-- Add Percent Dialog -->
        <PercentAssignmentAddDialog
            v-model="showAddPercentDialog"
            :person-id="instructor?.personId ?? 0"
            :percent-assign-types="percentAssignTypes"
            :units="units"
            @created="onPercentCreated"
        />

        <!-- Edit Percent Dialog -->
        <PercentAssignmentEditDialog
            v-model="showEditPercentDialog"
            :percentage="selectedPercentage"
            :percent-assign-types="percentAssignTypes"
            :units="units"
            @saved="onPercentUpdated"
        />

        <!-- Delete Percent Confirmation Dialog -->
        <q-dialog v-model="showDeletePercentConfirm">
            <q-card>
                <q-card-section class="row items-center">
                    <q-icon
                        name="warning"
                        color="warning"
                        size="md"
                        class="q-mr-md"
                    />
                    <span>Are you sure you want to delete this percentage assignment?</span>
                </q-card-section>
                <q-card-actions align="right">
                    <q-btn
                        v-close-popup
                        flat
                        label="Cancel"
                    />
                    <q-btn
                        flat
                        label="Delete"
                        color="negative"
                        @click="deletePercent"
                    />
                </q-card-actions>
            </q-card>
        </q-dialog>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { effortService } from "../services/effort-service"
import { percentageService } from "../services/percentage-service"
import { percentAssignTypeService } from "../services/percent-assign-type-service"
import { unitService } from "../services/unit-service"
import type {
    PersonDto,
    DepartmentDto,
    ReportUnitDto,
    TitleCodeDto,
    JobGroupDto,
    PercentageDto,
    PercentAssignTypeDto,
    UnitDto,
} from "../types"
import PercentAssignmentTable from "./PercentAssignmentTable.vue"
import PercentAssignmentAddDialog from "./PercentAssignmentAddDialog.vue"
import PercentAssignmentEditDialog from "./PercentAssignmentEditDialog.vue"

const props = defineProps<{
    modelValue: boolean
    instructor: PersonDto | null
    termCode: number | null
    canEdit: boolean
    canEditInstructor: boolean
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    updated: []
    closed: []
}>()

const $q = useQuasar()

// Handle close (X button, Close button, or Escape key) with unsaved changes check
async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
        emit("closed")
    }
}

// Instructor settings form state
const form = ref({
    effortDept: "",
    effortTitleCode: "",
    jobGroupId: "",
    reportUnits: [] as string[],
    volunteerWos: false,
})

// Unsaved changes tracking
const { setInitialState, confirmClose, resetDirtyState } = useUnsavedChanges(form)

// Instructor metadata lookups
const departments = ref<DepartmentDto[]>([])
const reportUnits = ref<ReportUnitDto[]>([])
const titleCodes = ref<TitleCodeDto[]>([])
const jobGroups = ref<JobGroupDto[]>([])
const filteredTitleCodes = ref<{ label: string; value: string }[]>([])
const isSavingSettings = ref(false)
const settingsErrorMessage = ref("")

// Percent assignment state
const percentages = ref<PercentageDto[]>([])
const percentAssignTypes = ref<PercentAssignTypeDto[]>([])
const units = ref<UnitDto[]>([])
const loadingPercentages = ref(false)

// Percent assignment dialog state
const showAddPercentDialog = ref(false)
const showEditPercentDialog = ref(false)
const selectedPercentage = ref<PercentageDto | null>(null)
const showDeletePercentConfirm = ref(false)

// Loading state for initial data
const isLoadingData = ref(false)

// Computed - Instructor settings
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

const isOrphanedTitleCode = computed(() => {
    if (!form.value.effortTitleCode) return false
    return !titleCodes.value.some((t) => t.code === form.value.effortTitleCode)
})

const isOrphanedJobGroup = computed(() => {
    if (!form.value.jobGroupId) return false
    return !jobGroups.value.some((j) => j.code === form.value.jobGroupId)
})

// Filter function for title code dropdown
function filterTitleCodes(val: string, update: (fn: () => void) => void) {
    update(() => {
        const needle = val.toLowerCase()
        filteredTitleCodes.value = titleCodeOptions.value.filter(
            (opt) => opt.label.toLowerCase().includes(needle) || opt.value.toLowerCase().includes(needle),
        )
    })
}

// Load all data when dialog opens
async function loadData() {
    if (!props.instructor) return

    isLoadingData.value = true

    try {
        // Load instructor metadata lookups and percent assignment data in parallel
        const [depts, unitsData, titles, groups, types, percentUnits] = await Promise.all([
            effortService.getInstructorDepartments(),
            effortService.getReportUnits(),
            effortService.getTitleCodes(),
            effortService.getJobGroups(),
            percentAssignTypeService.getPercentAssignTypes(true),
            unitService.getUnits(true),
        ])

        departments.value = depts
        reportUnits.value = unitsData
        titleCodes.value = titles
        jobGroups.value = groups
        percentAssignTypes.value = types
        units.value = percentUnits

        // Initialize filtered title codes
        filteredTitleCodes.value = titleCodeOptions.value

        // Load percentages
        await loadPercentages()
    } finally {
        isLoadingData.value = false
    }
}

// Load percentages for the instructor
async function loadPercentages() {
    if (!props.instructor) return

    loadingPercentages.value = true
    try {
        percentages.value = await percentageService.getPercentagesForPerson(props.instructor.personId)
    } catch {
        percentages.value = []
    } finally {
        loadingPercentages.value = false
    }
}

// Populate form when dialog opens
function populateForm() {
    if (!props.instructor) return

    form.value = {
        effortDept: props.instructor.effortDept || "",
        effortTitleCode: props.instructor.effortTitleCode || "",
        jobGroupId: props.instructor.jobGroupId || "",
        reportUnits: props.instructor.reportUnit ? props.instructor.reportUnit.split(",").map((s) => s.trim()) : [],
        volunteerWos: props.instructor.volunteerWos ?? false,
    }
    settingsErrorMessage.value = ""
    setInitialState()
}

// Watch for dialog open/close
watch(
    () => [props.modelValue, props.instructor],
    ([isOpen, instructor]) => {
        if (isOpen && instructor) {
            populateForm()
            loadData()
        }
    },
    { immediate: true },
)

// Instructor settings update
async function updateInstructor() {
    if (!props.instructor || !props.termCode) return

    isSavingSettings.value = true
    settingsErrorMessage.value = ""

    try {
        const result = await effortService.updateInstructor(props.instructor.personId, props.termCode, {
            effortDept: form.value.effortDept,
            effortTitleCode: form.value.effortTitleCode,
            jobGroupId: form.value.jobGroupId || null,
            reportUnits: form.value.reportUnits.length > 0 ? form.value.reportUnits : null,
            volunteerWos: form.value.volunteerWos,
        })

        if (result.success) {
            resetDirtyState()
            emit("updated")
        } else {
            settingsErrorMessage.value = result.error || "Failed to update instructor"
        }
    } catch {
        settingsErrorMessage.value = "An unexpected error occurred"
    } finally {
        isSavingSettings.value = false
    }
}

// Percent assignment handlers
function openEditPercentDialog(percentage: PercentageDto) {
    selectedPercentage.value = percentage
    showEditPercentDialog.value = true
}

function confirmDeletePercent(percentage: PercentageDto) {
    selectedPercentage.value = percentage
    showDeletePercentConfirm.value = true
}

async function deletePercent() {
    if (!selectedPercentage.value) return

    try {
        const success = await percentageService.deletePercentage(selectedPercentage.value.id)
        if (success) {
            $q.notify({
                type: "positive",
                message: "Percentage assignment deleted successfully",
            })
            await loadPercentages()
        } else {
            $q.notify({
                type: "negative",
                message: "Failed to delete percentage assignment",
            })
        }
    } catch {
        $q.notify({
            type: "negative",
            message: "An error occurred while deleting the percentage assignment",
        })
    } finally {
        showDeletePercentConfirm.value = false
    }
}

function onPercentCreated() {
    $q.notify({ type: "positive", message: "Percentage assignment created successfully" })
    loadPercentages()
    showAddPercentDialog.value = false
}

function onPercentUpdated() {
    $q.notify({ type: "positive", message: "Percentage assignment updated successfully" })
    loadPercentages()
    showEditPercentDialog.value = false
}
</script>
