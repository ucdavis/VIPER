<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        maximized-on-mobile
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 1000px; position: relative">
            <q-btn
                icon="close"
                flat
                round
                dense
                class="absolute-top-right q-ma-sm"
                style="z-index: 1"
                aria-label="Close dialog"
                @click="handleClose"
            />
            <q-card-section class="q-pb-none q-pr-xl">
                <div class="text-h6">Harvest Term: {{ props.termName }}</div>
                <div class="text-caption text-grey-7">
                    Import instructors and courses from CREST and Clinical Scheduler
                </div>
            </q-card-section>

            <!-- Loading State (Preview) -->
            <q-card-section
                v-if="isLoading"
                class="text-center q-py-xl"
            >
                <q-spinner-dots
                    size="50px"
                    color="primary"
                />
                <div class="q-mt-md text-grey-7">Generating preview...</div>
            </q-card-section>

            <!-- Committing State (Progress) -->
            <q-card-section
                v-else-if="isCommitting"
                class="q-py-xl"
            >
                <div class="text-h6 q-mb-md text-center">Processing Harvest</div>
                <q-linear-progress
                    :value="harvestProgress"
                    size="25px"
                    color="primary"
                    class="q-mb-md"
                >
                    <div class="absolute-full flex flex-center">
                        <q-badge
                            color="white"
                            text-color="primary"
                            :label="`${Math.round(harvestProgress * 100)}%`"
                        />
                    </div>
                </q-linear-progress>
                <div class="text-center text-grey-7">{{ harvestPhase }}</div>
                <div
                    v-if="harvestDetail"
                    class="text-center text-caption text-grey-6 q-mt-xs"
                >
                    {{ harvestDetail }}
                </div>
            </q-card-section>

            <!-- Error State -->
            <q-card-section
                v-else-if="loadError"
                class="text-center q-py-xl"
            >
                <q-icon
                    name="error"
                    color="negative"
                    size="48px"
                />
                <div class="q-mt-md text-negative">{{ loadError }}</div>
                <q-btn
                    label="Retry"
                    color="primary"
                    class="q-mt-md"
                    @click="loadPreview"
                />
            </q-card-section>

            <!-- Preview Content -->
            <template v-else-if="preview">
                <q-card-section class="q-pt-sm">
                    <!-- Summary Banner -->
                    <q-banner
                        class="bg-blue-1 q-mb-md"
                        rounded
                    >
                        <div class="row q-col-gutter-md">
                            <div class="col-6 col-sm-4 text-center">
                                <div class="text-h5">{{ preview.summary.totalInstructors }}</div>
                                <div class="text-caption">Instructors</div>
                            </div>
                            <div class="col-6 col-sm-4 text-center">
                                <div class="text-h5">{{ preview.summary.totalCourses }}</div>
                                <div class="text-caption">Courses</div>
                            </div>
                            <div class="col-6 col-sm-4 text-center">
                                <div class="text-h5">{{ preview.summary.totalEffortRecords }}</div>
                                <div class="text-caption">Effort Records</div>
                            </div>
                        </div>
                    </q-banner>

                    <!-- Warnings -->
                    <q-banner
                        v-if="preview.warnings.length > 0"
                        class="bg-orange-1 q-mb-md"
                        rounded
                    >
                        <div class="row items-center q-mb-xs">
                            <q-icon
                                name="warning"
                                color="orange"
                                size="sm"
                                class="q-mr-sm"
                            />
                            <span class="text-weight-medium">
                                {{ preview.warnings.length }} {{ inflect("Warning", preview.warnings.length) }}
                            </span>
                        </div>
                        <ul class="q-mb-none q-pl-lg">
                            <li
                                v-for="(warning, idx) in preview.warnings.slice(0, 5)"
                                :key="idx"
                            >
                                <span v-if="warning.phase">{{ warning.phase }}: </span>{{ warning.message }}
                                <div
                                    v-if="warning.details"
                                    class="text-caption text-grey-7"
                                >
                                    {{ warning.details }}
                                </div>
                            </li>
                        </ul>
                        <div
                            v-if="preview.warnings.length > 5"
                            class="text-caption text-grey-7"
                        >
                            ...and {{ preview.warnings.length - 5 }} more
                        </div>
                    </q-banner>

                    <!-- Errors -->
                    <q-banner
                        v-if="preview.errors.length > 0"
                        class="bg-red-1 q-mb-md"
                        rounded
                    >
                        <div class="row items-center q-mb-xs">
                            <q-icon
                                name="error"
                                color="negative"
                                size="sm"
                                class="q-mr-sm"
                            />
                            <span class="text-weight-medium text-negative">
                                {{ preview.errors.length }} {{ inflect("Error", preview.errors.length) }} - Harvest may
                                fail
                            </span>
                        </div>
                        <ul class="q-mb-none q-pl-lg">
                            <li
                                v-for="(error, idx) in preview.errors"
                                :key="idx"
                            >
                                {{ error.phase }}: {{ error.message }}
                            </li>
                        </ul>
                    </q-banner>

                    <!-- Removed Items Warning -->
                    <q-banner
                        v-if="hasRemovedItems"
                        class="bg-purple-1 q-mb-md"
                        rounded
                    >
                        <div class="row items-center q-mb-xs">
                            <q-icon
                                name="person_remove"
                                color="purple"
                                size="sm"
                                class="q-mr-sm"
                            />
                            <span class="text-weight-medium">Items that will be removed</span>
                        </div>
                        <div class="text-caption text-grey-7">
                            The following items exist in the current term but are not in the harvest sources and will be
                            deleted:
                        </div>
                        <div
                            v-if="preview.removedInstructors.length > 0"
                            class="q-mt-xs"
                        >
                            <strong>{{ preview.removedInstructors.length }}</strong>
                            {{ inflect("instructor", preview.removedInstructors.length) }}
                            <span class="text-caption text-grey-7">
                                ({{
                                    preview.removedInstructors
                                        .slice(0, 5)
                                        .map((i) => i.fullName)
                                        .join(" / ")
                                }}{{ preview.removedInstructors.length > 5 ? " / ..." : "" }})
                            </span>
                        </div>
                        <div
                            v-if="preview.removedCourses.length > 0"
                            class="q-mt-xs"
                        >
                            <strong>{{ preview.removedCourses.length }}</strong>
                            {{ inflect("course", preview.removedCourses.length) }}
                            <span class="text-caption text-grey-7">
                                ({{
                                    preview.removedCourses
                                        .slice(0, 3)
                                        .map((c) => `${c.subjCode.trim()} ${c.crseNumb.trim()}`)
                                        .join(", ")
                                }}{{ preview.removedCourses.length > 3 ? "..." : "" }})
                            </span>
                        </div>
                    </q-banner>

                    <!-- Tabs -->
                    <q-tabs
                        v-model="activeTab"
                        dense
                        align="left"
                        class="text-grey"
                        active-color="primary"
                        indicator-color="primary"
                        narrow-indicator
                    >
                        <q-tab
                            name="crest"
                            label="CREST"
                        />
                        <q-tab
                            name="noncrest"
                            label="Non-CREST"
                        />
                        <q-tab
                            name="clinical"
                            label="Clinical"
                        />
                    </q-tabs>

                    <q-separator />

                    <q-tab-panels
                        v-model="activeTab"
                        animated
                        keep-alive
                    >
                        <!-- CREST Tab -->
                        <q-tab-panel
                            name="crest"
                            class="q-pa-none q-pt-md"
                        >
                            <div class="row items-center justify-between q-mb-sm">
                                <div class="text-subtitle2">Instructors ({{ preview.crestInstructors.length }})</div>
                                <q-input
                                    v-model="crestInstructorFilter"
                                    placeholder="Search..."
                                    dense
                                    outlined
                                    clearable
                                    class="compact-search"
                                >
                                    <template #prepend>
                                        <q-icon
                                            name="search"
                                            size="xs"
                                        />
                                    </template>
                                </q-input>
                            </div>
                            <q-table
                                :rows="preview.crestInstructors"
                                :columns="instructorColumns"
                                row-key="personId"
                                :filter="crestInstructorFilter"
                                dense
                                flat
                                bordered
                                v-model:pagination="crestInstructorPagination"
                                class="q-mb-md"
                            >
                                <template #body-cell-status="slotProps">
                                    <q-td :props="slotProps">
                                        <q-badge
                                            :color="getStatusColor(slotProps.value)"
                                            :label="slotProps.value"
                                        />
                                    </q-td>
                                </template>
                            </q-table>

                            <div class="row items-center justify-between q-mb-sm">
                                <div class="text-subtitle2">Courses ({{ preview.crestCourses.length }})</div>
                                <q-input
                                    v-model="crestCourseFilter"
                                    placeholder="Search..."
                                    dense
                                    outlined
                                    clearable
                                    class="compact-search"
                                >
                                    <template #prepend>
                                        <q-icon
                                            name="search"
                                            size="xs"
                                        />
                                    </template>
                                </q-input>
                            </div>
                            <q-table
                                :rows="preview.crestCourses"
                                :columns="courseColumns"
                                row-key="crn"
                                :filter="crestCourseFilter"
                                dense
                                flat
                                bordered
                                v-model:pagination="crestCoursePagination"
                                class="q-mb-md"
                            >
                                <template #body-cell-status="slotProps">
                                    <q-td :props="slotProps">
                                        <q-badge
                                            :color="getStatusColor(slotProps.value)"
                                            :label="slotProps.value"
                                        />
                                    </q-td>
                                </template>
                            </q-table>

                            <div class="row items-center justify-between q-mb-sm">
                                <div class="text-subtitle2">Effort Records ({{ preview.crestEffort.length }})</div>
                                <q-input
                                    v-model="crestEffortFilter"
                                    placeholder="Search..."
                                    dense
                                    outlined
                                    clearable
                                    class="compact-search"
                                >
                                    <template #prepend>
                                        <q-icon
                                            name="search"
                                            size="xs"
                                        />
                                    </template>
                                </q-input>
                            </div>
                            <q-table
                                :rows="preview.crestEffort"
                                :columns="effortColumns"
                                :row-key="(row) => `${row.mothraId}-${row.crn}-${row.effortType}`"
                                :filter="crestEffortFilter"
                                dense
                                flat
                                bordered
                                v-model:pagination="crestEffortPagination"
                            >
                                <template #body-cell-status="slotProps">
                                    <q-td :props="slotProps">
                                        <q-badge
                                            :color="getStatusColor(slotProps.value)"
                                            :label="slotProps.value"
                                        />
                                    </q-td>
                                </template>
                            </q-table>
                        </q-tab-panel>

                        <!-- Non-CREST Tab -->
                        <q-tab-panel
                            name="noncrest"
                            class="q-pa-none q-pt-md"
                        >
                            <div class="row items-center justify-between q-mb-sm">
                                <div class="text-subtitle2">Instructors ({{ preview.nonCrestInstructors.length }})</div>
                                <q-input
                                    v-model="nonCrestInstructorFilter"
                                    placeholder="Search..."
                                    dense
                                    outlined
                                    clearable
                                    class="compact-search"
                                >
                                    <template #prepend>
                                        <q-icon
                                            name="search"
                                            size="xs"
                                        />
                                    </template>
                                </q-input>
                            </div>
                            <q-table
                                :rows="preview.nonCrestInstructors"
                                :columns="instructorColumns"
                                row-key="personId"
                                :filter="nonCrestInstructorFilter"
                                dense
                                flat
                                bordered
                                v-model:pagination="nonCrestInstructorPagination"
                                class="q-mb-md"
                            >
                                <template #body-cell-status="slotProps">
                                    <q-td :props="slotProps">
                                        <q-badge
                                            :color="getStatusColor(slotProps.value)"
                                            :label="slotProps.value"
                                        />
                                    </q-td>
                                </template>
                            </q-table>

                            <div class="row items-center justify-between q-mb-sm">
                                <div class="text-subtitle2">Courses ({{ preview.nonCrestCourses.length }})</div>
                                <q-input
                                    v-model="nonCrestCourseFilter"
                                    placeholder="Search..."
                                    dense
                                    outlined
                                    clearable
                                    class="compact-search"
                                >
                                    <template #prepend>
                                        <q-icon
                                            name="search"
                                            size="xs"
                                        />
                                    </template>
                                </q-input>
                            </div>
                            <q-table
                                :rows="preview.nonCrestCourses"
                                :columns="courseColumns"
                                row-key="crn"
                                :filter="nonCrestCourseFilter"
                                dense
                                flat
                                bordered
                                v-model:pagination="nonCrestCoursePagination"
                                class="q-mb-md"
                            >
                                <template #body-cell-status="slotProps">
                                    <q-td :props="slotProps">
                                        <q-badge
                                            :color="getStatusColor(slotProps.value)"
                                            :label="slotProps.value"
                                        />
                                    </q-td>
                                </template>
                            </q-table>

                            <div class="row items-center justify-between q-mb-sm">
                                <div class="text-subtitle2">Effort Records ({{ preview.nonCrestEffort.length }})</div>
                                <q-input
                                    v-model="nonCrestEffortFilter"
                                    placeholder="Search..."
                                    dense
                                    outlined
                                    clearable
                                    class="compact-search"
                                >
                                    <template #prepend>
                                        <q-icon
                                            name="search"
                                            size="xs"
                                        />
                                    </template>
                                </q-input>
                            </div>
                            <q-table
                                :rows="preview.nonCrestEffort"
                                :columns="effortColumns"
                                :row-key="(row) => `${row.mothraId}-${row.crn}-${row.effortType}`"
                                :filter="nonCrestEffortFilter"
                                dense
                                flat
                                bordered
                                v-model:pagination="nonCrestEffortPagination"
                            >
                                <template #body-cell-status="slotProps">
                                    <q-td :props="slotProps">
                                        <q-badge
                                            :color="getStatusColor(slotProps.value)"
                                            :label="slotProps.value"
                                        />
                                    </q-td>
                                </template>
                            </q-table>
                        </q-tab-panel>

                        <!-- Clinical Tab -->
                        <q-tab-panel
                            name="clinical"
                            class="q-pa-none q-pt-md"
                        >
                            <div class="row items-center justify-between q-mb-sm">
                                <div class="text-subtitle2">Instructors ({{ preview.clinicalInstructors.length }})</div>
                                <q-input
                                    v-model="clinicalInstructorFilter"
                                    placeholder="Search..."
                                    dense
                                    outlined
                                    clearable
                                    class="compact-search"
                                >
                                    <template #prepend>
                                        <q-icon
                                            name="search"
                                            size="xs"
                                        />
                                    </template>
                                </q-input>
                            </div>
                            <q-table
                                :rows="preview.clinicalInstructors"
                                :columns="instructorColumns"
                                row-key="mothraId"
                                :filter="clinicalInstructorFilter"
                                dense
                                flat
                                bordered
                                v-model:pagination="clinicalInstructorPagination"
                                class="q-mb-md"
                            >
                                <template #body-cell-status="slotProps">
                                    <q-td :props="slotProps">
                                        <q-badge
                                            :color="getStatusColor(slotProps.value)"
                                            :label="slotProps.value"
                                        />
                                    </q-td>
                                </template>
                            </q-table>

                            <div class="row items-center justify-between q-mb-sm">
                                <div class="text-subtitle2">Courses ({{ preview.clinicalCourses.length }})</div>
                                <q-input
                                    v-model="clinicalCourseFilter"
                                    placeholder="Search..."
                                    dense
                                    outlined
                                    clearable
                                    class="compact-search"
                                >
                                    <template #prepend>
                                        <q-icon
                                            name="search"
                                            size="xs"
                                        />
                                    </template>
                                </q-input>
                            </div>
                            <q-table
                                :rows="preview.clinicalCourses"
                                :columns="courseColumns"
                                :row-key="(row) => `${row.subjCode}-${row.crseNumb}-${row.seqNumb}`"
                                :filter="clinicalCourseFilter"
                                dense
                                flat
                                bordered
                                v-model:pagination="clinicalCoursePagination"
                                class="q-mb-md"
                            >
                                <template #body-cell-status="slotProps">
                                    <q-td :props="slotProps">
                                        <q-badge
                                            :color="getStatusColor(slotProps.value)"
                                            :label="slotProps.value"
                                        />
                                    </q-td>
                                </template>
                            </q-table>

                            <ClinicalEffortPreviewTable
                                :rows="preview.clinicalEffort"
                                title="Effort Records"
                                show-status
                                :pagination="{ rowsPerPage: 5 }"
                            />
                        </q-tab-panel>
                    </q-tab-panels>
                </q-card-section>

                <!-- Actions -->
                <q-card-actions
                    align="right"
                    class="q-px-md q-pb-md"
                >
                    <q-btn
                        label="Cancel"
                        flat
                        @click="handleClose"
                    />
                    <q-btn
                        label="Confirm Harvest"
                        color="primary"
                        :disable="preview.errors.length > 0 || isCommitting"
                        @click="confirmHarvest"
                    />
                </q-card-actions>
            </template>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import { harvestService } from "../services/harvest-service"
