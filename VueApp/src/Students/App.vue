<template>
    <main>
        <component :is="$route.meta.layout || 'div'"
                   nav="viper-students" navarea="false" highlightedTopNav="Students"
                   :breadcrumbs="$route.meta?.breadcrumbs">
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script>
    import GenericError from '@/components/GenericError.vue'
    export default {
        name: 'StudentsApplication',
        components: {
            GenericError
        }
       
    }
</script>
