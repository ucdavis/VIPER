
<script setup lang="ts">
    import { useUserStore } from '@/store/UserStore'
    import type { Ref } from 'vue'
    import { ref, inject } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import type { QTableProps } from 'quasar'
    import { useDateFunctions } from '@/composables/DateFunctions'
    import type { Student, Service, Person } from '@/CTS/types'

    const userStore = useUserStore()
    const { formatDate } = useDateFunctions()
    const { get, createUrlSearchParams } = useFetch()
    const assessments = ref([]) as Ref<Object[]>
    const assessmentType = ref("EPA")
    const assessmentTypes = [{ label: "EPA", value: "EPA" }]
    const paging = ref({ page: 1, sortBy: "enteredOn", descending: true, rowsPerPage: 25, rowsNumber: 100 }) as Ref<any>
    const loading = ref(false)

    const searchForm = ref({
        service: null as Service | null,
        student: null as Student | null,
        enteredBy: null as Person | null,
        dateFrom: null,
        dateTo: null
    })
    const columns: QTableProps['columns'] = [
        { name: "action", label: "", field: "id", align: "left" },
        { name: "studentName", label: "Student", field: "studentName", align: "left", sortable: true },
        { name: "epaName", label: "EPA", field: "epaName", align: "left", sortable: true },
        { name: "serviceName", label: "Service", field: "serviceName", align: "left", sortable: true },
        { name: "levelName", label: "Level", field: "levelName", align: "left", sortable: true },
        { name: "enteredByName", label: "Entered By", field: "enteredByName", align: "left", sortable: true },
        { name: "enteredOn", label: "Entered On", field: "enteredOn", align: "left", sortable: true, format: formatDate }
    ]
    const filter = ref("")
    const services = ref([])
    const students = ref([]) as Ref<Student[]>
    const assessors = ref([]) as Ref<Person[]>
    
    const baseUrl = inject('apiURL') + "cts/"

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

    async function loadAssessments(page: number, perPage: number, sortBy: string, descending: boolean) {
        const p = createUrlSearchParams({
            "serviceId": searchForm.value.service?.serviceId,
            "enteredById": searchForm.value.enteredBy?.personId,
            "studentId": searchForm.value.student?.personId,
            "dateFrom": searchForm.value.dateFrom,
            "dateTo": searchForm.value.dateTo,
        })

        switch (assessmentType.value) {
            case "EPA":
                p.append("type", "1")
                break;
            default: break;
        }

        const u = new URL(baseUrl + "assessments", document.baseURI);
        p.append("page", page.toString())
        p.append("perPage", perPage.toString())
        p.append("sortBy", sortBy)
        p.append("descending", descending.toString())
        u.search = p.toString()

        loading.value = true
        get(u.toString())
            .then(({ result, pagination: resultPagination, success }) => {
                assessments.value = result
                paging.value.rowsNumber = resultPagination?.totalRecords
            })
        loading.value = false
    }

    async function loadServices() {
        const r = await get(baseUrl + "clinicalservices")
        services.value = r.result
    }
    async function loadStudents() {
        const r = await get(import.meta.env.VITE_API_URL + "students/dvm")
        students.value = r.result
    }
    async function loadAssessors() {
        const r = await get(baseUrl + "assessments/assessors")
        assessors.value = r.result
    }

    loadAssessments(paging.value.page, paging.value.rowsPerPage, paging.value.sortBy, paging.value.descending)
    loadServices()
    loadStudents()
    loadAssessors()
</script>

<template>
    <h2>View Assessments</h2>

    <q-form>
        <div class="row">
            <div class="col-12 col-md-6 col-lg-3">
                <q-select outlined dense options-dense label="Service" v-model="searchForm.service" :options="services"
                          option-label="serviceName" option-value="serviceId" clearable></q-select>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-select outlined dense options-dense label="Student" v-model="searchForm.student" :options="students" clearable
                          option-value="personId">
                    <template v-slot:selected>
                        {{ searchForm.student?.lastName }}{{ searchForm.student?.lastName?.length ? ',' : '' }} {{searchForm.student?.firstName}}
                    </template>
                    <template v-slot:option="std">
                        <q-item v-bind="std.itemProps">
                            <q-item-section>
                                <q-item-label>{{std.opt.lastName}}, {{std.opt.firstName}}</q-item-label>
                            </q-item-section>
                        </q-item>
                    </template>
                </q-select>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-select outlined dense options-dense label="Entered By" v-model="searchForm.enteredBy" :options="assessors"
                          option-label="fullNameLastFirst" option-value="personId" clearable></q-select>
            </div>
        </div>
        <div class="row">
            <div class="col-12 col-md-6 col-lg-3">
                <q-input outlined dense type="date" v-model="searchForm.dateFrom" label="Date from" clearable></q-input>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-input outlined dense type="date" v-model="searchForm.dateTo" label="Date To" clearable></q-input>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-select outlined dense options-dense label="Assessment Type" v-model="assessmentType" :options="assessmentTypes" emit-value clearable></q-select>
            </div>
        </div>
        <div class="row q-my-sm">
            <div class="col-6 col-md-3 offset-3 text-center">
                <q-btn label="View Assessments" color="primary" @click="loadAssessmentsManual()"></q-btn>
            </div>
        </div>
    </q-form>

    <q-table row-key="studentEpaId"
             title="Assessments"
             :rows="assessments"
             :columns="columns"
             dense
             v-model:pagination="paging"
             :rows-per-page-options="[5, 10, 15, 25, 50, 100]"
             :filter="filter"
             :loading="loading"
            @request="loadAssessmentRows">
        <template v-slot:top-right>
            <q-input borderless dense debounce="300" v-model="filter" placeholder="Search">
                <template v-slot:append>
                    <q-icon name="search" />
                </template>
            </q-input>
        </template>
        <!-- to do: modify for non-epa assessment types-->
        <template v-slot:body-cell-action="props">
            <q-td :props="props">
                <router-link :props="props"
                             :to="{name: 'AssessmentEpaEdit', query: {assessmentId: props.row.encounterId}}"
                             v-slot:default="props">
                    <q-btn color="primary" square flat icon="edit" title="Edit EPA" />
                </router-link>
            </q-td>
        </template>
        <template v-slot:body-cell-level="props">
            <q-td :props="props">
                {{ props.row.levelValue }}. {{ props.row.levelName }}
            </q-td>
        </template>
    </q-table>
</template>
