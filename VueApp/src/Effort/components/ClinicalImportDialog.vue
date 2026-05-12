<template>
    <AsyncOperationDialog
        :model-value="modelValue"
        :title="`Import Clinical Effort: ${props.termName}`"
        subtitle="Import clinical rotation assignments from Clinical Scheduler"
        :is-loading="isLoading"
        :is-committing="isCommitting"
        :load-error="loadError"
        :progress="importProgress"
        progress-title="Processing Clinical Import"
        progress-color="info"
        :progress-phase="importPhase"
        :progress-detail="importDetail"
        @retry="loadPreview"
        @close="handleClose"
    >
        <template v-if="preview">
            <q-card-section class="q-pt-sm">
                <!-- Import Mode Selector -->
                <div class="q-mb-md">
                    <div class="text-subtitle2 q-mb-sm">Import Mode</div>
                    <q-option-group
                        v-model="selectedMode"
                        :options="modeOptions"
                        color="info"
                        :inline="$q.screen.gt.xs"
                        @update:model-value="onModeChange"
                    />
                </div>

                <!-- Summary Banner -->
                <StatusBanner type="info">
                    <div class="row q-col-gutter-md">
                        <div class="col-6 col-sm-3 text-center">
                            <div class="text-h5 text-positive">{{ preview.addCount }}</div>
                            <div class="text-caption">To add</div>
                        </div>
                        <div class="col-6 col-sm-3 text-center">
                            <div class="text-h5 text-info">{{ preview.updateCount }}</div>
                            <div class="text-caption">To update</div>
                        </div>
                        <div class="col-6 col-sm-3 text-center">
                            <div
                                class="text-h5"
                                :class="preview.deleteCount > 0 ? 'text-negative' : ''"
                            >
                                {{ preview.deleteCount }}
                            </div>
                            <div class="text-caption">To delete</div>
                        </div>
                        <div class="col-6 col-sm-3 text-center">
                            <div class="text-h5 text-grey-6">{{ preview.skipCount }}</div>
                            <div class="text-caption">To skip</div>
                        </div>
                    </div>
                    <div class="text-caption text-grey-7 q-mt-sm text-center">
                        Data as of {{ formatPreviewDateTime(preview.previewGeneratedAt) }}
                    </div>
                </StatusBanner>

                <!-- Warnings Section -->
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
                            v-for="(warning, idx) in preview.warnings"
                            :key="idx"
                        >
                            {{ warning }}
                        </li>
                    </ul>
                </StatusBanner>

                <!-- Delete Warning for Sync Mode with Empty Source -->
                <StatusBanner
                    v-if="selectedMode === 'Sync' && preview.addCount === 0 && preview.deleteCount > 0"
                    type="error"
                    icon="dangerous"
                >
                    <div class="row items-center q-mb-xs">
                        <span class="text-weight-medium text-negative"
                            >Sync will delete ALL clinical effort records</span
                        >
                    </div>
                    <div class="text-caption text-grey-8">
                        The source returned 0 records. Syncing will delete all {{ preview.deleteCount }} existing
                        clinical effort records for this term.
                    </div>
                </StatusBanner>

                <!-- Delete Warning Banner -->
                <StatusBanner
                    v-else-if="preview.deleteCount > 0"
                    type="error"
                >
                    <div class="row items-center q-mb-xs">
                        <span class="text-weight-medium text-negative">
                            {{ preview.deleteCount }} {{ inflect("record", preview.deleteCount) }} will be deleted
                        </span>
                    </div>
                    <div class="text-caption text-grey-7">
                        <template v-if="selectedMode === 'ClearReplace'">
                            Clear & Replace mode will delete all existing clinical records before importing.
                        </template>
                        <template v-else>
                            Sync mode will remove clinical records that no longer exist in the source.
                        </template>
                    </div>
                </StatusBanner>

                <!-- Nothing to Import Warning -->
                <StatusBanner
                    v-if="totalChanges === 0"
                    type="warning"
                >
                    <div class="row items-center q-mb-xs">
                        <span class="text-weight-medium">Nothing to import</span>
                    </div>
                    <div class="text-caption text-grey-7">
                        There are no changes to make with the selected import mode.
                    </div>
                </StatusBanner>

                <!-- Preview Table -->
                <ClinicalEffortPreviewTable
                    v-if="preview.assignments.length > 0"
                    :rows="preview.assignments"
                    title="Preview"
                    show-status
                    class="q-mb-md"
                />
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
                    label="Confirm Import"
                    color="info"
                    :disable="totalChanges === 0 || isCommitting"
                    @click="confirmImport"
                />
            </q-card-actions>
        </template>
    </AsyncOperationDialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import { clinicalService } from "../services/clinical-service"
import type { ClinicalImportPreviewDto, ClinicalImportMode } from "../types"
import { inflect } from "inflection"
import StatusBanner from "@/components/StatusBanner.vue"
import AsyncOperationDialog from "./AsyncOperationDialog.vue"
import ClinicalEffortPreviewTable from "./ClinicalEffortPreviewTable.vue"

const props = defineProps<{
    modelValue: boolean
    termCode: number | null
    termName: string
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    completed: []
}>()

const $q = useQuasar()

// Format datetime with timezone abbreviation (e.g., "Feb 4, 2026 2:35 PM PST")
const dateTimeOptions: Intl.DateTimeFormatOptions = {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
    timeZoneName: "short",
}

function formatPreviewDateTime(d: string): string {
    if (!d) return ""
    const dt = new Date(d)
    return dt instanceof Date && !Number.isNaN(dt.valueOf()) ? dt.toLocaleString("en-US", dateTimeOptions) : ""
}

