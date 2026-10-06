<template>
    <h2>Appointments</h2>
    <div
        v-if="loading"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading appointments</span>
    </div>
    <StatusBanner
        v-else-if="failed || data === null"
        type="error"
    >
        The appointments could not be loaded.
    </StatusBanner>
    <template v-else>
        <p v-if="data.appointments.length === 0 && data.stipends.length === 0">No appointments or stipends.</p>
        <q-markup-table
            v-for="(appointment, index) in data.appointments"
            :key="index"
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
                    <td class="text-right">{{ endDateText(distribution.endDate) }}</td>
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

        <q-markup-table
            v-if="data.stipends.length > 0"
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
                    v-for="(stipend, row) in data.stipends"
                    :key="row"
                >
                    <td class="text-right">{{ formatEisDate(stipend.effectiveDate) }}</td>
                    <td class="text-right">{{ endDateText(stipend.endDate) }}</td>
                    <td class="text-right">{{ stipend.earningsCode }}</td>
                    <td class="text-right">{{ formatMoney(stipend.annualAmount) }}</td>
                </tr>
            </tbody>
        </q-markup-table>

        <p class="text-weight-bold">Total annual: {{ formatMoney(data.totalAnnual) }}</p>
    </template>
</template>

<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"
import { useEisSection } from "../composables/use-eis-section"
import { describeAppointment, formatEisDate, formatMoney, formatPercent } from "../services/eis-format"
import { eisService } from "../services/eis-service"

/**
 * The employee's appointments with their distributions, stipends and the annual total, as the
 * legacy Appointments page showed them. Negative amounts are reductions, such as furloughs.
 */
const { data, loading, failed } = useEisSection((employeeId) => eisService.getAppointments(employeeId))

/** An open-ended distribution or stipend reads "INDEF", as in UCPath. */
function endDateText(date: string | null): string {
    return date === null ? "INDEF" : formatEisDate(date)
}
</script>
