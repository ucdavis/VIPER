<template>
    <dl
        v-if="totals.length > 0"
        class="report-totals q-my-sm"
    >
        <div
            v-for="(total, index) in totals"
            :key="index"
            class="report-totals__item"
        >
            <dt>{{ total.label }}</dt>
            <dd>{{ formatReportTotal(total, columns) }}</dd>
        </div>
    </dl>
</template>

<script setup lang="ts">
import { formatReportTotal } from "./report-format"
import type { ReportColumnMetadata, ReportTotal } from "./report-types"

/** Labelled aggregates: a report's summary facts, a group's subtotals or the grand totals. */
defineProps<{
    totals: ReportTotal[]
    columns: ReportColumnMetadata[]
}>()
</script>

<style scoped>
.report-totals {
    display: flex;
    flex-wrap: wrap;
    gap: 0.25rem 1.5rem;
    margin-left: 0;
}

.report-totals__item {
    display: flex;
    gap: 0.5rem;
}

.report-totals dt {
    font-weight: 700;
}

.report-totals dt::after {
    content: ":";
}

.report-totals dd {
    margin: 0;
}
</style>
