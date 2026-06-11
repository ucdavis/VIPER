<template>
    <div class="q-pa-md">
        <h1 class="text-h4 text-primary q-my-none q-mb-md">CMS Home</h1>

        <div class="row q-col-gutter-md">
            <div
                v-for="section in visibleSections"
                :key="section.title"
                class="col-12 col-sm-6 col-md-4"
            >
                <q-card
                    flat
                    bordered
                >
                    <q-card-section>
                        <div class="text-h6 text-primary">
                            <q-icon
                                :name="section.icon"
                                class="q-mr-sm"
                            />
                            {{ section.title }}
                        </div>
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
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useUserStore } from "@/store/UserStore"

const userStore = useUserStore()

type Section = {
    title: string
    icon: string
    description: string
    permission: string
    actions: { label: string; to: { name: string; query?: Record<string, string> } }[]
}

const sections: Section[] = [
    {
        title: "Files",
        icon: "folder",
        description: "Upload and manage files with permission-based access and audit logging.",
        permission: "SVMSecure.CMS.AllFiles",
        actions: [
            { label: "Manage Files", to: { name: "CmsFiles" } },
            { label: "Audit Log", to: { name: "CmsFileAudit" } },
        ],
    },
    {
        title: "Link Collections",
        icon: "link",
        description: "Curate collections of links with tags and ordering.",
        permission: "SVMSecure.CMS.ManageContentBlocks",
        actions: [{ label: "Manage Link Collections", to: { name: "ManageLinkCollections" } }],
    },
]

const visibleSections = computed(() => sections.filter((s) => userStore.userInfo.permissions.includes(s.permission)))
</script>
