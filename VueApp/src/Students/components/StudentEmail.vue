<script setup lang="ts">
/** A student's email as a mail link, or nothing at all when the roster has no address for them. */
defineProps<{
    email?: string | null
}>()
</script>

<template>
    <a
        v-if="email"
        :href="`mailto:${email}`"
        class="text-primary"
    >
        {{ email }}
    </a>
</template>
