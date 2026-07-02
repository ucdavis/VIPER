<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md list-page-header">
            <h1 class="q-my-none">Manage Content Blocks</h1>
            <q-space />
            <q-btn
                color="positive"
                icon="add"
                label="Add Content Block"
                no-caps
                dense
                :to="{ name: 'CmsContentBlockEdit' }"
            />
        </div>

        <div class="row q-col-gutter-md q-mb-sm">
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
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.viperSectionPath"
                    dense
                    options-dense
                    clearable
                    label="VIPER section"
                    :options="sectionPaths"
                    @update:model-value="reload"
                />
            </div>
            <div class="col-12 col-sm-3 col-lg-3">
                <q-input
                    v-model="filters.search"
                    dense
                    clearable
                    debounce="400"
                    label="Search title, name, page, or content"
                    @update:model-value="reload"
                >
                    <template #prepend>
                        <q-icon name="search" />
                    </template>
                </q-input>
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
            :rows="blocks"
            :columns="columns"
            row-key="contentBlockId"
            :loading="loading"
            :pagination="{ rowsPerPage: 50, sortBy: 'title' }"
            :rows-per-page-options="[25, 50, 100, 0]"
            :grid="$q.screen.lt.md"
            dense
            flat
            bordered
        >
            <template #body-cell-title="cellProps">
                <q-td :props="cellProps">
                    <div class="row items-center no-wrap q-gutter-x-xs">
                        <router-link
                            :to="{ name: 'CmsContentBlockEdit', params: { id: cellProps.row.contentBlockId } }"
                        >
                            {{ cellProps.row.title || "(untitled)" }}
                        </router-link>
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
                            label="Deleted"
                        />
                    </div>
                    <div class="text-caption text-grey-7">{{ cellProps.row.friendlyName }}</div>
                </q-td>
            </template>

            <template #body-cell-permissions="cellProps">
                <q-td :props="cellProps">
                    <PermissionChips
                        :permissions="cellProps.row.permissions"
                        stacked
                    />
                </q-td>
            </template>

            <template #body-cell-modifiedOn="cellProps">
                <ModifiedStamp :cell-props="cellProps" />
            </template>

            <template #body-cell-actions="cellProps">
                <q-td :props="cellProps">
                    <EditButton
                        entity-name="content block"
                        :to="{ name: 'CmsContentBlockEdit', params: { id: cellProps.row.contentBlockId } }"
                    />
                    <DeleteRestoreButtons
                        :deleted="!!cellProps.row.deletedOn"
                        entity-name="content block"
                        @delete="deleteBlock(cellProps.row)"
                        @restore="restoreBlock(cellProps.row)"
                    />
                </q-td>
            </template>

            <template #item="{ row }">
                <ListCard>
                    <template #header>
                        <div class="row items-center no-wrap q-gutter-x-xs">
                            <router-link
                                class="text-weight-medium"
                                :to="{ name: 'CmsContentBlockEdit', params: { id: row.contentBlockId } }"
                            >
                                {{ row.title || "(untitled)" }}
                            </router-link>
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
                                label="Deleted"
                            />
                        </div>
                        <div
                            v-if="row.friendlyName"
                            class="text-caption text-grey-7"
                        >
                            {{ row.friendlyName }}
                        </div>
                    </template>

                    <ListCardField
                        v-if="row.viperSectionPath"
                        label="VIPER section"
                        :value="row.viperSectionPath"
                    />
                    <ListCardField
                        v-if="row.page"
                        label="Page"
                        :value="row.page"
                    />
                    <ListCardField label="Access">
                        <PermissionChips :permissions="row.permissions" />
                    </ListCardField>
                    <ListCardField label="Modified">
                        <ModifiedStamp :row="row" />
                    </ListCardField>

                    <template #actions>
                        <EditButton
                            entity-name="content block"
                            :to="{ name: 'CmsContentBlockEdit', params: { id: row.contentBlockId } }"
                        />
                        <DeleteRestoreButtons
                            :deleted="!!row.deletedOn"
                            entity-name="content block"
                            @delete="deleteBlock(row)"
                            @restore="restoreBlock(row)"
                        />
                    </template>
                </ListCard>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
