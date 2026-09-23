<script setup lang="ts">
import { computed } from "vue"

const props = withDefaults(
    defineProps<{
        complete: boolean
        label?: string
        showMissing?: boolean
    }>(),
    { label: "", showMissing: true },
)

const iconName = computed(() => {
    if (props.complete) {
        return "check_circle"
    } else if (!props.showMissing) {
        return ""
    }
    return "cancel"
})

const iconColor = computed(() => {
    if (props.complete) {
        return "positive"
    } else if (!props.showMissing) {
        return ""
    }
    return "negative"
})

const tooltipText = computed(() => {
    if (props.complete) {
        return "Complete"
    } else if (!props.showMissing) {
        return ""
    }
    return props.label ? `Missing ${props.label}` : "Missing"
})
</script>

<template>
    <!-- Quasar puts aria-hidden on every q-icon, so an aria-label there never reaches the
         accessibility tree. The name has to live in a sibling the icon cannot suppress. -->
    <span v-if="iconName">
        <q-icon
            :name="iconName"
            :color="iconColor"
            size="1.25rem"
            aria-hidden="true"
        >
            <q-tooltip>{{ tooltipText }}</q-tooltip>
        </q-icon>
        <span class="sr-only">{{ tooltipText }}</span>
    </span>
</template>
