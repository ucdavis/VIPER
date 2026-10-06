<template>
    <h2>Appointment history</h2>
    <p>Data available from as far back as the PPS data warehouse contains.</p>
    <div
        v-if="loading"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading appointment history</span>
    </div>
    <StatusBanner
        v-else-if="failed || data === null"
        type="error"
    >
        The appointment history could not be loaded.
    </StatusBanner>
    <template v-else>
        <EisHistoryTable
            caption="Appointment history"
            empty-text="No appointment history available."
            :entries="data.appointments"
        />
        <EisHistoryTable
            caption="Appointment PPS history"
            empty-text="No PPS history available."
            :entries="data.ppsAppointments"
        />
        <EisLeaveTable
            caption="Leave of absence history"
            empty-text="No leave data available."
            :leaves="data.leaves"
        />
        <EisLeaveTable
            caption="Leave of absence PPS history"
            empty-text="No PPS leave data available."
            :leaves="data.ppsLeaves"
        />
    </template>
</template>

<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"
import EisHistoryTable from "../components/EisHistoryTable.vue"
import EisLeaveTable from "../components/EisLeaveTable.vue"
import { useEisSection } from "../composables/use-eis-section"
import { eisService } from "../services/eis-service"

/** Appointment and leave history from UCPath and, for older records, the retired PPS system. */
const { data, loading, failed } = useEisSection((employeeId) => eisService.getHistory(employeeId))
</script>
