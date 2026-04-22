<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useQuasar } from "quasar"
import type { QTableProps } from "quasar"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import CompletenessIcon from "../components/CompletenessIcon.vue"
import ExportToolbar from "@/components/ExportToolbar.vue"
import AppAccessControls from "../components/AppAccessControls.vue"
import { emergencyContactService } from "../services/emergency-contact-service"
import { formatPhone } from "../utils/phone"
import type { StudentContactListItem } from "../types"

const router = useRouter()
const $q = useQuasar()
const loading = ref(false)
const rows = ref<StudentContactListItem[]>([])
const filter = ref("")
const grantedPersonIds = ref(new Set<number>())

const isAdmin = computed(() => checkHasOnePermission(["SVMSecure.Students.EmergencyContactAdmin"]))

const columns = computed<NonNullable<QTableProps["columns"]>>(() => [
    ...(isAdmin.value
        ? [{ name: "access", label: "Access", field: () => null, align: "center" as const, style: "width: 4rem" }]
        : []),
    { name: "classLevel", label: "Class", field: "classLevel", align: "center" as const, sortable: true },
    { name: "fullName", label: "Name", field: "fullName", align: "left" as const, sortable: true },
    { name: "email", label: "Email", field: "email", align: "left" as const, sortable: true },
    {
        name: "cellPhone",
        label: "Phone",
        field: "cellPhone",
        align: "left" as const,
        sortable: true,
        format: (val: string | null) => (val ? formatPhone(val) : ""),
    },
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
])

const detailRoute = computed(() => (isAdmin.value ? "EmergencyContactEdit" : "EmergencyContactView"))

function navigateToStudent(row: StudentContactListItem): void {
    if (row.hasDetailRoute) {
        router.push({ name: detailRoute.value, params: { pidm: row.personId } })
    }
}

async function handleExcelExport(): Promise<void> {
    await emergencyContactService.downloadOverviewExcel()
}

async function handlePdfExport(): Promise<void> {
    await emergencyContactService.openOverviewPdf()
}

async function loadGrantedIds(): Promise<void> {
    const status = await emergencyContactService.getAccessStatus()
    if (status) {
        grantedPersonIds.value = new Set(status.individualGrants.map((g) => g.personId))
    }
}

async function load(): Promise<void> {
    loading.value = true
    const promises: Promise<void>[] = [
        emergencyContactService.getList().then((r) => {
            rows.value = r
        }),
    ]
    if (isAdmin.value) {
        promises.push(loadGrantedIds())
    }
    await Promise.all(promises)
    loading.value = false
}

onMounted(load)
</script>

