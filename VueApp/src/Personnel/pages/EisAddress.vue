<template>
    <h2>Address</h2>
    <EisSectionState
        v-slot="{ data: address }"
        :loading="loading"
        :data="data"
        what="address"
    >
        <h3>Permanent address (source: UCPath)</h3>
        <EisPermanentAddress :address="address" />

        <h3>Campus address (source: campus directory)</h3>
        <EisCampusListings
            :listings="address.campusListings"
            :available="address.campusDirectoryAvailable"
        />
    </EisSectionState>
</template>

<script setup lang="ts">
import EisCampusListings from "../components/EisCampusListings.vue"
import EisPermanentAddress from "../components/EisPermanentAddress.vue"
import EisSectionState from "../components/EisSectionState.vue"
import { useEisSection } from "../composables/use-eis-section"
import { eisService } from "../services/eis-service"

/**
 * The permanent address and home phone from UCPath, with the employee's release choices, and
 * their campus directory listings.
 */
const { data, loading } = useEisSection((employeeId) => eisService.getAddress(employeeId))
</script>
