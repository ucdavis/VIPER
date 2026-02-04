<script setup lang="ts">
import { useUserStore } from "@/store/UserStore"
import type { Ref } from "vue"
import { ref, inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import type { QTableProps } from "quasar"
import { useDateFunctions } from "@/composables/DateFunctions"
import type { Student, Service, Person } from "@/CTS/types"

const userStore = useUserStore()
const { formatDate } = useDateFunctions()
const { get, createUrlSearchParams } = useFetch()
const assessments = ref([]) as Ref<Object[]>
const assessmentType = ref("EPA")
const assessmentTypes = [{ label: "EPA", value: "EPA" }]
const paging = ref({ page: 1, sortBy: "enteredOn", descending: true, rowsPerPage: 25, rowsNumber: 100 }) as Ref<any>
const loading = ref(false)
const canViewAll = ref(null) as Ref<boolean | null>
const mineOnly = ref(true)
const mineOnlyOptions = ref([
    { label: "My Assessments", value: true },
    { label: "My service", value: false },
])

const searchForm = ref({
    service: null as Service | null,
    student: null as Student | null,
    enteredBy: null as Person | null,
    dateFrom: null,
    dateTo: null,
})
const columns: QTableProps["columns"] = [
    { name: "action", label: "", field: "id", align: "left" },
    { name: "studentName", label: "Student", field: "studentName", align: "left", sortable: true },
    { name: "epaName", label: "EPA", field: "epaName", align: "left", sortable: true },
    { name: "serviceName", label: "Service", field: "serviceName", align: "left", sortable: true },
    { name: "levelName", label: "Level", field: "levelName", align: "left", sortable: true },
    { name: "enteredByName", label: "Entered By", field: "enteredByName", align: "left", sortable: true },
    { name: "enteredOn", label: "Entered On", field: "enteredOn", align: "left", sortable: true, format: formatDate },
]
const filter = ref("")
const services = ref([]) as Ref<Service[]>
const students = ref([]) as Ref<Student[]>
const assessors = ref([]) as Ref<Person[]>

const baseUrl = inject("apiURL") + "cts/"
const studentsUrl = inject("apiURL") + "students/dvm"

async function loadAssessmentRows(props: any) {
    const { page, rowsPerPage, sortBy, descending } = props.pagination
    loading.value = true
    assessments.value = []

    await loadAssessments(page, rowsPerPage, sortBy, descending)
    paging.value.page = page
    paging.value.rowsPerPage = rowsPerPage
    paging.value.sortBy = sortBy
    paging.value.descending = descending
    loading.value = false
}

async function loadAssessmentsManual() {
    //always start over if filter has changed
    await loadAssessments(1, paging.value.rowsPerPage, paging.value.sortBy, paging.value.descending)
}

async function getCanViewAllAssessments() {
    let result = await get(baseUrl + "permissions?access=ViewAllAssessments")
    canViewAll.value = result.result.hasAccess
}

async function loadAssessments(page: number, perPage: number, sortBy: string, descending: boolean) {
    const p = createUrlSearchParams({
        serviceId: searchForm.value.service?.serviceId,
        enteredById: searchForm.value.enteredBy?.personId,
        studentUserId: searchForm.value.student?.personId,
        dateFrom: searchForm.value.dateFrom,
        dateTo: searchForm.value.dateTo,
    })

    if (!canViewAll.value) {
        if (services.value.length === 0 || mineOnly.value) {
            p.set("enteredById", userStore.userInfo.userId !== null ? userStore.userInfo.userId.toString() : "")
        } else {
            const firstService = services.value[0]
            if (firstService) {
                p.set("serviceId", firstService.serviceId.toString())
            }
        }
    }

    switch (assessmentType.value) {
        case "EPA":
            p.append("type", "1")
            break
        default:
            break
    }

    const u = new URL(baseUrl + "assessments", document.baseURI)
    p.append("page", page.toString())
    p.append("perPage", perPage.toString())
    p.append("sortBy", sortBy)
    p.append("descending", descending.toString())
    u.search = p.toString()

    loading.value = true
    get(u.toString()).then(({ result, pagination: resultPagination }) => {
        assessments.value = result
        paging.value.rowsNumber = resultPagination?.totalRecords
    })
    loading.value = false
}

async function loadServices() {
    const r = canViewAll.value
        ? await get(baseUrl + "clinicalservices")
        : await get(baseUrl + "clinicalservices?chiefId=" + userStore.userInfo.userId)
    services.value = r.result
}
async function loadStudents() {
    const r = await get(studentsUrl)
    students.value = r.result
}
async function loadAssessors() {
    assessors.value = canViewAll.value ? (await get(baseUrl + "assessments/assessors")).result : []
}

async function loadPageData() {
    await getCanViewAllAssessments()
    await loadServices()
    loadAssessments(paging.value.page, paging.value.rowsPerPage, paging.value.sortBy, paging.value.descending)
    loadStudents()
    loadAssessors()
}

loadPageData()
</script>

<template>
    <h2>View Assessments</h2>

    <q-form>
        <div class="row">
            <div class="col-12 col-sm-5 col-lg-3 q-ma-xs">
                <q-input
                    outlined
                    dense
                    type="date"
                    v-model="searchForm.dateFrom"
                    label="Date from"
                    clearable
                ></q-input>
            </div>
            <div class="col-12 col-sm-5 col-lg-3 q-ma-xs">
                <q-input
                    outlined
                    dense
                    type="date"
                    v-model="searchForm.dateTo"
                    label="Date To"
                    clearable
                ></q-input>
            </div>
            <div class="col-12 col-sm-5 col-lg-3 q-ma-xs">
                <q-select
                    outlined
                    dense
                    options-dense
                    label="Assessment Type"
                    v-model="assessmentType"
                    :options="assessmentTypes"
                    emit-value
                    clearable
                ></q-select>
            </div>
            <div
                class="col-12 col-sm-5 col-lg-3 q-ma-xs"
                v-if="canViewAll"
            >
                <q-select
                    outlined
                    dense
                    options-dense
                    label="Service"
                    v-model="searchForm.service"
                    :options="services"
                    option-label="serviceName"
                    option-value="serviceId"
                    clearable
                ></q-select>
            </div>
            <div class="col-12 col-sm-5 col-lg-3 q-ma-xs">
                <q-select
                    outlined
                    dense
                    options-dense
                    label="Student"
                    v-model="searchForm.student"
                    :options="students"
                    clearable
                    option-value="personId"
                >
                    <template #selected>
                        {{ searchForm.student?.lastName }}{{ searchForm.student?.lastName?.length ? "," : "" }}
                        {{ searchForm.student?.firstName }}
                    </template>
                    <template #option="std">
                        <q-item v-bind="std.itemProps">
                            <q-item-section>
                                <q-item-label>{{ std.opt.lastName }}, {{ std.opt.firstName }}</q-item-label>
                            </q-item-section>
                        </q-item>
                    </template>
                </q-select>
            </div>
            <div
                class="col-12 col-sm-5 col-lg-3 q-ma-xs"
                v-if="canViewAll"
            >
                <q-select
                    outlined
                    dense
                    options-dense
                    label="Entered By"
                    v-model="searchForm.enteredBy"
                    :options="assessors"
                    option-label="fullNameLastFirst"
                    option-value="personId"
                    clearable
                ></q-select>
            </div>
            <div
                class="col-12 col-sm-5 col-lg-3 q-ma-xs"
                v-if="!canViewAll && services.length > 0"
            >
                <q-btn-toggle
                    no-caps
                    v-model="mineOnly"
                    :options="mineOnlyOptions"
                    map-options
                    emit-value
                ></q-btn-toggle>
            </div>
        </div>
        <div class="row q-my-sm">
            <div class="col-6 col-md-3 offset-3 text-center">
                <q-btn
                    label="View Assessments"
                    color="primary"
                    @click="loadAssessmentsManual()"
                ></q-btn>
            </div>
        </div>
    </q-form>

    <q-table
        row-key="studentEpaId"
        title="Assessments"
        :rows="assessments"
        :columns="columns"
        dense
        v-model:pagination="paging"
        :rows-per-page-options="[5, 10, 15, 25, 50, 100]"
        :filter="filter"
        :loading="loading"
        @request="loadAssessmentRows"
    >
        <template #top-right>
            <q-input
                borderless
                dense
                debounce="300"
                v-model="filter"
                placeholder="Search"
            >
                <template #append>
                    <q-icon name="search" />
                </template>
            </q-input>
        </template>
        <!-- to do: modify for non-epa assessment types-->
        <template #body-cell-action="props">
            <q-td :props="props">
                <router-link
                    v-if="props.row.editable"
                    :to="{ name: 'AssessmentEpaEdit', query: { assessmentId: props.row.encounterId } }"
                >
                    <q-btn
                        color="primary"
                        square
                        flat
                        icon="edit"
                        title="Edit EPA"
                    />
                </router-link>
            </q-td>
        </template>
        <template #body-cell-studentName="props">
            <q-td :props="props">
                <RouterLink
                    :to="'MyAssessments?student=' + props.row.studentUserId"
                    v-if="canViewAll"
                    >{{ props.row.studentName }}</RouterLink
                >
                <span v-else>{{ props.row.studentName }}</span>
            </q-td>
        </template>
        <template #body-cell-level="props">
            <q-td :props="props"> {{ props.row.levelValue }}. {{ props.row.levelName }} </q-td>
        </template>
    </q-table>
</template>
