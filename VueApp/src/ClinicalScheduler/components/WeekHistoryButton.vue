<template>
    <q-btn
        flat
        round
        dense
        size="sm"
        icon="history"
        color="grey-7"
        class="week-history-btn"
        :aria-label="`View audit trail for week ${weekNumber}`"
        @click.stop="onTriggerClick"
    >
        <q-tooltip :delay="500">Audit Trail</q-tooltip>

        <!-- Desktop: anchored popover -->
        <q-menu
            v-if="!useDialog"
            anchor="bottom middle"
            self="top middle"
            :offset="[0, 6]"
            @show="loadHistory"
        >
            <WeekHistoryContent
                :title-id="titleId"
                :view-mode="viewMode"
                :week-number="weekNumber"
                :week-date-start="weekDateStart"
                :context-label="contextLabel"
                :entries="entries"
                :is-loading="isLoading"
                :error="error"
                @retry="loadHistory"
            />
        </q-menu>
    </q-btn>

    <!-- Mobile: centered dialog (an anchored popover can flip off-screen) -->
    <q-dialog
        v-if="useDialog"
        v-model="dialogOpen"
        :aria-labelledby="titleId"
    >
        <q-card class="week-history-dialog-card">
            <WeekHistoryContent
                fluid
                :title-id="titleId"
                :view-mode="viewMode"
                :week-number="weekNumber"
                :week-date-start="weekDateStart"
                :context-label="contextLabel"
                :entries="entries"
                :is-loading="isLoading"
                :error="error"
                @retry="loadHistory"
            />
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { computed, ref, useId } from "vue"
import { useQuasar } from "quasar"
import { AuditLogService } from "../services/audit-log-service"
import { useAuditEntries } from "../composables/use-audit-entries"
import WeekHistoryContent from "./WeekHistoryContent.vue"

const props = defineProps<{
    /** "rotation" reads rotation+week history; "clinician" reads clinician+week history */
    viewMode: "rotation" | "clinician"
    /** Rotation id (rotation view) or clinician MothraID (clinician view) */
    contextId: number | string
    weekId: number
    weekNumber: number
    weekDateStart: string
    /** Rotation name or clinician name, shown in the popover header */
    contextLabel: string
}>()

const $q = useQuasar()
const useDialog = computed(() => $q.screen.lt.md)

const titleId = `week-history-title-${useId()}`

const dialogOpen = ref(false)
const { entries, isLoading, error, load } = useAuditEntries()

// The desktop popover opens itself via @show; only the dialog needs an explicit open.
function onTriggerClick() {
    if (!useDialog.value) return
    dialogOpen.value = true
    loadHistory()
}

// Lazy-load each time the surface opens so it always reflects the latest grid edits.
async function loadHistory() {
    await load(() =>
        props.viewMode === "rotation"
            ? AuditLogService.getRotationWeekHistory(Number(props.contextId), props.weekId)
            : AuditLogService.getClinicianWeekHistory(String(props.contextId), props.weekId),
    )
}
</script>

<style scoped>
.week-history-btn {
    flex-shrink: 0;

    /* >=24px hit area (WCAG 2.5.8) so a near-miss tap doesn't fall through to the cell */
    min-width: 1.75rem;
    min-height: 1.75rem;
}

.week-history-dialog-card {
    /* Fixed width, not vw, which would overflow the dialog inset and clip the close button */
    width: 22rem;
    max-width: 100%;
}
</style>
