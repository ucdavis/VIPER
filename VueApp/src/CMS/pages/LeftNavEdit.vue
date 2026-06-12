<template>
    <div class="q-pa-md">
        <div class="row items-center q-mb-md">
            <h1 class="q-my-none">
                {{ isNew ? "Create Navigation Menu" : "Edit Navigation Menu" }}
            </h1>
            <q-space />
            <q-btn
                flat
                dense
                no-caps
                color="primary"
                icon="arrow_back"
                label="Back to Menus"
                :to="{ name: 'CmsLeftNavMenus' }"
            />
        </div>

        <div class="row q-col-gutter-lg">
            <div class="col-12 col-md-4">
                <q-card
                    flat
                    bordered
                >
                    <q-card-section class="q-gutter-y-sm">
                        <div class="text-h6">Menu Settings</div>

                        <q-form
                            ref="menuFormRef"
                            greedy
                            @submit.prevent="saveMenu"
                        >
                            <q-input
                                v-model="menu.menuHeaderText"
                                dense
                                outlined
                                label="Menu header"
                                class="required-field q-mb-sm"
                                :rules="[(v: string | null) => (v && v.trim().length > 0) || 'Menu header is required']"
                                aria-required="true"
                                hide-bottom-space
                            />

                            <q-select
                                v-model="menu.system"
                                dense
                                options-dense
                                outlined
                                label="System"
                                class="q-mb-sm"
                                :options="['Viper', 'Public']"
                            />

                            <q-input
                                v-model="menu.viperSectionPath"
                                dense
                                outlined
                                label="VIPER section path"
                                class="q-mb-sm"
                            />

                            <q-input
                                v-model="menu.page"
                                dense
                                outlined
                                label="Page"
                                class="q-mb-sm"
                            />

                            <q-input
                                v-model="menu.friendlyName"
                                dense
                                outlined
                                label="Friendly name"
                                hint="Used by pages to look up this menu"
                                class="q-mb-sm"
                            />

                            <q-btn
                                type="submit"
                                color="primary"
                                :label="isNew ? 'Create Menu' : 'Save Menu Settings'"
                                dense
                                no-caps
                                class="q-pr-md"
                                :loading="savingMenu"
                            />
                        </q-form>
                    </q-card-section>
                </q-card>
            </div>

            <div
                v-if="!isNew"
                class="col-12 col-md-8"
            >
                <q-card
                    flat
                    bordered
                >
                    <q-card-section>
                        <div class="row items-center q-mb-sm">
                            <div class="text-h6">Menu Items</div>
                            <q-space />
                            <q-btn
                                flat
                                dense
                                no-caps
                                color="positive"
                                icon="add"
                                label="Add Header"
                                @click="addItem(true)"
                            />
                            <q-btn
                                flat
                                dense
                                no-caps
                                color="positive"
                                icon="add"
                                label="Add Link"
                                class="q-ml-sm"
                                @click="addItem(false)"
                            />
                        </div>

                        <VueDraggable
                            v-model="items"
                            :animation="200"
                            handle=".handle"
                        >
                            <div
                                v-for="(item, index) in items"
                                :key="item.key"
                                :class="['nav-item row items-start q-col-gutter-sm', { 'is-header': item.isHeader }]"
                            >
                                <div class="col-auto flex items-center">
                                    <q-icon
                                        class="handle q-mt-sm"
                                        name="drag_handle"
                                    >
                                        <q-tooltip>Drag to reorder</q-tooltip>
                                    </q-icon>
                                </div>
                                <div class="col-3">
                                    <q-input
                                        v-model="item.menuItemText"
                                        dense
                                        outlined
                                        :label="item.isHeader ? 'Header text' : 'Link text'"
                                    />
                                </div>
                                <div class="col">
                                    <q-input
                                        v-if="!item.isHeader"
                                        v-model="item.url"
                                        dense
                                        outlined
                                        label="URL"
                                    />
                                    <div
                                        v-else
                                        class="text-grey-7 q-mt-sm text-caption"
                                    >
                                        Section header
                                    </div>
                                </div>
                                <div class="col-3">
                                    <PermissionSelector
                                        v-model="item.permissions"
                                        label="Visible to"
                                    />
                                </div>
                                <div class="col-auto">
                                    <q-btn
                                        dense
                                        flat
                                        no-caps
                                        size="sm"
                                        color="secondary"
                                        icon="arrow_upward"
                                        aria-label="Move item up"
                                        :disable="index === 0"
                                        @click="moveItem(index, -1)"
                                    />
                                    <q-btn
                                        dense
                                        flat
                                        no-caps
                                        size="sm"
                                        color="secondary"
                                        icon="arrow_downward"
                                        aria-label="Move item down"
                                        :disable="index === items.length - 1"
                                        @click="moveItem(index, 1)"
                                    />
                                    <q-btn
                                        dense
                                        flat
                                        no-caps
                                        size="sm"
                                        color="negative"
                                        icon="delete"
                                        aria-label="Remove item"
                                        @click="removeItem(index)"
                                    />
                                </div>
                            </div>
                        </VueDraggable>

                        <div
                            v-if="!items.length"
                            class="text-grey-7 q-my-md"
                        >
                            No items yet — add a header or link above.
                        </div>

                        <div class="q-mt-md">
                            <q-btn
                                color="primary"
                                label="Save Items"
                                dense
                                no-caps
                                class="q-pr-md"
                                :loading="savingItems"
                                @click="saveItems"
                            />
                            <q-btn
                                flat
                                label="Revert"
                                dense
                                no-caps
                                class="q-ml-sm"
                                @click="revertItems"
                            />
                        </div>
                    </q-card-section>
                </q-card>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, inject, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar } from "quasar"
