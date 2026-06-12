<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md">
            <h1 class="q-my-none">Manage Files</h1>
            <q-space />
            <q-btn-dropdown
                flat
                dense
                no-caps
                color="primary"
                label="File Tools"
                class="q-mr-sm"
            >
                <q-list dense>
                    <q-item
                        v-for="tool in fileTools"
                        :key="tool.label"
                        v-close-popup
                        clickable
                        :to="tool.to"
                    >
                        <q-item-section side>
                            <q-icon
                                :name="tool.icon"
                                size="xs"
                            />
                        </q-item-section>
                        <q-item-section>{{ tool.label }}</q-item-section>
                    </q-item>
                </q-list>
            </q-btn-dropdown>
            <q-btn
                color="positive"
                icon="add"
                label="Upload File"
                no-caps
                dense
                class="q-pr-md"
                @click="openUploadDialog"
            />
        </div>

        <div class="row q-col-gutter-md q-mb-sm">
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.folder"
                    dense
                    options-dense
                    clearable
                    label="VIPER app"
                    :options="folders"
                    @update:model-value="reload"
                />
            </div>
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.status"
                    dense
                    options-dense
                    emit-value
                    map-options
                    label="Status"
                    :options="statusOptions"
                    @update:model-value="reload"
                />
            </div>
            <div class="col-12 col-sm-4 col-lg-3">
                <q-input
                    v-model="filters.search"
                    dense
                    clearable
                    debounce="400"
                    label="Search name, description, or URL"
                    @update:model-value="reload"
                >
                    <template #prepend>
                        <q-icon name="search" />
                    </template>
                </q-input>
            </div>
            <div class="col-auto flex items-center">
                <q-toggle
                    v-model="filters.encryptedOnly"
                    dense
                    label="Encrypted only"
                    @update:model-value="reload"
                />
            </div>
            <div class="col-auto flex items-center">
                <q-btn
                    flat
                    dense
                    no-caps
                    color="primary"
                    icon="history"
                    label="Audit Log"
                    :to="{ name: 'CmsFileAudit' }"
                />
            </div>
        </div>

        <q-table
            :rows="files"
            :columns="columns"
            row-key="fileGuid"
            :loading="loading"
            v-model:pagination="pagination"
            :rows-per-page-options="[25, 50, 100, 250]"
            dense
            flat
            bordered
            @request="onRequest"
        >
            <template #body-cell-friendlyName="cellProps">
                <q-td :props="cellProps">
                    <div class="row items-center no-wrap q-gutter-x-xs">
                        <a
                            :href="cellProps.row.friendlyUrl"
                            target="_blank"
                            rel="noopener"
                        >
                            {{ cellProps.row.friendlyName }}
                        </a>
                        <q-icon
                            v-if="cellProps.row.encrypted"
                            name="lock"
                            color="warning"
                        >
                            <q-tooltip>Encrypted</q-tooltip>
                        </q-icon>
                        <q-icon
                            v-if="cellProps.row.allowPublicAccess"
                            name="public"
                            color="positive"
                        >
                            <q-tooltip>Public access</q-tooltip>
                        </q-icon>
                        <q-icon
                            v-if="cellProps.row.deletedOn"
                            name="delete_outline"
                            color="negative"
                        >
                            <q-tooltip>Deleted {{ formatDate(cellProps.row.deletedOn) }}</q-tooltip>
                        </q-icon>
                    </div>
                </q-td>
            </template>

            <template #body-cell-permissions="cellProps">
                <q-td :props="cellProps">
                    <PermissionChips
                        :permissions="cellProps.row.permissions"
                        :people-count="cellProps.row.people.length"
                    />
                </q-td>
            </template>

            <template #body-cell-oldUrl="cellProps">
                <q-td :props="cellProps">
                    <span
                        v-if="cellProps.row.oldUrl"
                        class="ellipsis old-url"
                    >
                        {{ cellProps.row.oldUrl }}
                        <q-tooltip>{{ cellProps.row.oldUrl }}</q-tooltip>
                    </span>
                </q-td>
            </template>

            <template #body-cell-modifiedOn="cellProps">
                <ModifiedStamp :cell-props="cellProps" />
            </template>

            <template #body-cell-actions="cellProps">
                <q-td :props="cellProps">
                    <EditButton
                        entity-name="file"
                        @click="openEditDialog(cellProps.row)"
                    />
                    <q-btn
                        dense
                        flat
                        no-caps
                        size="sm"
                        color="secondary"
                        icon="content_copy"
                        aria-label="Copy link"
                        @click="copyUrl(cellProps.row)"
                    >
                        <q-tooltip>Copy link</q-tooltip>
                    </q-btn>
                    <DeleteRestoreButtons
                        :deleted="!!cellProps.row.deletedOn"
                        entity-name="file"
                        @delete="deleteFile(cellProps.row)"
                        @restore="restoreFile(cellProps.row)"
                    />
                </q-td>
            </template>
        </q-table>

        <FileFormDialog
            v-model="showDialog"
            :folders="folders"
            :file="editingFile"
            @saved="reload"
        />
    </div>
