<template>
    <section aria-label="Recent activity">
        <!-- Bare h2: the compressed-heading rule renders it 1.2rem bold, matching the hub's
             primary card title (text-h6 would drop it to 1rem regular, weaker than the rows). -->
        <h2 class="text-primary q-my-none">Recent activity</h2>

        <template v-if="loading">
            <q-skeleton
                v-for="n in 4"
                :key="n"
                type="text"
                class="q-my-sm"
            />
        </template>

        <div
            v-else-if="failed"
            class="text-body2 text-grey-8 q-mt-xs"
        >
            Couldn't load recent activity.
        </div>

        <div
            v-else-if="items.length === 0"
            class="text-body2 text-grey-8 q-mt-xs"
        >
            Nothing edited recently.
        </div>

        <q-list
            v-else
            dense
        >
            <q-item
                v-for="item in items"
                :key="item.key"
                clickable
                :to="item.to"
                class="q-px-none"
            >
                <q-item-section side>
                    <q-icon
                        :name="item.icon"
                        :aria-label="item.typeLabel"
                        color="secondary"
                        size="sm"
                    />
                </q-item-section>
                <q-item-section>
                    <q-item-label lines="1">{{ item.label }}</q-item-label>
                    <q-item-label
                        caption
                        :title="formatFullDate(item.modifiedOn)"
                    >
                        {{ item.typeLabel }} &middot; {{ item.verb ? item.verb + " " : ""
                        }}{{ formatTimeAgo(new Date(item.modifiedOn)) }} by
                        {{ item.modifiedBy }}
                    </q-item-label>
                </q-item-section>
                <q-item-section
                    v-if="item.actions?.length"
                    side
                    class="activity-actions"
                >
                    <div class="row no-wrap">
                        <q-btn
                            v-for="action in item.actions"
                            :key="action.icon"
                            flat
                            dense
                            round
                            size="sm"
                            color="primary"
                            :icon="action.icon"
                            :aria-label="action.label"
                            :to="action.to"
                            @click.stop="runAction(action, $event)"
                        >
                            <q-tooltip>{{ action.label }}</q-tooltip>
                        </q-btn>
                    </div>
                </q-item-section>
            </q-item>
        </q-list>

        <ContentDiffDialog
            v-model="viewer.open"
            :loading="viewer.loading"
            :title="viewer.title"
            :subtitle="viewer.subtitle"
            :diff-html="viewer.content"
            :has-comparison="viewer.hasComparison"
            :has-changes="viewer.hasChanges"
        />
    </section>
</template>

<script setup lang="ts">
import { inject, onMounted, ref } from "vue"
import { useQuasar } from "quasar"
import { formatTimeAgo } from "@vueuse/core"
import { useFetch } from "@/composables/ViperFetch"
import { useContentDiffViewer } from "@/CMS/composables/use-content-diff-viewer"
import ContentDiffDialog from "@/CMS/components/ContentDiffDialog.vue"
import type {
    CmsContentBlock,
    CmsContentHistoryAudit,
    CmsContentHistoryDiff,
    CmsFile,
    CmsLeftNavMenu,
} from "@/CMS/types/"

const MAX_ITEMS = 8
const PER_SOURCE = 5

const {
    showBlocks = false,
    showFiles = false,
    showLeftNavs = false,
} = defineProps<{
    showBlocks?: boolean
    showFiles?: boolean
    showLeftNavs?: boolean
}>()

type RailAction = {
    icon: string
    label: string
    to?: { name: string; query?: Record<string, string> }
    run?: () => void
}

type ActivityItem = {
    key: string
    icon: string
    typeLabel: string
    // Rendered before the time-ago stamp (e.g. "deleted 2 days ago"); empty means plain recency.
    verb?: string
    label: string
    to: { name: string; params?: Record<string, string | number>; query?: Record<string, string> }
    modifiedOn: string
    modifiedBy: string
    actions?: RailAction[]
}

const $q = useQuasar()
const apiURL = inject("apiURL")
const { get, post, createUrlSearchParams } = useFetch()
const { viewer, openViewer, applyDiff, closeViewer, failViewer, diffStamp } = useContentDiffViewer()

