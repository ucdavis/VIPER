<template>
    <div class="q-pa-md">
        <!-- Breadcrumb -->
        <q-breadcrumbs class="q-mb-md">
            <q-breadcrumbs-el
                label="Instructors"
                :to="{ name: 'InstructorList', params: { termCode } }"
            />
            <q-breadcrumbs-el
                :label="instructor?.fullName ?? 'Instructor'"
                :to="{ name: 'InstructorDetail', params: { termCode, personId } }"
            />
            <q-breadcrumbs-el label="Edit" />
        </q-breadcrumbs>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading instructor...
        </div>

        <!-- Error state -->
        <q-banner
            v-else-if="loadError"
            class="bg-negative text-white q-mb-md"
        >
            {{ loadError }}
            <template #action>
                <q-btn
                    flat
                    label="Go Back"
                    :to="{ name: 'InstructorList', params: { termCode } }"
                />
            </template>
        </q-banner>

        <!-- Instructor content -->
        <template v-else-if="instructor">
            <!-- Instructor Header -->
            <q-card
                flat
                bordered
                class="q-mb-md"
            >
                <q-card-section
                    horizontal
                    class="items-center"
                >
                    <img
                        v-if="instructor.mailId"
                        :src="photoUrl"
                        :alt="instructor.fullName"
                        style="height: 100px; width: auto"
                    />
                    <q-avatar
                        v-else
                        size="100px"
                        square
                        class="bg-grey-3"
                    >
                        <q-icon
                            name="person"
                            size="80px"
                            color="grey-5"
                        />
                    </q-avatar>
                    <div class="q-ml-md">
                        <div class="text-h6">{{ instructor.fullName }}</div>
                        <div class="text-caption text-grey-7">Person ID: {{ instructor.personId }}</div>
                    </div>
                </q-card-section>
            </q-card>

            <!-- Instructor Settings Section -->
            <div
                v-if="canEditInstructor"
                class="q-mb-md"
            >
                <div class="text-subtitle1 q-mb-sm">Instructor Details</div>

                <q-form
                    ref="formRef"
                    class="effort-form"
                    greedy
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
                        :rules="[requiredRule('Department')]"
                        lazy-rules="ondemand"
                    >
                        <template #option="scope">
                            <q-item v-bind="scope.itemProps">
                                <q-item-section>
                                    <q-item-label>{{ scope.opt.label }}</q-item-label>
                                </q-item-section>
                            </q-item>
                        </template>
                    </q-select>

                    <q-chip
                        v-if="isUnknownDept"
                        color="warning"
                        text-color="dark"
                        icon="info"
                        dense
                        class="q-mb-sm form-message-chip"
                    >
                        Please provide a valid department if known
                    </q-chip>

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
                    />

                    <q-checkbox v-model="form.volunteerWos">
                        Volunteer / WOS
                        <span class="text-caption text-grey-7">
                            - This will prevent the instructor from showing up in the M&amp;P reports.
                        </span>
                    </q-checkbox>

                    <q-banner
                        v-if="settingsErrorMessage"
                        class="bg-negative text-white"
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
                </q-form>

                <div class="row justify-end q-mt-sm">
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
                <div class="text-subtitle1 q-mb-sm">Percent Assignments</div>
                <PercentAssignmentTable
                    :percentages="percentages"
                    :can-edit="canEdit && canEditInstructor"
                    :loading="loadingPercentages"
                    @add="showAddPercentDialog = true"
                    @edit="openEditPercentDialog"
                    @delete="confirmDeletePercent"
                />
            </div>
        </template>

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
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useRoute, useRouter, onBeforeRouteLeave } from "vue-router"
import { QForm, useQuasar } from "quasar"
import { requiredRule } from "../validation"
import "../effort-forms.css"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import { instructorService } from "../services/instructor-service"
import { recordService } from "../services/record-service"
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
import PercentAssignmentTable from "../components/PercentAssignmentTable.vue"
import PercentAssignmentAddDialog from "../components/PercentAssignmentAddDialog.vue"
import PercentAssignmentEditDialog from "../components/PercentAssignmentEditDialog.vue"

