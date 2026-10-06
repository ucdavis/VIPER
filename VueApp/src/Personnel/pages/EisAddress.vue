<template>
    <h2>Address</h2>
    <div
        v-if="loading"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading address</span>
    </div>
    <StatusBanner
        v-else-if="failed || data === null"
        type="error"
    >
        The address could not be loaded.
    </StatusBanner>
    <template v-else>
        <h3>Permanent address (source: UCPath)</h3>
        <p v-if="data.permanent === null && data.homePhone === null">No permanent address or home phone.</p>
        <q-markup-table
            v-else
            flat
            bordered
            dense
            class="q-mb-lg"
        >
            <thead>
                <tr>
                    <td></td>
                    <th scope="col">Release to campus</th>
                    <th scope="col">Release to organizations</th>
                    <th
                        scope="col"
                        class="text-left"
                    >
                        Value
                    </th>
                </tr>
            </thead>
            <tbody>
                <tr v-if="data.permanent">
                    <th
                        scope="row"
                        class="text-left"
                    >
                        Permanent address
                    </th>
                    <td class="text-center">
                        <ReleaseFlag :value="data.permanent.releaseCampus" />
                    </td>
                    <td class="text-center">
                        <ReleaseFlag :value="data.permanent.releaseOrganization" />
                    </td>
                    <td>{{ addressText(data.permanent) }}</td>
                </tr>
                <tr v-if="data.homePhone">
                    <th
                        scope="row"
                        class="text-left"
                    >
                        Home phone
                    </th>
                    <td class="text-center">
                        <ReleaseFlag :value="data.homePhone.releaseCampus" />
                    </td>
                    <td class="text-center">
                        <ReleaseFlag :value="data.homePhone.releaseOrganization" />
                    </td>
                    <td>{{ data.homePhone.phone }}</td>
                </tr>
            </tbody>
        </q-markup-table>

        <h3>Campus address (source: campus directory)</h3>
        <StatusBanner
            v-if="!data.campusDirectoryAvailable"
            type="warning"
        >
            The campus directory could not be reached. Try again later.
        </StatusBanner>
        <p v-else-if="data.campusListings.length === 0">No campus directory listing.</p>
        <div
            v-for="(listing, index) in data.campusListings"
            :key="index"
            class="q-mb-md"
        >
            <h4 class="q-mb-xs">
                {{ listing.isPrimary ? "Primary listing" : `Additional listing ${index}` }}
                <StatusBadge
                    v-if="!listing.isPublic"
                    color="warning"
                    label="Not public"
                    class="q-ml-sm"
                />
            </h4>
            <EisFactList :facts="listingFacts(listing)" />
        </div>
    </template>
</template>

<script setup lang="ts">
import StatusBadge from "@/components/StatusBadge.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import EisFactList from "../components/EisFactList.vue"
import ReleaseFlag from "../components/EisReleaseFlag.vue"
import { useEisSection } from "../composables/use-eis-section"
import { eisService } from "../services/eis-service"
import type { EisCampusListing, EisPermanentAddress } from "../types/eis-types"

/**
 * The permanent address and home phone from UCPath, with the employee's release choices, and
 * their campus directory listings. Additional listings the directory doesn't publish are marked.
 */
const { data, loading, failed } = useEisSection((employeeId) => eisService.getAddress(employeeId))

function addressText(address: EisPermanentAddress): string {
    const street = address.line2 === null ? address.line1 : `${address.line1}, ${address.line2}`
    return `${street}, ${address.city}, ${address.state} ${address.zip}`
}

function listingFacts(listing: EisCampusListing) {
    return [
        { label: "Title", value: listing.title ?? "" },
        { label: "Department", value: listing.department ?? "" },
        { label: "Address", value: listing.address ?? "" },
        { label: "Phone", value: listing.phone ?? "" },
    ]
}
</script>
