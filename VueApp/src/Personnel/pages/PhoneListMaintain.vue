<template>
    <h1>{{ listName }} Maintenance</h1>
    <div>
        <em>
            If an employee wishes to display a nickname, please direct them to the campus directory and change it there.
        </em>
    </div>

    <!-- Error Message -->
    <StatusBanner
        v-if="errorMessage"
        type="error"
    >
        {{ errorMessage }}
    </StatusBanner>

    <q-input
        v-model="search"
        class="q-ml-xs q-mr-xs"
        dense
        outlined
        debounce="300"
        placeholder="Filter Results"
    >
        <template #append>
            <q-icon name="filter_alt" />
        </template>
    </q-input>

    <PhoneListUnitTable
        v-for="unit in units"
        :key="unit.id"
        :is-maintain="true"
        :unit="unit"
        :search="search"
        :loading="loading"
        @add-record="addRecord"
        @edit-record="editRecord"
        @delete-record="deleteRecord"
    ></PhoneListUnitTable>

    <PhoneListAddRecordDialog
        v-model="showDialog"
        :unit="selectedUnit"
        :edit-data="editData"
        :list-code="listCode"
        @saved="loadPhoneData"
        @update:model-value="clearEditData"
    />
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import { useQuasar } from "quasar"
import { useRoute, useRouter } from "vue-router"
import { phoneListUnitService } from "../services/phone-list-unit-service.ts"
import { getPhoneListData } from "../composables/phone-list-data-fetch.ts"
import { useConfirmDialog } from "@/composables/use-confirm-dialog"
import { phoneListService } from "../services/phone-list-service.ts"
import StatusBanner from "@/components/StatusBanner.vue"
import PhoneListAddRecordDialog from "../components/PhoneListAddRecordDialog.vue"
import PhoneListUnitTable from "../components/PhoneListUnitTable.vue"
import type { Ref } from "vue"
import type { PhoneListDisplayRecord, PhoneListUnit } from "../types/phone-list-phone-types.ts"

const route = useRoute()
const router = useRouter()

const units = ref([]) as Ref<PhoneListUnit[]>
const loading = ref(false)
const errorMessage = ref("")
const showDialog = ref(false)
const selectedUnit = ref({ name: "", id: -1 })
const listCode = ref("")
const listName = ref("Phone List")
const editData = ref() as Ref<PhoneListDisplayRecord | null>
const search = ref("")
const { confirmAction } = useConfirmDialog()
const $q = useQuasar()

// Loads or reloads phone data for a generic phone list like VMDO.
async function loadPhoneData() {
    loading.value = true
    errorMessage.value = ""
    listCode.value = String(route.params.code ?? "")
    const listInfo = await phoneListService.getPhoneListInfo(listCode.value)
    if (listInfo === null) {
        errorMessage.value = "Phone list could not be found."
        loading.value = false
        return
    }
    // The maintain role lives on the list row, so it cannot be checked by a static route guard.
    // The API enforces this too; redirecting here keeps a non-maintainer out of an editor whose
    // every save would be rejected.
    if (!listInfo.canMaintain) {
        loading.value = false
        router.replace({ name: "PersonnelHome" })
        return
    }
    listName.value = listInfo.name

    units.value = await getPhoneListData(listCode.value, true, listInfo.canViewDirectPhone)
    loading.value = false
}

function addRecord(unit: PhoneListUnit) {
    selectedUnit.value = { name: unit.name, id: unit.id }
    showDialog.value = true
}

function editRecord(row: PhoneListDisplayRecord) {
    editData.value = row
    showDialog.value = true
}

async function deleteRecord(row: PhoneListDisplayRecord) {
    const confirmed = await confirmAction({
        title: "Delete Phone Record",
        message:
            `Permanently delete record for "${row.fullName}"? The record will be removed ` +
            `immediately and this cannot be undone.`,
        okLabel: "Delete Permanently",
        okColor: "negative",
    })
    if (!confirmed) return
    let isError: boolean = false
    let r = await phoneListUnitService.deleteUnitPersonData(listCode.value, row.unitPersonId)
    if (r.errors.length > 0) {
        $q.notify({ type: "negative", message: r.errors[0] })
        isError = true
    }
    if (!isError) {
        $q.notify({ type: "positive", message: "Record deleted" })
    }
    await loadPhoneData()
}

function clearEditData() {
    editData.value = null
    selectedUnit.value = { name: "", id: -1 }
}

// Every list renders through this one route, so moving between lists reuses this component
// instead of remounting it. Watching the code covers the initial load and every later change;
// an onMounted hook would only fire for the first list, leaving listCode pointed at it and
// sending edits to the wrong list.
watch(
    () => route.params.code,
    () => {
        units.value = []
        errorMessage.value = ""
        showDialog.value = false
        clearEditData()
        loadPhoneData()
    },
    { immediate: true },
)
</script>
