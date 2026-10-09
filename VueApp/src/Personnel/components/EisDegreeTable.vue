<!-- fallow-ignore-file css-broken-reference -- q-mb-lg is a Quasar spacing class, defined by Quasar rather than in this project -->
<template>
    <p v-if="degrees.length === 0">No degree information.</p>
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
                    v-for="column in DEGREE_COLUMNS"
                    :key="column"
                    scope="col"
                    class="text-left"
                >
                    {{ column }}
                </th>
            </tr>
        </thead>
        <tbody>
            <tr
                v-for="(degree, index) in degrees"
                :key="index"
            >
                <td>{{ degree.year }}</td>
                <td>{{ degree.degree }}</td>
                <td>{{ degree.institution }}</td>
                <td>{{ degree.location }}</td>
                <td>{{ degree.field }}</td>
            </tr>
        </tbody>
    </q-markup-table>
</template>

<script setup lang="ts">
import type { EisDegree } from "../types/eis-types"

/** The employee's MyInfoVault degrees. */
const DEGREE_COLUMNS = ["Year", "Degree", "Institution", "Location", "Field of study"] as const

defineProps<{ degrees: EisDegree[] }>()
</script>
