<template>
    <section aria-label="Recent activity">
        <h2 class="text-h6 text-primary q-my-none">Recent activity</h2>

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
                role="link"
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
                        {{ item.typeLabel }} &middot; {{ formatTimeAgo(new Date(item.modifiedOn)) }} by
                        {{ item.modifiedBy }}
                    </q-item-label>
                </q-item-section>
            </q-item>
        </q-list>
    </section>
</template>

<script setup lang="ts">
import { inject, onMounted, ref } from "vue"
import { formatTimeAgo } from "@vueuse/core"
import { useFetch } from "@/composables/ViperFetch"
import type { CmsContentBlock, CmsFile, CmsLeftNavMenu } from "@/CMS/types/"

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

type ActivityItem = {
    key: string
    icon: string
    typeLabel: string
    label: string
    to: { name: string; params?: Record<string, string | number>; query?: Record<string, string> }
    modifiedOn: string
    modifiedBy: string
}

const apiURL = inject("apiURL")
const { get, createUrlSearchParams } = useFetch()

const loading = ref(true)
const failed = ref(false)
const items = ref<ActivityItem[]>([])

function formatFullDate(value: string): string {
    return new Date(value).toLocaleString()
}

async function loadBlocks(): Promise<ActivityItem[]> {
    const res = await get(apiURL + "CMS/content?" + createUrlSearchParams({ status: "active" }))
    if (!res.success) throw new Error("blocks")
    return [...((res.result ?? []) as CmsContentBlock[])]
        .sort((a, b) => new Date(b.modifiedOn).getTime() - new Date(a.modifiedOn).getTime())
        .slice(0, PER_SOURCE)
        .map((b) => ({
            key: "block-" + b.contentBlockId,
            icon: "article",
            typeLabel: "Content block",
            label: b.title || b.friendlyName || "(untitled)",
            to: { name: "CmsContentBlockEdit", params: { id: b.contentBlockId } },
            modifiedOn: b.modifiedOn,
            modifiedBy: b.modifiedBy,
        }))
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
        ...(showFiles ? [loadFiles()] : []),
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
