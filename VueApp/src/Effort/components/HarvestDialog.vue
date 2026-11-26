<template>
    <AsyncOperationDialog
        :model-value="modelValue"
        :title="`Harvest Term: ${props.termName}`"
        subtitle="Import instructors and courses from CREST and Clinical Scheduler"
        :is-loading="isLoading"
        :is-committing="isCommitting"
        :load-error="loadError"
        :progress="harvestProgress"
        progress-title="Processing Harvest"
        :progress-phase="harvestPhase"
        :progress-detail="harvestDetail"
        @retry="loadPreview"
        @close="handleClose"
    >
        <template v-if="preview">
            <q-card-section class="q-pt-sm">
                <!-- Summary Banner -->
                <StatusBanner type="info">
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
                </StatusBanner>

                <!-- Warnings -->
                <StatusBanner
                    v-if="preview.warnings.length > 0"
                    type="warning"
                >
                    <div class="row items-center q-mb-xs">
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
                </StatusBanner>

                <!-- Errors -->
                <StatusBanner
                    v-if="preview.errors.length > 0"
                    type="error"
                >
                    <div class="row items-center q-mb-xs">
                        <span class="text-weight-medium text-negative">
                            {{ preview.errors.length }} {{ inflect("Error", preview.errors.length) }} - Harvest may fail
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
                </StatusBanner>

                <!-- Removed Items Warning -->
                <StatusBanner
                    v-if="hasRemovedItems"
                    type="warning"
                    icon="person_remove"
                >
                    <div class="row items-center q-mb-xs">
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
                </StatusBanner>

                <!-- Tabs -->
                <q-tabs
                    v-model="activeTab"
                    dense
                    align="left"
                    class="text-grey-8 tabs-no-fade"
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
                    <q-tab
                        name="crosslist"
                        :label="`Cross-Lists (${preview.crossListLinks.length})`"
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
                        <HarvestPreviewTable
                            title="Instructors"
                            :rows="preview.crestInstructors"
                            :columns="instructorColumns"
                            row-key="personId"
                        />
                        <HarvestPreviewTable
                            title="Courses"
                            :rows="preview.crestCourses"
                            :columns="courseColumns"
                            row-key="crn"
                        />
                        <HarvestPreviewTable
                            title="Effort Records"
                            :rows="preview.crestEffort"
                            :columns="effortColumns"
                            :row-key="(row) => `${row.mothraId}-${row.crn}-${row.effortType}`"
                        />
                    </q-tab-panel>

                    <!-- Non-CREST Tab -->
                    <q-tab-panel
                        name="noncrest"
                        class="q-pa-none q-pt-md"
                    >
                        <HarvestPreviewTable
                            title="Instructors"
                            :rows="preview.nonCrestInstructors"
                            :columns="instructorColumns"
                            row-key="personId"
                        />
                        <HarvestPreviewTable
                            title="Courses"
                            :rows="preview.nonCrestCourses"
                            :columns="courseColumns"
                            row-key="crn"
                        />
                        <HarvestPreviewTable
                            title="Effort Records"
                            :rows="preview.nonCrestEffort"
                            :columns="effortColumns"
                            :row-key="(row) => `${row.mothraId}-${row.crn}-${row.effortType}`"
                        />
                    </q-tab-panel>

                    <!-- Clinical Tab -->
                    <q-tab-panel
                        name="clinical"
                        class="q-pa-none q-pt-md"
                    >
                        <HarvestPreviewTable
                            title="Instructors"
                            :rows="preview.clinicalInstructors"
                            :columns="instructorColumns"
                            row-key="mothraId"
                        />
                        <HarvestPreviewTable
                            title="Courses"
                            :rows="preview.clinicalCourses"
                            :columns="courseColumns"
                            :row-key="(row) => `${row.subjCode}-${row.crseNumb}-${row.seqNumb}`"
                        />

                        <ClinicalEffortPreviewTable
                            :rows="preview.clinicalEffort"
                            title="Effort Records"
                            show-status
                            :pagination="{ rowsPerPage: 5 }"
                        />
                    </q-tab-panel>

                    <!-- Cross-Lists Tab -->
                    <q-tab-panel
                        name="crosslist"
                        class="q-pa-none q-pt-md"
                    >
                        <div class="text-caption text-grey-7 q-mb-sm">
                            Cross-listed courses from Banner are linked automatically. Effort is entered on the parent
                            course only, so effort harvested onto a child course moves to its parent. Links added by
                            hand are removed by a harvest.
                        </div>
                        <q-table
                            :rows="preview.crossListLinks"
                            :columns="crossListColumns"
                            row-key="childCourse"
                            dense
                            flat
                            bordered
                            no-data-label="Banner lists no cross-listed courses for this term"
                            v-model:pagination="crossListPagination"
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
    </AsyncOperationDialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import { harvestService } from "../services/harvest-service"
import type { HarvestPreviewDto } from "../types"
import type { QTableColumn } from "quasar"
import HarvestPreviewTable from "./HarvestPreviewTable.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import AsyncOperationDialog from "./AsyncOperationDialog.vue"
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

const crossListPagination = ref({ rowsPerPage: 10 })

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

const crossListColumns: QTableColumn[] = [
    { name: "parentCourse", label: "Parent", field: "parentCourse", align: "left", sortable: true },
    { name: "childCourse", label: "Child", field: "childCourse", align: "left", sortable: true },
    {
        name: "relationshipType",
        label: "Type",
        field: (row) => (row.relationshipType === "CrossList" ? "Cross-List" : row.relationshipType),
        align: "left",
        sortable: true,
    },
    { name: "reason", label: "Parent chosen by", field: "reason", align: "left" },
]

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
