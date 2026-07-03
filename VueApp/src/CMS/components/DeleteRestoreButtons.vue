<template>
    <q-btn
        v-if="!deleted"
        dense
        flat
        no-caps
        size="sm"
        color="negative"
        icon="delete"
        :aria-label="`Delete ${entityName}`"
        @click="emit('delete')"
    >
        <q-tooltip>Delete</q-tooltip>
    </q-btn>
    <q-btn
        v-else
        dense
        flat
        no-caps
        size="sm"
        color="positive"
        icon="restore_from_trash"
        :aria-label="`Restore ${entityName}`"
        @click="emit('restore')"
    >
        <q-tooltip>Restore</q-tooltip>
    </q-btn>
</template>

<script setup lang="ts">
defineProps<{
    deleted: boolean
    entityName: string
}>()

const emit = defineEmits<{ delete: []; restore: [] }>()
</script>
