<template>
    <main>
        <component
            :is="$route.meta.layout || 'div'"
            nav="viper-clinical-scheduler"
            :navarea="true"
            highlighted-top-nav="ClinicalScheduler"
        />
    </main>
    <GenericError />
</template>

<script lang="ts">
import GenericError from "@/components/GenericError.vue"
export default {
    name: "ClinicalSchedulerApplication",
    components: {
        GenericError,
    },
}
</script>