import type { HarvestPreviewDto } from "../types"
import type { QTableColumn } from "quasar"
import { inflect } from "inflection"
import ClinicalEffortPreviewTable from "./ClinicalEffortPreviewTable.vue"

const props = defineProps<{
    modelValue: boolean
    termCode: number | null
    termName: string
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    harvested: []
}>()

const $q = useQuasar()

// Close handler for X button, Cancel button, or Escape key
function handleClose() {
    if (!isCommitting.value) {
        emit("update:modelValue", false)
    }
}

// Preview state
const preview = ref<HarvestPreviewDto | null>(null)
const isLoading = ref(false)
const loadError = ref<string | null>(null)
const isCommitting = ref(false)
const activeTab = ref("crest")

// Progress tracking
const harvestProgress = ref(0)
const harvestPhase = ref("")
const harvestDetail = ref("")

// Each table needs its own pagination object to avoid shared state
const crestInstructorPagination = ref({ rowsPerPage: 5 })
const crestCoursePagination = ref({ rowsPerPage: 5 })
const crestEffortPagination = ref({ rowsPerPage: 5 })
const nonCrestInstructorPagination = ref({ rowsPerPage: 5 })
const nonCrestCoursePagination = ref({ rowsPerPage: 5 })
const nonCrestEffortPagination = ref({ rowsPerPage: 5 })
const clinicalInstructorPagination = ref({ rowsPerPage: 5 })
const clinicalCoursePagination = ref({ rowsPerPage: 5 })

