<template>
    <ContentBlock :contentBlockName="blockname" v-if="blockname != null"></ContentBlock>
</template>

<script setup lang="ts">
    import ContentBlock from '@/CMS/components/ContentBlock.vue'
    import type { Ref } from 'vue'
    import { ref } from 'vue'
    import { useRoute } from 'vue-router'

    const route = useRoute()
    const blockname: Ref<string | null> = ref(route.query.section as string | null)
</script>