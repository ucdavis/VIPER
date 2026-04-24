<script setup lang="ts">
defineProps<{
    submitLabel: string
    isSaving: boolean
}>()

defineEmits<{
    cancel: []
    submit: []
}>()
</script>

<template>
    <q-card-actions align="right">
        <q-btn
            flat
            label="Cancel"
            @click="$emit('cancel')"
        />
        <q-btn
            color="primary"
            :label="submitLabel"
            :loading="isSaving"
            @click="$emit('submit')"
        >
            <template #loading>
                <q-spinner
                    size="1em"
                    class="q-mr-sm"
                />
                {{ submitLabel }}
            </template>
        </q-btn>
    </q-card-actions>
</template>
