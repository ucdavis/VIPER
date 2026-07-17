<template>
    <!-- Centered (Quasar default) so it always opens in the same place, not anchored to the icon -->
    <q-dialog
        v-model="open"
        :aria-labelledby="titleId"
    >
        <q-card class="week-history-dialog-card">
            <WeekHistoryContent
                v-if="activeWeek"
                fluid
                :title-id="titleId"
                :view-mode="viewMode"
                :week-number="activeWeek.weekNumber"
                :week-date-start="activeWeek.dateStart"
                :context-label="contextLabel"
                :entries="entries"
                :is-loading="isLoading"
                :error="error"
                :can-prev="canPrev"
                :can-next="canNext"
                @prev="goPrev"
                @next="goNext"
                @retry="loadHistory"
            />
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { computed, ref, useId, watch } from "vue"
import { onKeyStroke } from "@vueuse/core"
import { AuditLogService } from "../services/audit-log-service"
import { useAuditEntries } from "../composables/use-audit-entries"
import WeekHistoryContent from "./WeekHistoryContent.vue"
import type { WeekItem } from "./schedule-view-types"

const props = defineProps<{
    /** "rotation" reads rotation+week history; "clinician" reads clinician+week history */
    viewMode: "rotation" | "clinician"
    /** Rotation id (rotation view) or clinician MothraID (clinician view) */
    contextId: number | string | null
    /** Rotation name or clinician name, shown in the header */
    contextLabel: string
    /** Every week in schedule order; Previous/Next page through the whole list */
    weeks: WeekItem[]
    /** Week whose icon was clicked; the dialog opens on this week */
    startWeekId: number | null
}>()

const open = defineModel<boolean>({ required: true })

const titleId = `week-history-title-${useId()}`

// Index into props.weeks of the shown week; re-seeded on open, then moved by the nav.
const activeIndex = ref(0)
const activeWeek = computed<WeekItem | null>(() => props.weeks[activeIndex.value] ?? null)
const canPrev = computed(() => activeIndex.value > 0)
const canNext = computed(() => activeIndex.value < props.weeks.length - 1)

const { entries, isLoading, error, load, reset } = useAuditEntries()

// Seed + load on open (not @show, which fires post-transition) so the first frame is
// the right week loading, not a flash of the empty/wrong-week box.
watch(open, (isOpen) => {
    if (!isOpen) return
    const idx = props.weeks.findIndex((w) => w.weekId === props.startWeekId)
    activeIndex.value = idx >= 0 ? idx : 0
    reset()
    loadHistory()
})

// Reload on each open/week change so it reflects the latest grid edits.
async function loadHistory() {
    const week = activeWeek.value
    if (!week || props.contextId === null) return
    await load(() =>
        props.viewMode === "rotation"
            ? AuditLogService.getRotationWeekHistory(Number(props.contextId), week.weekId)
            : AuditLogService.getClinicianWeekHistory(String(props.contextId), week.weekId),
    )
}

function goPrev() {
    if (!canPrev.value) return
    activeIndex.value -= 1
    loadHistory()
}

function goNext() {
    if (!canNext.value) return
    activeIndex.value += 1
    loadHistory()
}

// Arrow keys page weeks while open (guarded so they're inert when closed).
onKeyStroke("ArrowLeft", (e) => {
    if (!open.value) return
    e.preventDefault()
    goPrev()
})
onKeyStroke("ArrowRight", (e) => {
    if (!open.value) return
    e.preventDefault()
    goNext()
})
</script>

<style scoped>
.week-history-dialog-card {
    /* Fixed width, not vw, which would overflow the dialog inset and clip the close button */
    width: 22rem;
    max-width: 100%;
}
</style>
