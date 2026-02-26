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
                    :href="props.link.url"
                    target="_blank"
                >
                    {{ props.link.title }}
                </a>
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
                        :color="getLinkCollectionTagColor(lct.sortOrder)"
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
import { watch } from "vue"
import type { Link, LinkCollection } from "@/CMS/types"
const props = defineProps({
    link: {
        type: Object as () => Link,
        required: true,
    },
    linkCollection: {
        type: Object as () => LinkCollection,
        required: true,
    },
})

function getLinkCollectionTagColor(order: number) {
    const colors = ["orange", "grey", "purple", "green", "blue"]
    return colors.length >= order ? colors[order - 1] : colors[colors.length - 1]
}

function openWebReports(url: string) {
    window.open(url, "_blank")?.focus()
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
