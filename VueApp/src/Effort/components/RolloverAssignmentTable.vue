<template>
    <div>
        <div
            v-if="showSearch"
            class="row justify-end q-mb-sm"
        >
            <q-input
                v-model="localFilter"
                placeholder="Search..."
                dense
                outlined
                clearable
                class="compact-search"
            >
                <template #prepend>
                    <q-icon
                        name="search"
                        size="xs"
                    />
                </template>
            </q-input>
        </div>
        <q-table
            :rows="rows"
            :columns="columns"
            row-key="sourcePercentageId"
            :filter="localFilter"
            dense
            flat
            bordered
            :grid="$q.screen.lt.sm"
            :pagination="pagination"
        >
            <template #item="{ row }">
                <div class="q-pa-xs col-xs-12 col-sm-6">
                    <q-card
                        flat
                        bordered
                    >
                        <q-card-section class="q-pa-sm">
                            <div class="text-weight-medium">{{ row.personName }}</div>
                            <div class="text-caption text-grey-7">
                                {{ row.typeName }} · {{ Math.round(row.percentageValue * 100) }}%
                            </div>
                            <div class="text-caption">
                                {{ row.unitName }}
                                <span v-if="row.modifier"> · {{ row.modifier }}</span>
                                <span v-if="row.compensated"> · Compensated</span>
                            </div>
                        </q-card-section>
                    </q-card>
                </div>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import { useQuasar } from "quasar"
import type { QTableColumn } from "quasar"
import type { PercentRolloverItemPreview } from "../types"

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

const $q = useQuasar()

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
    { name: "typeName", label: "Type", field: "typeName", align: "left", sortable: true },
    {
        name: "percentageValue",
        label: "%",
        field: "percentageValue",
        align: "right",
        format: (val: number) => `${Math.round(val * 100)}%`,
        sortable: true,
    },
    { name: "unitName", label: "Unit", field: "unitName", align: "left" },
    { name: "modifier", label: "Modifier", field: "modifier", align: "left" },
    {
        name: "compensated",
        label: "Comp",
        field: "compensated",
        align: "center",
        format: (val: boolean) => (val ? "Yes" : "No"),
    },
]
</script>

<style scoped>
.compact-search {
    width: 10rem;
}

.compact-search :deep(.q-field__control) {
    height: 1.75rem;
    min-height: 1.75rem;
}

.compact-search :deep(.q-field__native) {
    font-size: 0.75rem;
    padding: 0;
}

.compact-search :deep(.q-field__prepend) {
    height: 1.75rem;
    padding-left: 0;
    padding-right: 0.25rem;
}

.compact-search :deep(.q-field__append) {
    height: 1.75rem;
    padding: 0 0.25rem;
}
</style>
