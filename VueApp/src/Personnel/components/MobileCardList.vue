<template>
    <div class="lt-md">
        <!-- Shown whether or not the list can be edited, unlike a table's own title: the column
             headers that identify a table on desktop are not rendered at this width, so without
             this heading the cards say nothing about what they belong to. A real heading rather
             than a styled div, so the page is navigable structure. -->
        <!-- tabindex allows a jump link to land focus here, not just the viewport. -->
        <h2
            :id="anchorId"
            class="text-h6 q-mt-none q-mb-sm sticky-filter-offset"
            tabindex="-1"
        >
            {{ title }}
            <slot name="title-append" />
        </h2>
        <MobileSortControl
            v-model="sortBy"
            v-model:descending="sortDescending"
            :options="sortOptions"
        />
        <q-list
            bordered
            separator
        >
            <q-item
                v-for="row in visibleRows"
                :key="keyFor(row)"
                class="sticky-filter-offset"
            >
                <q-item-section>
                    <q-item-label class="text-weight-medium">
                        <slot
                            name="card-title"
                            :row="row"
                        />
                    </q-item-label>
                    <!-- Not q-item-label's caption prop: it applies both .75rem type and a 0.54
                         alpha, and these values are the point of the card rather than secondary to
                         it. Only the field name is muted, via the .text-grey the palette overrides
                         remap to the AA-safe body grey. -->
                    <q-item-label
                        v-for="detail in detailLines(row)"
                        :key="detail.name"
                        class="text-body2"
                    >
                        <span class="text-grey">{{ detail.label }}:</span>
                        {{ detail.value }}
                    </q-item-label>
                    <!-- For a caller whose card says something the column labels cannot, such as a
                         value that needs no naming. -->
                    <slot
                        name="card-detail"
                        :row="row"
                    />
                </q-item-section>
                <q-item-section
                    v-if="$slots['card-actions']"
                    side
                >
                    <div class="row no-wrap">
                        <slot
                            name="card-actions"
                            :row="row"
                        />
                    </div>
                </q-item-section>
            </q-item>
            <q-item v-if="!loading && visibleRows.length === 0">
                <q-item-section class="text-grey">{{ emptyMessage }}</q-item-section>
            </q-item>
        </q-list>
    </div>
</template>

<script setup lang="ts" generic="T">
import MobileSortControl from "./MobileSortControl.vue"
import { columnText, useMobileTableRows } from "../composables/use-mobile-table-rows"
import type { QTableColumn, QTableProps } from "quasar"

/**
 * The card rendering of a phone list table, for the widths at which its columns stop fitting.
 * Each caller pairs one of these with its own QTable, shows one by CSS and hides the other, and
 * supplies the parts that differ: what heads a card, and what its buttons do.
 *
 * Column headers are gone at this width, so every value carries its own label, taken from the
 * column definition rather than written out here - the two views cannot then describe a field
 * differently, and a per-list label (a section's director title) is picked up for free.
 */
const props = defineProps<{
    /** Heads the list, since the table's own title is not rendered at this width. */
    title: string
    columns: QTableColumn[] | undefined
    rows: T[]
    search: string
    loading: boolean
    /** Row property holding a stable key, as QTable's row-key does. */
    rowKey: string
    /** Columns the caller renders itself, as a card title or as buttons. */
    omitColumns: string[]
    emptyMessage: string
    /** Set by a page offering jump links, so this heading can be one of the targets. */
    anchorId?: string
}>()

// The caller's own pagination model, the one bound to its table, so a sort chosen here is the
// sort the table shows if the window widens, and a header click there is reflected in the control.
const pagination = defineModel<QTableProps["pagination"]>("pagination", { required: true })

const { sortOptions, sortBy, sortDescending, visibleRows } = useMobileTableRows({
    columns: () => props.columns,
    rows: () => props.rows,
    search: () => props.search,
    pagination,
})

function keyFor(row: T): string | number {
    return (row as Record<string, string | number>)[props.rowKey]
}

/** The labelled values under a card's title, skipping fields this row has no value for. */
function detailLines(row: T) {
    return (props.columns ?? [])
        .filter((col) => !props.omitColumns.includes(col.name))
        .map((col) => ({ name: col.name, label: col.label, value: columnText(col, row) }))
        .filter((detail) => detail.value !== "")
}
</script>
