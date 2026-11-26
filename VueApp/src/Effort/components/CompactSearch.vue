<template>
    <q-input
        v-model="model"
        placeholder="Search..."
        dense
        outlined
        clearable
        class="compact-search"
    >
        <template #prepend>
            <q-icon
                name="search"
                size="xs"
            />
        </template>
    </q-input>
</template>

<script setup lang="ts">
// Clearing the input sets the model to null
const model = defineModel<string | null>({ default: "" })
</script>

<style scoped>
.compact-search {
    width: 10rem;
}

.compact-search :deep(.q-field__control) {
    height: 1.75rem;
    min-height: 1.75rem;
}

.compact-search :deep(.q-field__native) {
    font-size: 0.75rem;
    padding: 0;
}

.compact-search :deep(.q-field__prepend) {
    height: 1.75rem;
    padding-left: 0;
    padding-right: 0.25rem;
}

.compact-search :deep(.q-field__append) {
    height: 1.75rem;
    padding: 0 0.25rem;
}
</style>
