<!-- fallow-ignore-file css-broken-reference -- q-mb-lg is a Quasar spacing class, defined by Quasar rather than in this project -->
<template>
    <p v-if="address.permanent === null && address.homePhone === null">No permanent address or home phone.</p>
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
            <tr v-if="address.permanent">
                <th
                    scope="row"
                    class="text-left"
                >
                    Permanent address
                </th>
                <td class="text-center">
                    <ReleaseFlag :value="address.permanent.releaseCampus" />
                </td>
                <td class="text-center">
                    <ReleaseFlag :value="address.permanent.releaseOrganization" />
                </td>
                <td>{{ addressText(address.permanent) }}</td>
            </tr>
            <tr v-if="address.homePhone">
                <th
                    scope="row"
                    class="text-left"
                >
                    Home phone
                </th>
                <td class="text-center">
                    <ReleaseFlag :value="address.homePhone.releaseCampus" />
                </td>
                <td class="text-center">
                    <ReleaseFlag :value="address.homePhone.releaseOrganization" />
                </td>
                <td>{{ address.homePhone.phone }}</td>
            </tr>
        </tbody>
    </q-markup-table>
</template>

<script setup lang="ts">
import ReleaseFlag from "./EisReleaseFlag.vue"
import type { EisAddress, EisPermanentAddress } from "../types/eis-types"

/** The permanent address and home phone from UCPath, with the employee's release choices. */
defineProps<{ address: EisAddress }>()

function addressText(permanent: EisPermanentAddress): string {
    const street = permanent.line2 === null ? permanent.line1 : `${permanent.line1}, ${permanent.line2}`
    return `${street}, ${permanent.city}, ${permanent.state} ${permanent.zip}`
}
</script>
