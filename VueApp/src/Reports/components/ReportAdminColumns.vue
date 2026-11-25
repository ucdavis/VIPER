<template>
    <section
        aria-labelledby="report-admin-columns"
        class="q-mb-lg"
    >
        <h2 id="report-admin-columns">Columns</h2>
        <SortableList
            v-model="columns"
            item-key="key"
            :announce="
                (column: FormColumn, index: number, total: number) =>
                    `${column.sourceColumn || 'Column'} moved to position ${index + 1} of ${total}`
            "
        >
            <template #row="{ item, index }">
                <div class="row q-col-gutter-sm items-center">
                    <q-select
                        :model-value="item.sourceColumn || null"
                        :label="`Column ${index + 1}`"
                        :options="sourceColumns"
                        :use-input="sourceColumns.length === 0"
                        :new-value-mode="sourceColumns.length === 0 ? 'add-unique' : undefined"
                        outlined
                        dense
                        options-dense
                        class="col-12 col-sm-4"
                        @update:model-value="update(index, { sourceColumn: $event ?? '' })"
                    />
                    <q-input
                        :model-value="item.label"
                        label="Heading"
                        :placeholder="item.sourceColumn"
                        maxlength="500"
                        outlined
                        dense
                        class="col-12 col-sm-3"
                        @update:model-value="update(index, { label: emptyAsNull($event) })"
                    />
                    <q-select
                        :model-value="item.format"
                        label="Format"
                        :options="FORMATS"
                        emit-value
                        map-options
                        outlined
                        dense
                        options-dense
                        class="col-12 col-sm-2"
                        @update:model-value="update(index, { format: $event })"
                    />
                    <div class="col-12 col-sm-3">
                        <q-checkbox
                            :model-value="item.visible"
                            label="Shown"
                            dense
                            class="q-mr-md"
                            @update:model-value="update(index, { visible: $event })"
                        />
                        <q-checkbox
                            :model-value="item.sortable"
                            label="Sortable"
                            dense
                            @update:model-value="update(index, { sortable: $event })"
                        />
                    </div>
                </div>
            </template>
            <template #actions="{ item, index }">
                <q-btn
                    dense
                    flat
                    size="sm"
                    color="negative"
                    icon="delete"
                    :aria-label="`Remove ${item.sourceColumn || 'column'}`"
                    @click="remove(index)"
                />
            </template>
            <template #empty>
                <p class="text-grey">No columns yet. Choose a data source, or add one.</p>
            </template>
        </SortableList>
        <div class="q-mt-sm">
            <q-btn
                flat
                no-caps
                icon="add"
                label="Add column"
                color="primary"
                @click="columns = [...columns, newColumn()]"
            />
            <q-btn
                v-if="canAddMissing"
                flat
                no-caps
                icon="playlist_add"
                label="Add the source's other columns"
                color="primary"
                @click="emit('addMissing')"
            />
        </div>
    </section>
</template>

<script setup lang="ts">
import SortableList from "@/components/SortableList.vue"
import { newColumn } from "../admin/report-admin-form"
import type { FormColumn } from "../admin/report-admin-form"
import type { ReportColumnFormat } from "@/shared/report-builder/report-builder-types"

/** The report's columns, in order, with their headings and formats. */
const { sourceColumns, canAddMissing = false } = defineProps<{
    /** The source's showable columns; empty when SQL Server can't describe them. */
    sourceColumns: string[]
    canAddMissing?: boolean
}>()

const emit = defineEmits<{ (e: "addMissing"): void }>()

const columns = defineModel<FormColumn[]>({ required: true })

const FORMATS: { value: ReportColumnFormat; label: string }[] = [
    { value: "Text", label: "Text" },
    { value: "Number", label: "Number" },
    { value: "Currency", label: "Currency" },
    { value: "Percent", label: "Percent" },
    { value: "Date", label: "Date" },
    { value: "DateTime", label: "Date and time" },
    { value: "YesNo", label: "Yes/No" },
]

function emptyAsNull(value: string | number | null): string | null {
    return value === null || String(value).trim() === "" ? null : String(value)
}

function update(index: number, change: Partial<FormColumn>): void {
    columns.value = columns.value.map((column, at) => (at === index ? { ...column, ...change } : column))
}

function remove(index: number): void {
    columns.value = columns.value.filter((_, at) => at !== index)
}
</script>
