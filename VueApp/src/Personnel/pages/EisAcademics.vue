<template>
    <h2>Awards &amp; degrees</h2>
    <div
        v-if="loading"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading awards and degrees</span>
    </div>
    <StatusBanner
        v-else-if="failed || data === null"
        type="error"
    >
        The awards and degrees could not be loaded.
    </StatusBanner>
    <StatusBanner
        v-else-if="!data.hasMivAccount"
        type="warning"
    >
        This employee has no MyInfoVault account.
    </StatusBanner>
    <template v-else>
        <p class="text-caption">Source: MyInfoVault</p>

        <h3>Degrees</h3>
        <p v-if="data.degrees.length === 0">No degree information.</p>
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
                    v-for="(degree, index) in data.degrees"
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

        <h3>Boards &amp; licenses</h3>
        <EisDatedList
            :items="data.boards"
            label="Board or license"
            empty="No board or license information."
        />

        <h3>Professional memberships</h3>
        <p v-if="data.memberships.length === 0">No membership information.</p>
        <ol v-else>
            <li
                v-for="(membership, index) in data.memberships"
                :key="index"
            >
                {{ membership }}
            </li>
        </ol>

        <h3>Areas of research focus</h3>
        <p v-if="data.researchFocus.length === 0">No research focus information.</p>
        <ul v-else>
            <li
                v-for="(focus, index) in data.researchFocus"
                :key="index"
            >
                {{ focus }}
            </li>
        </ul>

        <h3>Areas of specialty focus</h3>
        <p v-if="data.specialtyFocus.length === 0">No specialty focus information.</p>
        <ul v-else>
            <li
                v-for="(focus, index) in data.specialtyFocus"
                :key="index"
            >
                {{ focus }}
            </li>
        </ul>

        <h3>Awards &amp; honors</h3>
        <EisDatedList
            :items="data.honors"
            label="Award or honor"
            empty="No award or honor information."
        />
    </template>
</template>

<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"
import EisDatedList from "../components/EisDatedList.vue"
import { useEisSection } from "../composables/use-eis-section"
import { eisService } from "../services/eis-service"

/**
 * The legacy "Education, Licenses, Memberships, Honors and Awards" page, from MyInfoVault.
 * The API returns MyInfoVault's HTML as plain text, so everything here is rendered as text.
 */
const DEGREE_COLUMNS = ["Year", "Degree", "Institution", "Location", "Field of study"] as const

const { data, loading, failed } = useEisSection((employeeId) => eisService.getAcademics(employeeId))
</script>
