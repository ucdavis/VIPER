<script setup lang="ts">
import { ref, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useQuasar } from "quasar"
import type { QTableProps } from "quasar"
import CompletenessIcon from "../components/CompletenessIcon.vue"
import ExportToolbar from "../components/ExportToolbar.vue"
import { emergencyContactService } from "../services/emergency-contact-service"
import type { StudentContactListItem } from "../types"

const router = useRouter()
const $q = useQuasar()
const loading = ref(false)
const rows = ref<StudentContactListItem[]>([])
const filter = ref("")

const columns: QTableProps["columns"] = [
    { name: "fullName", label: "Name", field: "fullName", align: "left", sortable: true },
    { name: "classLevel", label: "Class Level", field: "classLevel", align: "left", sortable: true },
    {
        name: "studentInfo",
        label: "Student Info",
        field: "studentInfoComplete",
        align: "center",
        sortable: true,
        sort: (a: number, b: number, rowA: StudentContactListItem, rowB: StudentContactListItem) =>
            a / rowA.studentInfoTotal - b / rowB.studentInfoTotal,
    },
    {
        name: "localContact",
        label: "Local Contact",
        field: "localContactComplete",
        align: "center",
        sortable: true,
        sort: (a: number, b: number, rowA: StudentContactListItem, rowB: StudentContactListItem) =>
            a / rowA.localContactTotal - b / rowB.localContactTotal,
    },
    {
        name: "emergencyContact",
        label: "Emergency Contact",
        field: "emergencyContactComplete",
        align: "center",
        sortable: true,
        sort: (a: number, b: number, rowA: StudentContactListItem, rowB: StudentContactListItem) =>
            a / rowA.emergencyContactTotal - b / rowB.emergencyContactTotal,
    },
    {
        name: "permanentContact",
        label: "Permanent Contact",
        field: "permanentContactComplete",
        align: "center",
        sortable: true,
        sort: (a: number, b: number, rowA: StudentContactListItem, rowB: StudentContactListItem) =>
            a / rowA.permanentContactTotal - b / rowB.permanentContactTotal,
    },
    {
        name: "lastUpdated",
        label: "Last Updated",
        field: "lastUpdated",
        align: "left",
        sortable: true,
        format: (val: string | null) => (val ? new Date(val).toLocaleDateString() : ""),
    },
]

function navigateToStudent(row: StudentContactListItem): void {
    router.push({ name: "EmergencyContactEdit", params: { pidm: row.personId } })
}

async function handleExcelExport(): Promise<void> {
    await emergencyContactService.downloadExcel()
}

async function handlePdfExport(): Promise<void> {
    await emergencyContactService.openPdf()
}

async function load(): Promise<void> {
    loading.value = true
    rows.value = await emergencyContactService.getList()
    loading.value = false
}

onMounted(load)
</script>

<template>
    <div>
        <div class="row items-center q-mb-md">
            <h2 class="q-ma-none">Emergency Contact Information</h2>
            <q-space />
            <q-btn
                flat
                dense
                no-caps
                icon="assessment"
                label="Report"
                :to="{ name: 'EmergencyContactReport' }"
            />
        </div>

        <q-table
            :rows="rows"
            :columns="columns"
            row-key="personId"
            :loading="loading"
            :filter="filter"
            :pagination="{ rowsPerPage: 25 }"
            dense
            :grid="$q.screen.lt.md"
            @row-click="(_evt, row) => navigateToStudent(row as StudentContactListItem)"
            class="emergency-contact-table"
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

            <template #body-cell-fullName="props">
                <q-td :props="props">
                    <a
                        class="cursor-pointer text-primary"
                        tabindex="0"
                        role="link"
                        :aria-label="`Edit ${props.row.fullName}`"
                        @keyup.enter="navigateToStudent(props.row as StudentContactListItem)"
                        @keyup.space.prevent="navigateToStudent(props.row as StudentContactListItem)"
                    >
                        {{ props.row.fullName }}
                        <q-icon
                            name="edit"
                            size="0.875rem"
                            class="q-ml-xs"
                        />
                    </a>
                </q-td>
            </template>

            <template #body-cell-studentInfo="props">
                <q-td :props="props">
                    <CompletenessIcon
                        :complete="props.row.studentInfoComplete"
                        :total="props.row.studentInfoTotal"
                    />
                </q-td>
            </template>

            <template #body-cell-localContact="props">
                <q-td :props="props">
                    <CompletenessIcon
                        :complete="props.row.localContactComplete"
                        :total="props.row.localContactTotal"
                    />
                </q-td>
            </template>

            <template #body-cell-emergencyContact="props">
                <q-td :props="props">
                    <CompletenessIcon
                        :complete="props.row.emergencyContactComplete"
                        :total="props.row.emergencyContactTotal"
                    />
                </q-td>
            </template>

            <template #body-cell-permanentContact="props">
                <q-td :props="props">
                    <CompletenessIcon
                        :complete="props.row.permanentContactComplete"
                        :total="props.row.permanentContactTotal"
                    />
                </q-td>
            </template>

            <template #item="props">
                <div class="q-pa-xs col-xs-12 col-sm-6 col-md-4">
                    <q-card
                        flat
                        bordered
                        class="cursor-pointer"
                        @click="navigateToStudent(props.row as StudentContactListItem)"
                    >
                        <q-card-section>
                            <div class="row items-center q-mb-xs">
                                <a
                                    class="text-primary text-weight-medium"
                                    tabindex="0"
                                    role="link"
                                    :aria-label="`Edit ${props.row.fullName}`"
                                    @keyup.enter="navigateToStudent(props.row as StudentContactListItem)"
                                    @keyup.space.prevent="navigateToStudent(props.row as StudentContactListItem)"
                                >
                                    {{ props.row.fullName }}
                                    <q-icon
                                        name="edit"
                                        size="0.875rem"
                                        class="q-ml-xs"
                                    />
                                </a>
                                <q-space />
                                <span class="text-caption text-grey">{{ props.row.classLevel }}</span>
                            </div>
                            <div class="row items-center q-gutter-md q-mb-xs">
                                <span class="row items-center q-gutter-xs">
                                    <CompletenessIcon
                                        :complete="props.row.studentInfoComplete"
                                        :total="props.row.studentInfoTotal"
                                    />
                                    <span class="text-caption">Info</span>
                                </span>
                                <span class="row items-center q-gutter-xs">
                                    <CompletenessIcon
                                        :complete="props.row.localContactComplete"
                                        :total="props.row.localContactTotal"
                                    />
                                    <span class="text-caption">Local</span>
                                </span>
                                <span class="row items-center q-gutter-xs">
                                    <CompletenessIcon
                                        :complete="props.row.emergencyContactComplete"
                                        :total="props.row.emergencyContactTotal"
                                    />
                                    <span class="text-caption">Emerg</span>
                                </span>
                                <span class="row items-center q-gutter-xs">
                                    <CompletenessIcon
                                        :complete="props.row.permanentContactComplete"
                                        :total="props.row.permanentContactTotal"
                                    />
                                    <span class="text-caption">Perm</span>
                                </span>
                            </div>
                            <div
                                v-if="props.row.lastUpdated"
                                class="text-caption text-grey"
                            >
                                Updated {{ new Date(props.row.lastUpdated).toLocaleDateString() }}
                            </div>
                        </q-card-section>
                    </q-card>
                </div>
            </template>
        </q-table>
    </div>
</template>

<style scoped>
.emergency-contact-table :deep(tbody tr) {
    cursor: pointer;
}
</style>
