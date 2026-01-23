<template>
    <main>
        <component
            :is="$route.meta.layout || 'div'"
            nav="cms"
            :navarea="true"
            highlighted-top-nav="Viper Home"
            :breadcrumbs="$route.meta?.breadcrumbs"
        >
        </component>
    </main>
    <GenericError />
</template>

<script setup lang="ts">
import GenericError from "@/components/GenericError.vue"
</script>
