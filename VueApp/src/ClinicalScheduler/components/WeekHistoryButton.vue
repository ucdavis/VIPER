<template>
    <!-- Trigger only; the shared WeekHistoryDialog in ScheduleView owns the modal -->
    <q-btn
        flat
        round
        dense
        size="sm"
        icon="history"
        color="grey-7"
        class="week-history-btn"
        :aria-label="`View audit trail for week ${weekNumber}`"
        @click.stop="$emit('view-history')"
    >
        <!-- Hover only: on touch, the opening tap also shows the tooltip and it lingers -->
        <q-tooltip
            v-if="supportsHover"
            :delay="500"
            >Audit Trail</q-tooltip
        >
    </q-btn>
</template>

<script setup lang="ts">
import { useMediaQuery } from "@vueuse/core"

defineProps<{
    /** Week number, used only to label the trigger for assistive tech */
    weekNumber: number
}>()

defineEmits<{ "view-history": [] }>()

// Tooltips are a hover affordance; render them only when the primary input can hover
const supportsHover = useMediaQuery("(hover: hover)")
</script>

<style scoped>
.week-history-btn {
    flex-shrink: 0;

    /* >=24px hit area (WCAG 2.5.8) so a near-miss tap doesn't fall through to the cell */
    min-width: 1.75rem;
    min-height: 1.75rem;
}
</style>
