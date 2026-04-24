<template>
    <q-badge
        :color="color"
        :text-color="getAccessibleTextColor(color)"
        :label="label"
    >
        <slot />
    </q-badge>
</template>

<script setup lang="ts">
import { getAccessibleTextColor } from "@/config/colors"

defineProps<{
    color: string
    label?: string | number
}>()
</script>
