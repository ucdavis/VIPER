<template>
    <div class="q-pa-md">
        <BreadcrumbHeading
            label="Bulk Encrypt"
            parent-label="Manage Files"
            :parent-to="{ name: 'CmsFiles' }"
        />

        <p class="text-body2 text-grey-8">
            Lists active files that are not encrypted. Select files and encrypt them in place; downloads decrypt
            automatically for permitted users.
        </p>

        <div class="row q-col-gutter-md q-mb-sm">
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.folder"
                    dense
                    options-dense
                    emit-value
                    map-options
                    label="VIPER app"
                    :options="folderOptions"
                    @update:model-value="reload"
                />
            </div>
            <div class="col-12 col-sm-4 col-lg-3">
                <q-input
                    v-model="filters.search"
                    dense
                    clearable
                    debounce="400"
                    label="Search"
                    @update:model-value="reload"
                >
                    <template #prepend>
                        <q-icon name="search" />
                    </template>
                </q-input>
            </div>
            <div class="col-auto flex items-center">
                <q-btn
                    color="primary"
                    icon="lock"
                    :label="`Encrypt Selected (${selected.length})`"
                    dense
                    no-caps
                    class="q-pr-md"
                    :disable="!selected.length"
                    :loading="encrypting"
                    @click="encryptSelected"
                >
                    <template #loading>
                        <q-spinner
                            size="1em"
                            class="q-mr-sm"
                        />
                        Encrypting...
                    </template>
                </q-btn>
            </div>
        </div>

        <q-table
            v-model:selected="selected"
            :rows="files"
            :columns="columns"
            row-key="fileGuid"
            selection="multiple"
            :loading="loading"
            v-model:pagination="pagination"
            :rows-per-page-options="[25, 50, 100, 250]"
            dense
            flat
            bordered
            @request="onRequest"
        >
            <template #body-cell-modifiedOn="cellProps">
                <ModifiedStamp :cell-props="cellProps" />
            </template>
        </q-table>

        <template v-if="results.length">
            <h2 class="text-h6 text-primary q-mb-sm q-mt-lg">Results</h2>
            <q-list
                bordered
                dense
                separator
            >
                <q-item
                    v-for="r in results"
                    :key="r.fileGuid"
                >
                    <q-item-section side>
                        <q-icon
                            :name="r.success ? 'check_circle' : 'error'"
                            :color="r.success ? 'positive' : 'negative'"
                        />
                        <span class="sr-only">{{ r.success ? "Encrypted" : "Failed" }}</span>
                    </q-item-section>
                    <q-item-section>
                        <q-item-label>{{ r.friendlyName ?? r.fileGuid }}</q-item-label>
                        <q-item-label
                            v-if="r.message"
                            caption
                        >
                            {{ r.message }}
                        </q-item-label>
                    </q-item-section>
                </q-item>
            </q-list>
        </template>
    </div>
</template>

<script setup lang="ts">
import { inject, onMounted, ref } from "vue"
import { inflect } from "inflection"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import ModifiedStamp from "@/CMS/components/ModifiedStamp.vue"
import type { CmsFile } from "@/CMS/types/"

type BulkEncryptResult = {
    fileGuid: string
    friendlyName: string | null
    success: boolean
    message: string | null
}

const apiURL = inject("apiURL") + "cms/files/"
const $q = useQuasar()
const { get, post, createUrlSearchParams } = useFetch()

type FolderOption = { label: string; value: string }

const ALL_FOLDERS = "All"

const files = ref<CmsFile[]>([])
const folderOptions = ref<FolderOption[]>([{ label: ALL_FOLDERS, value: ALL_FOLDERS }])
const selected = ref<CmsFile[]>([])
const results = ref<BulkEncryptResult[]>([])
const loading = ref(false)
const encrypting = ref(false)

const filters = ref({
    folder: ALL_FOLDERS,
    search: "",
})

const pagination = ref({
    sortBy: "friendlyName",
    descending: false,
    page: 1,
    rowsPerPage: 50,
    rowsNumber: 0,
})

const columns: QTableProps["columns"] = [
    { name: "friendlyName", label: "File", field: "friendlyName", align: "left", sortable: true },
    { name: "folder", label: "VIPER app", field: "folder", align: "left", sortable: true },
    { name: "modifiedOn", label: "Modified", field: "modifiedOn", align: "left", sortable: true },
]

type TableRequestPagination = {
    sortBy: string
    descending: boolean
    page: number
    rowsPerPage: number
    rowsNumber?: number
}

async function onRequest(requestProps: { pagination: TableRequestPagination }) {
    const { page, rowsPerPage, sortBy, descending } = requestProps.pagination
    loading.value = true
    const params = createUrlSearchParams({
        folder: filters.value.folder !== ALL_FOLDERS ? filters.value.folder : null,
        status: "active",
        encrypted: "false",
        search: filters.value.search || null,
        page,
        perPage: rowsPerPage,
        sortBy: sortBy ?? "friendlyName",
        descending: descending.toString(),
    })
    const res = await get(apiURL + "?" + params)
    if (res.success) {
        files.value = res.result
        pagination.value.rowsNumber = res.pagination?.totalRecords ?? res.result.length
        pagination.value.page = page
        pagination.value.rowsPerPage = rowsPerPage
        pagination.value.sortBy = sortBy
        pagination.value.descending = descending
    } else {
        $q.notify({ type: "negative", message: res.errors?.[0] ?? "Failed to load files" })
    }
    loading.value = false
}

function reload() {
    selected.value = []
    void onRequest({ pagination: { ...pagination.value, page: 1 } })
}

async function loadFolders() {
    // Counts match what this page lists: active, unencrypted files per VIPER app.
    const res = await get(apiURL + "folder-counts?status=active&encrypted=false")
    const counts: { folder: string; count: number }[] = res.success ? res.result : []
    const total = counts.reduce((sum, c) => sum + c.count, 0)
    folderOptions.value = [
        { label: `${ALL_FOLDERS} (${total})`, value: ALL_FOLDERS },
        ...counts.map((c) => ({ label: `${c.folder} (${c.count})`, value: c.folder })),
    ]
}

async function encryptSelected() {
    const confirmed = await new Promise<boolean>((resolve) => {
        $q.dialog({
            title: "Encrypt Files",
            message: `Encrypt ${selected.value.length} ${inflect("file", selected.value.length)} in place?`,
            cancel: { label: "Cancel", flat: true },
            persistent: true,
            ok: { label: "Encrypt", color: "primary", unelevated: true },
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
            .onDismiss(() => resolve(false))
    })
    if (!confirmed) return

    encrypting.value = true
    const res = await post(
        apiURL + "bulk-encrypt",
        selected.value.map((f) => f.fileGuid),
    )
    encrypting.value = false

    if (!res.success) {
        $q.notify({ type: "negative", message: res.errors?.[0] ?? "Bulk encrypt failed" })
        return
    }
    results.value = res.result
    const succeeded = res.result.filter((r: BulkEncryptResult) => r.success).length
    $q.notify({
        type: succeeded === res.result.length ? "positive" : "warning",
        message: `${succeeded} of ${res.result.length} ${inflect("file", res.result.length)} encrypted`,
    })
    reload()
    void loadFolders()
}

onMounted(() => {
    loadFolders()
    reload()
})
</script>
