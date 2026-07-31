<template>
    <h1 class="q-ma-none q-mb-md">
        <router-link :to="parentTo">{{ parentLabel }}</router-link>
        <span aria-hidden="true"> / </span>
        {{ label }}
    </h1>
</template>

<script setup lang="ts">
import type { RouteLocationRaw } from "vue-router"

// Breadcrumb-style page heading for subpages: the parent crumb links back, the
// current page is plain text. Link styling comes from base.css (h1 a rules).
defineProps<{
    label: string
    parentLabel: string
    parentTo: RouteLocationRaw
}>()
</script>
