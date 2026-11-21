<template>
    <p v-if="items.length === 0">{{ empty }}</p>
    <component
        :is="ordered ? 'ol' : 'ul'"
        v-else
    >
        <li
            v-for="(item, index) in items"
            :key="index"
        >
            {{ item }}
        </li>
    </component>
</template>

<script setup lang="ts">
/** A plain list of MyInfoVault text entries, or a note when there are none. */
defineProps<{ items: string[]; empty: string; ordered?: boolean }>()
</script>
