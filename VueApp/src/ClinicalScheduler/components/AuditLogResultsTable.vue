<template>
    <!-- Desktop (lg+): full table -->
    <q-table
        v-if="$q.screen.gt.md"
        row-key="scheduleAuditId"
        :rows="entries"
        :columns="columns"
        :loading="isLoading"
        v-model:pagination="pagination"
        :rows-per-page-options="[10, 25, 50, 100]"
        binary-state-sort
        dense
        flat
        bordered
    >
        <template #body-cell-timeStamp="props">
            <q-td :props="props">
                {{ formatDateTime(props.row.timeStamp, { dateStyle: "short", timeStyle: "short" }) }}
            </q-td>
        </template>
        <template #body-cell-action="props">
            <q-td :props="props">
                <StatusBadge :color="getAuditActionColor(props.row.action)">
                    {{ props.row.action }}
                </StatusBadge>
            </q-td>
        </template>
        <template #no-data>
            <div class="full-width text-center q-pa-md text-grey-7">No audit entries found matching this filter.</div>
        </template>
    </q-table>

    <!-- Tablet (sm/md): main columns + a stacked Action/Week/Area row -->
    <q-table
        v-else-if="$q.screen.gt.xs"
        row-key="scheduleAuditId"
        :rows="entries"
        :columns="tabletColumns"
        :loading="isLoading"
        v-model:pagination="pagination"
        :rows-per-page-options="[10, 25, 50, 100]"
        binary-state-sort
        dense
        flat
        bordered
    >
        <template #body="props">
            <q-tr :props="props">
                <q-td
                    key="timeStamp"
                    :props="props"
                >
                    {{ formatDateTime(props.row.timeStamp, { dateStyle: "short", timeStyle: "short" }) }}
                </q-td>
                <q-td
                    key="modifiedByName"
                    :props="props"
                >
                    {{ props.row.modifiedByName }}
                </q-td>
                <q-td
                    key="personName"
                    :props="props"
                >
                    {{ props.row.personName }}
                </q-td>
                <q-td
                    key="rotationName"
                    :props="props"
                >
                    {{ props.row.rotationName }}
                </q-td>
                <q-td
                    key="term"
                    :props="props"
                >
                    {{ props.row.term }}
                </q-td>
            </q-tr>
            <q-tr :props="props">
                <q-td
                    :colspan="tabletColumns.length"
                    class="bg-grey-1 q-py-xs"
                >
                    <div class="row items-center q-gutter-sm">
                        <StatusBadge :color="getAuditActionColor(props.row.action)">
                            {{ props.row.action }}
                        </StatusBadge>
                        <span
                            v-if="formatWeekCell(props.row.weekNum, props.row.weekStart)"
                            class="text-caption text-grey-8"
                        >
                            {{ formatWeekCell(props.row.weekNum, props.row.weekStart) }}
                        </span>
                        <span class="text-caption text-grey-7">{{ props.row.area }}</span>
                    </div>
                </q-td>
            </q-tr>
        </template>
    </q-table>

    <!-- Mobile (xs): card layout -->
    <q-table
        v-else
        row-key="scheduleAuditId"
        :rows="entries"
        :columns="columns"
        :loading="isLoading"
        v-model:pagination="pagination"
        :rows-per-page-options="[10, 25, 50, 100]"
        grid
        binary-state-sort
        dense
        flat
        bordered
    >
        <template #item="props">
            <div class="q-pa-xs col-12">
                <q-card
                    flat
                    bordered
                >
                    <q-card-section class="q-pa-sm">
                        <div class="row items-center q-mb-xs">
                            <StatusBadge
                                :color="getAuditActionColor(props.row.action)"
                                class="q-mr-sm"
                            >
                                {{ props.row.action }}
                            </StatusBadge>
                            <span class="text-caption text-grey-7">
                                {{
                                    formatDateTime(props.row.timeStamp, {
                                        dateStyle: "short",
                                        timeStyle: "short",
                                    })
                                }}
                            </span>
                        </div>
                        <div
                            v-if="props.row.personName"
                            class="text-subtitle2"
                        >
                            {{ props.row.personName }}
                        </div>
                        <div
                            v-if="formatRotationTerm(props.row)"
                            class="text-caption text-grey-8"
                        >
                            {{ formatRotationTerm(props.row) }}
                        </div>
                        <div
                            v-if="formatWeekCell(props.row.weekNum, props.row.weekStart)"
                            class="text-caption text-grey-8"
                        >
                            {{ formatWeekCell(props.row.weekNum, props.row.weekStart) }}
                        </div>
                        <div class="text-caption q-mt-xs">
                            <span class="text-grey-7">by</span> {{ props.row.modifiedByName }}
                            <span class="text-grey-7"> &middot; </span>{{ props.row.area }}
                        </div>
                    </q-card-section>
                </q-card>
            </div>
        </template>
    </q-table>
</template>

<script setup lang="ts">
import { ref } from "vue"
import type { Ref } from "vue"
import { useQuasar } from "quasar"
import type { QTableColumn, QTableProps } from "quasar"
import { useDateFunctions } from "@/composables/DateFunctions"
import StatusBadge from "@/components/StatusBadge.vue"
import { getAuditActionColor } from "../utils/audit-actions"
import type { AuditLogEntry } from "../types/audit-types"

defineProps<{
    entries: AuditLogEntry[]
    isLoading: boolean
}>()

const $q = useQuasar()
const { formatDate, formatDateTime } = useDateFunctions()

const pagination = ref({
    sortBy: "timeStamp",
    descending: true,
    rowsPerPage: 25,
}) as Ref<QTableProps["pagination"]>

function formatWeekCell(weekNum: number, weekStart: string | null): string {
    const date = weekStart ? formatDate(weekStart) : ""
    if (weekNum) {
        return date ? `Wk ${weekNum} · ${date}` : `Wk ${weekNum}`
    }
    return date
}

function formatRotationTerm(entry: AuditLogEntry): string {
    return [entry.rotationName, entry.term].filter(Boolean).join(" • ")
}

const columns: QTableColumn<AuditLogEntry>[] = [
    { name: "timeStamp", label: "Date", field: "timeStamp", align: "left", sortable: true },
    { name: "modifiedByName", label: "Modified By", field: "modifiedByName", align: "left", sortable: true },
    { name: "personName", label: "Person", field: "personName", align: "left", sortable: true },
    { name: "area", label: "Area", field: "area", align: "left", sortable: true },
    { name: "rotationName", label: "Rotation", field: "rotationName", align: "left", sortable: true },
    { name: "term", label: "Term", field: "term", align: "left", sortable: true },
    {
        name: "week",
        label: "Week",
        field: "weekNum",
        align: "left",
        sortable: true,
        format: (val: number, row: AuditLogEntry) => formatWeekCell(val, row.weekStart),
    },
    { name: "action", label: "Action", field: "action", align: "left", sortable: true },
]

// Tablet (sm/md): a reduced column set; Action/Week/Area move to a stacked detail row.
const tabletColumns = columns.filter((column) =>
    ["timeStamp", "modifiedByName", "personName", "rotationName", "term"].includes(column.name),
)
</script>
