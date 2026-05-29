<template>
    <q-badge
        :color="safeColor"
        :text-color="getAccessibleTextColor(safeColor)"
        :label="label"
    >
        <slot />
    </q-badge>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { getAccessibleTextColor, toContrastSafeColor } from "@/config/colors"

const props = defineProps<{
    color: string
    label?: string | number
}>()

const safeColor = computed(() => toContrastSafeColor(props.color))
</script>