// Table filters
const crestInstructorFilter = ref("")
const crestCourseFilter = ref("")
const crestEffortFilter = ref("")
const nonCrestInstructorFilter = ref("")
const nonCrestCourseFilter = ref("")
const nonCrestEffortFilter = ref("")
const clinicalInstructorFilter = ref("")
const clinicalCourseFilter = ref("")

// Computed for removed items warning
const hasRemovedItems = computed(
    () => preview.value && (preview.value.removedInstructors.length > 0 || preview.value.removedCourses.length > 0),
)

// Table columns
const instructorColumns: QTableColumn[] = [
    { name: "fullName", label: "Name", field: "fullName", align: "left", sortable: true },
    { name: "department", label: "Dept", field: "department", align: "left", sortable: true },
    { name: "title", label: "Title", field: "titleDescription", align: "left" },
    {
        name: "status",
        label: "Status",
        field: (row) => (row.isNew ? "New" : "Exists"),
        align: "left",
        sortable: true,
    },
]

const courseColumns: QTableColumn[] = [
    {
        name: "course",
        label: "Course",
        field: (row) => `${row.subjCode}${row.crseNumb}`,
        align: "left",
        sortable: true,
    },
    { name: "crn", label: "CRN", field: "crn", align: "left" },
    { name: "enrollment", label: "Enrollment", field: "enrollment", align: "center" },
    { name: "units", label: "Units", field: "units", align: "center" },
    { name: "custDept", label: "Custodial Dept", field: "custDept", align: "left" },
    {
        name: "status",
        label: "Status",
        field: (row) => (row.source === "In CREST" ? "In CREST" : row.isNew ? "New" : "Exists"),
        align: "left",
        sortable: true,
    },
]

