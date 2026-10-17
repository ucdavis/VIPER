<template>
    <span>{{ value ?? "" }}</span>
    <StatusBadge
        v-if="value === NOT_RELEASED"
        color="negative"
        label="Not released"
        class="q-ml-xs"
    />
</template>

<script setup lang="ts">
import StatusBadge from "@/components/StatusBadge.vue"

/**
 * A UCPath release flag. "N" means the employee has not released the value, which the legacy
 * page marked in red; here it also says so in words.
 */
const NOT_RELEASED = "N"

defineProps<{ value: string | null }>()
</script>
