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
                    outlined
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
                    outlined
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
                    outlined
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

        <!-- Server-paged like Files: rows come back one page at a time, so no "All" (0) option.
             Card mode kicks in below md (< 1024px) so iPad portrait drops to cards rather than a
             horizontally-scrolling table; VIPER section + Page are merged into one Section/Page column
             to keep the landscape/desktop table narrow. (Files still uses lt.sm and its own six columns.) -->
        <q-table
            :rows="blocks"
            :columns="columns"
            row-key="contentBlockId"
            :loading="loading"
            v-model:pagination="pagination"
            :rows-per-page-options="[25, 50, 100, 250]"
            :grid="$q.screen.lt.md"
            dense
            flat
            bordered
            @request="onRequest"
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

            <template #body-cell-location="cellProps">
                <q-td :props="cellProps">
                    <div>{{ cellProps.row.viperSectionPath }}</div>
                    <div
                        v-if="cellProps.row.page"
                        class="text-caption text-grey-7"
                    >
                        {{ cellProps.row.page }}
                    </div>
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
                <ListCard class="content-block-card">
                    <template #header>
                        <!-- Title, status icons, and the row actions all share the first line so the
                             edit/delete buttons align with the title (not centred against the taller
                             title+friendly-name block); the friendly name drops to a second line. -->
                        <div>
                            <div class="row items-center no-wrap q-gutter-x-xs">
                                <router-link
                                    class="ellipsis text-weight-medium card-header-title"
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
                                <q-space />
                                <div class="col-auto row items-center no-wrap list-card-actions">
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
                                </div>
                            </div>
                            <div
                                v-if="row.friendlyName"
                                class="text-caption text-grey-7"
                            >
                                {{ row.friendlyName }}
                            </div>
                        </div>
                    </template>

                    <!-- Two fields per row (VIPER section | Page, then Access | Modified) so the card
                         stays compact. A CSS grid (see <style>) keeps the fields aligned with the title
                         text - unlike q-col-gutter, whose negative margin pulls them left - and it
                         collapses to one column below sm where half-width would be cramped. -->
                    <div class="card-field-grid">
                        <ListCardField
                            label="VIPER section"
                            :value="row.viperSectionPath"
                        />
                        <ListCardField
                            label="Page"
                            :value="row.page"
                        />
                        <ListCardField label="Access">
                            <PermissionChips
                                :permissions="row.permissions"
                                stacked
                            />
                        </ListCardField>
                        <ListCardField label="Modified">
                            <ModifiedStamp :row="row" />
                        </ListCardField>
                    </div>
                </ListCard>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
