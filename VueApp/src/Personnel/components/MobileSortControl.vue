<template>
    <!-- Sorting a table is a click on a column header, which does not exist once the columns are
         rendered as cards. Renders nothing for a table with no sortable column. -->
    <div
        v-if="options.length > 0"
        class="row items-center no-wrap q-gutter-sm q-mb-sm"
    >
        <q-select
            v-model="sortBy"
            class="col"
            :options="options"
            emit-value
            map-options
            clearable
            dense
            options-dense
            outlined
            label="Sort by"
        />
        <q-btn
            type="button"
            flat
            dense
            :disable="sortBy === null"
            :icon="descending ? 'arrow_downward' : 'arrow_upward'"
            :aria-label="descending ? 'Sort ascending' : 'Sort descending'"
            @click="descending = !descending"
        >
            <q-tooltip>{{ descending ? "Sort ascending" : "Sort descending" }}</q-tooltip>
        </q-btn>
    </div>
</template>

<script setup lang="ts">
import type { SortOption } from "../composables/use-mobile-table-rows"

defineProps<{ options: SortOption[] }>()

const sortBy = defineModel<string | null>({ required: true })
// Named rather than the default model, so a caller reads as v-model + v-model:descending.
const descending = defineModel<boolean>("descending", { required: true })
</script>
