<script setup lang="ts">
    import { useQuasar } from 'quasar'
    import { ref, inject } from 'vue'
    import type { Ref } from 'vue'
    import { useRoute } from 'vue-router'
    import { useUserStore } from '@/store/UserStore'
    import { useFetch } from '@/composables/ViperFetch'
    import type { Assessment, Epa, Level, Person } from '@/CTS/types'
    import { useDateFunctions } from '@/composables/DateFunctions'
    import AssessmentBubble from '@/CTS/components/AssessmentBubble.vue'

    const userStore = useUserStore()
    const { get } = useFetch()
    const { formatDate } = useDateFunctions()

    const apiUrl = inject('apiURL')
    const person = ref(null) as Ref<Person | null>
    let studentUserId = userStore.userInfo.userId
    const showPersonName = ref(false)
    const epaAssessments = ref([]) as Ref<Assessment[]>
    const epaAssessment = ref() as Ref<Assessment>
    const epas = ref([]) as Ref<Epa[]>
    const levels = ref([]) as Ref<Level[]>
    const loaded = ref(false)
    const showDetails = ref([]) as Ref<boolean[]>
    const bubbleType = ref("bubble")

    const showAssessmentDetail = ref(false)

    async function load() {
        const $q = useQuasar()
        $q.loading.show({
            message: "Loading",
            delay: 250 // ms
        })

        const route = useRoute()
        const params = route.query
        if (params.student != null) {
            let result = await get(apiUrl + "cts/permissions?access=ViewStudentAssessments&studentId=" + params.student)
            let canAccess = result.result
            const studentParam = parseInt(params.student.toString())
            if (studentParam != undefined && canAccess) {
                studentUserId = studentParam
                showPersonName.value = true
            }
        }
        let p1 = get(apiUrl + "cts/epas").then(r => epas.value = r.result)
        let p2 = get(apiUrl + "cts/levels").then(r => levels.value = r.result)
        let p3 = get(apiUrl + "people/" + studentUserId).then(r => person.value = r.result)
        let p4 = getAssessments()
        await Promise.all([p1, p2, p3, p4])

        for (let i = 0; i < epas.value.length; i++) {
            showDetails.value[i] = false
        }
        loaded.value = true
        $q.loading.hide()
    }

    async function getAssessments() {
        const r = await get(apiUrl + "cts/assessments?studentUserId=" + studentUserId)
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

    function getText(date: Date, enteredBy: string, levelName: string, comment: string | null, serviceName: string | null) {
        return levelName + '\n' + (comment != null ? comment : "") + "\n" + formatDate(date.toString()) + ' ' + enteredBy + ' ' + '\n' + serviceName + '\n'
    }

    function toggleExpandAll() {
        let anyExpanded = false
        for (let i = 0; i < showDetails.value.length; i++) {
            if (showDetails.value[i]) {
                anyExpanded = true
                break;
            }
        }

        for (let i = 0; i < showDetails.value.length; i++) {
            showDetails.value[i] = !anyExpanded
        }
    }

    function handleAssessmentClick(id: number) {
        epaAssessment.value = epaAssessments.value.find(a => a.encounterId == id)!
        showAssessmentDetail.value = true
    }

    load()
</script>
<template>
    <div v-if="loaded">
        <h2>
            <span v-if="showPersonName && person != null">Assessments for {{ person.firstName }} {{ person.lastName }}</span>
            <span v-else>My Assessments</span>
        </h2>

        <q-dialog v-model="showAssessmentDetail">
            <q-card style="width:700px; max-width: 80vw;">
                <q-card-section>
                    <div class="text-h6">Assessment Details</div>
                    <div class="row">
                        <div class="col-12">
                            <strong>EPA:</strong> {{ epaAssessment.epaName }}
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-12">
                            <strong>Rating:</strong> 
                            <AssessmentBubble class="q-ml-sm" :maxValue="5" :value=epaAssessment.levelValue></AssessmentBubble>
                            {{ epaAssessment.levelName }}
                        </div>
                    </div>
                    <div class="row q-mt-xs">
                        <div class="col-12">
                            <strong>Comment:</strong> {{ epaAssessment.comment }}
                        </div>
                    </div>
                    <div class="row q-mt-xs">
                        <div class="col-12">
                            <strong>Entered:</strong>
                            {{ formatDate(epaAssessment.encounterDate.toString()) }}
                            by
                            {{ epaAssessment.enteredByName }}
                        </div>
                    </div>
                    <div class="row q-mt-xs">
                        <div class="col-12">
                            <strong>Service:</strong> {{ epaAssessment.serviceName }}
                        </div>
                    </div>
                </q-card-section>
            </q-card>
        </q-dialog>

        <div class="row">
            <div class="col col-md-10 col-lg-7 q-mr-sm">
                <h3>Entrustable Professional Activities</h3>
            </div>
            <div class="col-1">
                <q-btn dense color="secondary" :icon="showDetails.find(s => s) != undefined ? 'expand_less' : 'expand_more'" @click="toggleExpandAll()"></q-btn>
            </div>
        </div>
        <div v-for="(epa, index) in epas" class="row q-mt-sm q-pt-sm assessmentGroup">
            <div class="col col-md-4 col-lg-3 q-mr-sm">
                {{ epa.name }}
            </div>
            <div class="col col-md-6 col-lg-4">
                <AssessmentBubble :maxValue="5" :value=a.levelValue :text="getText(a.encounterDate, a.enteredByName, a.levelName, a?.comment, a?.serviceName)" :id="a.encounterId"
                                  @bubbleClick="handleAssessmentClick"
                                  v-for="a in getAssessmentsForEpa(epa.epaId)" :type="bubbleType"></AssessmentBubble>
            </div>
            <div class="col-1">
                <q-btn dense :icon="showDetails[index] ? 'expand_less' : 'expand_more'" @click="showDetails[index] = !showDetails[index]"
                       v-if="getAssessmentsForEpa(epa.epaId).length > 0"></q-btn>
            </div>
            <q-slide-transition>
                <div class="col-12 q-mb-md" v-if="showDetails[index]" :key="'epadetails' + index">
                    <div v-for="a in getAssessmentsForEpa(epa.epaId)" class="row q-mb-sm">
                        <div class="col-2 col-sm-auto q-pr-sm">
                            <AssessmentBubble :maxValue="5" :value=a.levelValue></AssessmentBubble>
                        </div>
                        <div class="col-10 col-sm-5 col-md-3 col-lg-2">
                            {{ formatDate(a.encounterDate.toString()) }}
                            {{ a.enteredByName }}
                        </div>
                        <div class="col-10 offset-2 col-sm-5 offset-sm-1 offset-md-0 col-md-3 col-lg-2">
                            {{ a.serviceName }}
                        </div>
                        <div class="col-10 offset-2 col-sm-5 offset-sm-1 offset-md-0 col-md-3 col-lg-2">
                            {{ a.levelName }}
                        </div>
                        <div class="col-10 offset-2 offset-sm-1 offset-md-0 col-md-4 col-lg-5">
                            {{ a.comment }}
                        </div>
                    </div>
                </div>
            </q-slide-transition>
        </div>
    </div>
</template>