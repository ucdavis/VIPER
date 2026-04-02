<script setup lang="ts">
import { ref, onMounted } from "vue"
import type { QTableProps } from "quasar"
import { emergencyContactService } from "../services/emergency-contact-service"
import ExportToolbar from "@/components/ExportToolbar.vue"
import { formatPhone } from "../utils/phone"
import type { StudentContactReport, ContactInfo } from "../types"

const loading = ref(false)
const rows = ref<StudentContactReport[]>([])
const filter = ref("")

const columns: QTableProps["columns"] = [
    { name: "classLevel", label: "Class", field: "classLevel", align: "left", sortable: true },
    { name: "fullName", label: "Name", field: "fullName", align: "left", sortable: true },
    { name: "studentInfo", label: "Student Info", field: "address", align: "left" },
    { name: "localContact", label: "Local Contact", field: (row) => row.localContact?.name, align: "left" },
    { name: "emergencyContact", label: "Emergency Contact", field: (row) => row.emergencyContact?.name, align: "left" },
    { name: "permanentContact", label: "Permanent Contact", field: (row) => row.permanentContact?.name, align: "left" },
]

function infoLine(value: string | null | undefined, abbrev?: string): string {
    if (!value) return ""
    return abbrev ? `${abbrev}: ${value}` : value
}

function formatStudentInfo(row: StudentContactReport): string[] {
    const lines: string[] = []
    if (row.address) lines.push(row.address)
    const cityZip = [row.city, row.zip].filter(Boolean).join(" ")
    if (cityZip) lines.push(cityZip)
    const home = infoLine(row.homePhone ? formatPhone(row.homePhone) : null, "H")
    if (home) lines.push(home)
    const cell = infoLine(row.cellPhone ? formatPhone(row.cellPhone) : null, "C")
    if (cell) lines.push(cell)
    return lines
}

function formatContact(contact: ContactInfo | null | undefined): string[] {
    if (!contact) return []
    const lines: string[] = []
    if (contact.name) lines.push(contact.name)
    if (contact.relationship) lines.push(contact.relationship)
    const work = infoLine(contact.workPhone ? formatPhone(contact.workPhone) : null, "W")
    if (work) lines.push(work)
    const home = infoLine(contact.homePhone ? formatPhone(contact.homePhone) : null, "H")
    if (home) lines.push(home)
    const cell = infoLine(contact.cellPhone ? formatPhone(contact.cellPhone) : null, "C")
    if (cell) lines.push(cell)
    const email = infoLine(contact.email, "E")
    if (email) lines.push(email)
    return lines
}

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
    <div class="q-pa-md">
        <q-breadcrumbs class="q-mb-sm">
            <q-breadcrumbs-el
                label="Emergency Contacts"
                :to="{ name: 'EmergencyContactList' }"
            />
            <q-breadcrumbs-el label="Report" />
        </q-breadcrumbs>

        <h1 class="q-ma-none q-mb-md">Emergency Contact Report</h1>

        <q-table
            :rows="rows"
            :columns="columns"
            row-key="rowKey"
            :loading="loading"
            :filter="filter"
            :pagination="{ rowsPerPage: 25 }"
            dense
        >
            <template #top-right>
                <ExportToolbar
                    v-model:filter="filter"
                    show-search
                    :excel-export="handleExcelExport"
                    :pdf-export="handlePdfExport"
                    :overview-route="{ name: 'EmergencyContactList' }"
                />
            </template>

            <template #body-cell-studentInfo="props">
                <q-td :props="props">
                    <div
                        v-for="(line, i) in formatStudentInfo(props.row)"
                        :key="i"
                    >
                        {{ line }}
                    </div>
                </q-td>
            </template>

            <template #body-cell-localContact="props">
                <q-td :props="props">
                    <div
                        v-for="(line, i) in formatContact(props.row.localContact)"
                        :key="i"
                    >
                        {{ line }}
                    </div>
                </q-td>
            </template>

            <template #body-cell-emergencyContact="props">
                <q-td :props="props">
                    <div
                        v-for="(line, i) in formatContact(props.row.emergencyContact)"
                        :key="i"
                    >
                        {{ line }}
                    </div>
                </q-td>
            </template>

            <template #body-cell-permanentContact="props">
                <q-td :props="props">
                    <div
                        v-for="(line, i) in formatContact(props.row.permanentContact)"
                        :key="i"
                    >
                        {{ line }}
                    </div>
                </q-td>
            </template>
        </q-table>
    </div>
</template>
