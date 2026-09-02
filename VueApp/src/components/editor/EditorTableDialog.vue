<template>
    <RecordFormDialog
        :model-value="modelValue"
        title-id="editor-table-dialog-title"
        title="Insert Table"
        :is-edit="false"
        :saving="false"
        form-error=""
        submit-label="Insert"
        @update:model-value="emit('update:modelValue', $event)"
        @submit="emit('submit', { rows, cols, header })"
        @hide="reset"
    >
        <q-input
            v-model.number="rows"
            data-autofocus
            outlined
            dense
            type="number"
            label="Rows"
            :min="1"
            :max="MAX_ROWS"
            hint="Including the header row"
            :rules="[(v: number) => inRange(v, MAX_ROWS) || `Enter a number of rows from 1 to ${MAX_ROWS}`]"
        />

        <q-input
            v-model.number="cols"
            outlined
            dense
            type="number"
            label="Columns"
            :min="1"
            :max="MAX_COLS"
            :rules="[(v: number) => inRange(v, MAX_COLS) || `Enter a number of columns from 1 to ${MAX_COLS}`]"
        />

        <q-checkbox
            v-model="header"
            label="First row is a header"
        />
    </RecordFormDialog>
</template>

<script setup lang="ts">
import { ref } from "vue"
import RecordFormDialog from "@/components/RecordFormDialog.vue"

/** Collects the shape of a new table; the parent builds and inserts the HTML. */

defineProps<{ modelValue: boolean }>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    submit: [value: { rows: number; cols: number; header: boolean }]
}>()

// Matching buildTableHtml's own clamps, so the dialog rejects what the builder would silently trim.
const MAX_ROWS = 50
const MAX_COLS = 20

const DEFAULT_ROWS = 3
const DEFAULT_COLS = 3

const rows = ref(DEFAULT_ROWS)
const cols = ref(DEFAULT_COLS)
const header = ref(true)

function inRange(value: number, max: number) {
    return Number.isInteger(value) && value >= 1 && value <= max
}

function reset() {
    rows.value = DEFAULT_ROWS
    cols.value = DEFAULT_COLS
    header.value = true
}
</script>
