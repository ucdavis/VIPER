<template>
    <!-- Reference value (old === new): show once without diff styling -->
    <template v-if="detail.oldValue === detail.newValue">
        <span>{{ detail.oldValue }}</span>
    </template>
    <!-- Changed value: show old → new -->
    <template v-else>
        <span
            v-if="detail.oldValue !== null"
            class="text-negative"
            >{{ detail.oldValue }}</span
        >
        <span v-if="detail.oldValue !== null && detail.newValue !== null"> &rarr; </span>
        <span
            v-if="detail.newValue !== null"
            class="text-positive"
            >{{ detail.newValue }}</span
        >
    </template>
</template>

<script setup lang="ts">
import type { ChangeDetail } from "../types"

// One audit change value, rendered the same way in every AuditList layout
// (desktop expando, tablet detail row, mobile card).
defineProps<{
    detail: ChangeDetail
}>()
</script>
