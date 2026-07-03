<template>
    <div class="list-card-field">
        <span class="list-card-label">{{ label }}</span>
        <div class="list-card-value">
            <slot>{{ value }}</slot>
        </div>
    </div>
</template>

<script setup lang="ts">
// One label/value row inside a ListCard. Pass a plain value via the prop, or richer
// content (chips, links, a stamp) via the default slot.
defineProps<{ label: string; value?: string | number | null }>()
</script>
