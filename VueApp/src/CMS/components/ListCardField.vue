<template>
    <div class="list-card-field">
        <span class="list-card-label">{{ label }}</span>
        <div class="list-card-value">
            <slot>{{ value }}</slot>
        </div>
    </div>
</template>

<script setup lang="ts">
defineProps<{ label: string; value?: string | number | null }>()
</script>
