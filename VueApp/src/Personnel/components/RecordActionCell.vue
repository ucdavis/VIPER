<template>
    <q-td :props="cell">
        <RecordActionButton
            :action="action"
            @action="emit('action')"
        />
    </q-td>
</template>

<script setup lang="ts">
import RecordActionButton from "./RecordActionButton.vue"

/**
 * A table's edit or delete cell. Wrapping the button in a QTd is not optional - QTd reads the
 * cell scope for the column's alignment and classes - and every maintained phone list table
 * needs the same pair, so the wrapper lives here rather than being written out per table.
 */
defineProps<{
    action: "edit" | "delete"
    /** QTable's body-cell scope, passed straight through to QTd. */
    cell: unknown
}>()

const emit = defineEmits<{ action: [] }>()
</script>
