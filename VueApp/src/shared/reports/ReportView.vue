<template>
    <div class="report-view">
        <p class="text-grey q-mb-sm">{{ formatGeneratedAt(result.generatedAt) }} &middot; {{ rowCountText }}</p>
        <ReportTotals
            :totals="result.summary"
            :columns="result.columns"
        />

        <StatusBanner
            v-if="result.rowCount === 0"
            type="info"
        >
            No rows match these parameters.
        </StatusBanner>

        <template v-else>
            <template
                v-for="(group, index) in result.groups"
                :key="index"
            >
                <h2
                    v-if="group.label !== null"
                    class="q-mt-lg"
                >
                    {{ group.label }}
                </h2>
                <ReportGroupTable
                    :result="result"
                    :rows="group.rows"
                />
                <ReportTotals
                    :totals="group.subtotals"
                    :columns="result.columns"
                />
                <template
                    v-for="chart in group.charts"
                    :key="chart.title"
                >
                    <h3>{{ chart.title }}</h3>
                    <ReportNumberTable :table="chartTable(chart)" />
                </template>
            </template>

            <ReportTotals
                :totals="result.totals"
                :columns="result.columns"
            />

            <template
                v-for="pivot in result.pivots"
                :key="pivot.title"
            >
                <h2 class="q-mt-lg">{{ pivot.title }}</h2>
                <ReportNumberTable :table="pivotTable(pivot)" />
            </template>

            <template
                v-for="chart in result.charts"
                :key="chart.title"
            >
                <h2 class="q-mt-lg">{{ chart.title }}</h2>
                <ReportNumberTable :table="chartTable(chart)" />
            </template>
        </template>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { inflect } from "inflection"
import StatusBanner from "@/components/StatusBanner.vue"
import ReportGroupTable from "./ReportGroupTable.vue"
import ReportNumberTable from "./ReportNumberTable.vue"
import ReportTotals from "./ReportTotals.vue"
import { formatGeneratedAt } from "./report-format"
import { chartTable, pivotTable } from "./report-number-tables"
import type { ReportResult } from "./report-types"

/**
 * A finished report, laid out in the same order as the exports: summary facts, each group with
 * its subtotals, grand totals, then pivots and chart data. Charts show as tables for now.
 */
const { result } = defineProps<{ result: ReportResult }>()

const rowCountText = computed(() => `${result.rowCount} ${inflect("row", result.rowCount)}`)
</script>