import { VueDraggable } from "vue-draggable-plus"
import { useFetch } from "@/composables/ViperFetch"
import PermissionSelector from "@/CMS/components/PermissionSelector.vue"
import type { CmsLeftNavMenu } from "@/CMS/types/"

const apiURL = inject("apiURL") + "cms/left-navs"
const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const { get, post, put } = useFetch()

const menuId = computed(() => (route.params.id ? Number(route.params.id) : null))
const isNew = computed(() => menuId.value === null)

const menuFormRef = ref()
const savingMenu = ref(false)
const savingItems = ref(false)

const menu = ref({
    menuHeaderText: "" as string | null,
    system: "Viper",
    viperSectionPath: null as string | null,
    page: null as string | null,
    friendlyName: null as string | null,
})

type EditableItem = {
    key: number
    leftNavItemId: number
    menuItemText: string | null
    isHeader: boolean
    url: string | null
    permissions: string[]
}

const items = ref<EditableItem[]>([])
let savedMenu: CmsLeftNavMenu | null = null
let nextKey = -1

function toEditableItems(source: CmsLeftNavMenu): EditableItem[] {
    return source.items.map((i) => ({
        key: i.leftNavItemId,
        leftNavItemId: i.leftNavItemId,
        menuItemText: i.menuItemText,
        isHeader: i.isHeader,
        url: i.url,
        permissions: [...i.permissions],
    }))
}

async function loadMenu() {
    if (menuId.value === null) return
    const res = await get(apiURL + "/" + menuId.value)
    if (!res.success) {
        $q.notify({ type: "negative", message: "Menu not found" })
        void router.push({ name: "CmsLeftNavMenus" })
        return
    }
    savedMenu = res.result
    menu.value = {
        menuHeaderText: res.result.menuHeaderText,
        system: res.result.system,
        viperSectionPath: res.result.viperSectionPath,
        page: res.result.page,
        friendlyName: res.result.friendlyName,
    }
    items.value = toEditableItems(res.result)
}

async function saveMenu() {
    const valid = await menuFormRef.value?.validate()
    if (!valid) return

    savingMenu.value = true
    const payload = {
        menuHeaderText: menu.value.menuHeaderText,
        system: menu.value.system,
        viperSectionPath: menu.value.viperSectionPath || null,
        page: menu.value.page || null,
        friendlyName: menu.value.friendlyName || null,
    }
    const res = isNew.value ? await post(apiURL, payload) : await put(apiURL + "/" + menuId.value, payload)
    savingMenu.value = false

    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to save menu" })
        return
    }
    $q.notify({ type: "positive", message: isNew.value ? "Menu created — now add items" : "Menu settings saved" })
    if (isNew.value) {
        // the menuId watch loads the created menu once the route changes
        void router.push({ name: "CmsLeftNavEdit", params: { id: res.result.leftNavMenuId } })
    }
}

function addItem(isHeader: boolean) {
    items.value.push({
        key: nextKey--,
        leftNavItemId: 0,
        menuItemText: "",
        isHeader,
        url: isHeader ? null : "",
        permissions: [],
    })
}

function removeItem(index: number) {
    items.value.splice(index, 1)
}

function moveItem(index: number, direction: -1 | 1) {
    const newIndex = index + direction
    if (newIndex < 0 || newIndex >= items.value.length) return
    const [item] = items.value.splice(index, 1)
    items.value.splice(newIndex, 0, item!)
}

async function saveItems() {
    const emptyItem = items.value.find((i) => !i.menuItemText || i.menuItemText.trim() === "")
    if (emptyItem) {
        $q.notify({ type: "negative", message: "Every item needs text" })
        return
    }

    savingItems.value = true
    const payload = items.value.map((i) => ({
        leftNavItemId: i.leftNavItemId,
        menuItemText: i.menuItemText,
        isHeader: i.isHeader,
        url: i.isHeader ? null : i.url,
        permissions: i.permissions,
    }))
    const res = await put(apiURL + "/" + menuId.value + "/items", payload)
    savingItems.value = false

    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to save items" })
        return
    }
    $q.notify({ type: "positive", message: "Items saved" })
    savedMenu = res.result
    items.value = toEditableItems(res.result)
}

function revertItems() {
    if (savedMenu) {
        items.value = toEditableItems(savedMenu)
        $q.notify({ type: "info", message: "Items reverted to last saved state" })
    }
}

// The create and edit routes share this component, so vue-router reuses the
// instance and onMounted does not re-fire when navigating between them
// (create -> redirect after save, or browser back to the create route).
watch(menuId, (id) => {
    if (id === null) {
        menu.value = {
            menuHeaderText: "",
            system: "Viper",
            viperSectionPath: null,
            page: null,
            friendlyName: null,
        }
        items.value = []
        savedMenu = null
    } else {
        void loadMenu()
    }
})

onMounted(loadMenu)
</script>

<style scoped>
.nav-item {
    border: 1px solid #e5e7eb;
    border-radius: 6px;
    margin-bottom: 8px;
    padding: 8px;
}

.nav-item.is-header {
    background: #f5f7fb;
}

.handle {
    cursor: grab;
}

.handle:active {
    cursor: grabbing;
}

.required-field :deep(.q-field__label)::after {
    content: " *";
    color: var(--q-negative);
}
</style>
