<template>
    <!-- Content sanitized by CMS.cs using HtmlSanitizerService -->
    <!-- eslint-disable vue/no-v-html -->
    <div
        v-if="contentBlock"
        v-html="contentBlock.content"
        class="content-block"
    ></div>
    <!-- eslint-enable vue/no-v-html -->
    <!-- Nothing came back (no such block, or not one this viewer may see). Embedded uses stay
         silent; the standalone view page fills this in. -->
    <slot
        v-else-if="loaded"
        name="empty"
    />
</template>

<script setup lang="ts">
import type { Ref } from "vue"
import { computed, ref, watch } from "vue"
import type { ContentBlock } from "@/CMS/types"
import { useFetch } from "@/composables/ViperFetch"

const props = defineProps<{
    contentBlockName?: string
    contentBlockId?: number
}>()

const contentBlock: Ref<ContentBlock | null> = ref(null)
// Separates "not loaded yet" from "loaded, nothing to show" so the empty slot does not flash
// before the first response.
const loaded = ref(false)

// Blocks with no friendly name (nothing to put in a fn URL) are addressed by id instead; both
// display endpoints return the same public shape.
const blockPath = computed(() =>
    props.contentBlockId ? `id/${props.contentBlockId}` : `fn/${props.contentBlockName ?? ""}`,
)

async function loadContentBlock() {
    const { get } = useFetch()
    // Capture the block we're loading; if the props change while this request is in flight, a
    // slower earlier response must not overwrite the newer block's content.
    const requested = blockPath.value
    const r = await get(import.meta.env.VITE_API_URL + "cms/content/" + requested)
    if (blockPath.value !== requested) return
    contentBlock.value = r.success ? r.result : null
    loaded.value = true
}

// Reload only when the block being shown changes. Watching the resolved path (instead of a deep
// watch on props) avoids the duplicate immediate fetch and unrelated re-runs. Headings for the
// sanitized HTML are styled globally in styles/base.css (shared with the diff view).
watch(blockPath, loadContentBlock, { immediate: true })
</script>
