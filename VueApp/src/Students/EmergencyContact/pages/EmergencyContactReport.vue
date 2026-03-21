<script setup lang="ts">
import { ref, onMounted } from "vue"
import type { QTableProps } from "quasar"
import { emergencyContactService } from "../services/emergency-contact-service"
import ExportToolbar from "../components/ExportToolbar.vue"
import type { StudentContactReport } from "../types"

const loading = ref(false)
const rows = ref<StudentContactReport[]>([])
const filter = ref("")

const columns: QTableProps["columns"] = [
    { name: "fullName", label: "Name", field: "fullName", align: "left", sortable: true },
    { name: "classLevel", label: "Class", field: "classLevel", align: "left", sortable: true },
    { name: "address", label: "Address", field: "address", align: "left", sortable: true },
    { name: "city", label: "City", field: "city", align: "left", sortable: true },
    { name: "zip", label: "Zip", field: "zip", align: "left", sortable: true },
    { name: "homePhone", label: "Home Phone", field: "homePhone", align: "left", sortable: true },
    { name: "cellPhone", label: "Cell Phone", field: "cellPhone", align: "left", sortable: true },
    // Local contact
    { name: "localName", label: "Local Name", field: (row) => row.localContact?.name, align: "left", sortable: true },
    {
        name: "localRelationship",
        label: "Local Rel.",
        field: (row) => row.localContact?.relationship,
        align: "left",
        sortable: true,
    },
    {
        name: "localWorkPhone",
        label: "Local Work",
        field: (row) => row.localContact?.workPhone,
        align: "left",
        sortable: true,
    },
    {
        name: "localHomePhone",
        label: "Local Home",
        field: (row) => row.localContact?.homePhone,
        align: "left",
        sortable: true,
    },
    {
        name: "localCellPhone",
        label: "Local Cell",
        field: (row) => row.localContact?.cellPhone,
        align: "left",
        sortable: true,
    },
    {
        name: "localEmail",
        label: "Local Email",
        field: (row) => row.localContact?.email,
        align: "left",
        sortable: true,
    },
    // Emergency contact
    {
        name: "emergencyName",
        label: "Emerg. Name",
        field: (row) => row.emergencyContact?.name,
        align: "left",
        sortable: true,
    },
    {
        name: "emergencyRelationship",
        label: "Emerg. Rel.",
        field: (row) => row.emergencyContact?.relationship,
        align: "left",
        sortable: true,
    },
    {
        name: "emergencyWorkPhone",
        label: "Emerg. Work",
        field: (row) => row.emergencyContact?.workPhone,
        align: "left",
        sortable: true,
    },
    {
        name: "emergencyHomePhone",
        label: "Emerg. Home",
        field: (row) => row.emergencyContact?.homePhone,
        align: "left",
        sortable: true,
    },
    {
        name: "emergencyCellPhone",
        label: "Emerg. Cell",
        field: (row) => row.emergencyContact?.cellPhone,
        align: "left",
        sortable: true,
    },
    {
        name: "emergencyEmail",
        label: "Emerg. Email",
        field: (row) => row.emergencyContact?.email,
        align: "left",
        sortable: true,
    },
    // Permanent contact
    {
        name: "permanentName",
        label: "Perm. Name",
        field: (row) => row.permanentContact?.name,
        align: "left",
        sortable: true,
    },
    {
        name: "permanentRelationship",
        label: "Perm. Rel.",
        field: (row) => row.permanentContact?.relationship,
        align: "left",
        sortable: true,
    },
    {
        name: "permanentWorkPhone",
        label: "Perm. Work",
        field: (row) => row.permanentContact?.workPhone,
        align: "left",
        sortable: true,
    },
    {
        name: "permanentHomePhone",
        label: "Perm. Home",
        field: (row) => row.permanentContact?.homePhone,
        align: "left",
        sortable: true,
    },
    {
        name: "permanentCellPhone",
        label: "Perm. Cell",
        field: (row) => row.permanentContact?.cellPhone,
        align: "left",
        sortable: true,
    },
    {
        name: "permanentEmail",
        label: "Perm. Email",
        field: (row) => row.permanentContact?.email,
        align: "left",
        sortable: true,
    },
    {
        name: "contactPermanent",
        label: "Contact Perm?",
        field: "contactPermanent",
        align: "center",
        sortable: true,
        format: (val: boolean) => (val ? "Yes" : "No"),
    },
]

async function handleExcelExport(): Promise<void> {
    await emergencyContactService.downloadExcel()
}

async function handlePdfExport(): Promise<void> {
    await emergencyContactService.openPdf()
}

async function load(): Promise<void> {
    loading.value = true
    rows.value = await emergencyContactService.getReport()
    loading.value = false
}

onMounted(load)
</script>

<template>
    <div>
        <div class="row items-center q-mb-md">
            <q-btn
                flat
                dense
                no-caps
                icon="arrow_back"
                label="Back to List"
                :to="{ name: 'EmergencyContactList' }"
            />
        </div>

        <div class="row items-center q-mb-md">
            <h2 class="q-ma-none">Emergency Contact Report</h2>
        </div>

        <q-table
            :rows="rows"
            :columns="columns"
            row-key="personId"
            :loading="loading"
            :filter="filter"
            :pagination="{ rowsPerPage: 25 }"
            dense
        >
            <template #top-right>
                <ExportToolbar
                    v-model:filter="filter"
                    :columns="columns"
                    :rows="rows"
                    :excel-export="handleExcelExport"
                    :pdf-export="handlePdfExport"
                />
            </template>
        </q-table>
    </div>
</template>
