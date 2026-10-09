<template>
    <h2>Appointment history</h2>
    <p>Data available from as far back as the PPS data warehouse contains.</p>
    <EisSectionState
        v-slot="{ data: history }"
        :loading="loading"
        :data="data"
        what="appointment history"
    >
        <EisHistoryTable
            caption="Appointment history"
            empty-text="No appointment history available."
            :entries="history.appointments"
        />
        <EisHistoryTable
            caption="Appointment PPS history"
            empty-text="No PPS history available."
            :entries="history.ppsAppointments"
        />
        <EisLeaveTable
            caption="Leave of absence history"
            empty-text="No leave data available."
            :leaves="history.leaves"
        />
        <EisLeaveTable
            caption="Leave of absence PPS history"
            empty-text="No PPS leave data available."
            :leaves="history.ppsLeaves"
        />
    </EisSectionState>
</template>

<script setup lang="ts">
import EisHistoryTable from "../components/EisHistoryTable.vue"
import EisLeaveTable from "../components/EisLeaveTable.vue"
import EisSectionState from "../components/EisSectionState.vue"
import { useEisSection } from "../composables/use-eis-section"
import { eisService } from "../services/eis-service"

/** Appointment and leave history from UCPath and, for older records, the retired PPS system. */
const { data, loading } = useEisSection((employeeId) => eisService.getHistory(employeeId))
</script>
