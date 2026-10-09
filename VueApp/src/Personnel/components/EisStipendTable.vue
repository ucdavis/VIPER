<!-- fallow-ignore-file css-broken-reference -- q-mb-lg is a Quasar spacing class, defined by Quasar rather than in this project -->
<template>
    <q-markup-table
        v-if="stipends.length > 0"
        flat
        bordered
        dense
        class="q-mb-lg"
    >
        <caption class="text-left text-weight-bold q-pa-sm">
            Stipends
        </caption>
        <thead>
            <tr>
                <th
                    scope="col"
                    class="text-right"
                >
                    Begin date
                </th>
                <th
                    scope="col"
                    class="text-right"
                >
                    End date
                </th>
                <th
                    scope="col"
                    class="text-right"
                >
                    Earnings code
                </th>
                <th
                    scope="col"
                    class="text-right"
                >
                    Annual amount
                </th>
            </tr>
        </thead>
        <tbody>
            <tr
                v-for="(stipend, row) in stipends"
                :key="row"
            >
                <td class="text-right">{{ formatEisDate(stipend.effectiveDate) }}</td>
                <td class="text-right">{{ formatEndDate(stipend.endDate) }}</td>
                <td class="text-right">{{ stipend.earningsCode }}</td>
                <td class="text-right">{{ formatMoney(stipend.annualAmount) }}</td>
            </tr>
        </tbody>
    </q-markup-table>
</template>

<script setup lang="ts">
import { formatEisDate, formatEndDate, formatMoney } from "../services/eis-format"
import type { EisStipend } from "../types/eis-types"

/** The employee's stipends, when there are any. */
defineProps<{ stipends: EisStipend[] }>()
</script>
