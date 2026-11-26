<template>
    <div>
        <div
            v-if="showSearch"
            class="row justify-end q-mb-sm"
        >
            <CompactSearch v-model="localFilter" />
        </div>
        <q-table
            :rows="rows"
            :columns="columns"
            row-key="sourcePercentageId"
            :filter="localFilter"
            dense
            flat
            bordered
            :pagination="pagination"
        >
            <template #body-cell-typeClass="{ row }">
                <q-td>
                    <StatusBadge
                        :color="getTypeClassColor(row.typeClass)"
                        :label="row.typeClass"
                    />
                </q-td>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import type { QTableColumn } from "quasar"
import type { PercentRolloverItemPreview } from "../types"
import { formatTypeWithModifier, getTypeClassColor } from "../utils/format"
import StatusBadge from "@/components/StatusBadge.vue"
import CompactSearch from "./CompactSearch.vue"

const props = withDefaults(
    defineProps<{
        rows: PercentRolloverItemPreview[]
        filter?: string
        showSearch?: boolean
        rowsPerPage?: number
    }>(),
    {
        filter: "",
        showSearch: true,
        rowsPerPage: 10,
    },
)

const emit = defineEmits<{
    "update:filter": [value: string]
}>()

const localFilter = ref(props.filter)

watch(
    () => props.filter,
    (val) => {
        localFilter.value = val
    },
)

watch(localFilter, (val) => {
    emit("update:filter", val)
})

const pagination = { rowsPerPage: props.rowsPerPage }

const columns: QTableColumn[] = [
    { name: "personName", label: "Instructor", field: "personName", align: "left", sortable: true },
    { name: "typeClass", label: "Type", field: "typeClass", align: "left", sortable: true },
    {
        name: "typeName",
        label: "Title",
        field: "typeName",
        align: "left",
        sortable: true,
        format: (val: string, row: PercentRolloverItemPreview) => formatTypeWithModifier(val, row.modifier),
    },
    {
        name: "percentageValue",
        label: "%",
        field: "percentageValue",
        align: "right",
        format: (val: number) => `${val}%`,
        sortable: true,
    },
    { name: "unitName", label: "Unit", field: "unitName", align: "left" },
    {
        name: "compensated",
        label: "Comp",
        field: "compensated",
        align: "center",
        format: (val: boolean) => (val ? "Yes" : "No"),
    },
]
</script>
