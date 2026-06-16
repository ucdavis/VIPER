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
            <div class="col-12 col-sm-3 col-lg-2">
                <q-input
                    v-model="filters.from"
                    dense
                    outlined
                    clearable
                    label="From"
                    type="date"
                    stack-label
                    @update:model-value="reload"
                />
            </div>
            <div class="col-12 col-sm-3 col-lg-2">
                <q-input
                    v-model="filters.to"
                    dense
                    outlined
                    clearable
                    label="To"
                    type="date"
                    stack-label
                    @update:model-value="reload"
                />
            </div>
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
                    <StatusBadge :color="getActionColor(cellProps.row.action)">
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
                            <StatusBadge :color="getActionColor(row.action)">
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
import { inject, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import { useDateFunctions } from "@/composables/DateFunctions"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import StatusBadge from "@/components/StatusBadge.vue"
import ListCard from "@/CMS/components/ListCard.vue"
import ListCardField from "@/CMS/components/ListCardField.vue"
import type { CmsFileAudit } from "@/CMS/types/"

const apiURL = inject("apiURL") + "cms/files/audit"
const $q = useQuasar()
const route = useRoute()
const router = useRouter()
const { get, createUrlSearchParams } = useFetch()
const { formatDateTime } = useDateFunctions()

const entries = ref<CmsFileAudit[]>([])
const loading = ref(false)
const fileGuid = ref<string | null>((route.query.fileGuid as string) || null)

// Filters initialize from the URL so filtered views can be shared/deep-linked.
const filters = ref({
    action: typeof route.query.action === "string" ? route.query.action : null,
    loginId: typeof route.query.loginId === "string" ? route.query.loginId : "",
    from: typeof route.query.from === "string" ? route.query.from : "",
    to: typeof route.query.to === "string" ? route.query.to : "",
    search: typeof route.query.search === "string" ? route.query.search : "",
})

// Reflect the active filters back into the URL (empty values are omitted).
function syncFiltersToUrl() {
    void router.replace({
        query: {
            fileGuid: fileGuid.value || undefined,
            action: filters.value.action || undefined,
            loginId: filters.value.loginId || undefined,
            from: filters.value.from || undefined,
            to: filters.value.to || undefined,
            search: filters.value.search || undefined,
        },
    })
}

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

const pagination = ref({
    sortBy: "timestamp",
    descending: true,
    page: 1,
    rowsPerPage: 50,
    rowsNumber: 0,
})

// Column order mirrors the Effort audit trail: date, who, what was affected,
// then the action badge and its detail.
const columns: QTableProps["columns"] = [
    { name: "timestamp", label: "Date", field: "timestamp", align: "left" },
    { name: "loginid", label: "Modified By", field: "loginid", align: "left" },
    { name: "filePath", label: "File", field: "filePath", align: "left" },
    { name: "action", label: "Action", field: "action", align: "left" },
    { name: "detail", label: "Detail", field: "detail", align: "left" },
]

// Same palette logic as the Effort audit trail (AuditList.getActionColor):
// create-like is green, edits blue, deletes red, restores teal, imports cyan.
function getActionColor(action: string): string {
    if (action === "AccessFileDenied") return "warning"
    if (action.startsWith("Add") || action.startsWith("Upload")) return "positive"
    if (action.startsWith("Edit")) return "primary"
    if (action.startsWith("Delete")) return "negative"
    if (action.startsWith("CancelDelete")) return "secondary"
    if (action.startsWith("Import")) return "info"
    return "grey-8"
}

type TableRequestPagination = {
    sortBy: string
    descending: boolean
    page: number
    rowsPerPage: number
    rowsNumber?: number
}

async function onRequest(requestProps: { pagination: TableRequestPagination }) {
    const { page, rowsPerPage } = requestProps.pagination
    loading.value = true
    const params = createUrlSearchParams({
        fileGuid: fileGuid.value,
        action: filters.value.action,
        loginId: filters.value.loginId || null,
        from: filters.value.from || null,
        to: filters.value.to || null,
        search: filters.value.search || null,
        page,
        perPage: rowsPerPage,
    })
    const res = await get(apiURL + "?" + params)
    if (res.success) {
        entries.value = res.result
        pagination.value.rowsNumber = res.pagination?.totalRecords ?? res.result.length
        pagination.value.page = page
        pagination.value.rowsPerPage = rowsPerPage
    } else {
        $q.notify({ type: "negative", message: res.errors?.[0] ?? "Failed to load audit trail" })
    }
    loading.value = false
}

function reload() {
    syncFiltersToUrl()
    void onRequest({ pagination: { ...pagination.value, page: 1 } })
}

function clearFileFilter() {
    fileGuid.value = null
    reload()
}

// Re-sync filters from the URL when in-app navigation reuses this view with a different
// query (e.g. re-clicking the left-nav link, or a per-file ?fileGuid deep-link). The
// equality guard skips our own syncFiltersToUrl write so it doesn't double-fetch.
watch(
    () => route.query,
    (query) => {
        const nextGuid = typeof query.fileGuid === "string" ? query.fileGuid : null
        const next = {
            action: typeof query.action === "string" ? query.action : null,
            loginId: typeof query.loginId === "string" ? query.loginId : "",
            from: typeof query.from === "string" ? query.from : "",
            to: typeof query.to === "string" ? query.to : "",
            search: typeof query.search === "string" ? query.search : "",
        }
        const f = filters.value
        if (
            nextGuid === fileGuid.value &&
            next.action === f.action &&
            next.loginId === f.loginId &&
            next.from === f.from &&
            next.to === f.to &&
            next.search === f.search
        ) {
            return
        }
        fileGuid.value = nextGuid
        filters.value = next
        void onRequest({ pagination: { ...pagination.value, page: 1 } })
    },
)

onMounted(reload)
</script>

<style scoped>
.file-path {
    max-width: 320px;
    display: inline-block;
    vertical-align: middle;
}
</style>
