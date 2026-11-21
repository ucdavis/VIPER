<template>
    <BreadcrumbHeading
        label="Manage reports"
        parent-label="Reports"
        :parent-to="{ name: 'ReportsHome' }"
    />
    <div class="row items-center q-mb-md q-gutter-sm">
        <q-btn
            color="primary"
            no-caps
            icon="add"
            label="New report"
            :to="{ name: 'ReportAdminNew' }"
        />
        <q-btn
            flat
            color="primary"
            no-caps
            icon="report_problem"
            label="Problems"
            :to="{ name: 'ReportAdminProblems' }"
        />
        <q-space />
        <q-toggle
            v-model="includeDeleted"
            label="Show deleted reports"
        />
    </div>
    <StatusBanner
        v-if="failed"
        type="error"
    >
        The reports couldn't be loaded. Please try again later.
    </StatusBanner>
    <q-table
        v-else
        :rows="reports"
        :columns="COLUMNS"
        :loading="loading"
        :pagination="{ rowsPerPage: 0 }"
        row-key="id"
        flat
        bordered
        dense
        hide-pagination
        no-data-label="No reports yet."
    >
        <template #body-cell-title="cell">
            <q-td :props="cell">
                <router-link :to="{ name: 'ReportAdminEdit', params: { id: cell.row.id } }">{{
                    cell.row.title
                }}</router-link>
                <StatusBadge
                    v-if="cell.row.deletedOn"
                    color="grey-7"
                    label="Deleted"
                    class="q-ml-sm"
                />
            </q-td>
        </template>
        <template #body-cell-permissions="cell">
            <q-td :props="cell">
                <span v-if="cell.row.permissions.length === 0">Admins only</span>
                <StatusBadge
                    v-for="permission in cell.row.permissions"
                    :key="permission"
                    :color="cell.row.missingPermissions.includes(permission) ? 'negative' : 'grey-7'"
                    :label="
                        cell.row.missingPermissions.includes(permission) ? `${permission} (not in RAPS)` : permission
                    "
                    class="q-mr-xs"
                />
            </q-td>
        </template>
        <template #body-cell-actions="cell">
            <q-td :props="cell">
                <q-btn
                    v-if="!cell.row.deletedOn"
                    flat
                    dense
                    no-caps
                    color="primary"
                    label="View"
                    :aria-label="`View ${cell.row.title}`"
                    :to="{ name: 'Report', params: { slug: cell.row.slug } }"
                />
            </q-td>
        </template>
    </q-table>
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import type { QTableColumn } from "quasar"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import StatusBadge from "@/components/StatusBadge.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { formatCell } from "@/shared/report-builder/report-builder-format"
import { reportAdminService } from "../admin/report-admin-service"
import type { ReportAdminListItem } from "../admin/report-admin-types"

/** Every report, with who can view it; permissions RAPS no longer has are flagged. */
const COLUMNS: QTableColumn<ReportAdminListItem>[] = [
    { name: "title", label: "Title", field: "title", align: "left", sortable: true },
    { name: "area", label: "Area", field: "area", align: "left", sortable: true },
    { name: "source", label: "Data source", field: "source", align: "left", sortable: true },
    { name: "permissions", label: "Who can view it", field: "permissions", align: "left" },
    {
        name: "modified",
        label: "Last changed",
        field: (row) => row.modifiedOn,
        format: (value: string | null, row) =>
            [formatCell(value, "DateTime"), row.modifiedBy].filter(Boolean).join(" by "),
        align: "left",
        sortable: true,
    },
    { name: "actions", label: "", field: "id", align: "right" },
]

const reports = ref<ReportAdminListItem[]>([])
const includeDeleted = ref(false)
const loading = ref(true)
const failed = ref(false)

let loadId = 0

/** Only the latest toggle's request may update the table. */
async function load(withDeleted: boolean): Promise<void> {
    loadId += 1
    const current = loadId
    loading.value = true
    const found = await reportAdminService.list(withDeleted)
    if (current !== loadId) {
        return
    }
    failed.value = found === null
    reports.value = found ?? []
    loading.value = false
}

watch(includeDeleted, load, { immediate: true })
</script>