<template>
    <div class="q-pa-md">
        <q-breadcrumbs class="q-mb-sm">
            <q-breadcrumbs-el label="Emergency Contacts" />
            <q-breadcrumbs-el label="Overview" />
        </q-breadcrumbs>

        <h1 class="q-ma-none q-mb-md">Emergency Contact Overview</h1>

        <AppAccessControls
            v-if="isAdmin"
            @access-status-changed="loadGrantedIds"
        />

        <q-table
            :rows="rows"
            :columns="columns"
            row-key="rowKey"
            :loading="loading"
            :filter="filter"
            :pagination="{ rowsPerPage: 50 }"
            dense
            :grid="$q.screen.xs"
            @row-click="(_evt, row) => navigateToStudent(row as StudentContactListItem)"
            class="emergency-contact-table"
        >
            <template #top-right>
                <ExportToolbar
                    v-model:filter="filter"
                    show-search
                    :excel-export="handleExcelExport"
                    :pdf-export="handlePdfExport"
                    :report-route="{ name: 'EmergencyContactReport' }"
                />
            </template>

            <template #body-cell-access="props">
                <q-td :props="props">
                    <q-icon
                        v-if="grantedPersonIds.has(props.row.personId)"
                        name="person_add"
                        color="primary"
                        size="1.25rem"
                        role="img"
                        aria-label="Individual access granted"
                    >
                        <q-tooltip>Individual access granted</q-tooltip>
                    </q-icon>
                </q-td>
            </template>

            <template #body-cell-fullName="props">
                <q-td :props="props">
                    <router-link
                        v-if="props.row.hasDetailRoute"
                        :to="{ name: detailRoute, params: { pidm: props.row.personId } }"
                        class="text-primary"
                        :aria-label="`${isAdmin ? 'Edit' : 'View'} ${props.row.fullName}`"
                    >
                        {{ props.row.fullName }}
                        <q-icon
                            :name="isAdmin ? 'edit' : 'visibility'"
                            size="0.875rem"
                            class="q-ml-xs"
                            aria-hidden="true"
                        />
                    </router-link>
                    <span
                        v-else
                        class="text-grey-7"
                    >
                        {{ props.row.fullName }}
                        <q-icon
                            name="warning"
                            size="0.875rem"
                            color="orange"
                            class="q-ml-xs"
                        >
                            <q-tooltip>No AAUD mapping, record cannot be opened</q-tooltip>
                        </q-icon>
                    </span>
                </q-td>
            </template>

            <template #body-cell-email="props">
                <q-td :props="props">
                    <a
                        v-if="props.row.email"
                        :href="`mailto:${props.row.email}`"
                        class="text-primary"
                    >
                        {{ props.row.email }}
                    </a>
                </q-td>
            </template>

            <template #body-cell-studentInfo="props">
                <q-td :props="props">
                    <CompletenessIcon
                        :complete="props.row.studentInfoComplete"
                        :total="props.row.studentInfoTotal"
                        :missing="props.row.studentInfoMissing"
                        label="Student Info"
                    />
                </q-td>
            </template>

            <template #body-cell-localContact="props">
                <q-td :props="props">
                    <CompletenessIcon
                        :complete="props.row.localContactComplete"
                        :total="props.row.localContactTotal"
                        :missing="props.row.localContactMissing"
                        label="Local Contact"
                    />
                </q-td>
            </template>

            <template #body-cell-emergencyContact="props">
                <q-td :props="props">
                    <CompletenessIcon
                        :complete="props.row.emergencyContactComplete"
                        :total="props.row.emergencyContactTotal"
                        :missing="props.row.emergencyContactMissing"
                        label="Emergency Contact"
                    />
                </q-td>
            </template>

            <template #body-cell-permanentContact="props">
                <q-td :props="props">
                    <CompletenessIcon
                        :complete="props.row.permanentContactComplete"
                        :total="props.row.permanentContactTotal"
                        :missing="props.row.permanentContactMissing"
                        label="Permanent Contact"
                    />
                </q-td>
            </template>

            <template #item="props">
                <div class="q-pa-xs col-12">
                    <q-card
                        flat
                        bordered
                        :class="props.row.hasDetailRoute ? 'cursor-pointer' : ''"
                        @click="navigateToStudent(props.row as StudentContactListItem)"
                    >
                        <q-card-section>
                            <div class="row items-center q-mb-xs">
                                <q-icon
                                    v-if="isAdmin && grantedPersonIds.has(props.row.personId)"
                                    name="person_add"
                                    color="primary"
                                    size="1.25rem"
                                    class="q-mr-xs"
                                    role="img"
                                    aria-label="Individual access granted"
                                >
                                    <q-tooltip>Individual access granted</q-tooltip>
                                </q-icon>
                                <router-link
                                    v-if="props.row.hasDetailRoute"
                                    :to="{ name: detailRoute, params: { pidm: props.row.personId } }"
                                    class="text-primary text-weight-medium"
                                    :aria-label="`${isAdmin ? 'Edit' : 'View'} ${props.row.fullName}`"
                                >
                                    {{ props.row.fullName }}
                                    <q-icon
                                        :name="isAdmin ? 'edit' : 'visibility'"
                                        size="0.875rem"
                                        class="q-ml-xs"
                                        aria-hidden="true"
                                    />
                                </router-link>
                                <span
                                    v-else
                                    class="text-grey-7 text-weight-medium"
                                >
                                    {{ props.row.fullName }}
                                    <q-icon
                                        name="warning"
                                        size="0.875rem"
                                        color="orange"
                                        class="q-ml-xs"
                                    >
                                        <q-tooltip>No AAUD mapping, record cannot be opened</q-tooltip>
                                    </q-icon>
                                </span>
                                <q-space />
                                <span class="text-caption text-grey">{{ props.row.classLevel }}</span>
                            </div>
                            <div class="row items-center q-gutter-md q-mb-xs">
                                <span class="row items-center q-gutter-xs">
                                    <CompletenessIcon
                                        :complete="props.row.studentInfoComplete"
                                        :total="props.row.studentInfoTotal"
                                        :missing="props.row.studentInfoMissing"
                                        label="Student Info"
                                    />
                                    <span class="text-caption">Info</span>
                                </span>
                                <span class="row items-center q-gutter-xs">
                                    <CompletenessIcon
                                        :complete="props.row.localContactComplete"
                                        :total="props.row.localContactTotal"
                                        :missing="props.row.localContactMissing"
                                        label="Local Contact"
                                    />
                                    <span class="text-caption">Local</span>
                                </span>
                                <span class="row items-center q-gutter-xs">
                                    <CompletenessIcon
                                        :complete="props.row.emergencyContactComplete"
                                        :total="props.row.emergencyContactTotal"
                                        :missing="props.row.emergencyContactMissing"
                                        label="Emergency Contact"
                                    />
                                    <span class="text-caption">Emerg</span>
                                </span>
                                <span class="row items-center q-gutter-xs">
                                    <CompletenessIcon
                                        :complete="props.row.permanentContactComplete"
                                        :total="props.row.permanentContactTotal"
                                        :missing="props.row.permanentContactMissing"
                                        label="Permanent Contact"
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
