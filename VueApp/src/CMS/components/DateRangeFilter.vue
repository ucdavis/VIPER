<template>
    <div class="col-12 col-sm-3 col-lg-2">
        <q-input
            v-model="from"
            dense
            outlined
            clearable
            label="From"
            type="date"
            stack-label
            @update:model-value="emit('change')"
        />
    </div>
    <div class="col-12 col-sm-3 col-lg-2">
        <q-input
            v-model="to"
            dense
            outlined
            clearable
            label="To"
            type="date"
            stack-label
            @update:model-value="emit('change')"
        />
    </div>
</template>

<script setup lang="ts">
// Shared From/To date-range inputs for the CMS audit/history list pages.
const from = defineModel<string>("from", { required: true })
const to = defineModel<string>("to", { required: true })
const emit = defineEmits<{ change: [] }>()
</script>