const loading = ref(true)
const failed = ref(false)
const items = ref<ActivityItem[]>([])

function formatFullDate(value: string): string {
    return new Date(value).toLocaleString()
}

// Row-level navigation must not fire for action clicks (`.stop` in the template). Actions with
// their own `to` navigate via QBtn; only run-actions preventDefault, both to keep the row's
// anchor inert and because a prevented default makes QBtn cancel its router navigation.
function runAction(action: RailAction, event: Event) {
    if (!action.run) return
    event.preventDefault()
    action.run()
}

// History rows hold only superseded versions, and the GET diff endpoint compares two of them —
// so the block's latest change (newest history row -> live content) needs the POST diff with
// the current content, exactly like the editor's compare-against-draft flow.
async function openLatestDiff(b: CmsContentBlock, label: string) {
    openViewer(label)
    const listParams = createUrlSearchParams({ contentBlockId: b.contentBlockId, page: 1, perPage: 1 })
    const [blockRes, listRes] = await Promise.all([
        get(apiURL + "CMS/content/" + b.contentBlockId),
        get(apiURL + "CMS/content/history?" + listParams),
    ])
    const previous = ((listRes.result ?? []) as CmsContentHistoryAudit[])[0]
    if (listRes.success && !previous) {
        closeViewer()
        $q.notify({ type: "info", message: "No edit history for this block yet." })
        return
    }
    if (!listRes.success || !blockRes.success) {
        failViewer("Failed to load the latest change")
        return
    }
    const res = await post(
        apiURL + "CMS/content/" + b.contentBlockId + "/history/" + previous.contentHistoryId + "/diff",
        {
            content: (blockRes.result as CmsContentBlock).content,
        },
    )
    if (res.success) {
        const diff = res.result as CmsContentHistoryDiff
        applyDiff(
            diff,
            `Changes from ${diffStamp(diff.oldModifiedOn, diff.oldModifiedBy)} to ${diffStamp(b.modifiedOn, b.modifiedBy)}`,
        )
    } else {
        failViewer(res.errors?.[0] ?? "Failed to load the latest change")
    }
}

async function loadBlocks(): Promise<ActivityItem[]> {
    // Sort/limit server-side like loadFiles: sorting a default page client-side would only
    // ever surface the recent-most of the first alphabetical page of blocks.
    const params = createUrlSearchParams({
        status: "active",
        page: 1,
        perPage: PER_SOURCE,
        sortBy: "modifiedOn",
        descending: "true",
    })
    const res = await get(apiURL + "CMS/content?" + params)
    if (!res.success) throw new Error("blocks")
    return ((res.result ?? []) as CmsContentBlock[]).map((b) => {
        const label = b.title || b.friendlyName || "(untitled)"
        return {
            key: "block-" + b.contentBlockId,
            icon: "article",
            typeLabel: "Content block",
            label,
            to: { name: "CmsContentBlockEdit", params: { id: b.contentBlockId } },
            modifiedOn: b.modifiedOn,
            modifiedBy: b.modifiedBy,
            actions: [
                {
                    icon: "difference",
                    label: "View latest change",
                    run: () => void openLatestDiff(b, label),
                },
                {
                    icon: "history",
                    label: "View edit history",
                    to: { name: "CmsContentBlockHistory", query: { contentBlockId: String(b.contentBlockId) } },
                },
            ],
        }
    })
}

function fileAuditAction(fileGuid: string): RailAction {
    return {
        icon: "fact_check",
        label: "View audit trail",
        to: { name: "CmsFileAudit", query: { fileGuid } },
    }
}

