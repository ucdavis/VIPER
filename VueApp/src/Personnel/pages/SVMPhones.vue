<template>
    <h1>School of Veterinary Medicine Phone List</h1>

    <template v-if="!loading">
        <span>Updated {{ formatDate(updatedDate?.toString() ?? "") || "Never" }}</span>
        <q-input
            class="q-ml-xs q-mr-xs"
            v-model="search"
            dense
            outlined
            debounce="300"
            placeholder="Filter Results"
        >
            <template #append>
                <q-icon name="filter_alt" />
            </template>
        </q-input>
    </template>

    <SVMPhoneSectionTable
        v-for="section in sections"
        :key="section.id"
        :section="section"
        :search="search"
        :is-modify="false"
        :loading="loading"
    ></SVMPhoneSectionTable>

    <SVMFrequentNumberTable
        :frequent-numbers="frequentNumbers"
        :search="search"
        :loading="loading"
        :edit-records="false"
    ></SVMFrequentNumberTable>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue"
import { getFrequentlyCalledNumbers, getSVMData } from "../composables/svm-data-fetch"
import { svmModifiedDateService } from "../services/svm-modified-date-service.ts"
import { useDateFunctions } from "@/composables/DateFunctions"
import SVMPhoneSectionTable from "../components/SVMPhoneSectionTable.vue"
import SVMFrequentNumberTable from "../components/SVMFrequentNumberTable.vue"
import type { Ref } from "vue"
import type { SVMFrequentNumberRecord, SVMPhoneSection } from "../types/svm-phone-types"

const sections = ref([]) as Ref<SVMPhoneSection[]>
const frequentNumbers = ref([]) as Ref<SVMFrequentNumberRecord[]>
const loading = ref(false)
const updatedDate = ref() as Ref<Date | null>
const search = ref("")

const { formatDate } = useDateFunctions()

async function loadPhoneData() {
    loading.value = true
    const { newSections } = await getSVMData(false)
    frequentNumbers.value = await getFrequentlyCalledNumbers()
    updatedDate.value = await svmModifiedDateService.getModifiedDate()
    sections.value = newSections
    loading.value = false
}

onMounted(() => {
    loadPhoneData()
})
</script>
