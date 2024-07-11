<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref, watch, inject } from 'vue'
    import { useRoute, useRouter } from 'vue-router'
    import { useFetch } from '@/composables/ViperFetch'
    import type { Epa, Level, ServiceSelect, Student, StudentEpaFormData } from '@/CTS/types'
    import { useDateFunctions } from '@/composables/DateFunctions'
    import StudentSelect from '@/components/StudentSelect.vue'
    import ServiceSelectBox from '@/CTS/components/ServiceSelect.vue'
    import LevelSelect from '@/CTS/components/LevelSelect.vue'

    const { formatDateForDateInput } = useDateFunctions()
    const route = useRoute()
    const router = useRouter()
    //the epa we are editing
    const studentEpaId = parseInt(route.query.assessmentId as string)

    //levels
    const levelId = ref(null) as Ref<number | null>
    const clearLevel = ref(false)

    //assessment data
    const studentEpa = ref({}) as Ref<any>
    const success = ref(false)
    const submitErrors = ref({}) as Ref<any>

    //urls
    const baseUrl = inject('apiURL') + "cts/"
    const photoBaseUrl = "https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid="

    async function submitEpa() {
        studentEpa.value.levelId = levelId.value
        const data = {
            studentId: studentEpa.value.studentUserId,
            epaId: studentEpa.value.epaId,
            levelId: studentEpa.value.levelId,
            comment: studentEpa.value.comment,
            serviceId: studentEpa.value.serviceId,
            encounterDate: studentEpa.value.encounterDate,
            encounterId: studentEpa.value.encounterId
        }
        const { put } = useFetch()
        const r = await put(baseUrl + "assessments/epa/" + studentEpaId, data)
        if (!r.success) {
            submitErrors.value = r.errors
        }
        else {
            
            clearLevel.value = true
            studentEpa.value.levelId = r.result.levelId
            studentEpa.value.comment = r.result.comment
            studentEpa.value.encounterDate = formatDateForDateInput(r.result.encounterDate)
            success.value = true
        }
    }

    async function getStudentEpa() {
        const { get } = useFetch()
        if (studentEpaId && studentEpaId > 0) {
            const r = await get(baseUrl + "assessments/" + studentEpaId)
            if (r.success) {
                studentEpa.value = r.result
                studentEpa.value.encounterDate = formatDateForDateInput(studentEpa.value.encounterDate)
            }
        }

        if (studentEpa.value?.encounterId === undefined) {
            router.push({ name: 'AssessmentList' })
        }
    }

    getStudentEpa()
</script>
<template>
    <div class="row epa justify-center items-start content-start">
        <div style="max-width: 1200px" class="col" v-show="studentEpa.encounterId">
            <q-banner inline-actions rounded v-if="success" class="bg-green text-white q-mb-md">
                EPA Saved
                <template v-slot:action>
                    <q-btn flat label="Dismiss" @click="success = false"></q-btn>
                </template>
            </q-banner>
            <div class="row justify-between items-end q-mb-lg">
                <div class="col-12 col-md-6 col-lg-5">
                    <h2 class="epa">{{ studentEpa.epaName }}</h2>
                </div>
                <div class="col-12 col-md-6 col-lg-3 q-mr-md text-weight-medium text-body1">
                    Service: {{ studentEpa.serviceName }}
                </div>
                <div class="col-12 col-md-6 col-lg-3 text-right">
                    <div class="row items-end">
                        <div class="col-10 text-body1">
                            Assessment for {{ studentEpa.studentName }}
                        </div>
                        <div class="col-2">
                            <q-avatar v-show="studentEpa.studentMailId" rounded class="fit">
                                <q-img :src="photoBaseUrl + studentEpa.studentMailId + '&altphoto=1'"
                                       class="smallPhoto rounded-borders" loading="eager" :no-spinner="true"></q-img>
                            </q-avatar>
                        </div>
                    </div>
                </div>
            </div>
            <q-form @submit="submitEpa" v-bind="studentEpa">
                <div class="bg-red-5 text-white q-pa-sm rounded" v-if="submitErrors?.message?.length > 0">
                    {{submitErrors.message}}
                    Please make sure you have selected a service, EPA, student, and a level on the entrustment scale.
                </div>
                <LevelSelect levelType="epa"
                             @levelChange="(selectedLevelId : number) => levelId = selectedLevelId"
                             :levelId="studentEpa.levelId"
                             :clearLevel="clearLevel" />
               
                <div class="row q-mb-md">
                    <q-input type="textarea" outlined dense v-model="studentEpa.comment" class="col-12"
                             label="Comments: What should the student keep doing? How can they improve performance?"></q-input>
                </div>

                <div class="row">
                    <div class="col-12">Entered By: {{ studentEpa.enteredByName }}</div>
                    <q-input dense outlined type="date" label="Encounter Date" v-model="studentEpa.encounterDate" class="col-8 col-md-4"></q-input>
                </div>

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
</template>

<style scoped>
    h2.epa {
        font-weight: 400;
        font-size: 2.0rem;
        margin-bottom: 4px;
    }
</style>