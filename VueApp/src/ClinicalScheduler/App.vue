<template>
    <main>
        <component :is="$route.meta.layout || 'div'"
                   nav="viper-clinical-scheduler" :navarea="false" highlightedTopNav="ClinicalScheduler">
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script lang="ts">
    import GenericError from '@/components/GenericError.vue'
    export default {
        name: 'ClinicalSchedulerApplication',
        components: {
            GenericError
        }
    }
</script>