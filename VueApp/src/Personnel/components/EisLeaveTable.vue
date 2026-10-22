<!-- fallow-ignore-file css-broken-reference -- q-mb-lg is a Quasar spacing class, defined by Quasar rather than in this project -->
<template>
    <h3>{{ caption }}</h3>
    <p v-if="leaves.length === 0">{{ emptyText }}</p>
    <q-markup-table
        v-else
        flat
        bordered
        dense
        class="q-mb-lg"
    >
        <thead>
            <tr>
                <th scope="col">Begin date</th>
                <th scope="col">Return date</th>
                <th
                    scope="col"
                    class="text-left"
                >
                    Description
                </th>
            </tr>
        </thead>
        <tbody>
            <tr
                v-for="(leave, index) in leaves"
                :key="index"
            >
                <td class="text-center">{{ formatEisDate(leave.beginDate) }}</td>
                <td class="text-center">{{ formatEisDate(leave.returnDate) }}</td>
                <td>{{ leave.description }}</td>
            </tr>
        </tbody>
    </q-markup-table>
</template>

<script setup lang="ts">
import { formatEisDate } from "../services/eis-format"
import type { EisLeave } from "../types/eis-types"

/** One leave of absence list, from UCPath or from the retired PPS system. */
defineProps<{
    caption: string
    emptyText: string
    leaves: EisLeave[]
}>()
</script>
