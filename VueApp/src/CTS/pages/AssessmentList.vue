
<script setup lang="ts">
    import { useUserStore } from '@/store/UserStore'
    import type { Ref } from 'vue'
    import { ref } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import type { QTableProps } from 'quasar'
    import { useDateFunctions } from '@/composables/DateFunctions'
    import type { Student, Service, Person } from '@/CTS/types'

    const userStore = useUserStore()
    const { formatDate } = useDateFunctions()
    const assessments = ref([]) as Ref<Object[]>
    const assessmentType = ref("EPA")
    const assessmentTypes = [{ label: "EPA", value: "EPA" }]
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
        { name: "service", label: "Service", field: "serviceName", align: "left", sortable: true },
        { name: "level", label: "Level", field: "levelName", align: "left", sortable: true },
        { name: "enteredBy", label: "Entered By", field: "enteredByName", align: "left", sortable: true },
        { name: "enteredOn", label: "Entered On", field: "enteredOn", align: "left", sortable: true, format: formatDate }
    ]
    const filter = ref("")
    const services = ref([])
    const students = ref([]) as Ref<Student[]>
    const assessors = ref([])

    const baseUrl = import.meta.env.VITE_API_URL + "cts/"

    async function loadAssessments() {
        const { success, result, get } = useFetch()
        const p = new URLSearchParams
        if (searchForm.value.service?.serviceId != null)
            p.append("serviceId", searchForm.value.service.serviceId.toString())
        if (searchForm.value.enteredBy?.personId != null)
            p.append("enteredById", searchForm.value.enteredBy.personId.toString())
        if (searchForm.value.student?.personId != null)
            p.append("studentId", searchForm.value.student.personId.toString())
        if (searchForm.value.dateFrom != null)
            p.append("dateFrom", searchForm.value.dateFrom)
        if (searchForm.value.dateTo != null)
            p.append("dateTo", searchForm.value.dateTo)

        var u = null as URL | null;
        switch (assessmentType.value) {
            case "EPA":
                u = new URL(baseUrl + "studentEpa", document.baseURI);
                break;
            default: break;
        }

        if (u != null) {
            u.search = p.toString()
            await get(u.toString())
            if (success.value) {
                assessments.value = result.value
            }
        }
    }
    async function loadServices() {
        const { result, get } = useFetch()
        await get(baseUrl + "clinicalservices")
        services.value = result.value
    }
    async function loadStudents() {
        const { result, get } = useFetch()
        await get(import.meta.env.VITE_API_URL + "students/dvm")
        students.value = result.value
    }
    async function loadAssessors() {
        const { result, get } = useFetch()
        await get(baseUrl + "clinicalservices")
        assessors.value = result.value
    }

    loadAssessments()
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
                <q-select outlined dense options-dense label="Entered By" v-model="searchForm.enteredBy" :options="assessors" emit-value></q-select>
            </div>
        </div>
        <div class="row">
            <div class="col-12 col-md-6 col-lg-3">
                <q-input outlined dense type="date" v-model="searchForm.dateFrom" label="Date from"></q-input>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-input outlined dense type="date" v-model="searchForm.dateTo" label="Date To"></q-input>
            </div>
            <div class="col-12 col-md-6 col-lg-3">
                <q-select outlined dense options-dense label="Assessment Type" v-model="assessmentType" :options="assessmentTypes" emit-value></q-select>
            </div>
        </div>
        <div class="row q-mt-sm">
            <div class="col-6 col-md-3 offset-3 text-center">
                <q-btn label="View Assessments" color="primary" @click="loadAssessments()"></q-btn>
            </div>
        </div>
    </q-form>

    <q-table row-key="studentEpaId"
             title="Assessments"
             :rows="assessments"
             :columns="columns"
             dense
             :pagination="{rowsPerPage: 25}"
             :filter="filter">
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
                             :to="{name: 'AssessmentEpaEdit', query: {studentEpaId: props.row.studentEpaId}}"
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
