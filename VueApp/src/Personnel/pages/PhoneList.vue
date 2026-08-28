<template>
    <h1>{{ listName }}</h1>
    <div>
        <em>
            If you wish to use a nickname, please go to the campus directory (<a href="http://directory.ucdavis.edu/"
                >http://directory.ucdavis.edu/</a
            >) and update it.
        </em>
    </div>

    <StatusBanner
        v-if="errorMessage"
        type="error"
    >
        {{ errorMessage }}
    </StatusBanner>

    <template v-if="!loading && !errorMessage">
        <br />
        <div>
            <span v-if="isInternal">FOR INTERNAL USE ONLY -- </span>
            <span>Updated {{ formatDate(updatedDate?.toString() ?? "") || "Never" }}</span>
        </div>
        <div>Click on a name to send an email</div>
        <PhoneListFilter v-model="search" />
    </template>

    <PhoneListUnitTable
        v-for="unit in units"
        :key="unit.id"
        :is-maintain="false"
        :unit="unit"
        :search="search"
        :loading="loading"
    ></PhoneListUnitTable>
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import { useRoute } from "vue-router"
import { phoneListModifiedDateService } from "../services/phone-list-modified-date-service.ts"
import { useDateFunctions } from "@/composables/DateFunctions.ts"
import { phoneListService } from "../services/phone-list-service.ts"
import { getPhoneListData } from "../composables/phone-list-data-fetch"
import PhoneListFilter from "../components/PhoneListFilter.vue"
import PhoneListUnitTable from "../components/PhoneListUnitTable.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import type { Ref } from "vue"
import type { PhoneListUnit } from "../types/phone-list-phone-types"

const route = useRoute()

const units = ref([]) as Ref<PhoneListUnit[]>
const loading = ref(false)
const updatedDate = ref() as Ref<Date | null>
const isInternal = ref(false)
const listName = ref("Phone List")
const errorMessage = ref("")
const search = ref("")

const { formatDate } = useDateFunctions()

// Loads or reloads phone data for a generic phone list like VMDO.
async function loadPhoneData() {
    loading.value = true
    errorMessage.value = ""
    const code = String(route.params.code ?? "")
    // The list metadata carries the display name and this user's capabilities, so the page
    // knows what to render before any rows are fetched.
    // The backend enforces permissions, so no unintended data is returned.
    const listInfo = await phoneListService.getPhoneListInfo(code)
    if (listInfo === null) {
        errorMessage.value = "Phone list could not be found."
        loading.value = false
        return
    }
    listName.value = listInfo.name
    isInternal.value = listInfo.canViewDirectPhone

    const [newUnits, modifiedDate] = await Promise.all([
        getPhoneListData(code, false, listInfo.canViewDirectPhone),
        phoneListModifiedDateService.getModifiedDate(code),
    ])
    // For the non-maintenance view only, hide empty units.
    units.value = newUnits.filter((e) => e.rows.length > 0)
    updatedDate.value = modifiedDate
    loading.value = false
}

// Every list renders through this one route, so moving between lists reuses this component
// instead of remounting it. Watching the code covers the initial load and every later change;
// an onMounted hook would only fire for the first list and leave the rest showing its data.
watch(
    () => route.params.code,
    () => {
        units.value = []
        updatedDate.value = null
        errorMessage.value = ""
        loadPhoneData()
    },
    { immediate: true },
)
</script>