</template>

<script setup lang="ts">
import { inject, nextTick, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import FileFormDialog from "@/CMS/components/FileFormDialog.vue"
import DeleteRestoreButtons from "@/CMS/components/DeleteRestoreButtons.vue"
import EditButton from "@/CMS/components/EditButton.vue"
import ModifiedStamp from "@/CMS/components/ModifiedStamp.vue"
import PermissionChips from "@/CMS/components/PermissionChips.vue"
import type { CmsFile } from "@/CMS/types/"

const apiURL = inject("apiURL") + "cms/files/"
const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const { get, del, post, createUrlSearchParams } = useFetch()

const files = ref<CmsFile[]>([])
const folders = ref<string[]>([])
const loading = ref(false)
const showDialog = ref(false)
const editingFile = ref<CmsFile | null>(null)

const filters = ref({
    folder: null as string | null,
    status: "active",
    // The hub's recent-activity rail deep-links here with ?search=<file name>
    search: typeof route.query.search === "string" ? route.query.search : "",
    encryptedOnly: false,
})

const statusOptions = [
    { label: "Active", value: "active" },
    { label: "Deleted", value: "deleted" },
    { label: "All", value: "all" },
]

const fileTools = [
    { label: "Audit Log", icon: "history", to: { name: "CmsFileAudit" } },
    { label: "Import Files", icon: "drive_file_move", to: { name: "CmsFileImport" } },
    { label: "Bulk Encrypt", icon: "lock", to: { name: "CmsBulkEncrypt" } },
]

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
    { name: "permissions", label: "Access", field: "permissions", align: "left" },
    { name: "oldUrl", label: "Old URL", field: "oldUrl", align: "left", sortable: true },
    { name: "modifiedOn", label: "Modified", field: "modifiedOn", align: "left", sortable: true },
    { name: "actions", label: "Actions", field: "fileGuid", align: "center" },
]

async function loadFolders() {
    const res = await get(apiURL + "folders")
    folders.value = res.success ? res.result : []
}

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
        folder: filters.value.folder,
        status: filters.value.status,
        search: filters.value.search || null,
        encrypted: filters.value.encryptedOnly ? "true" : null,
        page,
        perPage: rowsPerPage,
        sortBy: sortBy ?? "friendlyName",
        descending: descending ? "true" : "false",
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
    void onRequest({ pagination: { ...pagination.value, page: 1 } })
}

function openUploadDialog() {
    editingFile.value = null
    showDialog.value = true
}

function openEditDialog(file: CmsFile) {
    editingFile.value = file
    showDialog.value = true
}

async function copyUrl(file: CmsFile) {
    try {
        await navigator.clipboard.writeText(file.friendlyUrl)
        $q.notify({ type: "positive", message: "Link copied" })
    } catch {
        $q.notify({ type: "negative", message: "Failed to copy link" })
    }
}

async function deleteFile(file: CmsFile) {
    const confirmed = await confirmAction(
        "Delete File",
        `Mark "${file.friendlyName}" as deleted? It can be restored later.`,
        "Delete",
        "negative",
    )
    if (!confirmed) return
    const res = await del(apiURL + file.fileGuid)
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to delete file" })
        return
    }
    $q.notify({ type: "positive", message: "File marked as deleted" })
    reload()
}

async function restoreFile(file: CmsFile) {
    const res = await post(apiURL + file.fileGuid + "/restore")
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to restore file" })
        return
    }
    $q.notify({ type: "positive", message: "File restored" })
    reload()
}

async function confirmAction(title: string, message: string, okLabel: string, okColor = "primary") {
    return await new Promise<boolean>((resolve) => {
        $q.dialog({
            title,
            message,
            cancel: { label: "Cancel", flat: true },
            persistent: true,
            ok: { label: okLabel, color: okColor, unelevated: true },
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
            .onDismiss(() => resolve(false))
    })
}

function formatDate(value: string | null): string {
    if (!value) return ""
    return new Date(value).toLocaleDateString("en-US", { year: "2-digit", month: "2-digit", day: "2-digit" })
}

// The "Upload File" left-nav link targets ?upload=1; consume the flag so
// re-clicking the link re-opens the dialog after it has been closed.
// Strip the query BEFORE opening: route-focus moves focus to <main> after
// each navigation (use-route-focus.ts), which would close an already-open dialog.
watch(
    () => route.query.upload,
    async (upload) => {
        if (upload !== undefined) {
            await router.replace({ query: { ...route.query, upload: undefined } })
            await nextTick()
            openUploadDialog()
        }
    },
    { immediate: true },
)

onMounted(() => {
    loadFolders()
    reload()
})
</script>

<style scoped>
.old-url {
    max-width: 200px;
    display: inline-block;
    vertical-align: middle;
}
</style>
