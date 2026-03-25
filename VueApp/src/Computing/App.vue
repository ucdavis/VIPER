<template>
    <main>
        <component
            :is="$route.meta.layout || 'div'"
            nav="viper-cats"
            :navarea="true"
            highlighted-top-nav="Computing"
        >
        </component>
    </main>
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>
