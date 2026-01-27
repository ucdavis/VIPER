<template>
    <div>
        <!-- Filter checkboxes -->
        <div class="row q-gutter-md q-mb-md items-center">
            <q-checkbox
                v-model="showAdmin"
                label="Admin"
                dense
            />
            <q-checkbox
                v-model="showClinical"
                label="Clinical"
                dense
            />
            <q-checkbox
                v-model="showOther"
                label="Other"
                dense
            />
            <q-space />
            <q-btn
                v-if="canEdit"
                label="Add"
                icon="add"
                color="primary"
                dense
                aria-label="Add percentage assignment"
                @click="$emit('add')"
                @keyup.enter="$emit('add')"
                @keyup.space="$emit('add')"
            />
        </div>

        <!-- Mobile Card View -->
        <div
            v-if="$q.screen.lt.md"
            class="q-mb-md"
        >
            <div
                v-if="filteredPercentages.length === 0"
                class="text-center text-grey q-py-lg"
            >
                <q-icon
                    name="percent"
                    size="2em"
                    class="q-mb-sm"
                />
                <div>No percentage assignments found</div>
            </div>
            <q-card
                v-for="pct in filteredPercentages"
                :key="pct.id"
                flat
                bordered
                class="q-mb-sm"
                :class="{ 'inactive-card': !pct.isActive }"
            >
                <q-card-section class="q-py-sm">
                    <div class="row items-center justify-between q-mb-xs">
                        <span class="text-weight-bold">{{ pct.typeName }}</span>
                        <div v-if="canEdit">
                            <q-btn
                                flat
                                dense
                                round
                                icon="edit"
                                color="primary"
                                size="sm"
                                aria-label="Edit percentage assignment"
                                @click="$emit('edit', pct)"
                                @keyup.enter="$emit('edit', pct)"
                                @keyup.space="$emit('edit', pct)"
                            />
                            <q-btn
                                flat
                                dense
                                round
                                icon="delete"
                                color="negative"
                                size="sm"
                                aria-label="Delete percentage assignment"
                                @click="$emit('delete', pct)"
                                @keyup.enter="$emit('delete', pct)"
                                @keyup.space="$emit('delete', pct)"
                            />
                        </div>
                    </div>
                    <div class="text-body2 q-mb-xs">
                        <q-badge
                            :color="getClassColor(pct.typeClass)"
                            :label="pct.typeClass"
                            class="q-mr-sm"
                        />
                        <span v-if="pct.unitName">{{ pct.unitName }}</span>
                        <span v-if="pct.modifier">({{ pct.modifier }})</span>
                    </div>
                    <div class="row q-gutter-md text-caption text-grey-7">
                        <span
                            >{{ formatDate(pct.startDate) }} -
                            {{ pct.endDate ? formatDate(pct.endDate) : "Present" }}</span
                        >
                        <span :class="{ 'text-warning text-weight-bold': pct.percentageValue > 100 }">
                            {{ pct.percentageValue.toFixed(1) }}%
                        </span>
                        <span v-if="pct.compensated">
                            <q-icon
                                name="attach_money"
                                size="xs"
                            />
                            Compensated
                        </span>
                    </div>
                    <div
                        v-if="pct.comment"
                        class="text-caption text-grey-8 q-mt-xs"
                    >
                        {{ pct.comment }}
                    </div>
                </q-card-section>
            </q-card>
        </div>

        <!-- Desktop Table View -->
        <q-table
            v-else
            :rows="filteredPercentages"
            :columns="columns"
            row-key="id"
            dense
            flat
            bordered
            :loading="loading"
            hide-pagination
            :rows-per-page-options="[0]"
            class="percent-table"
        >
            <template #body="bodyProps">
                <q-tr
                    :props="bodyProps"
                    :class="{ 'inactive-row': !bodyProps.row.isActive }"
                >
                    <q-td
                        key="typeClass"
                        :props="bodyProps"
                    >
                        <q-badge
                            :color="getClassColor(bodyProps.row.typeClass)"
                            :label="bodyProps.row.typeClass"
                        />
                    </q-td>
                    <q-td
                        key="typeName"
                        :props="bodyProps"
                    >
                        {{ bodyProps.row.typeName }}
                    </q-td>
                    <q-td
                        key="unitName"
                        :props="bodyProps"
                    >
                        {{ bodyProps.row.unitName }}
                    </q-td>
                    <q-td
                        key="modifier"
                        :props="bodyProps"
                    >
                        {{ bodyProps.row.modifier || "-" }}
                    </q-td>
                    <q-td
                        key="startDate"
                        :props="bodyProps"
                    >
                        {{ formatDate(bodyProps.row.startDate) }}
                    </q-td>
                    <q-td
                        key="endDate"
                        :props="bodyProps"
                    >
                        {{ bodyProps.row.endDate ? formatDate(bodyProps.row.endDate) : "Present" }}
                    </q-td>
                    <q-td
                        key="percentageValue"
                        :props="bodyProps"
                    >
                        {{ bodyProps.row.percentageValue.toFixed(1) }}%
                    </q-td>
                    <q-td
                        key="comment"
                        :props="bodyProps"
                    >
                        {{ bodyProps.row.comment || "" }}
                    </q-td>
                    <q-td
                        key="compensated"
                        :props="bodyProps"
                    >
                        <q-icon
                            v-if="bodyProps.row.compensated"
                            name="check"
                            color="positive"
                        />
                    </q-td>
                    <q-td
                        key="actions"
                        :props="bodyProps"
                    >
                        <template v-if="canEdit">
                            <q-btn
                                flat
                                dense
                                round
                                icon="edit"
                                color="primary"
                                size="sm"
                                aria-label="Edit percentage assignment"
                                @click="$emit('edit', bodyProps.row)"
                                @keyup.enter="$emit('edit', bodyProps.row)"
                                @keyup.space="$emit('edit', bodyProps.row)"
                            >
                                <q-tooltip>Edit</q-tooltip>
                            </q-btn>
                            <q-btn
                                flat
                                dense
                                round
                                icon="delete"
                                color="negative"
                                size="sm"
                                aria-label="Delete percentage assignment"
                                @click="$emit('delete', bodyProps.row)"
                                @keyup.enter="$emit('delete', bodyProps.row)"
                                @keyup.space="$emit('delete', bodyProps.row)"
                            >
                                <q-tooltip>Delete</q-tooltip>
                            </q-btn>
                        </template>
                    </q-td>
                </q-tr>
            </template>
            <template #header-cell-compensated="headerProps">
                <q-th :props="headerProps">
                    {{ headerProps.col.label }}
                    <q-tooltip>Compensated</q-tooltip>
                </q-th>
            </template>
            <template #no-data>
                <div class="full-width row flex-center text-grey q-gutter-sm q-py-lg">
                    <q-icon
                        name="percent"
                        size="2em"
                    />
                    <span>No percentage assignments found</span>
                </div>
            </template>
        </q-table>

        <!-- Total Active % -->
        <div class="q-mt-sm text-right">
            <span :class="totalActivePercent > 100 ? 'text-warning text-weight-bold' : ''">
                Total Active: {{ totalActivePercent.toFixed(1) }}%
            </span>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from "vue"
