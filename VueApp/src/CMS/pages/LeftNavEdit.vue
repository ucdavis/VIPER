<template>
    <div class="q-pa-md">
        <BreadcrumbHeading
            :label="isNew ? 'Add Left-Nav Menu' : 'Edit Left-Nav Menu'"
            parent-label="Manage Left-Nav Menus"
            :parent-to="{ name: 'CmsLeftNavMenus' }"
        />

        <div class="row q-col-gutter-lg">
            <div class="col-12 col-md-4">
                <q-card
                    flat
                    bordered
                >
                    <q-card-section class="q-gutter-y-sm">
                        <h2 class="text-h6 q-my-none">Menu Settings</h2>

                        <q-form
                            ref="menuFormRef"
                            greedy
                            @submit.prevent="saveMenu"
                            @validation-error="onMenuValidationError"
                        >
                            <LeftNavMenuSettingsFields v-model="menu" />

                            <StatusBanner
                                v-if="menuFormError"
                                type="error"
                                class="q-mb-sm"
                            >
                                {{ menuFormError }}
                            </StatusBanner>

                            <div class="row items-center q-gutter-sm">
                                <q-btn
                                    type="submit"
                                    color="primary"
                                    :label="isNew ? 'Create Menu' : 'Save Menu Settings'"
                                    dense
                                    no-caps
                                    :loading="savingMenu"
                                >
                                    <template #loading>
                                        <q-spinner
                                            size="1em"
                                            class="q-mr-sm"
                                        />
                                        {{ isNew ? "Create Menu" : "Save Menu Settings" }}
                                    </template>
                                </q-btn>
                                <span
                                    v-if="!isNew && menuDirty"
                                    class="unsaved-hint text-warning"
                                >
                                    <q-icon
                                        name="edit"
                                        size="1rem"
                                    />
                                    Unsaved changes
                                </span>
                            </div>
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
                            <h2 class="text-h6 q-my-none">Menu Items</h2>
                            <q-space />
                            <q-btn
                                dense
                                no-caps
                                color="positive"
                                icon="add"
                                label="Add Header"
                                class="q-pr-md"
                                @click="addItem(true)"
                            />
                            <q-btn
                                dense
                                no-caps
                                color="positive"
                                icon="add"
                                label="Add Link"
                                class="q-ml-sm q-pr-md"
                                @click="addItem(false)"
                            />
                        </div>

                        <SortableList
                            v-model="items"
                            item-key="key"
                            class="menu-items"
                            move-up-label="Move item up"
                            move-down-label="Move item down"
                            :row-class="itemRowClass"
                            :announce="announceItem"
                            @reorder="onItemsReorder"
                        >
                            <template #row="{ item }">
                                <div
                                    class="menu-item-fields"
                                    role="group"
                                    :aria-label="`${item.isHeader ? 'Section header' : 'Menu link'}: ${item.menuItemText || '(untitled)'}`"
                                >
                                    <div class="menu-item-fields__text">
                                        <q-input
                                            v-model="item.menuItemText"
                                            dense
                                            outlined
                                            :label="item.isHeader ? 'Header text' : 'Link text'"
                                        />
                                    </div>
                                    <div class="menu-item-fields__url">
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
                                    <div class="menu-item-fields__perms">
                                        <PermissionSelector
                                            v-model="item.permissions"
                                            label="Visible to"
                                        />
                                    </div>
                                </div>
                            </template>

                            <template #actions="{ index }">
                                <q-btn
                                    dense
                                    flat
                                    no-caps
                                    size="sm"
                                    color="negative"
                                    icon="delete"
                                    aria-label="Remove item"
                                    @click="removeItem(index)"
                                >
                                    <q-tooltip>Remove item</q-tooltip>
                                </q-btn>
                            </template>

                            <template #empty>
                                <div class="text-grey-7 q-my-md">No items yet — add a header or link above.</div>
                            </template>
                        </SortableList>

                        <StatusBanner
                            v-if="itemsError"
                            type="error"
                            class="q-mt-md"
                        >
                            {{ itemsError }}
                        </StatusBanner>

                        <div class="q-mt-md row items-center">
                            <q-btn
                                color="primary"
                                label="Save Items"
                                dense
                                no-caps
                                :loading="savingItems"
                                @click="saveItems"
                            >
                                <template #loading>
                                    <q-spinner
                                        size="1em"
                                        class="q-mr-sm"
                                    />
                                    Save Items
                                </template>
                            </q-btn>
                            <q-btn
                                flat
                                label="Discard Item Changes"
                                dense
                                no-caps
                                class="q-ml-sm"
                                :disable="!itemsDirty"
                                @click="revertItems"
                            />
                            <span
                                v-if="itemsDirty"
                                class="unsaved-hint text-warning q-ml-md"
                            >
                                <q-icon
                                    name="edit"
                                    size="1rem"
                                />
                                Unsaved changes
                            </span>
                        </div>
                    </q-card-section>
                </q-card>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
