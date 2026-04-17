<template>
    <q-card
        :bordered="true"
        clickable
        @click="openWebReports(props.link.url)"
        class="link-card cursor-pointer q-hoverable"
        v-ripple
    >
        <span class="q-focus-helper"></span>
        <q-card-section class="q-py-sm">
            <div class="text-h3">
                <a
                    v-if="hrefForLink !== '#'"
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
                    <q-badge
                        v-if="tag.linkCollectionTagCategoryId == lct.linkCollectionTagCategoryId"
                        :color="getTagStyle(lct.sortOrder).color"
                        :text-color="getTagStyle(lct.sortOrder).textColor"
                        class="link-tag q-px-sm"
                    >
                        {{ tag.value }}
                    </q-badge>
                </template>
            </template>
        </q-card-section>
    </q-card>
</template>

<script setup lang="ts">
import { computed, watch } from "vue"
import type { Link, LinkCollection } from "@/CMS/types"
const props = defineProps<{
    link: Link
    linkCollection: LinkCollection
}>()

const hrefForLink = computed(() => safeHref(props.link.url))

// Each tag category's sortOrder (1-based) indexes this palette. Gold and Tahoe
// are light enough to require dark text for WCAG AA contrast (≥4.5:1).
const TAG_STYLES: ReadonlyArray<{ color: string; textColor: string }> = [
    { color: "warning", textColor: "dark" },
    { color: "secondary", textColor: "white" },
    { color: "negative", textColor: "white" },
    { color: "positive", textColor: "white" },
    { color: "info", textColor: "dark" },
    { color: "primary", textColor: "white" },
]

function getTagStyle(order: number) {
    const idx = order >= 1 ? (order - 1) % TAG_STYLES.length : 0
    return TAG_STYLES[idx]!
}

const SAFE_PROTOCOLS = ["http:", "https:", "mailto:", "tel:"]

function safeHref(url: string): string {
    const normalized = url?.trim()
    if (!normalized) return "#"
    try {
        const parsed = new URL(normalized)
        return SAFE_PROTOCOLS.includes(parsed.protocol) ? normalized : "#"
    } catch {
        return "#"
    }
}

function openWebReports(url: string) {
    const href = safeHref(url)
    if (href === "#") return
    window.open(href, "_blank", "noopener,noreferrer")?.focus()
}

watch(props, () => {}, { deep: true })
</script>

<style scoped>
.link-card {
    max-width: 350px;
    width: 100%;
}

.link-card a {
    text-decoration: none;
    color: inherit;
}

.link-tag {
    margin-right: 2px;
}
</style>
