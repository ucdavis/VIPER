<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"

/**
 * Shows the notice a route guard left when it redirected here, explaining why the user did not
 * reach the page they asked for. Renders nothing without one.
 *
 * The page takes the notice with useAccessNotice() in its own setup and passes it in, rather than
 * this component taking it: a record page renders its content only once the record loads, and a
 * notice left untaken until then would surface on some later page instead.
 */
defineProps<{
    notice: string | null
}>()
</script>

<template>
    <StatusBanner
        v-if="notice"
        type="info"
    >
        {{ notice }}
    </StatusBanner>
</template>
