<template>
    <main>
        <component :is="$route.meta.layout || 'div'"
                   nav="example" navarea="true" highlightedTopNav="Computing">
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script>
    import GenericError from '@/components/GenericError.vue'
    export default {
        name: 'ExampleApplication',
        components: {
            GenericError
        }
    }
</script>