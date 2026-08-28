<template>
    <h1>School of Veterinary Medicine Phone List Maintenance</h1>

    <StatusBanner
        v-if="errorMessage"
        type="error"
    >
        {{ errorMessage }}
    </StatusBanner>

    <template v-if="!loading && !errorMessage">
        <!-- Not wrapped in a div: a sticky element can only travel within its own parent, so a
         wrapper holding nothing but the filter would pin it to a box its own height and it would
         scroll away immediately. Its siblings are the lists it filters. -->
        <PhoneListFilter
            v-if="!loading"
            v-model="search"
        />
    </template>

    <SVMPhoneSectionTable
        v-for="section in sections"
        :key="section.id"
        :section="section"
        :search="search"
        :is-modify="true"
        :loading="loading"
        @add-record="addRecord"
        @edit-record="editRecord"
        @delete-record="deleteRecord"
    ></SVMPhoneSectionTable>

    <SVMFrequentNumberTable
        :frequent-numbers="frequentNumbers"
        :loading="loading"
        :search="search"
        :edit-records="true"
        @add-frequent-number="addFrequentNumber"
        @edit-frequent-number="editFrequentNumber"
        @delete-frequent-number="deleteFrequentNumber"
    ></SVMFrequentNumberTable>

    <SVMAddRecordDialog
        v-model="showDialog"
        :section="sectionQSelectOption"
        :units="unitOptions"
        :unit-fax-numbers="unitFaxNumbers"
        :unit-admin-staff="unitAdminStaff"
        :edit-data="editData"
        @saved="loadPhoneData"
        @update:model-value="clearEditData"
    />

    <SVMAddFrequentNumberDialog
        v-model="showFrequentDialog"
        :edit-frequent-data="editFrequentData"
        @saved="loadPhoneData"
        @update:model-value="clearFrequentEditData"
    />
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue"
import { useQuasar } from "quasar"
import { svmUnitService } from "../services/svm-unit-service"
import SVMAddRecordDialog from "../components/SVMAddRecordDialog.vue"
import SVMAddFrequentNumberDialog from "../components/SVMAddFrequentNumberDialog.vue"
import SVMFrequentNumberTable from "../components/SVMFrequentNumberTable.vue"
import PhoneListFilter from "../components/PhoneListFilter.vue"
import SVMPhoneSectionTable from "../components/SVMPhoneSectionTable.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { svmFrequentNumberService } from "../services/svm-frequent-number-service.ts"
import { useConfirmDialog } from "@/composables/use-confirm-dialog"
import { getFrequentlyCalledNumbers, getSVMData } from "../composables/svm-data-fetch.ts"
import type { Ref } from "vue"
import type { QSelectOption } from "quasar"
import type {
    SVMPhoneDisplayRecord,
    SVMPhoneSection,
    UnitOptions,
    SVMFrequentNumberRecord,
    UnitFaxNumber,
    UnitAdminStaff,
} from "../types/svm-phone-types"

const sections = ref([]) as Ref<SVMPhoneSection[]>
const unitOptions = ref([]) as Ref<UnitOptions[]>
const unitFaxNumbers = ref([]) as Ref<UnitFaxNumber[]>
const unitAdminStaff = ref([]) as Ref<UnitAdminStaff[]>
const search = ref("")
const loading = ref(false)
const errorMessage = ref("")
const showDialog = ref(false)
const showFrequentDialog = ref(false)
const sectionQSelectOption = ref({ label: "", value: "" }) as Ref<QSelectOption>
const editData = ref() as Ref<SVMPhoneDisplayRecord | null>
const editFrequentData = ref() as Ref<SVMFrequentNumberRecord | null>
const frequentNumbers = ref([]) as Ref<SVMFrequentNumberRecord[]>
const { confirmAction } = useConfirmDialog()
const $q = useQuasar()

async function loadPhoneData() {
    loading.value = true
    errorMessage.value = ""
    // The two reads are independent, so they go out together rather than one after the other.
    const [svmData, frequent] = await Promise.all([getSVMData(true), getFrequentlyCalledNumbers()])
    const { newSections, newUnitOptions, newUnitFaxNumbers, newUnitAdminStaff } = svmData
    frequentNumbers.value = frequent.rows
    sections.value = newSections
    unitOptions.value = newUnitOptions
    unitFaxNumbers.value = newUnitFaxNumbers
    unitAdminStaff.value = newUnitAdminStaff ?? []
    // One banner however many of the reads failed - see LOAD_ERROR_MESSAGE.
    errorMessage.value = svmData.error ?? frequent.error ?? ""
    loading.value = false
}

function addRecord(sectionName: string, sectionId: number) {
    sectionQSelectOption.value = { label: sectionName, value: sectionId.toString() }
    showDialog.value = true
}

function editRecord(row: SVMPhoneDisplayRecord) {
    editData.value = row
    showDialog.value = true
}

async function deleteRecord(row: SVMPhoneDisplayRecord) {
    // Name exactly the people this delete removes. The admin staff belongs to the unit rather
    // than to this row, so they are removed only when this is the unit's last row.
    const removedPeople = []
    if (row.deanDirectorFullName) {
        removedPeople.push(row.deanDirectorFullName)
    }
    if (row.adminStaffFullName && row.isOnlyRowForUnit) {
        removedPeople.push(row.adminStaffFullName)
    }
    const removedPeopleString = removedPeople.length > 0 ? ` - ${removedPeople.join(" and ")}` : ""
    const rowLabel = `${row.unitName}${removedPeopleString}`

    await confirmAndDelete(rowLabel, "The record", () => svmUnitService.deleteRow(row.entryId))
}

/**
 * Confirms a permanent delete, runs it, reports the outcome, and reloads the page data.
 * Both maintained tables delete the same way and differ only in what is being removed, so the
 * caller supplies the label, the noun for the confirmation text, and the service call.
 */
async function confirmAndDelete(label: string, subject: string, remove: () => Promise<{ errors: string[] }>) {
    const confirmed = await confirmAction({
        title: "Delete Phone Record",
        message:
            `Permanently delete record for "${label}"? ${subject} will be removed ` +
            `immediately and this cannot be undone.`,
        okLabel: "Delete Permanently",
        okColor: "negative",
    })
    if (!confirmed) return
    const r = await remove()
    if (r.errors.length > 0) {
        $q.notify({ type: "negative", message: r.errors[0] })
    } else {
        $q.notify({ type: "positive", message: "Record deleted" })
    }
    await loadPhoneData()
}

function addFrequentNumber() {
    editFrequentData.value = null
    showFrequentDialog.value = true
}

function editFrequentNumber(row: SVMFrequentNumberRecord) {
    editFrequentData.value = row
    showFrequentDialog.value = true
}

async function deleteFrequentNumber(row: SVMFrequentNumberRecord) {
    await confirmAndDelete(row.label, "The record for this number", () =>
        svmFrequentNumberService.deleteFrequentNumber(row.entryId),
    )
}

function clearEditData() {
    editData.value = null
}

function clearFrequentEditData() {
    editFrequentData.value = null
}

onMounted(() => {
    loadPhoneData()
})
</script>
