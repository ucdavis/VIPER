<template>
    <q-icon
        :name="icon"
        :color="color"
    >
        <q-tooltip>{{ label }}</q-tooltip>
    </q-icon>
    <span class="sr-only">{{ label }}</span>
</template>

<script setup lang="ts">
// Row-status icon: tooltip for sighted users, sr-only text for screen readers.
defineProps<{
    icon: string
    color: string
    label: string
}>()
</script>
