<template>
    <div class="col-12 col-sm-3 col-lg-2">
        <q-input
            :model-value="from"
            dense
            outlined
            clearable
            label="From"
            type="date"
            stack-label
            @update:model-value="(value) => setDate('from', value)"
        />
    </div>
    <div class="col-12 col-sm-3 col-lg-2">
        <q-input
            :model-value="to"
            dense
            outlined
            clearable
            label="To"
            type="date"
            stack-label
            @update:model-value="(value) => setDate('to', value)"
        />
    </div>
</template>

<script setup lang="ts">
// Shared From/To date-range inputs for the CMS audit/history list pages.
const from = defineModel<string>("from", { required: true })
const to = defineModel<string>("to", { required: true })
const emit = defineEmits<{ change: [] }>()

// `clearable` makes QInput emit null when the field is cleared; coerce that back to "" so the model
// keeps its string contract and consumers treat a cleared date as their empty "no filter" sentinel
// rather than null.
function setDate(field: "from" | "to", value: string | number | null): void {
    const next = value === null || value === undefined ? "" : String(value)
    if (field === "from") {
        from.value = next
    } else {
        to.value = next
    }
    emit("change")
}
</script>
