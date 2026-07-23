<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md list-page-header">
            <h1 class="q-my-none">Manage Left-Nav Menus</h1>
            <q-space />
            <q-btn
                color="positive"
                icon="add"
                label="Add Left-Nav Menu"
                no-caps
                dense
                @click="showAddDialog = true"
            />
        </div>

        <div class="row q-col-gutter-md q-mb-sm">
            <div class="col-12 col-sm-3 col-lg-2">
                <q-select
                    v-model="filters.system"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    label="System"
                    :options="systemOptions"
                    @update:model-value="loadMenus"
                />
            </div>
            <div class="col-12 col-sm-4 col-lg-3">
                <q-input
                    v-model="filters.search"
                    dense
                    clearable
                    outlined
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

        <!-- Left-nav menus are a small, bounded set (one row per menu), so the list stays
             client-side; the "All" (0) rows-per-page option is dropped since even the full
             list is a short page. -->
        <q-table
            :rows="menus"
            :columns="columns"
            row-key="leftNavMenuId"
            :loading="loading"
            :pagination="{ rowsPerPage: 50, sortBy: 'menuHeaderText' }"
            :rows-per-page-options="[25, 50, 100]"
            :grid="$q.screen.lt.sm"
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

            <template #item="{ row }">
                <ListCard>
                    <template #header>
                        <router-link
                            class="text-weight-medium"
                            :to="{ name: 'CmsLeftNavEdit', params: { id: row.leftNavMenuId } }"
                        >
                            {{ row.menuHeaderText || "(untitled)" }}
                        </router-link>
                    </template>

                    <ListCardField
                        label="System"
                        :value="row.system"
                    />
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
                    <ListCardField
                        v-if="row.friendlyName"
                        label="Friendly name"
                        :value="row.friendlyName"
                    />
                    <ListCardField
                        label="Items"
                        :value="row.items.length"
                    />
                    <ListCardField label="Modified">
                        <ModifiedStamp :row="row" />
                    </ListCardField>

                    <template #actions>
                        <EditButton
                            entity-name="menu"
                            :to="{ name: 'CmsLeftNavEdit', params: { id: row.leftNavMenuId } }"
                        />
                        <q-btn
                            dense
                            flat
                            no-caps
                            size="sm"
                            color="negative"
                            icon="delete"
                            aria-label="Delete menu"
                            @click="deleteMenu(row)"
                        >
                            <q-tooltip>Delete</q-tooltip>
                        </q-btn>
                    </template>
                </ListCard>
            </template>

            <template #no-data>
                <div class="full-width text-center text-grey-7 q-py-md">
                    No left-nav menus yet. Use the "Add Left-Nav Menu" button to create one.
                </div>
            </template>
        </q-table>

        <LeftNavMenuDialog
            v-model="showAddDialog"
            @created="onMenuCreated"
        />
    </div>
</template>

<script setup lang="ts">
import { inject, nextTick, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar, type QTableProps } from "quasar"
import { inflect } from "inflection"
import { useFetch } from "@/composables/ViperFetch"
import EditButton from "@/CMS/components/EditButton.vue"
import LeftNavMenuDialog from "@/CMS/components/LeftNavMenuDialog.vue"
import ListCard from "@/CMS/components/ListCard.vue"
import ListCardField from "@/CMS/components/ListCardField.vue"
import ModifiedStamp from "@/CMS/components/ModifiedStamp.vue"
import type { CmsLeftNavMenu } from "@/CMS/types/"

const apiURL = inject("apiURL") + "cms/left-navs"
const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const { get, del, createUrlSearchParams } = useFetch()

const menus = ref<CmsLeftNavMenu[]>([])
const loading = ref(false)
const showAddDialog = ref(false)

const filters = ref({
    system: null as string | null,
    search: "",
})

const systemOptions = [
    { label: "All", value: null },
    { label: "Viper", value: "Viper" },
    { label: "Public", value: "Public" },
]

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
    if (res.success) {
        menus.value = res.result
    } else {
        menus.value = []
        $q.notify({ type: "negative", message: res.errors?.[0] ?? "Failed to load menus" })
    }
    loading.value = false
}

async function deleteMenu(menu: CmsLeftNavMenu) {
    const confirmed = await new Promise<boolean>((resolve) => {
        $q.dialog({
            title: "Delete Menu",
            message: `Permanently delete "${menu.menuHeaderText || "(untitled)"}" and its ${menu.items.length} ${inflect("item", menu.items.length)}? This cannot be undone.`,
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

function onMenuCreated(leftNavMenuId: number) {
    // After creating the menu's settings, go to the page to manage its items.
    void router.push({ name: "CmsLeftNavEdit", params: { id: leftNavMenuId } })
}

// The "Add Left-Nav Menu" left-nav link targets ?add=1; consume the flag so re-clicking
// the link opens the dialog even when this page is already mounted (in-app navigation
// reuses the component instance, so onMounted alone would miss it).
// Strip the query BEFORE opening: route-focus moves focus to <main> after each
// navigation (use-route-focus.ts), which would close an already-open dialog.
watch(
    () => route.query.add,
    async (add) => {
        if (add !== undefined) {
            await router.replace({ query: { ...route.query, add: undefined } })
            await nextTick()
            showAddDialog.value = true
        }
    },
    { immediate: true },
)

onMounted(() => {
    loadMenus()
})
</script>
