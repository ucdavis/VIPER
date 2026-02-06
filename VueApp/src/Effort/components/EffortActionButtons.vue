<template>
    <div class="row q-mb-md q-gutter-sm">
        <q-btn
            color="secondary"
            icon="cloud_download"
            dense
            outline
            aria-label="Import course from Banner"
            @click="$emit('import')"
        >
            <span class="lt-sm q-ml-xs">Import Course</span>
            <span class="gt-xs q-ml-xs">Import Course from Banner</span>
        </q-btn>
        <q-btn
            color="primary"
            icon="add"
            label="Add Effort"
            dense
            aria-label="Add effort record"
            @click="$emit('add')"
        />
    </div>
</template>

<script setup lang="ts">
defineEmits<{
    import: []
    add: []
}>()
</script>
