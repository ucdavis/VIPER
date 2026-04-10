<script setup lang="ts">
import { useRoute } from "vue-router"
const route = useRoute()
</script>

<template>
    <h1>Error 404 - not found</h1>
    <p>The page at {{ route.path }} could not be found.</p>
</template>
