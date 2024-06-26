<script setup lang="ts">
    import { useRoute } from 'vue-router'
    const route = useRoute()
</script>

<template>
    <h2>Error 404 - not found</h2>
    <p>
        The page at {{ route.path }} could not be found.
    </p>
</template>
