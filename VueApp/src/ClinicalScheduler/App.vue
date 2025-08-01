<template>
    <main>
        <component :is="$route.meta.layout || 'div'"
                   nav="viper-clinical-scheduler" navarea="false" highlightedTopNav="ClinicalScheduler"
                   :breadcrumbs="$route.meta?.breadcrumbs">
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script>
    import GenericError from '@/components/GenericError.vue'
    export default {
        name: 'ClinicalSchedulerApplication',
        components: {
            GenericError
        }
    }
</script>