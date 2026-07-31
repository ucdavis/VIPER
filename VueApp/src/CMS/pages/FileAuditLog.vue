<template>
    <div class="q-pa-md">
        <BreadcrumbHeading
            label="Audit Trail"
            parent-label="Manage Files"
            :parent-to="{ name: 'CmsFiles' }"
        />

        <div class="row q-col-gutter-md q-mb-sm">
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.action"
                    dense
                    options-dense
                    outlined
                    clearable
                    label="Action"
                    :options="actionOptions"
                    @update:model-value="reload"
                />
            </div>
            <div class="col-12 col-sm-3 col-lg-2">
                <q-input
                    v-model="filters.loginId"
                    dense
                    outlined
                    clearable
                    debounce="400"
                    label="Login ID"
                    @update:model-value="reload"
                />
            </div>
            <DateRangeFilter
                v-model:from="filters.from"
                v-model:to="filters.to"
                @change="reload"
            />
        </div>

        <div class="row q-mb-sm">
            <div class="col-12 col-sm-4 col-lg-3">
                <q-input
                    v-model="filters.search"
                    dense
                    outlined
                    clearable
                    debounce="400"
                    label="Search path or detail"
                    @update:model-value="reload"
                >
                    <template #prepend>
                        <q-icon name="search" />
                    </template>
                </q-input>
            </div>
        </div>

        <div
            v-if="fileGuid"
            class="q-mb-sm"
        >
            <q-chip
                removable
                dense
                color="primary"
                text-color="white"
                @remove="clearFileFilter"
            >
                Filtered to one file: {{ fileGuid }}
            </q-chip>
        </div>

        <q-table
            :rows="entries"
            :columns="columns"
            row-key="auditId"
            :loading="loading"
            v-model:pagination="pagination"
            :rows-per-page-options="[25, 50, 100, 250]"
            :grid="$q.screen.lt.sm"
            dense
            flat
            bordered
            @request="onRequest"
        >
            <template #body-cell-timestamp="cellProps">
                <q-td :props="cellProps">
                    {{ formatDateTime(cellProps.row.timestamp, { dateStyle: "short", timeStyle: "short" }) }}
                </q-td>
            </template>
            <template #body-cell-action="cellProps">
                <q-td :props="cellProps">
                    <StatusBadge :color="getAuditActionColor(cellProps.row.action)">
                        {{ cellProps.row.action }}
                    </StatusBadge>
                </q-td>
            </template>
            <template #body-cell-filePath="cellProps">
                <q-td :props="cellProps">
                    <span class="ellipsis file-path">
                        {{ cellProps.row.filePath }}
                        <q-tooltip v-if="cellProps.row.filePath">{{ cellProps.row.filePath }}</q-tooltip>
                    </span>
                </q-td>
            </template>

            <template #item="{ row }">
                <ListCard>
                    <template #header>
                        <div class="row items-center q-gutter-x-sm">
                            <StatusBadge :color="getAuditActionColor(row.action)">
                                {{ row.action }}
                            </StatusBadge>
                            <span class="text-caption text-grey-7">
                                {{ formatDateTime(row.timestamp, { dateStyle: "short", timeStyle: "short" }) }}
                            </span>
                        </div>
                    </template>

                    <ListCardField
                        label="Modified By"
                        :value="row.loginid"
                    />
                    <ListCardField
                        v-if="row.filePath"
                        label="File"
                        :value="row.filePath"
                    />
                    <ListCardField
                        v-if="row.detail"
                        label="Detail"
                        :value="row.detail"
                    />
                </ListCard>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { inject, onMounted } from "vue"
import { useQuasar, type QTableProps } from "quasar"
import { useDateFunctions } from "@/composables/DateFunctions"
import { getAuditActionColor } from "@/composables/use-audit-colors"
import { useUrlFilteredTable } from "@/CMS/composables/use-url-filtered-table"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import StatusBadge from "@/components/StatusBadge.vue"
import ListCard from "@/CMS/components/ListCard.vue"
import ListCardField from "@/CMS/components/ListCardField.vue"
import DateRangeFilter from "@/CMS/components/DateRangeFilter.vue"
import type { CmsFileAudit } from "@/CMS/types/"

const apiURL = inject("apiURL") + "cms/files/audit"
const $q = useQuasar()
const { formatDateTime } = useDateFunctions()

const actionOptions = [
    "AccessFile",
    "AccessFileDenied",
    "AddFile",
    "UploadFile",
    "EditFile",
    "DeleteFile",
    "CancelDelete",
    "ImportFile",
]

// Column order mirrors the Effort audit trail: date, who, what was affected,
// then the action badge and its detail.
const columns: QTableProps["columns"] = [
    { name: "timestamp", label: "Date", field: "timestamp", align: "left" },
    { name: "loginid", label: "Modified By", field: "loginid", align: "left" },
    { name: "filePath", label: "File", field: "filePath", align: "left" },
    { name: "action", label: "Action", field: "action", align: "left" },
    { name: "detail", label: "Detail", field: "detail", align: "left" },
]

// Filters + the per-file deep-link (?fileGuid) sync to the URL and drive the server-paged fetch;
// see useUrlFilteredTable. The action filter defaults to null (unset) to match the clearable select.
const {
    rows: entries,
    loading,
    pagination,
    onRequest,
    filters,
    primary: fileGuid,
    reload,
    clearPrimaryFilter: clearFileFilter,
} = useUrlFilteredTable<
    CmsFileAudit,
    { action: string | null; loginId: string; from: string; to: string; search: string }
>({
    url: apiURL,
    errorMessage: "Failed to load audit trail",
    primaryKey: "fileGuid",
    defaultFilters: () => ({ action: null, loginId: "", from: "", to: "", search: "" }),
    pagination: { sortBy: "timestamp", descending: true },
})

onMounted(reload)
</script>

<style scoped>
.file-path {
    max-width: 20rem;
    display: inline-block;
    vertical-align: middle;
}
</style>
