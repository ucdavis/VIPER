<script setup lang="ts">
    //Imports from vue
    import type { Ref } from 'vue'
    import { ref, inject, defineProps, watch } from 'vue'
    //We'll use our fetch wrapper
    import { useFetch } from '@/composables/ViperFetch'

    //get the api root from our application settings
    const baseUrl = inject("apiURL")

    const props = defineProps({
        contentName: {
            type: String,
            required: true
        }
    })

    //creating a type is not necessary, but type checking can be helpful
    type ContentBlock = {
        content: string,
        title: string
    }

    //create a reactive value for our content block
    const cb = ref(null) as Ref<ContentBlock | null>

    async function getContentBlock() {
        const { get } = useFetch()
        get(baseUrl + "cms/content/fn/" + props.contentName)
            .then(r => cb.value = r.result)
    }

    watch(props, () => {
        if (props.contentName.length) {
            getContentBlock()
        }
    })

    getContentBlock()
</script>
<template>
    <div v-html="cb?.content"></div>
</template>