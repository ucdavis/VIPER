<template>
    <component
        :is="$route.meta.layout || 'div'"
        nav="viper-effort"
        :navarea="true"
        highlighted-top-nav="Faculty"
    />
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>
