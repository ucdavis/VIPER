<template>
    <component
        :is="$route.meta.layout || 'div'"
        nav="viper-clinical-scheduler"
        :navarea="true"
        highlighted-top-nav="ClinicalScheduler"
    />
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>
