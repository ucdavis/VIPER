<template>
    <h2>Appointments</h2>
    <EisSectionState
        v-slot="{ data: appointments }"
        :loading="loading"
        :data="data"
        what="appointments"
    >
        <p v-if="appointments.appointments.length === 0 && appointments.stipends.length === 0">
            No appointments or stipends.
        </p>
        <EisAppointmentTable
            v-for="(appointment, index) in appointments.appointments"
            :key="index"
            :appointment="appointment"
        />
        <EisStipendTable :stipends="appointments.stipends" />

        <p class="text-weight-bold">Total annual: {{ formatMoney(appointments.totalAnnual) }}</p>
    </EisSectionState>
</template>

<script setup lang="ts">
import EisAppointmentTable from "../components/EisAppointmentTable.vue"
import EisSectionState from "../components/EisSectionState.vue"
import EisStipendTable from "../components/EisStipendTable.vue"
import { useEisSection } from "../composables/use-eis-section"
import { formatMoney } from "../services/eis-format"
import { eisService } from "../services/eis-service"

/**
 * The employee's appointments with their distributions, stipends and the annual total, as the
 * legacy Appointments page showed them.
 */
const { data, loading } = useEisSection((employeeId) => eisService.getAppointments(employeeId))
</script>
