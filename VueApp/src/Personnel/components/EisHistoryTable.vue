<!-- fallow-ignore-file css-broken-reference -- q-mb-lg is a Quasar spacing class, defined by Quasar rather than in this project -->
<template>
    <h3>{{ caption }}</h3>
    <p v-if="entries.length === 0">{{ emptyText }}</p>
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
                    Action date
                </th>
                <th
                    scope="col"
                    class="text-left"
                >
                    Title (title code)
                </th>
                <th
                    scope="col"
                    class="text-left"
                >
                    Department
                </th>
                <th scope="col">Begin date</th>
                <th scope="col">End date</th>
                <th scope="col">Step</th>
                <th scope="col">Percent</th>
                <th
                    scope="col"
                    class="text-right"
                >
                    Pay rate
                </th>
                <th
                    scope="col"
                    class="text-left"
                >
                    Comment
                </th>
            </tr>
        </thead>
        <tbody>
            <tr
                v-for="(entry, index) in entries"
                :key="index"
            >
                <td>{{ formatEisDate(entry.actionDate) }}</td>
                <td>{{ titleText(entry) }}</td>
                <td>{{ entry.department }}</td>
                <td class="text-center">{{ formatEisDate(entry.beginDate) }}</td>
                <td class="text-center">{{ formatEisDate(entry.endDate) }}</td>
                <td class="text-center">{{ entry.step }}</td>
                <td class="text-center">{{ formatDecimal(entry.percent) }}</td>
                <td class="text-right">{{ formatMoney(entry.payRate) }}</td>
                <td>{{ entry.comment }}</td>
            </tr>
        </tbody>
    </q-markup-table>
</template>

<script setup lang="ts">
import { formatDecimal, formatEisDate, formatMoney } from "../services/eis-format"
import type { EisHistoryEntry } from "../types/eis-types"

/** One appointment history list, from UCPath or from the retired PPS system. */
defineProps<{
    caption: string
    emptyText: string
    entries: EisHistoryEntry[]
}>()

function titleText(entry: EisHistoryEntry): string {
    return entry.titleCode === null ? (entry.title ?? "") : `${entry.title ?? ""} (${entry.titleCode})`
}
</script>