// Template-size synthetic complexity only; the payload build is extracted from saveMenu below.
// fallow-ignore-file complexity
import { computed, inject, onMounted, ref, watch } from "vue"
import { useRoute, useRouter, onBeforeRouteLeave } from "vue-router"
import { useQuasar } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import SortableList from "@/components/SortableList.vue"
import LeftNavMenuSettingsFields, {
    type MenuSettings,
    createEmptyMenuSettings,
} from "@/CMS/components/LeftNavMenuSettingsFields.vue"
import PermissionSelector from "@/CMS/components/PermissionSelector.vue"
import StatusBanner from "@/components/StatusBanner.vue"
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
const menuFormError = ref("")
const itemsError = ref("")

const menu = ref<MenuSettings>(createEmptyMenuSettings())

// The concurrency stamp for the settings save: the menu's modifiedOn as loaded, advanced from each
// save response so a second save isn't rejected as stale. Kept out of `menu` so it doesn't skew the
// settings dirty check.
const menuModifiedOn = ref<string | null>(null)

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

// The settings form and the items list save independently, so track them separately:
// saving one must not clear the other's unsaved flag, and each section shows its own
// unsaved indicator.
const {
    isDirty: menuDirty,
    setInitialState: setMenuBaseline,
    resetDirtyState: resetMenuDirty,
} = useUnsavedChanges(menu)
const {
    isDirty: itemsDirty,
    setInitialState: setItemsBaseline,
    resetDirtyState: resetItemsDirty,
} = useUnsavedChanges(items)
const hasUnsavedChanges = computed(() => menuDirty.value || itemsDirty.value)

function captureBaselines() {
    setMenuBaseline()
    setItemsBaseline()
}

onBeforeRouteLeave(async () => {
    if (!hasUnsavedChanges.value) {
        return true
    }
    return await confirmLeave()
})

