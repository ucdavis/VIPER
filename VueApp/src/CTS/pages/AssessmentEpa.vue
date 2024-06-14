<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref, watch } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import { useUserStore } from '@/store/UserStore'
    import type { Epa, Level, ServiceSelect, Student, StudentEpaFormData } from '@/CTS/types'

    const userStore = useUserStore()

    //services
    const selectedService = ref({ serviceId: 0, serviceName: "" }) as Ref<ServiceSelect>
    const services = ref([]) as Ref<ServiceSelect[]>

    //epas
    const epas = ref([]) as Ref<Epa[]>
    const epa = ref({ epaId: 0, name: "" }) as Ref<Epa>

    //students, selected students and student list options
    const students = ref([]) as Ref<Student[]>
    const allStudents = ref([]) as Ref<Student[]>
    const studentsOnService = ref([]) as Ref<Student[]>
    const studentsLoadedForServiceId = ref(0)
    const selectedStudent = ref({ lastName: "", firstName: "", mailId: "", personId: 0 }) as Ref<Student>
    const studentOptionsType = ref("Service")

    //levels
    const levels = ref([]) as Ref<Level[]>
    const level = ref({}) as Ref<Level>

    //assessment data
    const studentEpa = ref({}) as Ref<any>
    const success = ref(false)
    const submitErrors = ref({}) as Ref<any>

    //urls
    const baseUrl = import.meta.env.VITE_API_URL + "cts/"
    const studentsUrl = import.meta.env.VITE_API_URL + "students/"
    const photoBaseUrl = "https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid="

    async function getServices() {
        const { result, get } = useFetch()
        await get(baseUrl + "clinicalservices")
        services.value = result.value

        //get all scheduled services along with services scheduled this week and last week
        await get(baseUrl + "clinicalschedule/instructor?mothraId=" + userStore.userInfo.mothraId)
        const scheduledServices = result.value

        let today = new Date()
        today.setHours(0, 0, 0, 0)
        let sunday = new Date(today)
        sunday.setDate(sunday.getDate() - sunday.getDay())
        let schedThisWeek = scheduledServices.find((s: any) => today >= new Date(s.dateStart) && today <= new Date(s.dateEnd))
        let schedLastWeek = scheduledServices.find((s: any) => new Date(s.dateEnd).getTime() == sunday.getTime())

        services.value.forEach(s => {
            s.thisWeek = schedThisWeek && schedThisWeek.serviceId == s.serviceId
            s.lastWeek = schedLastWeek && schedLastWeek.serviceId == s.serviceId
            s.scheduled = scheduledServices.find((ss: any) => ss.serviceId == s.serviceId)
        })

        //auto select a service - this week, then last week
        if (schedThisWeek) {
            selectedService.value = services.value.find((s: any) => s.serviceId == schedThisWeek.serviceId) ?? {} as ServiceSelect
        }
        else if (schedLastWeek) {
            selectedService.value = services.value.find((s: any) => s.serviceId == schedLastWeek.serviceId) ?? {} as ServiceSelect
        }
    }

    async function getEpas() {
        const { result, get } = useFetch()
        epa.value = { epaId: 0, name: "" } as Epa
        if (selectedService.value?.serviceId) {
            await get(baseUrl + "epas?serviceId=" + selectedService.value.serviceId)
            epas.value = result.value
            if (epas.value.length == 1) {
                epa.value = epas.value[0]
            }
        }
    }
    async function getStudents() {
        const { get, result } = useFetch()
        //load all students and students on service, if necessary
        if (allStudents.value.length == 0) {
            await get(studentsUrl + "dvm")
            allStudents.value = result.value
        }

        if (selectedService.value.serviceId && selectedService.value.serviceId != studentsLoadedForServiceId.value) {
            var d = new Date().toJSON().split("T")[0]
            get(baseUrl + "clinicalschedule/student?serviceId" + selectedService.value.serviceId + "&startDate=" + d + "&endDate=" + d)
            studentsOnService.value = result.value
            studentsLoadedForServiceId.value = selectedService.value.serviceId
        }

        switch (studentOptionsType.value) {
            case "Service":
                students.value = studentsOnService.value
                break;
            case "V4":
                students.value = allStudents.value.filter(s => s.classLevel == 'V4')
                break;
            default:
                students.value = allStudents.value
                break;
        }
    }
    async function getLevels() {
        const { get, result } = useFetch()
        await get(baseUrl + "levels?epa=true")
        levels.value = result.value
    }
    async function submitEpa() {
        studentEpa.value.epaId = epa.value.epaId
        studentEpa.value.studentId = selectedStudent.value.personId
        studentEpa.value.serviceId = selectedService.value.serviceId

        const { post, success: submitSuccess, errors } = useFetch()
        await post(baseUrl + "studentEpa", studentEpa.value)
        if (!submitSuccess) {
            submitErrors.value = errors
        }
        else {
            selectedStudent.value = { lastName: "", firstName: "", mailId: "", personId: 0 } as Student
            studentEpa.value.levelId = ""
            studentEpa.value.comment = ""
            success.value = true
        }
    }
    function studentSearch(val: string, update: any, abort: any) {
        if (val === '') {
            update(() => getStudents())
            return
        }
        update(() => {
            const s = val.toLowerCase()
            students.value = allStudents.value
                .filter(v => (v.firstName.toLowerCase() + v.lastName.toLowerCase()).indexOf(s) > -1)
        })
    }

    watch(selectedService, () => {
        getEpas()
        getStudents()
    })
    watch(studentOptionsType, () => {
        getStudents()
    })
    watch(selectedStudent, (v) => {
        console.log("Selected Student changed", v)
        if (v.personId > 0) {
            success.value = false
        }
    })
    getServices()
    getLevels()
