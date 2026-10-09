<!-- fallow-ignore-file css-broken-reference -- q-mb-lg is a Quasar spacing class, defined by Quasar rather than in this project -->
<template>
    <p v-if="items.length === 0">{{ empty }}</p>
    <q-markup-table
        v-else
        flat
        bordered
        dense
        class="q-mb-lg"
    >
        <thead>
            <tr>
                <th
                    scope="col"
                    class="text-left"
                >
                    Year
                </th>
                <th
                    scope="col"
                    class="text-left"
                >
                    {{ label }}
                </th>
            </tr>
        </thead>
        <tbody>
            <tr
                v-for="(item, index) in items"
                :key="index"
            >
                <td class="text-no-wrap">{{ item.year }}</td>
                <td class="eis-dated-text">{{ item.text }}</td>
            </tr>
        </tbody>
    </q-markup-table>
</template>

<script setup lang="ts">
import type { EisDatedItem } from "../types/eis-types"

/** A year-and-text table for MyInfoVault boards, licenses, awards and honors. */
defineProps<{
    items: EisDatedItem[]
    /** The heading of the text column. */
    label: string
    /** What to say when there are no items. */
    empty: string
}>()
</script>

<style scoped>
.eis-dated-text {
    white-space: normal;
}
</style>
