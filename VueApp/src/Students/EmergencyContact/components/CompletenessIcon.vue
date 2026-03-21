<script setup lang="ts">
import { computed } from "vue"

const props = defineProps<{
    complete: string | number
    total: string | number
}>()

const completeNum = computed(() => Number(props.complete))
const totalNum = computed(() => Number(props.total))

const iconName = computed(() => {
    if (completeNum.value >= totalNum.value) {
        return "check_circle"
    }
    if (completeNum.value === 0) {
        return "cancel"
    }
    return "warning"
})

const iconColor = computed(() => {
    if (completeNum.value >= totalNum.value) {
        return "positive"
    }
    if (completeNum.value === 0) {
        return "negative"
    }
    return "warning"
})

const tooltipText = computed(() => `${completeNum.value} of ${totalNum.value} complete`)
</script>

<template>
    <q-icon
        :name="iconName"
        :color="iconColor"
        size="1.25rem"
        role="img"
        :aria-label="tooltipText"
    >
        <q-tooltip>{{ tooltipText }}</q-tooltip>
    </q-icon>
</template>
