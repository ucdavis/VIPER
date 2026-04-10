<template>
    <component
        :is="$route.meta.layout || 'div'"
        nav="cts"
        :navarea="true"
        highlighted-top-nav="Curriculum"
        :breadcrumbs="$route.meta?.breadcrumbs"
    >
    </component>
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>
