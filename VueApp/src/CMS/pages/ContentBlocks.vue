<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md">
            <h1 class="q-my-none">Manage Content Blocks</h1>
            <q-space />
            <q-btn
                color="positive"
                icon="add"
                label="Add Content Block"
                no-caps
                dense
                class="q-pr-md"
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
                    @update:model-value="loadBlocks"
                />
            </div>
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.system"
                    dense
                    options-dense
                    clearable
                    label="System"
                    :options="['Viper', 'Public']"
                    @update:model-value="loadBlocks"
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
                    @update:model-value="loadBlocks"
                />
            </div>
            <div class="col-12 col-sm-3 col-lg-3">
                <q-input
                    v-model="filters.search"
                    dense
                    clearable
                    debounce="400"
                    label="Search title, name, page, or content"
                    @update:model-value="loadBlocks"
                >
                    <template #prepend>
                        <q-icon name="search" />
                    </template>
                </q-input>
            </div>
        </div>

        <q-table
            :rows="blocks"
            :columns="columns"
            row-key="contentBlockId"
            :loading="loading"
            :pagination="{ rowsPerPage: 50, sortBy: 'title' }"
            :rows-per-page-options="[25, 50, 100, 0]"
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
                    <PermissionChips :permissions="cellProps.row.permissions" />
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
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { inject, onMounted, ref } from "vue"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import DeleteRestoreButtons from "@/CMS/components/DeleteRestoreButtons.vue"
import EditButton from "@/CMS/components/EditButton.vue"
import ModifiedStamp from "@/CMS/components/ModifiedStamp.vue"
import PermissionChips from "@/CMS/components/PermissionChips.vue"
import StatusIcon from "@/CMS/components/StatusIcon.vue"
import type { CmsContentBlock } from "@/CMS/types/"

const apiURL = inject("apiURL") + "CMS/content"
const $q = useQuasar()
const { get, del, post, createUrlSearchParams } = useFetch()

const blocks = ref<CmsContentBlock[]>([])
const sectionPaths = ref<string[]>([])
const loading = ref(false)

const filters = ref({
    status: "active",
    system: null as string | null,
    viperSectionPath: null as string | null,
    search: "",
})

const statusOptions = [
    { label: "Active", value: "active" },
    { label: "Deleted", value: "deleted" },
    { label: "All", value: "all" },
]

const columns: QTableProps["columns"] = [
    { name: "title", label: "Title", field: "title", align: "left", sortable: true },
    { name: "system", label: "System", field: "system", align: "left", sortable: true },
    { name: "viperSectionPath", label: "VIPER section", field: "viperSectionPath", align: "left", sortable: true },
    { name: "page", label: "Page", field: "page", align: "left", sortable: true },
    { name: "blockOrder", label: "Order", field: "blockOrder", align: "center", sortable: true },
    { name: "permissions", label: "Access", field: "permissions", align: "left" },
    { name: "modifiedOn", label: "Modified", field: "modifiedOn", align: "left", sortable: true },
    { name: "actions", label: "Actions", field: "contentBlockId", align: "center" },
]

async function loadBlocks() {
    loading.value = true
    const params = createUrlSearchParams({
        status: filters.value.status,
        system: filters.value.system,
        viperSectionPath: filters.value.viperSectionPath,
        search: filters.value.search || null,
    })
    const res = await get(apiURL + "?" + params)
    blocks.value = res.success ? res.result : []
    loading.value = false
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

onMounted(() => {
    loadSectionPaths()
    loadBlocks()
})
</script>
