<template>
    <component
        :is="$route.meta.layout || 'div'"
        nav="viper-home"
        :navarea="true"
        highlighted-top-nav="Personnel"
    >
    </component>
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>
