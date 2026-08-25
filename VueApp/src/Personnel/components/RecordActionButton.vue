<template>
    <q-btn
        dense
        flat
        no-caps
        size="sm"
        :color="action === 'edit' ? 'positive' : 'negative'"
        :icon="action"
        :aria-label="label"
        @click="emit('action')"
    >
        <q-tooltip>{{ label }}</q-tooltip>
    </q-btn>
</template>

<script setup lang="ts">
import { computed } from "vue"

const props = defineProps<{
    action: "edit" | "delete"
}>()

const emit = defineEmits<{
    action: []
}>()

const label = computed(() => (props.action === "edit" ? "Edit Record" : "Delete Record"))
</script>
