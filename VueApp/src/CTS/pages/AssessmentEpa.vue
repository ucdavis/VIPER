<script setup lang="ts">
import type { Ref } from "vue"
import { ref, watch, inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import type { Epa } from "@/CTS/types"
import StudentSelect from "@/components/StudentSelect.vue"
import ServiceSelectBox from "@/CTS/components/ServiceSelect.vue"
import LevelSelect from "@/CTS/components/LevelSelect.vue"

//epas
const epas = ref([]) as Ref<Epa[]>
const defaultEpa: Epa = { epaId: 0, name: "", order: null, description: "", active: false, services: [] }
const epa = ref(defaultEpa) as Ref<Epa>
const serviceId = ref(0)

//students select list value and filter
const selectedStudentId = ref(0)
const clearStudent = ref(false)

//levels
const levelId = ref(null) as Ref<number | null>
const clearLevel = ref(false)

//assessment data
const studentEpa = ref({}) as Ref<any>
const success = ref(false)
const submitErrors = ref(false)
const commentValidationMessage = ref("")

//urls
const baseUrl = inject("apiURL") + "cts/"

async function getEpas() {
    const { get } = useFetch()
    epa.value = defaultEpa
    if (serviceId.value) {
        const r = await get(baseUrl + "epas?serviceId=" + serviceId.value)
        epas.value = r.result
        if (epas.value.length === 1) {
            epa.value = epas.value[0] ?? defaultEpa
        }
    }
}
async function submitEpa() {
    studentEpa.value.epaId = epa.value.epaId
    studentEpa.value.studentId = selectedStudentId.value
    studentEpa.value.serviceId = serviceId.value
    studentEpa.value.levelId = levelId.value

    commentValidationMessage.value = ""
    submitErrors.value = false

    if (studentEpa.value.comment === undefined || studentEpa.value.comment.length < 5) {
        commentValidationMessage.value =
            "Please describe what the student should keep doing and how they can improve performance."
        submitErrors.value = true
    } else if (studentEpa.value.levelId === undefined) {
        submitErrors.value = true
    } else {
        const { post } = useFetch()
        const r = await post(baseUrl + "assessments/epa", studentEpa.value)
        if (!r.success) {
            submitErrors.value = true
        } else {
            clearStudent.value = true
            clearLevel.value = true
            selectedStudentId.value = 0
            studentEpa.value.levelId = ""
            studentEpa.value.comment = ""
            success.value = true

            //set back to false so the next submission clears student and level
            setTimeout(() => {
                clearStudent.value = false
                clearLevel.value = false
            }, 500)
        }
    }
}

//load EPAs when service is changed
watch(serviceId, () => {
    getEpas()
})

//when student is changed after the form is submitted, hide the success banner
watch(selectedStudentId, () => {
    if (selectedStudentId.value > 0) {
        success.value = false
    }
})
</script>
<template>
    <div class="row epa justify-center items-start content-start">
        <div
            style="max-width: 1200px"
            class="col"
        >
            <div class="row"><h2>Select Service and EPA</h2></div>
            <!--Select EPA and service before showing form-->
            <div class="row items-start q-mb-lg">
                <!--Service-->
                <div class="col-12 col-md-6 col-lg-4 q-mr-md">
                    <ServiceSelectBox
                        @service-change="(s: number) => (serviceId = s)"
                        :for-scheduled-instructor="true"
                    />
                </div>

                <!--Epa-->
                <div class="col-12 col-md-6">
                    <q-select
                        label="Select EPA"
                        dense
                        options-dense
                        outlined
                        v-model="epa"
                        option-label="name"
                        option-value="epaId"
                        :options="epas"
                    >
                    </q-select>
                </div>
            </div>

            <q-banner
                inline-actions
                rounded
                v-if="success"
                class="bg-green text-white q-mb-md"
            >
                EPA Saved
                <template #action>
                    <q-btn
                        flat
                        label="Dismiss"
                        @click="success = false"
                    ></q-btn>
                </template>
            </q-banner>

            <div v-if="serviceId != null && epa?.epaId">
                <div class="row justify-between items-center q-mb-lg">
                    <div class="col-12 col-md-6">
                        <h2 class="epa text-weight-regular">{{ epa.name }}</h2>
                    </div>
                    <div class="col-12 col-md-6 text-right">
                        <StudentSelect
                            @student-change="(s: number) => (selectedStudentId = s)"
                            selected-filter="Service"
                            :service-id="serviceId"
                            :clear-student="clearStudent"
                            :borderless="true"
                            :outlined="false"
                        />
                    </div>
                </div>
                <!--Show form once student is selected-->
                <q-form
                    @submit="submitEpa"
                    v-bind="studentEpa"
                    v-show="selectedStudentId > 0"
                >
                    <div
                        class="bg-red-5 text-white q-pa-sm rounded q-mb-md"
                        v-if="submitErrors"
                    >
                        Please make sure you have selected a service, EPA, student, a level on the entrustment scale,
                        and entered a comment.
                    </div>
                    <LevelSelect
                        level-type="epa"
                        @level-change="(selectedLevelId: number) => (levelId = selectedLevelId)"
                        :clear-level="clearLevel"
                    />
                    <q-input
                        type="textarea"
                        outlined
                        dense
                        v-model="studentEpa.comment"
                        class="q-mb-md"
                        label="Comments: What should the student keep doing? How can they improve performance?"
                        :error="commentValidationMessage != ''"
                        :error-message="commentValidationMessage"
                    ></q-input>
                    <div class="column">
                        <q-btn
                            no-caps
                            label="Submit EPA"
                            type="submit"
                            padding="sm xl"
                            color="primary"
                            size="md"
                            class="self-center"
                        ></q-btn>
                    </div>
                </q-form>
            </div>
        </div>
    </div>
</template>

<style scoped>
h2.epa {
    font-weight: 400;
    font-size: 1.3rem;
    margin-bottom: 4px;
    line-height: normal;
}
</style>
