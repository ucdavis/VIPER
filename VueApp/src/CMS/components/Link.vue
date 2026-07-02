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

// Each tag category's sortOrder (1-based) indexes this categorical palette. It is
// built from non-semantic brand roles only (Aggie Blue, the secondary-palette
// arboretum/cabernet accents, Blue 70, and Ink) so status colors — positive,
// negative, warning, info — keep their meaning and never read as a tag category.
// StatusBadge derives the WCAG-AA text color for each (dark on arboretum, white
// on the rest).
const TAG_COLORS = ["primary", "arboretum", "cabernet", "secondary", "dark"] as const

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