function handleClose() {
    if (!isCommitting.value) {
        emit("update:modelValue", false)
    }
}

// Import mode options
const modeOptions = [
    {
        value: "AddNewOnly" as ClinicalImportMode,
        label: "Add new only",
    },
    {
        value: "ClearReplace" as ClinicalImportMode,
        label: "Clear & replace",
    },
    {
        value: "Sync" as ClinicalImportMode,
        label: "Sync",
    },
]

// Preview state
const preview = ref<ClinicalImportPreviewDto | null>(null)
const selectedMode = ref<ClinicalImportMode>("AddNewOnly")
const isLoading = ref(false)
const loadError = ref<string | null>(null)
const isCommitting = ref(false)

// Progress tracking
const importProgress = ref(0)
const importPhase = ref("")
const importDetail = ref("")

// Computed total changes (excluding skipped)
const totalChanges = computed(() => {
    if (!preview.value) return 0
    return preview.value.addCount + preview.value.updateCount + preview.value.deleteCount
})

// Load preview when dialog opens
watch(
    () => props.modelValue,
    async (open) => {
        if (open && props.termCode) {
            selectedMode.value = "AddNewOnly"
            await loadPreview()
        } else if (!open) {
            preview.value = null
            loadError.value = null
        }
    },
)

async function onModeChange() {
    await loadPreview()
}

async function loadPreview() {
    if (!props.termCode) return

    isLoading.value = true
    loadError.value = null

    try {
        const result = await clinicalService.getPreview(props.termCode, selectedMode.value)
        if (result) {
            preview.value = result
        } else {
            loadError.value = "Failed to load clinical import preview"
        }
    } catch (err) {
        loadError.value = err instanceof Error ? err.message : "Failed to load clinical import preview"
    } finally {
        isLoading.value = false
    }
}

function confirmImport() {
    if (!props.termCode || !preview.value || totalChanges.value === 0) return

    // Build confirmation message
    const parts: string[] = []
    if (preview.value.addCount > 0) {
        parts.push(`add ${preview.value.addCount}`)
    }
    if (preview.value.updateCount > 0) {
        parts.push(`update ${preview.value.updateCount}`)
    }
    if (preview.value.deleteCount > 0) {
        parts.push(`delete ${preview.value.deleteCount}`)
    }

    const hasDeletes = preview.value.deleteCount > 0
    const modeDescription =
        selectedMode.value === "ClearReplace"
            ? "This will delete ALL existing clinical records before importing."
            : selectedMode.value === "Sync"
              ? "This will sync clinical data with the source, including deletions."
              : ""

    $q.dialog({
        title: hasDeletes ? "Confirm Clinical Import (Deletes Pending)" : "Confirm Clinical Import",
        message: `This will ${parts.join(", ")} clinical effort record(s). ${modeDescription} Are you sure you want to proceed?`,
        persistent: true,
        ok: {
            label: hasDeletes ? "Yes, Import with Deletes" : "Yes, Import",
            color: hasDeletes ? "negative" : "teal-8",
        },
        cancel: {
            label: "Cancel",
            flat: true,
        },
    }).onOk(() => {
        commitImport()
    })
}

function commitImport() {
    if (!props.termCode || !preview.value) return

    isCommitting.value = true
    importProgress.value = 0
    importPhase.value = "Connecting..."
    importDetail.value = ""

    // Use SSE for real-time progress updates
    const streamUrl = clinicalService.getStreamUrl(props.termCode, selectedMode.value)
    const eventSource = new EventSource(streamUrl, { withCredentials: true })

    // Handle progress events (server emits "preparing", "importing", "finalizing")
    const handleProgress = (event: MessageEvent) => {
        const data = JSON.parse(event.data) as {
            phase: string
            progress: number
            message: string
            detail?: string
        }
        importProgress.value = data.progress
        importPhase.value = data.message
        importDetail.value = data.detail ?? ""
    }

    eventSource.addEventListener("preparing", handleProgress)
    eventSource.addEventListener("importing", handleProgress)
    eventSource.addEventListener("finalizing", handleProgress)

    eventSource.addEventListener("complete", (event) => {
        const data = JSON.parse(event.data) as {
            result: {
                success: boolean
                recordsAdded: number
                recordsUpdated: number
                recordsDeleted: number
                recordsSkipped: number
                errorMessage?: string
            }
        }

        eventSource.close()
        importProgress.value = 1
        importPhase.value = "Complete!"
        importDetail.value = ""

        // Brief pause to show completion
        setTimeout(() => {
            if (data.result?.success) {
                const summary = []
                if (data.result.recordsAdded > 0) summary.push(`${data.result.recordsAdded} added`)
                if (data.result.recordsUpdated > 0) summary.push(`${data.result.recordsUpdated} updated`)
                if (data.result.recordsDeleted > 0) summary.push(`${data.result.recordsDeleted} deleted`)
                if (data.result.recordsSkipped > 0) summary.push(`${data.result.recordsSkipped} skipped`)

                $q.notify({
                    type: "positive",
                    message: `Clinical import completed: ${summary.join(", ")}`,
                })
                emit("update:modelValue", false)
                emit("completed")
            } else {
                $q.notify({
                    type: "negative",
                    message: data.result?.errorMessage ?? "Clinical import failed",
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
                message: data.error ?? "Clinical import failed",
            })
        } else {
            // Connection error
            $q.notify({
                type: "negative",
                message: "Connection lost during import. Please check the term status.",
            })
        }
        eventSource.close()
        resetCommitState()
    })
}

function resetCommitState() {
    isCommitting.value = false
    importProgress.value = 0
    importPhase.value = ""
    importDetail.value = ""
}
</script>