async function loadFiles(): Promise<ActivityItem[]> {
    const params = createUrlSearchParams({
        status: "active",
        page: 1,
        perPage: PER_SOURCE,
        sortBy: "modifiedOn",
        descending: "true",
    })
    const res = await get(apiURL + "cms/files/?" + params)
    if (!res.success) throw new Error("files")
    return ((res.result ?? []) as CmsFile[]).map((f) => ({
        key: "file-" + f.fileGuid,
        icon: "description",
        typeLabel: "File",
        label: f.friendlyName,
        to: { name: "CmsFiles", query: { search: f.friendlyName } },
        modifiedOn: f.modifiedOn,
        modifiedBy: f.modifiedBy,
        actions: [fileAuditAction(f.fileGuid)],
    }))
}

async function loadDeletedFiles(): Promise<ActivityItem[]> {
    const params = createUrlSearchParams({
        status: "deleted",
        page: 1,
        perPage: PER_SOURCE,
        sortBy: "deletedOn",
        descending: "true",
    })
    const res = await get(apiURL + "cms/files/?" + params)
    if (!res.success) throw new Error("deletedFiles")
    return ((res.result ?? []) as CmsFile[]).map((f) => ({
        key: "trash-" + f.fileGuid,
        icon: "delete",
        typeLabel: "File",
        verb: "deleted",
        label: f.friendlyName,
        to: { name: "CmsFiles", query: { status: "deleted", search: f.friendlyName } },
        // Soft delete stamps ModifiedOn/ModifiedBy alongside DeletedOn, so the deleter is correct.
        modifiedOn: f.deletedOn ?? f.modifiedOn,
        modifiedBy: f.modifiedBy,
        actions: [fileAuditAction(f.fileGuid)],
    }))
}

async function loadLeftNavs(): Promise<ActivityItem[]> {
    // The left-nav list endpoint returns all menus unsorted, so sort by modifiedOn here.
    const res = await get(apiURL + "cms/left-navs")
    if (!res.success) throw new Error("leftNavs")
    return [...((res.result ?? []) as CmsLeftNavMenu[])]
        .sort((a, b) => new Date(b.modifiedOn).getTime() - new Date(a.modifiedOn).getTime())
        .slice(0, PER_SOURCE)
        .map((m) => ({
            key: "leftnav-" + m.leftNavMenuId,
            icon: "menu",
            typeLabel: "Left-nav menu",
            label: m.menuHeaderText || m.friendlyName || "(untitled)",
            to: { name: "CmsLeftNavEdit", params: { id: m.leftNavMenuId } },
            modifiedOn: m.modifiedOn,
            modifiedBy: m.modifiedBy,
        }))
}

async function loadActivity() {
    const sources = [
        ...(showBlocks ? [loadBlocks()] : []),
        ...(showFiles ? [loadFiles(), loadDeletedFiles()] : []),
        ...(showLeftNavs ? [loadLeftNavs()] : []),
    ]
    const results = await Promise.allSettled(sources)
    const loaded = results.filter((r) => r.status === "fulfilled").flatMap((r) => r.value)
    failed.value = sources.length > 0 && results.every((r) => r.status === "rejected")
    items.value = loaded
        .sort((a, b) => new Date(b.modifiedOn).getTime() - new Date(a.modifiedOn).getTime())
        .slice(0, MAX_ITEMS)
    loading.value = false
}

onMounted(() => {
    void loadActivity()
})
</script>

<style scoped>
/* Keep the two icon actions from inflating the dense rows; the buttons carry their own
   padding and get the standard 44px targets on coarse pointers via base.css. */
.activity-actions {
    padding-left: 0;
}

/* Quiet the rail: row actions fade in on row hover or keyboard focus. Opacity (not display)
   keeps the buttons in the layout and the tab order, so rows never shift and focus reveals
   them for keyboard users. */
.activity-actions .q-btn {
    opacity: 0;
    transition: opacity 0.15s ease-out;
}

.q-item:hover .activity-actions .q-btn,
.q-item:focus-within .activity-actions .q-btn {
    opacity: 1;
}

/* No hover on touch: keep the actions always visible so they don't need a discovery tap
   that would fight the row's own navigation. */
@media (pointer: coarse) {
    .activity-actions .q-btn {
        opacity: 1;
    }
}

@media (prefers-reduced-motion: reduce) {
    .activity-actions .q-btn {
        transition: none;
    }
}
</style>
