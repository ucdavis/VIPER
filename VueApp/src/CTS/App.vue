<template>
    <main>
        <component :is="$route.meta.layout || 'div'"
                   nav="cts" navarea="true" highlightedTopNav="Curriculum">
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script>
    import GenericError from '@/components/GenericError.vue'
    export default {
        name: 'CTSApplication',
        components: {
            GenericError
        }
       
    }
</script>