const effortColumns: QTableColumn[] = [
    { name: "personName", label: "Instructor", field: "personName", align: "left", sortable: true },
    { name: "courseCode", label: "Course", field: "courseCode", align: "left", sortable: true },
    { name: "effortType", label: "Type", field: "effortType", align: "left" },
    { name: "hours", label: "Hours", field: "hours", align: "center" },
    { name: "roleName", label: "Role", field: "roleName", align: "left" },
    {
        name: "status",
        label: "Status",
        field: (row) => (row.isNew ? "New" : "Exists"),
        align: "left",
        sortable: true,
    },
]

function getStatusColor(status: string): string {
    switch (status) {
        case "New":
            return "positive"
        case "Exists":
            return "grey-6"
        case "In CREST":
            return "info"
        default:
            return "grey"
    }
}

// Load preview when dialog opens
watch(
    () => props.modelValue,
    async (open) => {
        if (open && props.termCode) {
            await loadPreview()
        } else if (!open) {
            // Reset state when dialog closes
            preview.value = null
            loadError.value = null
            activeTab.value = "crest"
        }
    },
)

async function loadPreview() {
    if (!props.termCode) return

    isLoading.value = true
    loadError.value = null

    try {
        const result = await harvestService.getPreview(props.termCode)
        if (result) {
            preview.value = result
        } else {
            loadError.value = "Failed to load harvest preview"
        }
    } catch (err) {
        loadError.value = err instanceof Error ? err.message : "Failed to load harvest preview"
    } finally {
        isLoading.value = false
    }
}

