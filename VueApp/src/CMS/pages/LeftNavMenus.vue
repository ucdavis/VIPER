<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md">
            <h1 class="q-my-none">Manage Left-Nav Menus</h1>
            <q-space />
            <q-btn
                color="positive"
                icon="add"
                label="New Menu"
                no-caps
                dense
                class="q-pr-md"
                :to="{ name: 'CmsLeftNavEdit' }"
            />
        </div>

        <div class="row q-col-gutter-md q-mb-sm">
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.system"
                    dense
                    options-dense
                    clearable
                    label="System"
                    :options="['Viper', 'Public']"
                    @update:model-value="loadMenus"
                />
            </div>
            <div class="col-12 col-sm-4 col-lg-3">
                <q-input
                    v-model="filters.search"
                    dense
                    clearable
                    debounce="400"
                    label="Search header, name, or page"
                    @update:model-value="loadMenus"
                >
                    <template #prepend>
                        <q-icon name="search" />
                    </template>
                </q-input>
            </div>
        </div>

        <q-table
            :rows="menus"
            :columns="columns"
            row-key="leftNavMenuId"
            :loading="loading"
            :pagination="{ rowsPerPage: 50, sortBy: 'menuHeaderText' }"
            :rows-per-page-options="[25, 50, 100, 0]"
            dense
            flat
            bordered
        >
            <template #body-cell-menuHeaderText="cellProps">
                <q-td :props="cellProps">
                    <router-link :to="{ name: 'CmsLeftNavEdit', params: { id: cellProps.row.leftNavMenuId } }">
                        {{ cellProps.row.menuHeaderText || "(untitled)" }}
                    </router-link>
                </q-td>
            </template>

            <template #body-cell-items="cellProps">
                <q-td :props="cellProps">{{ cellProps.row.items.length }}</q-td>
            </template>

            <template #body-cell-modifiedOn="cellProps">
                <ModifiedStamp :cell-props="cellProps" />
            </template>

            <template #body-cell-actions="cellProps">
                <q-td :props="cellProps">
                    <EditButton
                        entity-name="menu"
                        :to="{ name: 'CmsLeftNavEdit', params: { id: cellProps.row.leftNavMenuId } }"
                    />
                    <q-btn
                        dense
                        flat
                        no-caps
                        size="sm"
                        color="negative"
                        icon="delete"
                        aria-label="Delete menu"
                        @click="deleteMenu(cellProps.row)"
                    >
                        <q-tooltip>Delete</q-tooltip>
                    </q-btn>
                </q-td>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { inject, onMounted, ref } from "vue"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import EditButton from "@/CMS/components/EditButton.vue"
import ModifiedStamp from "@/CMS/components/ModifiedStamp.vue"
import type { CmsLeftNavMenu } from "@/CMS/types/"

const apiURL = inject("apiURL") + "cms/left-navs"
const $q = useQuasar()
const { get, del, createUrlSearchParams } = useFetch()

const menus = ref<CmsLeftNavMenu[]>([])
const loading = ref(false)

const filters = ref({
    system: null as string | null,
    search: "",
})

const columns: QTableProps["columns"] = [
    { name: "menuHeaderText", label: "Menu Header", field: "menuHeaderText", align: "left", sortable: true },
    { name: "system", label: "System", field: "system", align: "left", sortable: true },
    { name: "viperSectionPath", label: "VIPER section", field: "viperSectionPath", align: "left", sortable: true },
    { name: "page", label: "Page", field: "page", align: "left", sortable: true },
    { name: "friendlyName", label: "Friendly name", field: "friendlyName", align: "left", sortable: true },
    { name: "items", label: "Items", field: (row: CmsLeftNavMenu) => row.items.length, align: "center" },
    { name: "modifiedOn", label: "Modified", field: "modifiedOn", align: "left", sortable: true },
    { name: "actions", label: "Actions", field: "leftNavMenuId", align: "center" },
]

async function loadMenus() {
    loading.value = true
    const params = createUrlSearchParams({
        system: filters.value.system,
        search: filters.value.search || null,
    })
    const res = await get(apiURL + "?" + params)
    menus.value = res.success ? res.result : []
    loading.value = false
}

async function deleteMenu(menu: CmsLeftNavMenu) {
    const confirmed = await new Promise<boolean>((resolve) => {
        $q.dialog({
            title: "Delete Menu",
            message: `Permanently delete "${menu.menuHeaderText}" and its ${menu.items.length} items? This cannot be undone.`,
            cancel: { label: "Cancel", flat: true },
            persistent: true,
            ok: { label: "Delete Menu", color: "negative", unelevated: true },
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
            .onDismiss(() => resolve(false))
    })
    if (!confirmed) return
    const res = await del(apiURL + "/" + menu.leftNavMenuId)
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to delete menu" })
        return
    }
    $q.notify({ type: "positive", message: "Menu deleted" })
    loadMenus()
}

onMounted(loadMenus)
</script>
