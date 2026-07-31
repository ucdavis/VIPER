<template>
    <!-- Content sanitized by CMS.cs using HtmlSanitizerService -->
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

const props = defineProps<{
    contentBlockName: string
}>()

const contentBlock: Ref<ContentBlock | null> = ref(null)

async function loadContentBlock() {
    const { get } = useFetch()
    // Capture the name we're loading; if props.contentBlockName changes while this request is in
    // flight, a slower earlier response must not overwrite the newer block's content.
    const requestedName = props.contentBlockName
    const r = await get(import.meta.env.VITE_API_URL + "cms/content/fn/" + requestedName)
    if (props.contentBlockName !== requestedName) return
    contentBlock.value = r.success ? r.result : null
}

// Reload only when the block name changes. props is reactive, so watching the
// specific source (instead of a deep watch on props) avoids the duplicate
// immediate fetch and unrelated re-runs. Headings for the sanitized HTML are
// styled globally in styles/base.css (shared with the diff view).
watch(() => props.contentBlockName, loadContentBlock, { immediate: true })
</script>
