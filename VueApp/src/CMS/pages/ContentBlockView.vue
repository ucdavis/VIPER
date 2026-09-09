<template>
    <div class="q-pa-md">
        <ContentBlock
            :content-block-name="friendlyName"
            :content-block-id="contentBlockId"
        >
            <template #empty>
                <StatusBanner type="error">
                    This content block does not exist, or you do not have permission to view it.
                </StatusBanner>
            </template>
        </ContentBlock>
    </div>
</template>

<script setup lang="ts">
// One content block on its own page, rendered through the same component the site embeds, so a
// manager can open (and share) a block's display URL from the content-block list.
import { computed } from "vue"
import { useRoute } from "vue-router"
import ContentBlock from "@/CMS/components/ContentBlock.vue"
import StatusBanner from "@/components/StatusBanner.vue"

const route = useRoute()
const friendlyName = computed(() => String(route.params.fn ?? ""))
const contentBlockId = computed(() => (route.params.id ? Number(route.params.id) : undefined))
</script>
