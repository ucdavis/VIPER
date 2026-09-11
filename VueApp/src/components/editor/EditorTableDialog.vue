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
        @submit="emit('submit', { ...form })"
        @hide="reset"
    >
        <q-input
            v-model.number="form.rows"
            data-autofocus
            outlined
            dense
            type="number"
            label="Rows"
            :min="1"
            :max="MAX_TABLE_ROWS"
            hint="Including the header row"
            :rules="[(v: number) => inRange(v, MAX_TABLE_ROWS) || `Enter a number of rows from 1 to ${MAX_TABLE_ROWS}`]"
        />

        <q-input
            v-model.number="form.cols"
            outlined
            dense
            type="number"
            label="Columns"
            :min="1"
            :max="MAX_TABLE_COLS"
            :rules="[
                (v: number) => inRange(v, MAX_TABLE_COLS) || `Enter a number of columns from 1 to ${MAX_TABLE_COLS}`,
            ]"
        />

        <q-checkbox
            v-model="form.header"
            label="First row is a header"
        />

        <q-checkbox
            v-model="form.border"
            label="Show borders"
        />

        <q-select
            v-model="form.align"
            outlined
            dense
            options-dense
            emit-value
            map-options
            label="Alignment"
            :options="ALIGN_OPTIONS"
        />
    </RecordFormDialog>
</template>

<script setup lang="ts">
import { reactive } from "vue"
import RecordFormDialog from "@/components/RecordFormDialog.vue"
import type { TableAlign, TableOptions } from "@/components/editor/editor-html"
import { MAX_TABLE_COLS, MAX_TABLE_ROWS } from "@/components/editor/editor-html"

/** Collects the shape of a new table, in the order VIPER 1's CKEditor dialog asked for it; the parent
 *  builds and inserts the HTML. */

defineProps<{ modelValue: boolean }>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    submit: [value: TableOptions]
}>()

const ALIGN_OPTIONS: { label: string; value: TableAlign }[] = [
    { label: "Not set", value: "" },
    { label: "Left", value: "left" },
    { label: "Center", value: "center" },
    { label: "Right", value: "right" },
]

// One literal so a new field can't be added to the form and forgotten in reset().
const DEFAULTS = { rows: 3, cols: 3, header: true, border: true, align: "" as TableAlign }

const form = reactive({ ...DEFAULTS })

function inRange(value: number, max: number) {
    return Number.isInteger(value) && value >= 1 && value <= max
}

function reset() {
    Object.assign(form, DEFAULTS)
}
</script>
