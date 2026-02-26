<template>
    <!-- Content sanitized by CMS.cs using OWASP AntiSamy -->
    <!-- eslint-disable vue/no-v-html -->
    <div
        v-html="contentBlock?.content"
        class="content-block"
    ></div>
    <!-- eslint-enable vue/no-v-html -->
</template>

<script setup lang="ts">
import type { Ref } from "vue"
import { ref, watch } from "vue"
import type { ContentBlock } from "@/CMS/types"
import { useFetch } from "@/composables/ViperFetch"

const props = defineProps({
    contentBlockName: {
        type: String,
        required: true,
    },
})

const contentBlock: Ref<ContentBlock | null> = ref(null)

async function loadContentBlock() {
    const { get } = useFetch()
    const r = await get(import.meta.env.VITE_API_URL + "cms/content/fn/" + props.contentBlockName)
    contentBlock.value = r.result
}

watch(
    () => props,
    () => {
        loadContentBlock()
    },
    { immediate: true, deep: true },
)

loadContentBlock()
</script>

<style>
.content-block h1 {
    font-size: 2rem;
    line-height: 2rem;
}

.content-block h2 {
    font-size: 2rem;
    line-height: 2rem;
}

.content-block h3 {
    font-size: 2rem;
    line-height: 2rem;
}
</style>
