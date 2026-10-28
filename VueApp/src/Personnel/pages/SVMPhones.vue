<template>
    <h1>School of Veterinary Medicine Phone List</h1>

    <StatusBanner
        v-if="errorMessage"
        type="error"
    >
        {{ errorMessage }}
    </StatusBanner>

    <!-- Not gated on errorMessage: a failed read here still renders whatever arrived, so hiding
         the filter would leave a full page of sections with no way to search them. The banner
         above says what was lost. -->
    <template v-if="!loading">
        <span>Updated {{ formatDate(updatedDate?.toString() ?? "") || "Never" }}</span>
        <PhoneListFilter
            v-model="search"
            :no-matches="noMatches"
        >
            <!-- This page stacks every section at once, which runs to many screens on desktop
                 and more on a phone. -->
            <SectionJumpLinks :targets="jumpTargets" />
        </PhoneListFilter>
    </template>

    <SVMPhoneSectionTable
        v-for="section in sections"
        :key="section.id"
        :section="section"
        :anchor-id="sectionAnchorId(section.id)"
        :search="search"
        :is-modify="false"
        :loading="loading"
    ></SVMPhoneSectionTable>

    <SVMFrequentNumberTable
        :frequent-numbers="frequentNumbers"
        :anchor-id="FREQUENT_NUMBERS_ANCHOR_ID"
        :search="search"
        :loading="loading"
        :edit-records="false"
    ></SVMFrequentNumberTable>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from "vue"
import { getFrequentlyCalledNumbers, getSVMData } from "../composables/svm-data-fetch"
import {
    FREQUENT_NUMBERS_ANCHOR_ID,
    matchingJumpTargets,
    sectionAnchorId,
    svmJumpSections,
} from "../composables/section-jump-targets"
import { svmModifiedDateService } from "../services/svm-modified-date-service.ts"
import { useDateFunctions } from "@/composables/DateFunctions"
import PhoneListFilter from "../components/PhoneListFilter.vue"
import SectionJumpLinks from "../components/SectionJumpLinks.vue"
import SVMPhoneSectionTable from "../components/SVMPhoneSectionTable.vue"
import SVMFrequentNumberTable from "../components/SVMFrequentNumberTable.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import type { Ref } from "vue"
import type { SVMFrequentNumberRecord, SVMPhoneSection } from "../types/svm-phone-types"

const sections = ref([]) as Ref<SVMPhoneSection[]>
const frequentNumbers = ref([]) as Ref<SVMFrequentNumberRecord[]>
const loading = ref(false)
const errorMessage = ref("")
const updatedDate = ref() as Ref<Date | null>
const search = ref("")

const { formatDate } = useDateFunctions()

/**
 * The sections a jump link can land on. Sections with no visible rows are skipped.
 */
const jumpTargets = computed(() =>
    matchingJumpTargets(svmJumpSections(sections.value, frequentNumbers.value), search.value),
)

// Indicates if the search has emptied every section after a successful load. Failed loads are reported elsewhere.
const noMatches = computed(() => search.value !== "" && jumpTargets.value.length === 0 && errorMessage.value === "")

async function loadPhoneData() {
    loading.value = true
    errorMessage.value = ""
    // The three reads are independent, so they go out together rather than one after another.
    const [svmData, frequent, modifiedDate] = await Promise.all([
        getSVMData(false),
        getFrequentlyCalledNumbers(),
        svmModifiedDateService.getModifiedDate(),
    ])
    sections.value = svmData.newSections
    frequentNumbers.value = frequent.rows
    updatedDate.value = modifiedDate
    // One banner however many of the reads failed - see LOAD_ERROR_MESSAGE.
    errorMessage.value = svmData.error ?? frequent.error ?? ""
    loading.value = false
}

onMounted(() => {
    loadPhoneData()
})
</script>