// Template-size synthetic complexity only (large filter + table markup); script logic is small.
// fallow-ignore-file complexity
import { inject, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import DeleteRestoreButtons from "@/CMS/components/DeleteRestoreButtons.vue"
import EditButton from "@/CMS/components/EditButton.vue"
import ListCard from "@/CMS/components/ListCard.vue"
import ListCardField from "@/CMS/components/ListCardField.vue"
import ModifiedStamp from "@/CMS/components/ModifiedStamp.vue"
import PermissionChips from "@/CMS/components/PermissionChips.vue"
import StatusIcon from "@/CMS/components/StatusIcon.vue"
import type { CmsContentBlock } from "@/CMS/types/"

const apiURL = inject("apiURL") + "CMS/content"
const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const { get, del, post, createUrlSearchParams } = useFetch()

const blocks = ref<CmsContentBlock[]>([])
const sectionPaths = ref<string[]>([])
const loading = ref(false)

// Filters initialize from the URL so views can be shared/deep-linked, matching
// the Files list and audit trail.
const filters = ref({
    status: typeof route.query.status === "string" ? route.query.status : "active",
    viperSectionPath: typeof route.query.section === "string" ? route.query.section : null,
    search: typeof route.query.search === "string" ? route.query.search : "",
    publicOnly: route.query.public === "1",
})

// Reflect the active filters back into the URL (defaults are omitted).
function syncFiltersToUrl() {
    void router.replace({
        query: {
            ...route.query,
            status: filters.value.status !== "active" ? filters.value.status : undefined,
            section: filters.value.viperSectionPath || undefined,
            search: filters.value.search || undefined,
            public: filters.value.publicOnly ? "1" : undefined,
        },
    })
}

const statusOptions = [
    { label: "Active", value: "active" },
    { label: "Deleted", value: "deleted" },
    { label: "All", value: "all" },
]

const columns: QTableProps["columns"] = [
    { name: "title", label: "Title", field: "title", align: "left", sortable: true },
    { name: "viperSectionPath", label: "VIPER section", field: "viperSectionPath", align: "left", sortable: true },
    { name: "page", label: "Page", field: "page", align: "left", sortable: true },
    { name: "permissions", label: "Access", field: "permissions", align: "left" },
    { name: "modifiedOn", label: "Modified", field: "modifiedOn", align: "left", sortable: true },
    { name: "actions", label: "Actions", field: "contentBlockId", align: "center" },
]

async function loadBlocks() {
    loading.value = true
    const params = createUrlSearchParams({
        status: filters.value.status,
        viperSectionPath: filters.value.viperSectionPath,
        search: filters.value.search || null,
        isPublic: filters.value.publicOnly ? "true" : null,
    })
    const res = await get(apiURL + "?" + params)
    blocks.value = res.success ? res.result : []
    loading.value = false
}

// Filter changes both reload the list and update the URL; the route watcher
// guards against re-fetching on our own URL write.
function reload() {
    syncFiltersToUrl()
    loadBlocks()
}

async function loadSectionPaths() {
    const res = await get(apiURL + "/section-paths")
    sectionPaths.value = res.success ? res.result : []
}

async function deleteBlock(block: CmsContentBlock) {
    const confirmed = await new Promise<boolean>((resolve) => {
        $q.dialog({
            title: "Delete Content Block",
            message: `Mark "${block.title}" as deleted? It can be restored later.`,
            cancel: { label: "Cancel", flat: true },
            persistent: true,
            ok: { label: "Delete", color: "negative", unelevated: true },
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
            .onDismiss(() => resolve(false))
    })
    if (!confirmed) return
    const res = await del(apiURL + "/" + block.contentBlockId)
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to delete content block" })
        return
    }
    $q.notify({ type: "positive", message: "Content block marked as deleted" })
    loadBlocks()
}

async function restoreBlock(block: CmsContentBlock) {
    const res = await post(apiURL + "/" + block.contentBlockId + "/restore")
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to restore content block" })
        return
    }
    $q.notify({ type: "positive", message: "Content block restored" })
    loadBlocks()
}

// Re-sync filters when in-app navigation reuses this view with a different query
// (e.g. a hub deep-link or re-clicked nav link). The equality guard skips our own
// syncFiltersToUrl write, which would otherwise trigger a redundant fetch.
watch(
    () => route.query,
    (query) => {
        const next = {
            status: typeof query.status === "string" ? query.status : "active",
            viperSectionPath: typeof query.section === "string" ? query.section : null,
            search: typeof query.search === "string" ? query.search : "",
            publicOnly: query.public === "1",
        }
        const f = filters.value
        if (
            next.status === f.status &&
            next.viperSectionPath === f.viperSectionPath &&
            next.search === f.search &&
            next.publicOnly === f.publicOnly
        ) {
            return
        }
        filters.value = next
        loadBlocks()
    },
)

onMounted(() => {
    loadSectionPaths()
    loadBlocks()
})
</script>
