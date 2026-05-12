<template>
    <AsyncOperationDialog
        :model-value="modelValue"
        title="Percent Assignment Rollover"
        subtitle="Roll forward percent assignments to the new academic year"
        max-width="900px"
        :is-loading="isLoading"
        :is-committing="isCommitting"
        :load-error="loadError"
        :progress="rolloverProgress"
        progress-title="Processing Rollover"
        :progress-phase="rolloverPhase"
        :progress-detail="rolloverDetail"
        @retry="loadPreview"
        @close="handleClose"
    >
        <template #before-body>
            <q-card-section
                v-if="isOutsideIdealMonths && !isLoading"
                class="q-py-sm"
            >
                <StatusBanner type="warning">
                    <div class="text-weight-medium">Unusual Timing</div>
                    <div class="text-caption text-grey-8">
                        Percent rollover is typically performed in June or July at the academic year boundary. The
                        current month is {{ currentMonthName }}.
                    </div>
                </StatusBanner>
            </q-card-section>
        </template>

        <template v-if="preview">
            <q-card-section class="q-pt-sm">
                <!-- Summary Banner -->
                <StatusBanner type="info">
                    <div class="row q-col-gutter-md">
                        <div class="col-4 text-center">
                            <div class="text-h5">{{ preview.assignments.length }}</div>
                            <div class="text-caption">To Roll Forward</div>
                        </div>
                        <div class="col-4 text-center">
                            <div class="text-h5">{{ preview.existingAssignments.length }}</div>
                            <div class="text-caption">Already Rolled</div>
                        </div>
                        <div class="col-4 text-center">
                            <div class="text-h5">{{ preview.excludedByAudit?.length ?? 0 }}</div>
                            <div class="text-caption">Excluded</div>
                        </div>
                    </div>
                    <div class="text-caption text-grey-7 q-mt-sm text-center">
                        Rolling from {{ preview.sourceAcademicYearDisplay }} to
                        {{ preview.targetAcademicYearDisplay }}
                    </div>
                </StatusBanner>

                <!-- Warning when nothing to rollover -->
                <StatusBanner
                    v-if="preview.assignments.length === 0"
                    type="warning"
                >
                    <div class="text-weight-medium">No assignments to roll forward</div>
                    <div class="text-caption text-grey-7">
                        There are no percent assignments ending on {{ formatDate(preview.oldEndDate) }} that need to be
                        rolled forward.
                    </div>
                </StatusBanner>

                <!-- Will be Rolled Forward Section (cyan, expanded by default) -->
                <q-expansion-item
                    v-if="preview.assignments.length > 0"
                    default-opened
                    class="q-mb-md"
                >
                    <template #header>
                        <div class="row items-center full-width">
                            <q-icon
                                name="event_repeat"
                                color="info"
                                size="24px"
                                class="q-mr-sm"
                            />
                            <div>
                                <div class="text-weight-medium">Will be rolled forward</div>
                                <div class="text-caption text-grey-7">
                                    {{ preview.assignments.length }}
                                    {{ inflect("assignment", preview.assignments.length) }} ending
                                    {{ formatDate(preview.oldEndDate) }} will be extended to
                                    {{ formatDate(preview.newEndDate) }}
                                </div>
                            </div>
                        </div>
                    </template>
                    <div class="bg-cyan-1 q-pa-sm">
                        <RolloverAssignmentTable :rows="preview.assignments" />
                    </div>
                </q-expansion-item>

                <!-- Already Rolled Section (grey, collapsed by default, shows first 10 with Show all option) -->
                <q-expansion-item
                    v-if="preview.existingAssignments.length > 0"
                    class="q-mb-md"
                >
                    <template #header>
                        <div class="row items-center full-width">
                            <q-icon
                                name="check_circle"
                                color="grey-6"
                                size="24px"
                                class="q-mr-sm"
                            />
                            <div>
                                <div class="text-weight-medium text-grey-7">Already rolled</div>
                                <div class="text-caption text-grey-6">
                                    {{ preview.existingAssignments.length }}
                                    {{ inflect("assignment", preview.existingAssignments.length) }} already have a
                                    successor in {{ preview.targetAcademicYearDisplay }}
                                </div>
                            </div>
                        </div>
                    </template>
                    <div class="bg-grey-2 q-pa-sm">
                        <RolloverAssignmentTable :rows="preview.existingAssignments" />
                    </div>
                </q-expansion-item>

                <!-- Excluded by Audit Section (orange, collapsed by default) -->
                <q-expansion-item
                    v-if="preview.excludedByAudit?.length > 0"
                    class="q-mb-md"
                >
                    <template #header>
                        <div class="row items-center full-width">
                            <q-icon
                                name="block"
                                color="warning"
                                size="24px"
                                class="q-mr-sm"
                            />
                            <div>
                                <div class="text-weight-medium text-orange-9">Excluded - Post-Harvest Changes</div>
                                <div class="text-caption text-grey-7">
                                    {{ preview.excludedByAudit.length }}
                                    {{ inflect("assignment", preview.excludedByAudit.length) }} excluded due to manual
                                    edits/deletes after harvest
                                </div>
                            </div>
                        </div>
                    </template>
                    <div class="bg-orange-1 q-pa-sm">
                        <StatusBanner type="warning">
                            These assignments will not be rolled forward because someone manually edited or deleted a
                            percent assignment of the same type for this instructor after the term was harvested.
                        </StatusBanner>
                        <RolloverAssignmentTable :rows="preview.excludedByAudit" />
                    </div>
                </q-expansion-item>
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
                    label="Confirm Rollover"
                    color="primary"
                    :disable="preview.assignments.length === 0 || isCommitting"
                    @click="confirmRollover"
                />
            </q-card-actions>
        </template>
    </AsyncOperationDialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import { rolloverService } from "../services/rollover-service"
