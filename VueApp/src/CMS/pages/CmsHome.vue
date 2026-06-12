<template>
    <div class="q-pa-md">
        <h1 class="q-my-none q-mb-md">Content Management System</h1>

        <StatusBanner
            v-if="visibleSections.length === 0"
            type="info"
        >
            Your account does not have access to any CMS tools. Contact the VIPER team if you need access.
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
                />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useUserStore } from "@/store/UserStore"
import StatusBanner from "@/components/StatusBanner.vue"
import RecentActivity from "@/CMS/components/RecentActivity.vue"

const userStore = useUserStore()

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

const sections: Section[] = [
    {
        title: "Files",
        icon: "folder",
        description:
            "Store files for VIPER pages and share them through permission-checked download links. Every download is logged.",
        permissions: ["SVMSecure.CMS.AllFiles"],
        actions: [
            { label: "Manage Files", to: { name: "CmsFiles" } },
            { label: "Audit Log", to: { name: "CmsFileAudit" } },
        ],
    },
    {
        title: "Content Blocks",
        icon: "article",
        description: "Edit the text and images displayed on VIPER pages. Every change is kept in version history.",
        permissions: ["SVMSecure.CMS.ManageContentBlocks", "SVMSecure.CMS.CreateContentBlock"],
        actions: [
            {
                label: "Manage Content Blocks",
                to: { name: "CmsContentBlocks" },
                permissions: ["SVMSecure.CMS.ManageContentBlocks"],
            },
            { label: "Add Content Block", to: { name: "CmsContentBlockEdit" } },
        ],
    },
    {
        title: "Link Collections",
        icon: "link",
        description: "Maintain the tagged link lists displayed on VIPER pages.",
        permissions: ["SVMSecure.CMS.ManageContentBlocks"],
        actions: [{ label: "Manage Link Collections", to: { name: "ManageLinkCollections" } }],
    },
    {
        title: "Left Navigation",
        icon: "menu",
        description: "Control the left-hand menus shown on VIPER sections and pages.",
        permissions: ["SVMSecure.CMS.ManageNavigation"],
        actions: [{ label: "Manage Left-Nav Menus", to: { name: "CmsLeftNavMenus" } }],
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
const showRecentActivity = computed(() => canManageBlocks.value || canManageFiles.value)
</script>

<style scoped>
/* Cards as a 2x2 grid with the activity rail beside them; cap the width
   so neither stretches into low-density slabs on wide monitors. */
.cms-home-grid {
    max-width: 80rem;
}

/* CSS grid with equal rows so all four cards render as same-size tiles
   regardless of description length; collapses to one column when narrow. */
.cms-home-cards {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(17rem, 1fr));
    grid-auto-rows: 1fr;
    gap: 16px;
}

.cms-home-cards .q-card {
    display: flex;
    flex-direction: column;
}

.cms-home-cards .q-card .q-card__actions {
    margin-top: auto;
}
</style>
