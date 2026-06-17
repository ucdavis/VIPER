<template>
    <component
        :is="$route.meta.layout || 'div'"
        nav="viper-eval"
        :navarea="true"
        highlighted-top-nav="Curriculum"
    >
    </component>
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>

