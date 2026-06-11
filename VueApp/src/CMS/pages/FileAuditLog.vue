<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md">
            <h1 class="text-h4 text-primary q-my-none">File Audit Log</h1>
            <q-space />
            <q-btn
                flat
                dense
                no-caps
                color="primary"
                icon="arrow_back"
                label="Back to Files"
                :to="{ name: 'CmsFiles' }"
            />
        </div>

        <div class="row q-col-gutter-md q-mb-sm">
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.action"
                    dense
                    options-dense
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
                    clearable
                    label="To"
                    type="date"
                    stack-label
                    @update:model-value="reload"
                />
            </div>
            <div class="col-12 col-sm-4 col-lg-3">
                <q-input
                    v-model="filters.search"
                    dense
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
            dense
            flat
            bordered
            @request="onRequest"
        >
            <template #body-cell-timestamp="cellProps">
                <q-td :props="cellProps">
                    {{ formatDateTime(cellProps.row.timestamp) }}
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
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { inject, onMounted, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import type { CmsFileAudit } from "@/CMS/types/"

const apiURL = inject("apiURL") + "cms/files/audit"
const route = useRoute()
const router = useRouter()
const { get, createUrlSearchParams } = useFetch()

const entries = ref<CmsFileAudit[]>([])
const loading = ref(false)
const fileGuid = ref<string | null>((route.query.fileGuid as string) || null)

const filters = ref({
    action: null as string | null,
    loginId: "",
    from: "",
    to: "",
    search: "",
})

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

const columns: QTableProps["columns"] = [
    { name: "timestamp", label: "When", field: "timestamp", align: "left" },
    { name: "loginid", label: "User", field: "loginid", align: "left" },
    { name: "action", label: "Action", field: "action", align: "left" },
    { name: "detail", label: "Detail", field: "detail", align: "left" },
    { name: "filePath", label: "File", field: "filePath", align: "left" },
]

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
    }
    loading.value = false
}

function reload() {
    void onRequest({ pagination: { ...pagination.value, page: 1 } })
}

function clearFileFilter() {
    fileGuid.value = null
    void router.replace({ query: {} })
    reload()
}

function formatDateTime(value: string | null): string {
    if (!value) return ""
    return new Date(value).toLocaleString("en-US", {
        year: "2-digit",
        month: "2-digit",
        day: "2-digit",
        hour: "numeric",
        minute: "2-digit",
    })
}

onMounted(reload)
</script>

<style scoped>
.file-path {
    max-width: 320px;
    display: inline-block;
    vertical-align: middle;
}
</style>
