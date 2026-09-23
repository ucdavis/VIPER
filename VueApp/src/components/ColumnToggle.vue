<script setup lang="ts">
import { useVModel } from "@vueuse/core"
import type { QTableProps } from "quasar"

/**
 * A button opening a checklist of a table's columns, so a user can hide the ones they do not
 * need. Binds to the table's `visible-columns`, and is built for ExportToolbar's `prepend` slot.
 * Results are not persisted beyond the current page session.
 */
const props = defineProps<{
    columns: NonNullable<QTableProps["columns"]>
    modelValue: string[] // Names of the columns currently shown.
}>()

const emit = defineEmits<{
    (e: "update:modelValue", value: string[]): void
}>()

const visibleColumns = useVModel(props, "modelValue", emit)
</script>

<template>
    <q-btn
        flat
        dense
        no-caps
        icon="view_column"
        label="Columns"
        class="q-mr-sm"
    >
        <q-menu
            anchor="bottom left"
            self="top left"
        >
            <q-list
                dense
                class="column-toggle-list"
            >
                <q-item-label
                    header
                    class="q-py-xs"
                >
                    Show Columns
                </q-item-label>
                <q-item
                    v-for="col in columns"
                    :key="col.name"
                    dense
                >
                    <q-item-section>
                        <q-checkbox
                            v-model="visibleColumns"
                            :val="col.name"
                            :label="col.label"
                            dense
                        />
                    </q-item-section>
                </q-item>
            </q-list>
        </q-menu>
    </q-btn>
</template>

<style scoped>
.column-toggle-list {
    min-width: 11rem;
}
</style>
