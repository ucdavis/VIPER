<!-- fallow-ignore-file css-broken-reference -- q-mb-lg is a Quasar spacing class, defined by Quasar rather than in this project -->
<template>
    <q-markup-table
        flat
        bordered
        dense
        class="q-mb-lg"
    >
        <caption class="text-left text-weight-bold q-pa-sm">
            {{
                describeAppointment(appointment)
            }}
        </caption>
        <thead>
            <tr>
                <th
                    scope="col"
                    class="text-right"
                >
                    Dist
                </th>
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
                    Percent
                </th>
                <th
                    scope="col"
                    class="text-left"
                >
                    Account
                </th>
                <th scope="col">Step</th>
                <th
                    scope="col"
                    class="text-right"
                >
                    DOS code
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
                v-for="(distribution, row) in appointment.distributions"
                :key="row"
            >
                <td class="text-right">{{ distribution.number }}</td>
                <td class="text-right">{{ formatEisDate(distribution.beginDate) }}</td>
                <td class="text-right">{{ formatEndDate(distribution.endDate) }}</td>
                <td class="text-right">{{ formatPercent(distribution.percent) }}</td>
                <td>{{ distribution.account }}</td>
                <td class="text-center">{{ distribution.step }}</td>
                <td class="text-right">{{ distribution.dosCode }}</td>
                <td class="text-right">
                    {{ formatMoney(distribution.amount) }}
                    <span
                        v-if="distribution.amount < 0"
                        class="text-weight-bold"
                        >(reduction)</span
                    >
                </td>
            </tr>
        </tbody>
        <tfoot>
            <tr>
                <th
                    scope="row"
                    colspan="7"
                    class="text-right"
                >
                    Appointment total
                </th>
                <td class="text-right text-weight-bold">{{ formatMoney(appointment.total) }}</td>
            </tr>
        </tfoot>
    </q-markup-table>
</template>

<script setup lang="ts">
import { describeAppointment, formatEisDate, formatEndDate, formatMoney, formatPercent } from "../services/eis-format"
import type { EisAppointment } from "../types/eis-types"

/** One appointment with its distributions and total. Negative amounts are reductions, such as furloughs. */
defineProps<{ appointment: EisAppointment }>()
</script>