import { useQuasar, type QTableColumn } from "quasar"
import type { PercentageDto } from "../types"

const props = defineProps<{
    percentages: PercentageDto[]
    canEdit: boolean
    loading: boolean
}>()

defineEmits<{
    add: []
    edit: [percentage: PercentageDto]
    delete: [percentage: PercentageDto]
}>()

const $q = useQuasar()

// Filter state
const showAdmin = ref(true)
const showClinical = ref(true)
const showOther = ref(true)

// Filter percentages by type class
const filteredPercentages = computed(() => {
    return props.percentages.filter((pct) => {
        const typeClass = pct.typeClass.toLowerCase()
        if (typeClass === "admin" && !showAdmin.value) return false
        if (typeClass === "clinical" && !showClinical.value) return false
        if (typeClass !== "admin" && typeClass !== "clinical" && !showOther.value) return false
        return true
    })
})

// Calculate total active percentage
const totalActivePercent = computed(() => {
    return props.percentages.filter((pct) => pct.isActive).reduce((sum, pct) => sum + pct.percentageValue, 0)
})

// Get badge color based on type class
function getClassColor(typeClass: string): string {
    const lc = typeClass.toLowerCase()
    if (lc === "admin") return "blue"
    if (lc === "clinical") return "green"
    return "orange"
}

// Format date for display
function formatDate(dateStr: string): string {
    if (!dateStr) return ""
    const date = new Date(dateStr)
    return date.toLocaleDateString("en-US", { month: "short", year: "numeric" })
}

// Table columns
const columns: QTableColumn[] = [
    {
        name: "typeClass",
        label: "Class",
        field: "typeClass",
        align: "left",
        sortable: true,
        style: "width: 80px",
    },
    {
        name: "typeName",
        label: "Title",
        field: "typeName",
        align: "left",
        sortable: true,
    },
    {
        name: "unitName",
        label: "Unit",
        field: "unitName",
        align: "left",
        sortable: true,
    },
    {
        name: "modifier",
        label: "Modifier",
        field: "modifier",
        align: "left",
        format: (val: string | null) => val ?? "-",
    },
    {
        name: "startDate",
        label: "Start",
        field: "startDate",
        align: "left",
        sortable: true,
        format: (val: string) => formatDate(val),
    },
    {
        name: "endDate",
        label: "End",
        field: "endDate",
        align: "left",
        sortable: true,
        format: (val: string | null) => (val ? formatDate(val) : "Present"),
    },
    {
        name: "percentageValue",
        label: "Percent",
        field: "percentageValue",
        align: "right",
        sortable: true,
        format: (val: number) => `${val.toFixed(1)}%`,
    },
    {
        name: "comment",
        label: "Comments",
        field: "comment",
        align: "left",
    },
    {
        name: "compensated",
        label: "Comp",
        field: "compensated",
        align: "center",
        style: "width: 60px",
    },
    {
        name: "actions",
        label: "Actions",
        field: "id",
        align: "center",
        style: "width: 100px",
    },
]
</script>

<style scoped>
/* Gray background for inactive (past) percent assignments */
.percent-table :deep(.inactive-row) {
    background-color: var(--ucdavis-black-10);
}

.inactive-card {
    background-color: var(--ucdavis-black-10);
}
</style>
