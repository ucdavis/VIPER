<template>
    <div class="q-pa-md">
        <BreadcrumbHeading
            label="Edit History"
            parent-label="Manage Content Blocks"
            :parent-to="{ name: 'CmsContentBlocks' }"
        />

        <p class="text-body2 text-grey-8 q-mb-md">
            Every saved edit keeps the version it replaced. This lists those past versions across all content blocks.
            The current version of each block is on its edit page.
        </p>

        <div class="row q-col-gutter-md q-mb-sm">
            <div class="col-12 col-sm-3 col-lg-2">
                <q-input
                    v-model="filters.modifiedBy"
                    dense
                    outlined
                    clearable
                    debounce="400"
                    label="Modified By"
                    @update:model-value="reload"
                />
            </div>
            <DateRangeFilter
                v-model:from="filters.from"
                v-model:to="filters.to"
                @change="reload"
            />
            <div class="col-12 col-sm-4 col-lg-3">
                <q-input
                    v-model="filters.search"
                    dense
                    outlined
                    clearable
                    debounce="400"
                    label="Search block title or page"
                    @update:model-value="reload"
                >
                    <template #prepend>
                        <q-icon name="search" />
                    </template>
                </q-input>
            </div>
        </div>

        <div
            v-if="contentBlockId"
            class="q-mb-sm"
        >
            <q-chip
                removable
                dense
                color="primary"
                text-color="white"
                @remove="clearBlockFilter"
            >
                Filtered to one block: {{ contentBlockId }}
            </q-chip>
        </div>

        <q-table
            :rows="entries"
            :columns="columns"
            row-key="contentHistoryId"
            :loading="loading"
            v-model:pagination="pagination"
            :rows-per-page-options="[25, 50, 100, 250]"
            :grid="$q.screen.lt.sm"
            dense
            flat
            bordered
            @request="onRequest"
        >
            <template #body-cell-modifiedOn="cellProps">
                <q-td :props="cellProps">
                    {{
                        cellProps.row.modifiedOn
                            ? formatDateTime(cellProps.row.modifiedOn, { dateStyle: "short", timeStyle: "short" })
                            : ""
                    }}
                </q-td>
            </template>
            <template #body-cell-block="cellProps">
                <q-td :props="cellProps">
                    <router-link :to="{ name: 'CmsContentBlockEdit', params: { id: cellProps.row.contentBlockId } }">
                        {{ blockLabel(cellProps.row) }}
                    </router-link>
                    <StatusBadge
                        v-if="cellProps.row.blockDeleted"
                        color="negative"
                        class="q-ml-sm"
                        label="deleted"
                    />
                    <div
                        v-if="cellProps.row.page"
                        class="text-caption text-grey-7"
                    >
                        {{ cellProps.row.page }}
                    </div>
                </q-td>
            </template>
            <template #body-cell-actions="cellProps">
                <q-td :props="cellProps">
                    <q-btn
                        dense
                        flat
                        no-caps
                        size="sm"
                        color="secondary"
                        icon="compare_arrows"
                        class="diff-action-btn"
                        aria-label="Diff with previous version"
                        @click="viewDiff(cellProps.row)"
                    >
                        <q-tooltip>Diff with previous version</q-tooltip>
                    </q-btn>
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
                                {{ blockLabel(row) }}
                            </router-link>
                            <StatusBadge
                                v-if="row.blockDeleted"
                                color="negative"
                                label="deleted"
                            />
                        </div>
                        <div
                            v-if="row.page"
                            class="text-caption text-grey-7"
                        >
                            {{ row.page }}
                        </div>
                    </template>

                    <ListCardField label="Date">
                        {{
                            row.modifiedOn
                                ? formatDateTime(row.modifiedOn, { dateStyle: "short", timeStyle: "short" })
                                : ""
                        }}
                    </ListCardField>
                    <ListCardField
                        label="Modified By"
                        :value="row.modifiedBy"
                    />

                    <template #actions>
                        <q-btn
                            dense
                            flat
                            no-caps
                            size="sm"
                            color="secondary"
                            icon="compare_arrows"
                            label="Diff"
                            class="diff-action-btn"
                            aria-label="Diff with previous version"
                            @click="viewDiff(row)"
                        />
                    </template>
                </ListCard>
            </template>
        </q-table>

        <ContentDiffDialog
            v-model="viewer.open"
            :loading="viewer.loading"
            :title="viewer.title"
            :subtitle="viewer.subtitle"
            :diff-html="viewer.content"
            :has-comparison="viewer.hasComparison"
            :has-changes="viewer.hasChanges"
        />
    </div>
</template>

