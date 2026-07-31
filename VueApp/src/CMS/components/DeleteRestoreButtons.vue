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
    <template v-else>
        <q-btn
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
        <!-- Legacy "delete now": skips the 30-day trash. Admin-only, so the parent passes the
             server-decided capability rather than this component guessing at permissions. -->
        <q-btn
            v-if="canPermanentlyDelete"
            dense
            flat
            no-caps
            size="sm"
            color="negative"
            icon="delete_forever"
            :aria-label="`Permanently delete ${entityName}`"
            @click="emit('permanentDelete')"
        >
            <q-tooltip>Delete now</q-tooltip>
        </q-btn>
    </template>
</template>

<script setup lang="ts">
withDefaults(
    defineProps<{
        deleted: boolean
        entityName: string
        canPermanentlyDelete?: boolean
    }>(),
    { canPermanentlyDelete: false },
)

const emit = defineEmits<{ delete: []; restore: []; permanentDelete: [] }>()
</script>
