<template>
    <div class="q-pa-md">
        <h1 class="q-my-none q-mb-md">Content Management System</h1>

        <StatusBanner
            v-if="visibleSections.length === 0"
            type="info"
        >
            Your account does not have access to any CMS tools. Contact the VIPER team if you need access.
        </StatusBanner>

        <StatusBanner
            v-if="purgeSoonCount > 0"
            type="warning"
            class="q-mb-md"
        >
            {{ purgeWarningText }}
            <router-link :to="{ name: 'CmsFiles', query: { status: 'deleted' } }">Review the Trash</router-link>
            to restore anything still needed.
        </StatusBanner>

        <div class="row q-col-gutter-lg cms-home-grid">
            <div class="col-12 col-lg-8">
                <div class="cms-home-cards">
                    <q-card
                        v-for="section in visibleSections"
                        :key="section.title"
                        flat
                        bordered
                    >
                        <q-card-section>
                            <h2 class="text-h6 text-primary q-my-none">
                                <q-icon
                                    :name="section.icon"
                                    size="sm"
                                    class="q-mr-sm"
                                />
                                {{ section.title }}
                            </h2>
                            <div class="text-body2 text-grey-8 q-mt-xs">{{ section.description }}</div>
                        </q-card-section>
                        <q-card-actions>
                            <q-btn
                                v-for="action in section.actions"
                                :key="action.label"
                                flat
                                dense
                                no-caps
                                color="primary"
                                :label="action.label"
                                :to="action.to"
                            />
                        </q-card-actions>
                    </q-card>
                </div>
            </div>
            <div
                v-if="showRecentActivity"
                class="col-12 col-lg-4"
            >
                <RecentActivity
                    :show-blocks="canManageBlocks"
                    :show-files="canManageFiles"
                    :show-left-navs="canManageNav"
                />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, inject, ref, watch } from "vue"
import { inflect } from "inflection"
import { useUserStore } from "@/store/UserStore"
import { useFetch } from "@/composables/ViperFetch"
import StatusBanner from "@/components/StatusBanner.vue"
import RecentActivity from "@/CMS/components/RecentActivity.vue"
import type { CmsFile } from "@/CMS/types/"

const userStore = useUserStore()
const apiURL = inject("apiURL")
const { get, createUrlSearchParams } = useFetch()

type Action = {
    label: string
    to: { name: string; query?: Record<string, string> }
    // Defaults to the section's permissions when omitted.
    permissions?: string[]
}

type Section = {
    title: string
    icon: string
    description: string
    // Any one of these grants the section (mirrors CmsNavMenu.cs).
    permissions: string[]
    actions: Action[]
}

// One box per left-nav group, in the nav's order (CmsNavMenu.cs: Content Blocks, Files,
// Left Navigation Menus), with Manage Link Collections inside Content Blocks like the nav.
const sections: Section[] = [
    {
        title: "Content Blocks",
        icon: "article",
        description:
            "Edit the text, images, and tagged link lists displayed on VIPER pages. Every change is kept in version history.",
        permissions: ["SVMSecure.CMS.ManageContentBlocks", "SVMSecure.CMS.CreateContentBlock"],
        actions: [
            {
                label: "Manage Content Blocks",
                to: { name: "CmsContentBlocks" },
                permissions: ["SVMSecure.CMS.ManageContentBlocks"],
            },
            { label: "Add Content Block", to: { name: "CmsContentBlockEdit" } },
            {
                label: "Edit History",
                to: { name: "CmsContentBlockHistory" },
                permissions: ["SVMSecure.CMS.ManageContentBlocks"],
            },
            {
                label: "Manage Link Collections",
                to: { name: "ManageLinkCollections" },
                permissions: ["SVMSecure.CMS.ManageContentBlocks"],
            },
        ],
    },
    {
        title: "Files",
        icon: "folder",
        description:
            "Store files for VIPER pages and share them through permission-checked download links. Every download is logged.",
        permissions: ["SVMSecure.CMS.AllFiles"],
        actions: [
            { label: "Manage Files", to: { name: "CmsFiles" } },
            // Mirrors the nav's ManageFiles?upload=1 deep-link: opens the upload dialog on arrival.
            { label: "Add File", to: { name: "CmsFiles", query: { upload: "1" } } },
            { label: "Audit Trail", to: { name: "CmsFileAudit" } },
            // Home-only extra (not in the nav): file managers see their own deleted files here;
            // admins see the whole trash (scoped by the API). Inherits the section's AllFiles gate.
            { label: "Trash", to: { name: "CmsFiles", query: { status: "deleted" } } },
        ],
    },
    {
        title: "Left Navigation Menus",
        icon: "menu",
        description: "Control the left-hand menus shown on VIPER sections and pages.",
        permissions: ["SVMSecure.CMS.ManageNavigation"],
        actions: [
            { label: "Manage Left-Nav Menus", to: { name: "CmsLeftNavMenus" } },
            // Mirrors the nav's ManageLeftNav?add=1 deep-link: opens the create dialog on arrival.
            { label: "Add Left-Nav Menu", to: { name: "CmsLeftNavMenus", query: { add: "1" } } },
        ],
    },
]

const hasAny = (permissions: string[]) => permissions.some((p) => userStore.userInfo.permissions.includes(p))

const visibleSections = computed(() =>
    sections
        .filter((s) => hasAny(s.permissions))
        .map((s) => ({ ...s, actions: s.actions.filter((a) => hasAny(a.permissions ?? s.permissions)) })),
)
const canManageBlocks = computed(() => userStore.userInfo.permissions.includes("SVMSecure.CMS.ManageContentBlocks"))
const canManageFiles = computed(() => userStore.userInfo.permissions.includes("SVMSecure.CMS.AllFiles"))
const canManageNav = computed(() => userStore.userInfo.permissions.includes("SVMSecure.CMS.ManageNavigation"))
const showRecentActivity = computed(() => canManageBlocks.value || canManageFiles.value || canManageNav.value)

// Warn file managers when trashed files approach the purge cutoff. PurgeOn comes from the API
// (deletedOn + retention), so the warning stays correct if the retention window ever changes.
// One ascending page bounds the check; a trash deeper than one page still warns correctly
// because the soonest-purging files sort first.
const PURGE_WARNING_DAYS = 7
const purgeSoonCount = ref(0)

watch(
    canManageFiles,
    async (canManage) => {
        if (!canManage) {
            purgeSoonCount.value = 0
            return
        }
        const params = createUrlSearchParams({
            status: "deleted",
            page: 1,
            perPage: 50,
            sortBy: "deletedOn",
            descending: "false",
        })
        const res = await get(`${apiURL}cms/files/?${params}`)
        if (!res.success) {
            return
        }
        const cutoff = Date.now() + PURGE_WARNING_DAYS * 24 * 60 * 60 * 1000
        purgeSoonCount.value = ((res.result ?? []) as CmsFile[]).filter(
            (f) => f.purgeOn !== null && new Date(f.purgeOn).getTime() <= cutoff,
        ).length
    },
    { immediate: true },
)

const purgeWarningText = computed(
    () =>
        `${purgeSoonCount.value} trashed ${inflect("file", purgeSoonCount.value)} will be permanently ` +
        `deleted within ${PURGE_WARNING_DAYS} days.`,
)
</script>

<style scoped>
/* One full-width card per nav group, stacked in the nav's top-to-bottom order, with the
   activity rail beside them; cap the width so neither stretches into low-density slabs
   on wide monitors. */
.cms-home-grid {
    max-width: 80rem;
}

.cms-home-cards {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}
</style>
