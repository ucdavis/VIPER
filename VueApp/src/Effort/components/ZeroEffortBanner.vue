<template>
    <StatusBanner
        v-if="show"
        type="warning"
    >
        <div v-if="mode === 'self'">
            <strong>You have effort items with ZERO effort.</strong>
            You will not be able to verify your effort until these items have been updated to document hours/weeks or
            are removed.
        </div>
        <template v-else>
            NOTE: This instructor has one or more effort items documented as ZERO effort. Effort cannot be verified
            until these items have been updated or removed.
        </template>
    </StatusBanner>
</template>

<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"

defineProps<{
    show: boolean
    mode: "self" | "staff"
}>()
</script>
