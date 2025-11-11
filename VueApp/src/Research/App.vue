<template>
    <main>
        <component :is="$route.meta.layout || 'div'"
                   nav="viper-research" :navarea="true" highlightedTopNav="Research">
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script>
    import GenericError from '@/components/GenericError.vue'
    export default {
        name: 'Research',
        components: {
            GenericError
        }
    }
</script>