const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const { hasEditInstructor } = useEffortPermissions()

// Route params
const termCode = computed(() => route.params.termCode as string)
const personId = computed(() => parseInt(route.params.personId as string, 10))
const termCodeNum = computed(() => parseInt(termCode.value, 10))

// Loading state
const isLoading = ref(true)
const loadError = ref<string | null>(null)

// Instructor data
const instructor = ref<PersonDto | null>(null)
const canEditTerm = ref(false)

const formRef = ref<QForm | null>(null)

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

// Handle navigation away with unsaved changes
onBeforeRouteLeave(async () => {
    return await confirmClose()
})

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

// Computed - permissions
const canEdit = computed(() => canEditTerm.value && hasEditInstructor.value)
const canEditInstructor = computed(() => canEditTerm.value && hasEditInstructor.value)

// Computed - photo URL for instructor profile picture
const photoUrl = computed(() => {
    if (!instructor.value?.mailId) return ""
    return `https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid=${encodeURIComponent(instructor.value.mailId)}&altphoto=1`
})

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

const isUnknownDept = computed(() => form.value.effortDept === "UNK")

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

// Populate form from instructor data
function populateForm() {
    if (!instructor.value) return

    form.value = {
        effortDept: instructor.value.effortDept || "UNK",
        effortTitleCode: instructor.value.effortTitleCode || "",
        jobGroupId: instructor.value.jobGroupId || "",
        reportUnits: instructor.value.reportUnit ? instructor.value.reportUnit.split(",").map((s) => s.trim()) : [],
        volunteerWos: instructor.value.volunteerWos ?? false,
    }
    settingsErrorMessage.value = ""
    setInitialState()
    formRef.value?.resetValidation()
}

// Load percentages for the instructor
async function loadPercentages() {
    if (!instructor.value) return

    loadingPercentages.value = true
    try {
        percentages.value = await percentageService.getPercentagesForPerson(instructor.value.personId)
    } catch {
        percentages.value = []
    } finally {
        loadingPercentages.value = false
    }
}

// Load all data on mount
async function loadData() {
    isLoading.value = true
    loadError.value = null

    try {
        // Load instructor and permission check first
        const [instructorResult, canEditResult] = await Promise.all([
            instructorService.getInstructor(personId.value, termCodeNum.value),
            recordService.canEditTerm(termCodeNum.value),
        ])

        if (!instructorResult) {
            loadError.value = "Instructor not found or you do not have permission to view them."
            return
        }

        instructor.value = instructorResult
        canEditTerm.value = canEditResult

        // Populate form before loading lookups
        populateForm()

        // Load percentages (needed for all users including view-only)
        await loadPercentages()

        // Only load edit-specific metadata if the user has edit permission
        if (canEditInstructor.value) {
            const [depts, unitsData, titles, groups, types, percentUnits] = await Promise.all([
                instructorService.getInstructorDepartments(),
                instructorService.getReportUnits(),
                instructorService.getTitleCodes(),
                instructorService.getJobGroups(),
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
        }
    } catch {
        loadError.value = "Failed to load instructor. Please try again."
    } finally {
        isLoading.value = false
    }
}

// Instructor settings update
async function updateInstructor() {
    if (!instructor.value) return

    const valid = await formRef.value?.validate(true)
    if (!valid) return

    isSavingSettings.value = true
    settingsErrorMessage.value = ""

    try {
        const result = await instructorService.updateInstructor(instructor.value.personId, termCodeNum.value, {
            effortDept: form.value.effortDept,
            effortTitleCode: form.value.effortTitleCode,
            jobGroupId: form.value.jobGroupId || null,
            reportUnits: form.value.reportUnits.length > 0 ? form.value.reportUnits : null,
            volunteerWos: form.value.volunteerWos,
        })

        if (result.success) {
            resetDirtyState()
            $q.notify({
                type: "positive",
                message: "Instructor updated successfully",
            })
            router.push({ name: "InstructorList", params: { termCode: termCode.value } })
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

watch([termCode, personId], loadData, { immediate: true })
</script>