import type { PercentRolloverPreviewDto } from "../types"
import StatusBanner from "@/components/StatusBanner.vue"
import AsyncOperationDialog from "./AsyncOperationDialog.vue"
import { inflect } from "inflection"
import { useDateFunctions } from "@/composables/DateFunctions"
import RolloverAssignmentTable from "./RolloverAssignmentTable.vue"

const props = defineProps<{
    modelValue: boolean
    year: number
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    completed: []
}>()

const $q = useQuasar()
const { formatDate } = useDateFunctions()

function handleClose() {
    if (!isCommitting.value) {
        emit("update:modelValue", false)
    }
}

// June/July warning
const isOutsideIdealMonths = computed(() => {
    const month = new Date().getMonth() // 0-indexed
    return month !== 5 && month !== 6 // Not June (5) or July (6)
})

const currentMonthName = computed(() => {
    return new Date().toLocaleString("default", { month: "long" })
})

// Preview state
const preview = ref<PercentRolloverPreviewDto | null>(null)
const isLoading = ref(false)
const loadError = ref<string | null>(null)
const isCommitting = ref(false)

// Progress tracking
const rolloverProgress = ref(0)
const rolloverPhase = ref("")
const rolloverDetail = ref("")

// Load preview when dialog opens
watch(
    () => props.modelValue,
    async (open) => {
        if (open && props.year) {
            await loadPreview()
        } else if (!open) {
            // Reset state when dialog closes
            preview.value = null
            loadError.value = null
        }
    },
)

async function loadPreview() {
    if (!props.year) return

    isLoading.value = true
    loadError.value = null

    try {
        const result = await rolloverService.getPreview(props.year)
        if (result) {
            preview.value = result
        } else {
            loadError.value = "Failed to load rollover preview"
        }
    } catch (err) {
        loadError.value = err instanceof Error ? err.message : "Failed to load rollover preview"
    } finally {
        isLoading.value = false
    }
}

function confirmRollover() {
    if (!props.year || !preview.value || preview.value.assignments.length === 0) return

    const doRollover = () => {
        $q.dialog({
            title: "Confirm Percent Rollover",
            message: `This will create ${preview.value!.assignments.length} new percent assignment(s) for ${preview.value!.targetAcademicYearDisplay}. Are you sure you want to proceed?`,
            persistent: true,
            ok: {
                label: "Yes, Roll Forward",
                color: "primary",
            },
            cancel: {
                label: "Cancel",
                flat: true,
            },
        }).onOk(() => {
            commitRollover()
        })
    }

    if (isOutsideIdealMonths.value) {
        $q.dialog({
            title: "Unusual Timing",
            message: `It is currently ${currentMonthName.value}. Percent rollover is typically performed in June or July at the academic year boundary. Do you want to continue?`,
            persistent: true,
            ok: {
                label: "Continue",
                color: "warning",
            },
            cancel: {
                label: "Cancel",
                flat: true,
            },
        }).onOk(doRollover)
    } else {
        doRollover()
    }
}

function commitRollover() {
    if (!props.year || !preview.value) return

    isCommitting.value = true
    rolloverProgress.value = 0
    rolloverPhase.value = "Connecting..."
    rolloverDetail.value = ""

    // Use SSE for real-time progress updates
    const streamUrl = rolloverService.getStreamUrl(props.year)
    const eventSource = new EventSource(streamUrl, { withCredentials: true })

    eventSource.addEventListener("progress", (event) => {
        const data = JSON.parse(event.data) as {
            phase: string
            progress: number
            message: string
            detail?: string
        }
        rolloverProgress.value = data.progress
        rolloverPhase.value = data.message
        rolloverDetail.value = data.detail ?? ""
    })

    eventSource.addEventListener("complete", (event) => {
        const data = JSON.parse(event.data) as {
            result: {
                success: boolean
                assignmentsCreated: number
                errorMessage?: string
                sourceAcademicYear: string
                targetAcademicYear: string
            }
        }

        eventSource.close()
        rolloverProgress.value = 1
        rolloverPhase.value = "Complete!"
        rolloverDetail.value = ""

        // Brief pause to show completion
        setTimeout(() => {
            if (data.result?.success) {
                $q.notify({
                    type: "positive",
                    message: `Rollover completed: ${data.result.assignmentsCreated} percent assignment(s) created for ${data.result.targetAcademicYear}`,
                })
                emit("update:modelValue", false)
                emit("completed")
            } else {
                $q.notify({
                    type: "negative",
                    message: data.result?.errorMessage ?? "Rollover failed",
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
                message: data.error ?? "Rollover failed",
            })
        } else {
            // Connection error
            $q.notify({
                type: "negative",
                message: "Connection lost during rollover. Please check the term status.",
            })
        }
        eventSource.close()
        resetCommitState()
    })
}

function resetCommitState() {
    isCommitting.value = false
    rolloverProgress.value = 0
    rolloverPhase.value = ""
    rolloverDetail.value = ""
}
</script>
