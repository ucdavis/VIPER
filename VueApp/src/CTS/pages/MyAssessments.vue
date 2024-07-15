<script setup lang="ts">
    import { ref, inject } from 'vue'
    import type { Ref } from 'vue'
    import { useRoute } from 'vue-router'
    import { useUserStore } from '@/store/UserStore'
    import { useFetch } from '@/composables/ViperFetch'
    import type { Assessment, Epa, Level } from '@/CTS/types'
    import { useDateFunctions } from '@/composables/DateFunctions'
    import {
        Chart as ChartJS,
        Title,
        Tooltip,
        Legend,
        LineElement,
        CategoryScale,
        LinearScale,
    } from 'chart.js/auto'
    import { Line } from 'vue-chartjs'
    import type { ChartData, Point } from 'chart.js'

    const apiUrl = inject('apiURL')
    const baseUrl = apiUrl + "cts/"
    const epaAssessments = ref([]) as Ref<Assessment[]>
    const epas = ref([]) as Ref<Epa[]>
    const assessmentURL = baseUrl + 'assessments'
    const userStore = useUserStore()
    const { get } = useFetch()
    const { formatDate } = useDateFunctions()
    const levels = ref([]) as Ref<Level[]>

    function getEpas() {
        get(baseUrl + "epas")
            .then(r => epas.value = r.result)
    }

    function getLevels() {
        get(baseUrl + "levels")
            .then(r => levels.value = r.result)
    }

    async function getAssessments() {
        //check for url param specifying id of student to get assessments for
        //for id other than the logged in user, check permission (though server should return 403 if they don't have access)
        const route = useRoute()
        const params = route.query

        let studentUserId = userStore.userInfo.userId
        if (params.student != null) {
            const studentParam = parseInt(params.student.toString())
            if (studentParam != undefined && userStore.userInfo.permissions.includes("SVMSecure.CTS.StudentAssessments")) {
                studentUserId = studentParam
            }
        }

        const r = await get(assessmentURL + "?studentUserId=" + studentUserId)
        epaAssessments.value = r.result
            .sort((e1: Assessment, e2: Assessment) => {
                if (e1.epaName == null || e2.epaName == null || e1.epaName == e2.epaName) {
                    return new Date(e2.encounterDate).getTime() - new Date(e1.encounterDate).getTime();
                }
                return e1.epaName.localeCompare(e2.epaName)
            })
    }

    function getAssessmentsForEpa(epaId: number | null) {
        return epaAssessments.value.filter(e => e.epaId == epaId)
    }

    getEpas()
    getAssessments()

    /* Chart */
    function getEpaData(epaId: number | null) {
        const datasets: ChartData<'line', { key: string, value: number }[]> = {
            datasets: [{
                data: [],
                parsing: {
                    xAxisKey: 'key',
                    yAxisKey: 'value'
                }
            }],
        };

        const epas = getAssessmentsForEpa(epaId)
        const data = [] as number[]
        const labels = [] as string[]
        epas.forEach(e => {
            labels.push(formatDate(e.encounterDate.toString()))
            datasets.datasets[0].data.push({
                key: formatDate(e.encounterDate.toString()).toString(),
                value: e.levelValue
            })
            data.push(e.levelValue)
        })
        //return datasets
        return {
            labels: labels,
            datasets: [
                {
                    label: "",
                    data: data
                }
            ],
        }
    }

    const options = {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
            y: {
                suggestedMin: 1,
                suggestedMax: 5,
                display: false
            }
        }
    }
    ChartJS.register(CategoryScale, LinearScale, LineElement, Title, Tooltip, Legend)
</script>
<template>
    <div v-if="epaAssessments.length">
        <h2>Assessments for {{ epaAssessments[0].studentName }}</h2>
        <div class="row">
            <div class="col-12 col-lg-2"><strong>EPA</strong></div>
            <div class="col-12 col-lg-4"><strong>Level</strong></div>
            <div class="col-12 col-lg-6"><strong>Comments</strong></div>
        </div>
        <div v-for="epa in epas" class="row">
            <div class="col-12 col-lg-2">
                {{ epa.name }}
            </div>
            <div class="col-12 col-lg-4">
                <div style="position: relative; height:20vh; width:30vh;">
                    <Line :data="getEpaData(epa.epaId)" :options="options" />
                </div>
            </div>
            <div class="col-12 col-lg-6">
                <q-expansion-item expand-separator label="View Comments" dense class="q-pa-none" header-class="q-pa-none">
                    <span v-for="ea in getAssessmentsForEpa(epa.epaId)">
                        <strong>{{ formatDate(ea.encounterDate.toString()) }} - {{ ea.enteredByName }}</strong>
                        <br />
                        {{ ea.comment }}
                        <br />
                    </span>
                </q-expansion-item>
            </div>
        </div>
    </div>
</template>