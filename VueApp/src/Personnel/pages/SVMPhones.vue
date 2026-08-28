<template>
    <h1>School of Veterinary Medicine Phone List</h1>

    <StatusBanner
        v-if="errorMessage"
        type="error"
    >
        {{ errorMessage }}
    </StatusBanner>

    <template v-if="!loading && !errorMessage">
        <span>Updated {{ formatDate(updatedDate?.toString() ?? "") || "Never" }}</span>
        <PhoneListFilter v-model="search">
            <!-- Read-only only: this page stacks every section at once, which on a phone runs to
                 a dozen screens. The maintain page is worked one section at a time. -->
            <SectionJumpLinks
                class="lt-md"
                :targets="jumpTargets"
            />
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
        :anchor-id="frequentNumbersAnchorId"
        :search="search"
        :loading="loading"
        :edit-records="false"
    ></SVMFrequentNumberTable>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from "vue"
import { getFrequentlyCalledNumbers, getSVMData } from "../composables/svm-data-fetch"
import { buildFrequentNumberColumns } from "../composables/svm-phone-columns"
import { filterRows } from "../composables/use-mobile-table-rows"
import { svmModifiedDateService } from "../services/svm-modified-date-service.ts"
import { useDateFunctions } from "@/composables/DateFunctions"
import PhoneListFilter from "../components/PhoneListFilter.vue"
import SectionJumpLinks from "../components/SectionJumpLinks.vue"
import SVMPhoneSectionTable from "../components/SVMPhoneSectionTable.vue"
import SVMFrequentNumberTable from "../components/SVMFrequentNumberTable.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import type { JumpTarget } from "../components/SectionJumpLinks.vue"
import type { Ref } from "vue"
import type { SVMFrequentNumberRecord, SVMPhoneSection } from "../types/svm-phone-types"

const sections = ref([]) as Ref<SVMPhoneSection[]>
const frequentNumbers = ref([]) as Ref<SVMFrequentNumberRecord[]>
const loading = ref(false)
const errorMessage = ref("")
const updatedDate = ref() as Ref<Date | null>
const search = ref("")

const { formatDate } = useDateFunctions()

const frequentNumbersAnchorId = "phone-section-frequent-numbers"
const frequentNumberColumns = buildFrequentNumberColumns(false)

function sectionAnchorId(sectionId: number): string {
    return `phone-section-${sectionId}`
}

/**
 * The sections a jump link would actually land on. Filtering leaves empty sections rendered, with
 * their "no records" line, so offering links to them would send the reader somewhere with nothing
 * in it - and filtering is exactly when the page is hardest to navigate. Uses the same filterRows
 * the lists themselves do, so a link is offered if and only if that section has cards.
 */
const jumpTargets = computed<JumpTarget[]>(() => {
    const targets: JumpTarget[] = sections.value
        .filter((section) => filterRows(section.cols, section.rows, search.value).length > 0)
        .map((section) => ({ id: sectionAnchorId(section.id), label: section.title }))

    if (filterRows(frequentNumberColumns, frequentNumbers.value, search.value).length > 0) {
        targets.push({ id: frequentNumbersAnchorId, label: "Frequently Called Numbers" })
    }
    return targets
})

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