function confirmHarvest() {
    if (!props.termCode || !preview.value) return

    const totalRecords =
        preview.value.summary.totalInstructors +
        preview.value.summary.totalCourses +
        preview.value.summary.totalEffortRecords

    $q.dialog({
        title: "Confirm Harvest",
        message: `This will replace all existing data for ${props.termName} with ${totalRecords.toLocaleString()} new records. This action cannot be undone. Are you sure you want to proceed?`,
        persistent: true,
        ok: {
            label: "Yes, Harvest",
            color: "primary",
        },
        cancel: {
            label: "Cancel",
            flat: true,
        },
    }).onOk(() => {
        commitHarvest()
    })
}

function commitHarvest() {
    if (!props.termCode || !preview.value) return

    isCommitting.value = true
    harvestProgress.value = 0
    harvestPhase.value = "Connecting..."
    harvestDetail.value = ""

    // Use SSE for real-time progress updates
    const streamUrl = harvestService.getStreamUrl(props.termCode)
    const eventSource = new EventSource(streamUrl, { withCredentials: true })

    eventSource.addEventListener("progress", (event) => {
        const data = JSON.parse(event.data) as {
            phase: string
            progress: number
            message: string
            detail?: string
        }
        harvestProgress.value = data.progress
        harvestPhase.value = data.message
        harvestDetail.value = data.detail ?? ""
    })

    eventSource.addEventListener("complete", (event) => {
        const data = JSON.parse(event.data) as {
            result: {
                success: boolean
                summary: { totalInstructors: number; totalCourses: number; totalEffortRecords: number }
                errorMessage?: string
            }
        }

        eventSource.close()
        harvestProgress.value = 1
        harvestPhase.value = "Complete!"
        harvestDetail.value = ""

        // Brief pause to show completion
        setTimeout(() => {
            if (data.result?.success) {
                $q.notify({
                    type: "positive",
                    message: `Harvest completed: ${data.result.summary.totalInstructors} instructors, ${data.result.summary.totalCourses} courses, ${data.result.summary.totalEffortRecords} effort records`,
                })
                emit("update:modelValue", false)
                emit("harvested")
            } else {
                $q.notify({
                    type: "negative",
                    message: data.result?.errorMessage ?? "Harvest failed",
                })
            }
            resetCommitState()
        }, 500)
    })

    eventSource.addEventListener("error", (event) => {
        // Check if it's a custom error event from our server
        if (event instanceof MessageEvent && event.data) {
            const data = JSON.parse(event.data) as { error?: string }
            $q.notify({
                type: "negative",
                message: data.error ?? "Harvest failed",
            })
        } else {
            // Connection error
            $q.notify({
                type: "negative",
                message: "Connection lost during harvest. Please check the term status.",
            })
        }
        eventSource.close()
        resetCommitState()
    })
}

function resetCommitState() {
    isCommitting.value = false
    harvestProgress.value = 0
    harvestPhase.value = ""
    harvestDetail.value = ""
}
</script>

<style scoped>
.compact-search {
    width: 10rem;
}

.compact-search :deep(.q-field__control) {
    height: 1.75rem;
    min-height: 1.75rem;
}

.compact-search :deep(.q-field__native) {
    font-size: 0.75rem;
    padding: 0;
}

.compact-search :deep(.q-field__prepend) {
    height: 1.75rem;
    padding-left: 0;
    padding-right: 0.25rem;
}

.compact-search :deep(.q-field__append) {
    height: 1.75rem;
    padding: 0 0.25rem;
}
</style>