<script setup lang="ts">
import { inject, onMounted, ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import { useDateFunctions } from "@/composables/DateFunctions"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import StatusBadge from "@/components/StatusBadge.vue"
import DateRangeFilter from "@/CMS/components/DateRangeFilter.vue"
import ContentDiffDialog from "@/CMS/components/ContentDiffDialog.vue"
import ListCard from "@/CMS/components/ListCard.vue"
import ListCardField from "@/CMS/components/ListCardField.vue"
import type { CmsContentHistoryAudit, CmsContentHistoryDiff } from "@/CMS/types/"

const apiBase = inject("apiURL")
const listUrl = apiBase + "CMS/content/history"
const $q = useQuasar()
const route = useRoute()
const router = useRouter()
const { get, createUrlSearchParams } = useFetch()
const { formatDateTime } = useDateFunctions()

const entries = ref<CmsContentHistoryAudit[]>([])
const loading = ref(false)
const contentBlockId = ref<string | null>((route.query.contentBlockId as string) || null)

// Filters initialize from the URL so filtered views can be shared/deep-linked.
const filters = ref({
    modifiedBy: typeof route.query.modifiedBy === "string" ? route.query.modifiedBy : "",
    from: typeof route.query.from === "string" ? route.query.from : "",
    to: typeof route.query.to === "string" ? route.query.to : "",
    search: typeof route.query.search === "string" ? route.query.search : "",
})

// Reflect the active filters back into the URL (empty values are omitted).
function syncFiltersToUrl() {
    void router.replace({
        query: {
            contentBlockId: contentBlockId.value || undefined,
            modifiedBy: filters.value.modifiedBy || undefined,
            from: filters.value.from || undefined,
            to: filters.value.to || undefined,
            search: filters.value.search || undefined,
        },
    })
}

const pagination = ref({
    sortBy: "modifiedOn",
    descending: true,
    page: 1,
    rowsPerPage: 50,
    rowsNumber: 0,
})

const columns: QTableProps["columns"] = [
    { name: "modifiedOn", label: "Date", field: "modifiedOn", align: "left" },
    { name: "modifiedBy", label: "Modified By", field: "modifiedBy", align: "left" },
    { name: "block", label: "Block", field: "title", align: "left" },
    { name: "actions", label: "Actions", field: "contentHistoryId", align: "center" },
]

function blockLabel(row: CmsContentHistoryAudit): string {
    return row.title || row.friendlyName || "(untitled)"
}

type TableRequestPagination = {
    sortBy: string
    descending: boolean
    page: number
    rowsPerPage: number
    rowsNumber?: number
}

async function onRequest(requestProps: { pagination: TableRequestPagination }) {
    const { page, rowsPerPage } = requestProps.pagination
    loading.value = true
    const params = createUrlSearchParams({
        contentBlockId: contentBlockId.value,
        modifiedBy: filters.value.modifiedBy || null,
        from: filters.value.from || null,
        to: filters.value.to || null,
        search: filters.value.search || null,
        page,
        perPage: rowsPerPage,
    })
    const res = await get(listUrl + "?" + params)
    if (res.success) {
        entries.value = res.result
        pagination.value.rowsNumber = res.pagination?.totalRecords ?? res.result.length
        pagination.value.page = page
        pagination.value.rowsPerPage = rowsPerPage
    } else {
        $q.notify({ type: "negative", message: res.errors?.[0] ?? "Failed to load edit history" })
    }
    loading.value = false
}

function reload() {
    syncFiltersToUrl()
    void onRequest({ pagination: { ...pagination.value, page: 1 } })
}

function clearBlockFilter() {
    contentBlockId.value = null
    reload()
}

const viewer = ref({
    open: false,
    loading: false,
    title: "",
    subtitle: "",
    content: "",
    hasComparison: true,
    hasChanges: true,
})

function diffStamp(on: string | null, by: string | null): string {
    return (
        (on ? formatDateTime(on, { dateStyle: "short", timeStyle: "short" }) : "unknown date") + (by ? ` by ${by}` : "")
    )
}

async function viewDiff(row: CmsContentHistoryAudit) {
    viewer.value = {
        open: true,
        loading: true,
        title: blockLabel(row),
        subtitle: "",
        content: "",
        hasComparison: true,
        hasChanges: true,
    }
    const res = await get(apiBase + "CMS/content/" + row.contentBlockId + "/history/" + row.contentHistoryId + "/diff")
    if (res.success) {
        const diff = res.result as CmsContentHistoryDiff
        viewer.value.content = diff.content
        viewer.value.hasComparison = diff.hasComparison
        viewer.value.hasChanges = diff.hasChanges
        viewer.value.subtitle = diff.hasComparison
            ? `Changes from ${diffStamp(diff.oldModifiedOn, diff.oldModifiedBy)} to ${diffStamp(diff.newModifiedOn, diff.newModifiedBy)}`
            : `Original version, ${diffStamp(diff.newModifiedOn, diff.newModifiedBy)}`
    } else {
        $q.notify({ type: "negative", message: res.errors?.[0] ?? "Failed to load this version" })
        viewer.value.open = false
    }
    viewer.value.loading = false
}

// Re-sync filters from the URL when in-app navigation reuses this view with a different
// query (e.g. a per-block ?contentBlockId deep-link). The equality guard skips our own
// syncFiltersToUrl write so it doesn't double-fetch.
watch(
    () => route.query,
    (query) => {
        const nextBlock = typeof query.contentBlockId === "string" ? query.contentBlockId : null
        const next = {
            modifiedBy: typeof query.modifiedBy === "string" ? query.modifiedBy : "",
            from: typeof query.from === "string" ? query.from : "",
            to: typeof query.to === "string" ? query.to : "",
            search: typeof query.search === "string" ? query.search : "",
        }
        const f = filters.value
        if (
            nextBlock === contentBlockId.value &&
            next.modifiedBy === f.modifiedBy &&
            next.from === f.from &&
            next.to === f.to &&
            next.search === f.search
        ) {
            return
        }
        contentBlockId.value = nextBlock
        filters.value = next
        void onRequest({ pagination: { ...pagination.value, page: 1 } })
    },
)

onMounted(reload)
</script>

<style scoped>
/* The diff action is icon-only and its tooltip label is unavailable on touch, so give it a
   44px touch target on coarse pointers. Mouse/trackpad keep the dense table-action size. */
@media (pointer: coarse) {
    .diff-action-btn {
        min-width: 44px;
        min-height: 44px;
    }
}
</style>
