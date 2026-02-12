<template>
    <main>
        <component
            :is="$route.meta.layout || 'div'"
            nav="viper-effort"
            :navarea="true"
            highlighted-top-nav="Faculty"
        />
    </main>
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>
