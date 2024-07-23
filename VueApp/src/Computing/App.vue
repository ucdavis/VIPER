<template>
    <main>
        <component :is="$route.meta.layout || 'div'"
                   nav="viper-cats" navarea="false" highlightedTopNav="Computing">
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script>
    import GenericError from '@/components/GenericError.vue'
    export default {
        name: 'Computing',
        components: {
            GenericError
        }
    }
</script>