function confirmLeave(): Promise<boolean> {
    return new Promise((resolve) => {
        $q.dialog({
            title: "Unsaved Changes",
            message: "This menu has unsaved changes. Leave without saving?",
            cancel: { label: "Keep Editing", flat: true },
            ok: { label: "Leave", color: "negative", unelevated: true },
            persistent: true,
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
            .onDismiss(() => resolve(false))
    })
}

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
    menuModifiedOn.value = res.result.modifiedOn
    menu.value = {
        menuHeaderText: res.result.menuHeaderText,
        system: res.result.system,
        viperSectionPath: res.result.viperSectionPath,
        page: res.result.page,
        friendlyName: res.result.friendlyName,
    }
    items.value = toEditableItems(res.result)
    captureBaselines()
}

// The q-form focuses the first invalid field on a failed submit; this surfaces a matching
// message next to the save button so the failure is obvious without scrolling up.
function onMenuValidationError() {
    menuFormError.value = isNew.value
        ? "Please complete the required fields before creating this menu."
        : "Please complete the required fields before saving."
}

function buildMenuPayload() {
    return {
        menuHeaderText: menu.value.menuHeaderText,
        system: menu.value.system,
        viperSectionPath: menu.value.viperSectionPath || null,
        page: menu.value.page || null,
        friendlyName: menu.value.friendlyName || null,
        // Create ignores this; edit uses it as the stale-edit guard (409 on mismatch).
        lastModifiedOn: isNew.value ? null : menuModifiedOn.value,
    }
}

// A 409 means someone else saved the menu settings first; offer to reload their version.
function handleMenuSaveConflict(res: { errors: string[] | null }) {
    $q.dialog({
        title: "Edit Conflict",
        message:
            (res.errors?.[0] ?? "This menu was changed by someone else.") +
            " Reload the latest version? Your unsaved changes will be lost.",
        cancel: { label: "Keep editing", flat: true },
        persistent: true,
        ok: { label: "Reload", color: "primary", unelevated: true },
    }).onOk(() => {
        // loadMenu re-fetches and re-captures the baselines, clearing the settings dirty state.
        void loadMenu()
    })
}

async function saveMenu() {
    menuFormError.value = ""

    savingMenu.value = true
    const payload = buildMenuPayload()
    const res = isNew.value ? await post(apiURL, payload) : await put(apiURL + "/" + menuId.value, payload)
    savingMenu.value = false

    if (res.status === 409) {
        handleMenuSaveConflict(res)
        return
    }
    if (!res.success) {
        menuFormError.value = res.errors?.[0] ?? "Failed to save menu"
        return
    }
    $q.notify({ type: "positive", message: isNew.value ? "Menu created — now add items" : "Menu settings saved" })
    // Advance the concurrency stamp from the save response so a subsequent save isn't stale.
    menuModifiedOn.value = res.result.modifiedOn
    resetMenuDirty()
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

function itemRowClass(item: EditableItem) {
    return { "menu-item--header": item.isHeader }
}

function announceItem(item: EditableItem, newIndex: number, total: number) {
    const kind = item.isHeader ? "header" : "link"
    const name = item.menuItemText?.trim() || "item"
    return `Moved ${kind} ${name} to position ${newIndex + 1} of ${total}`
}

// Reorder is staged here (commits on Save Items), so the toast stays neutral; `group`
// collapses rapid nudges into one. The "Unsaved changes" indicator signals it needs saving.
function onItemsReorder() {
    $q.notify({ type: "info", message: "Order updated", group: "leftnav-order", timeout: 1500 })
}

async function saveItems() {
    itemsError.value = ""
    // Headers may be blank (legacy spacer rows still render as an empty divider); links need text.
    const emptyLink = items.value.find((i) => !i.isHeader && (!i.menuItemText || i.menuItemText.trim() === ""))
    if (emptyLink) {
        itemsError.value = "Every link needs text — fill in the empty link before saving."
        return
    }

    savingItems.value = true
    const payload = {
        // Items ride on the menu's ModifiedOn; a stale stamp gets 409 so concurrent editors
        // can't silently overwrite each other's item changes.
        lastModifiedOn: menuModifiedOn.value,
        items: items.value.map((i) => ({
            leftNavItemId: i.leftNavItemId,
            menuItemText: i.menuItemText,
            isHeader: i.isHeader,
            url: i.isHeader ? null : i.url,
            permissions: i.permissions,
        })),
    }
    const res = await put(apiURL + "/" + menuId.value + "/items", payload)
    savingItems.value = false

    if (res.status === 409) {
        handleMenuSaveConflict(res)
        return
    }
    if (!res.success) {
        itemsError.value = res.errors?.[0] ?? "Failed to save items"
        return
    }
    $q.notify({ type: "positive", message: "Items saved" })
    savedMenu = res.result
    items.value = toEditableItems(res.result)
    // Saving items bumps the menu's ModifiedOn; advance the shared stamp so a following
    // settings (or items) save isn't rejected as stale.
    menuModifiedOn.value = res.result.modifiedOn
    resetItemsDirty()
}

function revertItems() {
    if (savedMenu) {
        items.value = toEditableItems(savedMenu)
        itemsError.value = ""
        $q.notify({ type: "info", message: "Item changes discarded" })
    }
}

// The create and edit routes share this component, so vue-router reuses the
// instance and onMounted does not re-fire when navigating between them
// (create -> redirect after save, or browser back to the create route).
watch(menuId, (id) => {
    if (id === null) {
        menu.value = createEmptyMenuSettings()
        items.value = []
        savedMenu = null
        menuModifiedOn.value = null
        menuFormError.value = ""
        itemsError.value = ""
        captureBaselines()
    } else {
        void loadMenu()
    }
})

onMounted(() => {
    loadMenu()
    // loadMenu sets the baseline for an existing menu; a brand-new form's baseline is empty.
    if (isNew.value) {
        captureBaselines()
    }
})
</script>

<style scoped>
.unsaved-hint {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    font-size: 0.8125rem;
    font-weight: 500;
}

/* The row's available width (not the viewport) decides whether the three fields fit:
   in the two-column layout the Menu Items column can be narrow even on a wide screen.
   Query the row body so the fields stack until there's genuinely room for a row. */
.menu-items :deep(.sortable-row__body) {
    container-type: inline-size;
}

.menu-item-fields {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

@container (min-width: 30rem) {
    .menu-item-fields {
        flex-direction: row;
        align-items: flex-start;
        gap: 0.75rem;
    }

    .menu-item-fields__text {
        flex: 1 1 0;
        min-width: 0;
    }

    .menu-item-fields__url {
        flex: 1.3 1 0;
        min-width: 0;
    }

    .menu-item-fields__perms {
        flex: 1.2 1 0;
        min-width: 0;
    }
}

/* Match the Link Collections rows exactly: tinted cards on small/medium screens,
   borderless grouped rows in the panel on desktop. Header rows keep a distinct tint. */
.menu-items :deep(.sortable-row) {
    border: 0.0625rem solid var(--ucdavis-black-10);
    border-radius: 0.25rem;
    padding: 0.75rem;
    background: var(--surface-tint-raised);
}

.menu-items :deep(.sortable-row.menu-item--header) {
    background: var(--surface-tint);
}

@media (width >= 64rem) {
    .menu-items :deep(.sortable-list__items) {
        gap: 0;
    }

    .menu-items :deep(.sortable-row) {
        border: none;
        border-bottom: 0.0625rem solid var(--ucdavis-black-10);
        border-radius: 0;
        padding: 0.75rem 0.5rem;
        background: transparent;
    }

    .menu-items :deep(.sortable-row:last-child) {
        border-bottom: none;
    }

    .menu-items :deep(.sortable-row.menu-item--header) {
        background: var(--surface-tint);
    }
}
</style>