</script>
<template>
    <div class="row epa justify-center items-start content-start">
        <div style="max-width: 1200px" class="col">
            <div class="row"><h2>Select Service and EPA</h2></div>
            <!--Select EPA and service before showing form-->
            <div class="row items-start q-mb-lg">
                <!--Service-->
                <div class="col-12 col-md-6 col-lg-4 q-mr-md">
                    <q-select label="Select Service" dense options-dense outlined
                              v-model="selectedService" option-label="serviceName" option-value="serviceId" :options="services">
                        <template v-slot:option="scope">
                            <q-item v-bind="scope.itemProps">
                                <q-item-section side v-if="scope.opt.thisWeek">
                                    <q-badge color="green">This Week</q-badge>
                                </q-item-section>
                                <q-item-section side v-if="scope.opt.lastWeek && !scope.opt.thisWeek">
                                    <q-badge color="blue">Last Week</q-badge>
                                </q-item-section>
                                <q-item-section side v-if="scope.opt.scheduled && !scope.opt.lastWeek && !scope.opt.thisWeek">
                                    <q-badge color="grey-5">Scheduled</q-badge>
                                </q-item-section>
                                <q-item-section>
                                    <q-item-label>{{scope.opt.serviceName}}</q-item-label>
                                </q-item-section>
                            </q-item>
                        </template>
                    </q-select>
                </div>

                <!--Epa-->
                <div class="col-12 col-md-6 col-lg-4">
                    <q-select label="Select EPA" dense options-dense outlined v-model="epa"
                              option-label="name" option-value="epaId" :options="epas">
                    </q-select>
                </div>
            </div>

            <q-banner inline-actions rounded v-if="success" class="bg-green text-white q-mb-md">
                EPA Saved
                <template v-slot:action>
                    <q-btn flat label="Dismiss" @click="success = false"></q-btn>
                </template>
            </q-banner>

            <div v-if="selectedService.serviceId != null && epa?.epaId">
                <div class="row justify-between items-center q-mb-lg">
                    <div class="col-12 col-md-6">
                        <h2 class="epa text-weight-regular">{{epa.name}}</h2>
                    </div>
                    <div class="col-12 col-md-6 text-right">
                        <div class="row items-center">
                            <div class="col-12 col-md-8">
                                <q-select dense options-dense label="Student" class="q-mr-md items-center"
                                          :outlined="selectedStudent == null" :borderless="selectedStudent != null" :stack-label="false"
                                          use-input input-debounce="300" @filter="studentSearch" clearable
                                          v-model="selectedStudent" :options="students" option-label="lastName" option-value="personId">
                                    <template v-slot:selected>
                                        {{ selectedStudent.lastName }}{{ selectedStudent.lastName.length ? ',' : '' }} {{selectedStudent.firstName}}
                                    </template>
                                    <template v-slot:after>
                                        <q-avatar v-if="selectedStudent?.mailId" rounded class="fit">
                                            <q-img :src="photoBaseUrl + selectedStudent?.mailId +'&altphoto=1'"
                                                   class="smallPhoto rounded-borders" loading="eager" :no-spinner="true"></q-img>
                                        </q-avatar>
                                    </template>
                                    <template v-slot:no-option>
                                        <div class="q-pa-sm">No students found matching the filter</div>
                                    </template>
                                    <template v-slot:option="std">
                                        <q-item v-bind="std.itemProps">
                                            <!--
                                            <q-item-section avatar>
                                                <q-avatar rounded>
                                                    <q-img :src="photoBaseUrl + std.opt.mailId +'&altphoto=1'"
                                                           class="smallPhoto" loading="eager" no-spinner="true"></q-img>
                                                </q-avatar>
                                            </q-item-section>
                                            -->
                                            <q-item-section>
                                                <q-item-label>{{std.opt.lastName}}, {{std.opt.firstName}}</q-item-label>
                                            </q-item-section>
                                        </q-item>
                                    </template>
                                </q-select>
                            </div>
                            <div class="col-auto">
                                <q-btn-toggle v-model="studentOptionsType" push toggle-color="primary"
                                              :options="[{label: 'Service', value: 'Service'}, {label: 'V4', value: 'V4'}, {label: 'All', value: 'All'}]">
                                </q-btn-toggle>
                            </div>
                        </div>
                    </div>
                </div>
                <!--Show form once student is selected-->
                <q-form @submit="submitEpa" v-bind="studentEpa" v-if="selectedStudent.personId > 0">
                    <div class="bg-red-5 text-white q-pa-sm rounded q-mb-md" v-if="submitErrors?.message?.length > 0">
                        {{submitErrors.message}}
                        Please make sure you have selected a service, EPA, student, and a level on the entrustment scale.
                    </div>
                    <div class="row q-mb-sm text-center gt-sm">
                        <div class="col" v-for="level in levels">
                            {{level.levelName}}
                        </div>
                    </div>
                    <div class="row q-mb-md gt-sm">
                        <div class="col q-mx-sm levelSelection" v-for="level in levels">
                            <q-btn :label="level.order"
                                   push
                                   unelevated
                                   flat
                                   size="md"
                                   dense
                                   :class="studentEpa.levelId == level.levelId ? 'selectedLevel q-py-sm' : 'q-py-sm'"
                                   @click="studentEpa.levelId = level.levelId">
                            </q-btn>
                        </div>
                    </div>
                    <div class="q-mb-sm text-center lt-md">
                        <div class="q-mx-sm levelSelection" v-for="level in levels">
                            <q-btn push
                                   unelevated
                                   flat
                                   no-caps
                                   size="md"
                                   dense
                                   :class="studentEpa.levelId == level.levelId ? 'selectedLevel' : ''"
                                   @click="studentEpa.levelId = level.levelId">
                                <template v-slot:default>
                                    {{level.order}}. {{level.levelName}}
                                </template>
                            </q-btn>
                        </div>
                    </div>
                    <q-input type="textarea" outlined dense v-model="studentEpa.comment" class="q-mb-md"
                             label="Comments: What should the student keep doing? How can they improve performance?"></q-input>
                    <div class="column">
                        <q-btn no-caps
                               label="Submit EPA"
                               type="submit"
                               padding="sm xl"
                               color="primary"
                               size="md"
                               class="self-center"></q-btn>
                    </div>
                </q-form>
            </div>
        </div>
    </div>
</template>

<style>
    h2.epa {
        font-weight: 400;
        font-size: 2.0rem;
        margin-bottom: 4px;
    }

    div.levelSelection button {
        border: 1px solid rgb(30, 136, 229);
        color: rgb(30, 136, 229);
        width: 100%;
        margin-bottom: .2rem;
    }

        div.levelSelection button.selectedLevel {
            background-color: rgb(30, 136, 229);
            color: white;
        }
</style>