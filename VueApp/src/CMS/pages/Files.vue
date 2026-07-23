<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md list-page-header">
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
                label="Add File"
                no-caps
                dense
                @click="openUploadDialog"
            />
        </div>

        <div class="row q-col-gutter-md q-mb-sm">
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.folder"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    label="VIPER app"
                    :options="filterFolders"
                    @update:model-value="reload"
                />
            </div>
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.status"
                    dense
                    options-dense
                    outlined
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
                    outlined
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
                <q-toggle
                    v-model="filters.publicOnly"
                    dense
                    label="Public only"
                    @update:model-value="reload"
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
            :grid="$q.screen.lt.md"
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
                            <span class="sr-only">(opens in new window)</span>
                        </a>
                        <StatusIcon
                            v-if="cellProps.row.encrypted"
                            icon="lock"
                            color="warning"
                            label="Encrypted"
                        />
                        <StatusIcon
                            v-if="cellProps.row.allowPublicAccess"
                            icon="public"
                            color="positive"
                            label="Public access"
                        />
                        <StatusIcon
                            v-if="cellProps.row.deletedOn"
                            icon="delete_outline"
                            color="negative"
                            :label="deletedLabel(cellProps.row)"
                        />
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
                    <a
                        v-if="cellProps.row.oldUrl"
                        :href="oldUrlHref(cellProps.row.oldUrl)"
                        target="_blank"
                        rel="noopener"
                    >
                        Old URL
                        <span class="sr-only">{{ cellProps.row.oldUrl }} (opens in new window)</span>
                        <q-tooltip>{{ cellProps.row.oldUrl }}</q-tooltip>
                    </a>
                </q-td>
            </template>

            <template #body-cell-modifiedOn="cellProps">
                <ModifiedStamp :cell-props="cellProps" />
            </template>

            <template #body-cell-purgeOn="cellProps">
                <q-td :props="cellProps">
                    <template v-if="cellProps.row.purgeOn">
                        {{ formatDate(cellProps.row.purgeOn) }}
                        <StatusBadge
                            v-if="purgeImminent(cellProps.row)"
                            color="warning"
                            label="soon"
                            class="q-ml-xs"
                        />
                    </template>
                </q-td>
            </template>

            <template #body-cell-actions="cellProps">
                <q-td :props="cellProps">
                    <EditButton
                        entity-name="file"
                        @click="openEditDialog(cellProps.row)"
                    />
                    <DeleteRestoreButtons
                        :deleted="!!cellProps.row.deletedOn"
                        entity-name="file"
                        @delete="deleteFile(cellProps.row)"
                        @restore="restoreFile(cellProps.row)"
                    />
                </q-td>
            </template>

            <template #item="{ row }">
                <ListCard class="list-card-compact">
                    <template #header>
                        <!-- Title, status icons, and row actions share the first line so edit/delete
                             align with the file name rather than centring against a taller block. -->
                        <div class="row items-center no-wrap q-gutter-x-xs">
                            <a
                                class="ellipsis card-header-title"
                                :href="row.friendlyUrl"
                                target="_blank"
                                rel="noopener"
                            >
                                {{ row.friendlyName }}
                                <span class="sr-only">(opens in new window)</span>
                            </a>
                            <StatusIcon
                                v-if="row.encrypted"
                                icon="lock"
                                color="warning"
                                label="Encrypted"
                            />
                            <StatusIcon
                                v-if="row.allowPublicAccess"
                                icon="public"
                                color="positive"
                                label="Public access"
                            />
                            <StatusIcon
                                v-if="row.deletedOn"
                                icon="delete_outline"
                                color="negative"
                                :label="deletedLabel(row)"
                            />
                            <q-space />
                            <div class="col-auto row items-center no-wrap list-card-actions">
                                <EditButton
                                    entity-name="file"
                                    @click="openEditDialog(row)"
                                />
                                <DeleteRestoreButtons
                                    :deleted="!!row.deletedOn"
                                    entity-name="file"
                                    @delete="deleteFile(row)"
                                    @restore="restoreFile(row)"
                                />
                            </div>
                        </div>
                    </template>

                    <!-- Two fields per row (see .card-field-grid in base.css), always-present fields
                         first, then the conditional Old URL / Purges. -->
                    <div class="card-field-grid">
                        <ListCardField
                            label="VIPER app"
                            :value="row.folder"
                        />
                        <ListCardField label="Access">
                            <PermissionChips
                                :permissions="row.permissions"
                                :people-count="row.people.length"
                            />
                        </ListCardField>
                        <ListCardField label="Modified">
                            <ModifiedStamp :row="row" />
                        </ListCardField>
                        <ListCardField
                            v-if="row.oldUrl"
                            label="Old URL"
                        >
                            <a
                                :href="oldUrlHref(row.oldUrl)"
                                target="_blank"
                                rel="noopener"
                            >
                                {{ row.oldUrl }}
                                <span class="sr-only">(opens in new window)</span>
                            </a>
                        </ListCardField>
                        <ListCardField
                            v-if="row.purgeOn"
                            label="Purges"
                        >
                            {{ formatDate(row.purgeOn) }}
                            <StatusBadge
                                v-if="purgeImminent(row)"
                                color="warning"
                                label="soon"
                                class="q-ml-xs"
                            />
                        </ListCardField>
                    </div>
                </ListCard>
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
// Template-size synthetic complexity only; the page's data logic lives in useServerTable.
// fallow-ignore-file complexity
import { computed, inject, nextTick, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import { useServerTable } from "@/CMS/composables/use-server-table"
import StatusBadge from "@/components/StatusBadge.vue"
import FileFormDialog from "@/CMS/components/FileFormDialog.vue"
import DeleteRestoreButtons from "@/CMS/components/DeleteRestoreButtons.vue"
import EditButton from "@/CMS/components/EditButton.vue"
import ListCard from "@/CMS/components/ListCard.vue"
import ListCardField from "@/CMS/components/ListCardField.vue"
import ModifiedStamp from "@/CMS/components/ModifiedStamp.vue"
import PermissionChips from "@/CMS/components/PermissionChips.vue"
import StatusIcon from "@/CMS/components/StatusIcon.vue"
import type { CmsFile } from "@/CMS/types/"

const apiURL = inject("apiURL") + "cms/files/"
const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const { get, del, post, createUrlSearchParams } = useFetch()

const ALL_FOLDERS = "All"

const folders = ref<string[]>([])
type FolderOption = { label: string; value: string }

const filterFolders = ref<FolderOption[]>([{ label: ALL_FOLDERS, value: ALL_FOLDERS }])
const showDialog = ref(false)
const editingFile = ref<CmsFile | null>(null)

// Maps the URL query into the filter shape, shared by the initial state and the re-sync watcher
// below so the two can't drift.
function parseFiltersFromQuery(query: typeof route.query) {
    return {
        folder: typeof query.folder === "string" ? query.folder : ALL_FOLDERS,
        status: typeof query.status === "string" ? query.status : "active",
        search: typeof query.search === "string" ? query.search : "",
        encryptedOnly: query.encrypted === "1",
        publicOnly: query.public === "1",
    }
}

// Filters initialize from the URL so views can be shared/deep-linked
// (the hub's recent-activity rail uses ?search=<file name>). The deleted/all views are open to all
// file managers; the API scopes them to files the user deleted (admins see the whole trash).
const filters = ref(parseFiltersFromQuery(route.query))

// Reflect the active filters back into the URL (defaults are omitted).
// `upload` is a one-shot action flag, never a persisted filter: strip it here so a
// filter-sync write that races the upload-watcher's own strip (both fire on mount)
// can't re-add the stale flag via the ...route.query spread.
function syncFiltersToUrl() {
    void router.replace({
        query: {
            ...route.query,
            folder: filters.value.folder !== ALL_FOLDERS ? filters.value.folder : undefined,
            status: filters.value.status !== "active" ? filters.value.status : undefined,
            search: filters.value.search || undefined,
            encrypted: filters.value.encryptedOnly ? "1" : undefined,
            public: filters.value.publicOnly ? "1" : undefined,
            upload: undefined,
        },
    })
}

// The old URL is the file's original VIPER 1 webroot path, kept so links
// that predate the import still resolve (via the 404 -> missingFile -> CMS
// lookup chain). Link it on the VIPER 1 host, matching the legacy UI, so
// clicking exercises the same path end users' old links take.
function oldUrlHref(oldUrl: string): string {
    // Join with exactly one slash regardless of whether the env var carries a trailing one.
    const viper1Host = (import.meta.env.VITE_VIPER_1_HOME ?? "").replace(/\/$/, "")
    return viper1Host + "/" + oldUrl.replace(/^\//, "")
}

const statusOptions = [
    { label: "Active", value: "active" },
    { label: "Deleted", value: "deleted" },
    { label: "All", value: "all" },
]

const fileTools = [
    { label: "Audit Trail", icon: "history", to: { name: "CmsFileAudit" } },
    { label: "Import Files", icon: "drive_file_move", to: { name: "CmsFileImport" } },
    { label: "Bulk Encrypt", icon: "lock", to: { name: "CmsBulkEncrypt" } },
]

const columns = computed<QTableProps["columns"]>(() => {
    const cols: QTableProps["columns"] = [
        { name: "friendlyName", label: "File", field: "friendlyName", align: "left", sortable: true },
        { name: "folder", label: "VIPER app", field: "folder", align: "left", sortable: true },
        { name: "permissions", label: "Access", field: "permissions", align: "left" },
        { name: "oldUrl", label: "Old URL", field: "oldUrl", align: "left", sortable: true },
    ]
    // Drop Modified below lg (< 1440px) so the table fits iPad landscape without horizontal scroll
    // (the date + editor still show in card mode and on wide desktop). Mirrors Content Blocks.
    if (!$q.screen.lt.lg) {
        cols.push({ name: "modifiedOn", label: "Modified", field: "modifiedOn", align: "left", sortable: true })
    }
    // Only trash rows carry a purge date, so the column exists only where they can appear.
    if (filters.value.status !== "active") {
        cols.push({ name: "purgeOn", label: "Purges", field: "purgeOn", align: "left", sortable: true })
    }
    cols.push({ name: "actions", label: "Actions", field: "fileGuid", align: "center" })
    return cols
})

const {
    rows: files,
    loading,
    pagination,
    onRequest,
    reloadFirstPage,
} = useServerTable<CmsFile>({
    url: apiURL,
    errorMessage: "Failed to load files",
    pagination: { sortBy: "friendlyName", descending: false },
    buildParams: (p) => ({
        folder: filters.value.folder !== ALL_FOLDERS ? filters.value.folder : null,
        status: filters.value.status,
        search: filters.value.search || null,
        encrypted: filters.value.encryptedOnly ? "true" : null,
        isPublic: filters.value.publicOnly ? "true" : null,
        page: p.page,
        perPage: p.rowsPerPage,
        // PurgeOn is deletedOn plus the fixed retention window, so they order identically.
        sortBy: p.sortBy === "purgeOn" ? "deletedOn" : (p.sortBy ?? "friendlyName"),
        descending: p.descending ? "true" : "false",
    }),
})

async function loadFolders() {
    // Upload destinations are the disk allow-list; the filter dropdown is built
    // separately (loadFolderOptions) from record folders plus per-folder counts.
    const destinations = await get(apiURL + "folders")
    folders.value = destinations.success ? destinations.result : []
}

async function loadFolderOptions() {
    // Counts reflect the status/encrypted/public filters so they match the list;
    // search is intentionally excluded (the dropdown is a folder inventory).
    const countParams = createUrlSearchParams({
        status: filters.value.status,
        encrypted: filters.value.encryptedOnly ? "true" : null,
        isPublic: filters.value.publicOnly ? "true" : null,
    })
    const [filterable, countsRes] = await Promise.all([
        get(apiURL + "folders?includeData=true"),
        get(apiURL + "folder-counts?" + countParams),
    ])
    const counts: { folder: string; count: number }[] = countsRes.success ? countsRes.result : []
    const countByFolder = new Map(counts.map((c) => [c.folder.toLowerCase(), c.count]))
    const names: string[] = filterable.success ? filterable.result : []
    const total = counts.reduce((sum, c) => sum + c.count, 0)
    filterFolders.value = [
        { label: `${ALL_FOLDERS} (${total})`, value: ALL_FOLDERS },
        ...names.map((n) => ({ label: `${n} (${countByFolder.get(n.toLowerCase()) ?? 0})`, value: n })),
    ]
}

function reload() {
    syncFiltersToUrl()
    void loadFolderOptions()
    void reloadFirstPage()
}

function openUploadDialog() {
    editingFile.value = null
    showDialog.value = true
}

function openEditDialog(file: CmsFile) {
    editingFile.value = file
    showDialog.value = true
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

// In the trash, show when the file will be auto-purged so it's clear it isn't kept forever.
function deletedLabel(row: CmsFile): string {
    const base = `Deleted ${formatDate(row.deletedOn)}`
    return row.purgeOn ? `${base} · purges ${formatDate(row.purgeOn)}` : base
}

// An overdue purge date (the purge job hasn't caught up yet) still counts as imminent.
const PURGE_SOON_DAYS = 7

function purgeImminent(row: CmsFile): boolean {
    if (!row.purgeOn) return false
    return new Date(row.purgeOn).getTime() - Date.now() <= PURGE_SOON_DAYS * 86_400_000
}

// When in-app navigation reuses this view with a different query (e.g. re-clicking the
// left-nav "Manage Files" link while a filter is active, or a hub deep-link), the component
// instance is kept, so re-sync the filters from the URL. The equality guard skips our own
// syncFiltersToUrl write, which would otherwise trigger a redundant second fetch.
watch(
    () => route.query,
    (query) => {
        const next = parseFiltersFromQuery(query)
        const f = filters.value
        if (
            next.folder === f.folder &&
            next.status === f.status &&
            next.search === f.search &&
            next.encryptedOnly === f.encryptedOnly &&
            next.publicOnly === f.publicOnly
        ) {
            return
        }
        filters.value = next
        void loadFolderOptions()
        void reloadFirstPage()
    },
)

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
