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
                <div class="text-h6">Import Clinical Effort: {{ props.termName }}</div>
                <div class="text-caption text-grey-7">Import clinical rotation assignments from Clinical Scheduler</div>
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
                <div class="text-h6 q-mb-md text-center">Processing Clinical Import</div>
                <q-linear-progress
                    :value="importProgress"
                    size="25px"
                    color="teal-8"
                    class="q-mb-md"
                >
                    <div class="absolute-full flex flex-center">
                        <q-badge
                            color="white"
                            text-color="teal-8"
                            :label="`${Math.round(importProgress * 100)}%`"
                        />
                    </div>
                </q-linear-progress>
                <div class="text-center text-grey-7">{{ importPhase }}</div>
                <div
                    v-if="importDetail"
                    class="text-center text-caption text-grey-6 q-mt-xs"
                >
                    {{ importDetail }}
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
                    <!-- Import Mode Selector -->
                    <div class="q-mb-md">
                        <div class="text-subtitle2 q-mb-sm">Import Mode</div>
                        <q-option-group
                            v-model="selectedMode"
                            :options="modeOptions"
                            color="teal-8"
                            :inline="$q.screen.gt.xs"
                            @update:model-value="onModeChange"
                        />
                    </div>

                    <!-- Summary Banner -->
                    <q-banner
                        class="bg-blue-1 q-mb-md"
                        rounded
                    >
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
                    </q-banner>

                    <!-- Warnings Section -->
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
                                v-for="(warning, idx) in preview.warnings"
                                :key="idx"
                            >
                                {{ warning }}
                            </li>
                        </ul>
                    </q-banner>

                    <!-- Delete Warning for Sync Mode with Empty Source -->
                    <q-banner
                        v-if="selectedMode === 'Sync' && preview.addCount === 0 && preview.deleteCount > 0"
                        class="bg-red-1 q-mb-md"
                        rounded
                    >
                        <div class="row items-center q-mb-xs">
                            <q-icon
                                name="dangerous"
                                color="negative"
                                size="sm"
                                class="q-mr-sm"
                            />
                            <span class="text-weight-medium text-negative"
                                >Sync will delete ALL clinical effort records</span
                            >
                        </div>
                        <div class="text-caption text-grey-8">
                            The source returned 0 records. Syncing will delete all {{ preview.deleteCount }} existing
                            clinical effort records for this term.
                        </div>
                    </q-banner>

                    <!-- Delete Warning Banner -->
                    <q-banner
                        v-else-if="preview.deleteCount > 0"
                        class="bg-red-1 q-mb-md"
                        rounded
                    >
                        <div class="row items-center q-mb-xs">
                            <q-icon
                                name="warning"
                                color="negative"
                                size="sm"
                                class="q-mr-sm"
                            />
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
                    </q-banner>

                    <!-- Nothing to Import Warning -->
                    <q-banner
                        v-if="totalChanges === 0"
                        class="bg-orange-1 q-mb-md"
                        rounded
                    >
                        <div class="row items-center q-mb-xs">
                            <q-icon
                                name="info"
                                color="orange"
                                size="sm"
                                class="q-mr-sm"
                            />
                            <span class="text-weight-medium">Nothing to import</span>
                        </div>
                        <div class="text-caption text-grey-7">
                            There are no changes to make with the selected import mode.
                        </div>
                    </q-banner>

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
                        color="teal-8"
                        :disable="totalChanges === 0 || isCommitting"
                        @click="confirmImport"
                    />
                </q-card-actions>
            </template>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import { clinicalService } from "../services/clinical-service"
import type { ClinicalImportPreviewDto, ClinicalImportMode } from "../types"
import { inflect } from "inflection"
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
