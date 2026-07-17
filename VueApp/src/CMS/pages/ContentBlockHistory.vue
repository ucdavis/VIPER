<template>
    <div class="q-pa-md">
        <BreadcrumbHeading
            label="Edit History"
            parent-label="Manage Content Blocks"
            :parent-to="{ name: 'CmsContentBlocks' }"
        />

        <p class="text-body2 text-grey-8 q-mb-md">
            Every row is a past version a save replaced. Use "Diff vs current" to see what's changed since then.
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
                    <q-btn
                        dense
                        flat
                        no-caps
                        size="sm"
                        color="secondary"
                        icon="difference"
                        class="diff-action-btn"
                        :disable="cellProps.row.blockDeleted"
                        aria-label="Diff vs current"
                        @click="viewDiffVsCurrent(cellProps.row)"
                    >
                        <q-tooltip>{{
                            cellProps.row.blockDeleted ? "Not available for deleted blocks" : "Diff vs current"
                        }}</q-tooltip>
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
                            label="Diff vs previous"
                            class="diff-action-btn"
                            aria-label="Diff with previous version"
                            @click="viewDiff(row)"
                        />
                        <q-btn
                            dense
                            flat
                            no-caps
                            size="sm"
                            color="secondary"
                            icon="difference"
                            label="Diff vs current"
                            class="diff-action-btn"
                            :disable="row.blockDeleted"
                            aria-label="Diff vs current"
                            @click="viewDiffVsCurrent(row)"
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
import { inject, onMounted } from "vue"
import { useQuasar, type QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import { useDateFunctions } from "@/composables/DateFunctions"
import { useContentDiffViewer } from "@/CMS/composables/use-content-diff-viewer"
import { useUrlFilteredTable } from "@/CMS/composables/use-url-filtered-table"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import StatusBadge from "@/components/StatusBadge.vue"
import DateRangeFilter from "@/CMS/components/DateRangeFilter.vue"
import ContentDiffDialog from "@/CMS/components/ContentDiffDialog.vue"
import ListCard from "@/CMS/components/ListCard.vue"
import ListCardField from "@/CMS/components/ListCardField.vue"
import type { CmsContentBlock, CmsContentHistoryAudit, CmsContentHistoryDiff } from "@/CMS/types/"

const apiBase = inject("apiURL")
const $q = useQuasar()
const { get, post } = useFetch()
const { formatDateTime } = useDateFunctions()

const columns: QTableProps["columns"] = [
    { name: "modifiedOn", label: "Date", field: "modifiedOn", align: "left" },
    { name: "modifiedBy", label: "Modified By", field: "modifiedBy", align: "left" },
    { name: "block", label: "Block", field: "title", align: "left" },
    { name: "actions", label: "Actions", field: "contentHistoryId", align: "center" },
]

function blockLabel(row: CmsContentHistoryAudit): string {
    return row.title || row.friendlyName || "(untitled)"
}

// Filters + the per-block deep-link (?contentBlockId) sync to the URL and drive the server-paged
// fetch; see useUrlFilteredTable.
const {
    rows: entries,
    loading,
    pagination,
    onRequest,
    filters,
    primary: contentBlockId,
    reload,
    clearPrimaryFilter: clearBlockFilter,
} = useUrlFilteredTable<CmsContentHistoryAudit, { modifiedBy: string; from: string; to: string; search: string }>({
    url: apiBase + "CMS/content/history",
    errorMessage: "Failed to load edit history",
    primaryKey: "contentBlockId",
    defaultFilters: () => ({ modifiedBy: "", from: "", to: "", search: "" }),
    pagination: { sortBy: "modifiedOn", descending: true },
})

const { viewer, openViewer, applyDiff, failViewer, savedDiffSubtitle, diffStamp } = useContentDiffViewer()

async function viewDiff(row: CmsContentHistoryAudit) {
    openViewer(blockLabel(row))
    const res = await get(apiBase + "CMS/content/" + row.contentBlockId + "/history/" + row.contentHistoryId + "/diff")
    if (res.success) {
        const diff = res.result as CmsContentHistoryDiff
        applyDiff(diff, savedDiffSubtitle(diff))
    } else {
        failViewer(res.errors?.[0] ?? "Failed to load this version")
    }
}

// Diffs this row's saved content against the block's CURRENT live content — the comparison
// readers actually want after a recent save (bniedzie: the newest row's own "diff with
// previous" shows the change *before* the latest one, not the latest change itself). Same
// POST-diff flow as the hub rail's "View latest change" (RecentActivity.vue openLatestDiff):
// history rows only hold superseded versions, so comparing against "now" needs the posted
// draft content rather than a second saved version.
async function viewDiffVsCurrent(row: CmsContentHistoryAudit) {
    openViewer(blockLabel(row))
    const blockRes = await get(apiBase + "CMS/content/" + row.contentBlockId)
    if (!blockRes.success) {
        failViewer("Failed to load the current version")
        return
    }
    const current = blockRes.result as CmsContentBlock
    const res = await post(
        apiBase + "CMS/content/" + row.contentBlockId + "/history/" + row.contentHistoryId + "/diff",
        {
            content: current.content,
        },
    )
    if (res.success) {
        const diff = res.result as CmsContentHistoryDiff
        applyDiff(
            diff,
            `Changes from ${diffStamp(diff.oldModifiedOn, diff.oldModifiedBy)} to ${diffStamp(current.modifiedOn, current.modifiedBy)}`,
        )
    } else {
        failViewer(res.errors?.[0] ?? "Failed to load the current version")
    }
}

onMounted(reload)
</script>

<style scoped>
/* The diff action is icon-only and its tooltip label is unavailable on touch, so give it a
   44px touch target on coarse pointers. Mouse/trackpad keep the dense table-action size. */
@media (pointer: coarse) {
    .diff-action-btn {
        min-width: 2.75rem;
        min-height: 2.75rem;
    }
}
</style>
