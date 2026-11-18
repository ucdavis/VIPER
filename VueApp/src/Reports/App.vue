<template>
    <!-- Reports come from every area, so this SPA has no section menu of its own. -->
    <component
        :is="$route.meta.layout || 'div'"
        :navarea="false"
    >
    </component>
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>
