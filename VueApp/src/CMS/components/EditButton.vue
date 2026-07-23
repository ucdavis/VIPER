<template>
    <q-btn
        dense
        flat
        no-caps
        size="sm"
        color="secondary"
        icon="edit"
        :aria-label="`Edit ${entityName}`"
        :to="to"
        @click="emit('click')"
    >
        <q-tooltip>Edit</q-tooltip>
    </q-btn>
</template>

<script setup lang="ts">
import type { RouteLocationRaw } from "vue-router"

defineProps<{
    entityName: string
    to?: RouteLocationRaw
}>()

const emit = defineEmits<{ click: [] }>()
</script>