// Template-size synthetic complexity only (large filter + table markup); script logic is small.
// fallow-ignore-file complexity
import { computed, inject, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import { useServerTable } from "@/CMS/composables/use-server-table"
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
const { get, del, post } = useFetch()

const sectionPaths = ref<string[]>([])

// Filters initialize from the URL so views can be shared/deep-linked, matching
// the Files list and audit trail.
// Map the URL query into filter state; shared by the initial ref and the re-sync watcher below
// (mirrors Files.vue's parseFiltersFromQuery) so the two never drift.
function parseFiltersFromQuery(query: typeof route.query) {
    return {
        status: typeof query.status === "string" ? query.status : "active",
        viperSectionPath: typeof query.section === "string" ? query.section : null,
        search: typeof query.search === "string" ? query.search : "",
        publicOnly: query.public === "1",
    }
}

const filters = ref(parseFiltersFromQuery(route.query))

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

const columns = computed<QTableProps["columns"]>(() => {
    const cols: QTableProps["columns"] = [
        { name: "title", label: "Title", field: "title", align: "left", sortable: true },
        // VIPER section (the placement folder) and Page (the placement key within it) are merged into
        // one Section/Page column to keep the table narrow; it sorts by section, the coarser of the two.
        { name: "location", label: "Section/Page", field: "viperSectionPath", align: "left", sortable: true },
        { name: "permissions", label: "Access", field: "permissions", align: "left" },
    ]
    // Drop Modified below lg (< 1440px) so the table fits iPad landscape without horizontal scroll
    // (the modified date + editor still show in card mode and on wide desktop). It returns at >= 1440.
    if (!$q.screen.lt.lg) {
        cols.push({ name: "modifiedOn", label: "Modified", field: "modifiedOn", align: "left", sortable: true })
    }
    cols.push({ name: "actions", label: "Actions", field: "contentBlockId", align: "center" })
    return cols
})

// Server-paged list, mirroring Files: useServerTable owns rows/loading/pagination and
// issues the GET on @request; buildParams maps the current filters + page/sort each request.
const {
    rows: blocks,
    loading,
    pagination,
    onRequest,
    reloadFirstPage,
} = useServerTable<CmsContentBlock>({
    url: apiURL,
    errorMessage: "Failed to load content blocks",
    pagination: { sortBy: "title", descending: false },
    buildParams: (p) => ({
        status: filters.value.status,
        viperSectionPath: filters.value.viperSectionPath,
        search: filters.value.search || null,
        isPublic: filters.value.publicOnly ? "true" : null,
        page: p.page,
        perPage: p.rowsPerPage,
        sortBy: p.sortBy ?? "title",
        descending: p.descending ? "true" : "false",
    }),
})

// Filter changes both reload the list (from page 1) and update the URL; the route watcher
// guards against re-fetching on our own URL write.
function reload() {
    syncFiltersToUrl()
    void reloadFirstPage()
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
    reload()
}

async function restoreBlock(block: CmsContentBlock) {
    const res = await post(apiURL + "/" + block.contentBlockId + "/restore")
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to restore content block" })
        return
    }
    $q.notify({ type: "positive", message: "Content block restored" })
    reload()
}

// Re-sync filters when in-app navigation reuses this view with a different query
// (e.g. a hub deep-link or re-clicked nav link). The equality guard skips our own
// syncFiltersToUrl write, which would otherwise trigger a redundant fetch.
watch(
    () => route.query,
    (query) => {
        const next = parseFiltersFromQuery(query)
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
        void reloadFirstPage()
    },
)

onMounted(() => {
    loadSectionPaths()
    reload()
})
</script>

<style scoped>
/* Let a long title shrink and ellipsis instead of pushing the inline row actions off the card. */
.card-header-title {
    min-width: 0;
}

/* Tighten the grid-mode card: trim the section padding (it was too airy up top) so the header
   sits closer to the card edge. */
.content-block-card :deep(.q-card__section) {
    padding: 0.5rem 1rem;
}

/* The 44px touch target made the row-action buttons tower over the single-line title, opening a
   gap above the friendly name. A 32px target keeps them comfortable and above the 24px WCAG 2.5.8
   (AA) floor while letting the title row stay compact. */
@media (pointer: coarse) {
    .content-block-card :deep(.list-card-actions .q-btn) {
        min-width: 2rem;
        min-height: 2rem;
    }
}

/* Two fields per row on sm+ (one below), via a CSS grid so the fields line up with the title text.
   q-col-gutter would negative-margin the row 8px left of the title; a grid gap avoids that. */
.card-field-grid {
    display: grid;
    grid-template-columns: 1fr;
    gap: 0.375rem 1rem;
    margin-top: 0.5rem;
}

@media (min-width: 600px) {
    .card-field-grid {
        grid-template-columns: 1fr 1fr;
    }
}

/* Stack each field's label above its value. The shared ListCardField's default 7rem side label
   leaves too little room at half-width, so long values - notably permission chips - spilled into
   the neighbouring column. min-width:0 lets each grid cell shrink so values wrap, and the q-chip
   rules let a long permission wrap within its column instead of overflowing. */
.card-field-grid :deep(.list-card-field) {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.125rem;
    min-width: 0;
}

.card-field-grid :deep(.list-card-label) {
    flex: none;
}

.card-field-grid :deep(.q-chip) {
    max-width: 100%;
    height: auto;
}

.card-field-grid :deep(.q-chip__content) {
    white-space: normal;
    overflow-wrap: anywhere;
}
</style>
