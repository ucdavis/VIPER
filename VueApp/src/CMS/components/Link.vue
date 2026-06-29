<template>
    <q-card
        :bordered="true"
        class="link-card"
    >
        <q-card-section class="q-py-sm">
            <div class="text-h3">
                <a
                    v-if="isSafe"
                    :href="hrefForLink"
                    target="_blank"
                    rel="noopener noreferrer"
                    @click.stop
                >
                    {{ props.link.title }}
                </a>
                <span v-else>
                    {{ props.link.title }}
                </span>
            </div>
        </q-card-section>
        <q-card-section class="q-py-sm">
            {{ props.link.description }}
        </q-card-section>
        <q-card-section class="q-py-sm">
            <template
                v-for="lct in props.linkCollection.linkCollectionTagCategories"
                :key="lct.linkCollectionTagCategoryId"
            >
                <template
                    v-for="tag in props.link.linkTags"
                    :key="tag.linkTagId"
                >
                    <StatusBadge
                        v-if="tag.linkCollectionTagCategoryId == lct.linkCollectionTagCategoryId"
                        :color="getTagColor(lct.sortOrder)"
                        class="q-px-sm q-mr-xs"
                    >
                        {{ tag.value }}
                    </StatusBadge>
                </template>
            </template>
        </q-card-section>
    </q-card>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { Link, LinkCollection } from "@/CMS/types"
import { safeHref } from "@/CMS/utils/url"
import StatusBadge from "@/components/StatusBadge.vue"
const props = defineProps<{
    link: Link
    linkCollection: LinkCollection
}>()

const hrefForLink = computed(() => safeHref(props.link.url))
const isSafe = computed(() => hrefForLink.value !== "#")

// Each tag category's sortOrder (1-based) indexes this palette of Quasar brand
// roles. StatusBadge derives the WCAG-AA text color for each (dark on the light
// warning/info tints, white on the rest).
const TAG_COLORS = ["warning", "secondary", "negative", "positive", "info", "primary"] as const

function getTagColor(order: number) {
    const idx = order >= 1 ? (order - 1) % TAG_COLORS.length : 0
    return TAG_COLORS[idx]
}
</script>

<style scoped>
.link-card {
    max-width: 21.875rem;
    width: 100%;
}

.link-card a {
    text-decoration: none;
    color: inherit;
}
</style>
