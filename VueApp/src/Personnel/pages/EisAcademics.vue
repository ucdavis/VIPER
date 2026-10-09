<template>
    <h2>Awards &amp; degrees</h2>
    <EisSectionState
        v-slot="{ data: academics }"
        :loading="loading"
        :data="data"
        what="awards and degrees"
    >
        <StatusBanner
            v-if="!academics.hasMivAccount"
            type="warning"
        >
            This employee has no MyInfoVault account.
        </StatusBanner>
        <template v-else>
            <p class="text-caption">Source: MyInfoVault</p>

            <h3>Degrees</h3>
            <EisDegreeTable :degrees="academics.degrees" />

            <h3>Boards &amp; licenses</h3>
            <EisDatedList
                :items="academics.boards"
                label="Board or license"
                empty="No board or license information."
            />

            <h3>Professional memberships</h3>
            <EisTextList
                :items="academics.memberships"
                empty="No membership information."
                ordered
            />

            <h3>Areas of research focus</h3>
            <EisTextList
                :items="academics.researchFocus"
                empty="No research focus information."
            />

            <h3>Areas of specialty focus</h3>
            <EisTextList
                :items="academics.specialtyFocus"
                empty="No specialty focus information."
            />

            <h3>Awards &amp; honors</h3>
            <EisDatedList
                :items="academics.honors"
                label="Award or honor"
                empty="No award or honor information."
            />
        </template>
    </EisSectionState>
</template>

<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"
import EisDatedList from "../components/EisDatedList.vue"
import EisDegreeTable from "../components/EisDegreeTable.vue"
import EisSectionState from "../components/EisSectionState.vue"
import EisTextList from "../components/EisTextList.vue"
import { useEisSection } from "../composables/use-eis-section"
import { eisService } from "../services/eis-service"

/**
 * The legacy "Education, Licenses, Memberships, Honors and Awards" page, from MyInfoVault.
 * The API returns MyInfoVault's HTML as plain text, so everything here is rendered as text.
 */
const { data, loading } = useEisSection((employeeId) => eisService.getAcademics(employeeId))
</script>
