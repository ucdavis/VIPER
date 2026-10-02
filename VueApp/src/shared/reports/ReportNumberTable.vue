<template>
    <q-table
        :rows="table.rows"
        :columns="columns"
        :pagination="ALL_ROWS"
        row-key="label"
        flat
        bordered
        dense
        hide-bottom
        class="q-mb-md"
    >
        <template
            v-if="table.total"
            #bottom-row
        >
            <q-tr class="text-weight-bold">
                <q-td>{{ table.total.label }}</q-td>
                <q-td
                    v-for="(value, index) in table.total.values"
                    :key="index"
                    class="text-right"
                >
                    {{ formatPlainNumber(value) }}
                </q-td>
            </q-tr>
        </template>
    </q-table>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { QTableColumn } from "quasar"
import { formatPlainNumber } from "./report-format"
import type { NumberRow, NumberTable } from "./report-number-tables"

/**
 * A small table of labelled numbers, used for pivots (a row per category and a column per
 * value) and for chart data, which doubles as the chart's text alternative. The caller supplies
 * the heading, so it sits in the page's heading outline.
 */
const { table } = defineProps<{ table: NumberTable }>()

const ALL_ROWS = { rowsPerPage: 0 }

const columns = computed<QTableColumn<NumberRow>[]>(() => {
    const [labelHeader = "", ...valueHeaders] = table.headers
    return [
        { name: "label", label: labelHeader, field: "label", align: "left" },
        ...valueHeaders.map((header, index) => ({
            name: `value${index}`,
            label: header,
            field: (row: NumberRow) => row.values[index] ?? null,
            format: formatPlainNumber,
            align: "right" as const,
        })),
    ]
})
</script>
