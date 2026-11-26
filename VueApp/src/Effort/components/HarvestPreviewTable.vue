<template>
    <div class="q-mb-md">
        <div class="row items-center justify-between q-mb-sm">
            <div class="text-subtitle2">{{ title }} ({{ rows.length }})</div>
            <CompactSearch v-model="filter" />
        </div>
        <q-table
            :rows="rows"
            :columns="columns"
            :row-key="rowKey"
            :filter="filter"
            dense
            flat
            bordered
            v-model:pagination="pagination"
        >
            <template #body-cell-status="slotProps">
                <q-td :props="slotProps">
                    <StatusBadge
                        :color="getStatusColor(slotProps.value)"
                        :label="slotProps.value"
                    />
                </q-td>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts" generic="T">
import { ref } from "vue"
import type { QTableColumn } from "quasar"
import StatusBadge from "@/components/StatusBadge.vue"
import CompactSearch from "./CompactSearch.vue"

defineProps<{
    title: string
    rows: T[]
    columns: QTableColumn[]
    rowKey: string | ((row: T) => string)
}>()

const filter = ref<string | null>("")
const pagination = ref({ rowsPerPage: 5 })

function getStatusColor(status: string): string {
    switch (status) {
        case "New":
            return "positive"
        case "Exists":
            return "grey-6"
        case "In CREST":
            return "info"
        default:
            return "grey"
    }
}
</script>
