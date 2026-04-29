<script setup lang="ts">
import { useQuasar } from "quasar"
import { ref, computed, inject } from "vue"
import type { Ref } from "vue"
import { useRoute } from "vue-router"
import { useUserStore } from "@/store/UserStore"
import { useFetch } from "@/composables/ViperFetch"
import type { Assessment, Epa, Level, Person } from "@/CTS/types"
import { useDateFunctions } from "@/composables/DateFunctions"
import AssessmentBubble from "@/CTS/components/AssessmentBubble.vue"
import EpaVoiceThread from "@/CTS/components/EpaVoiceThread.vue"
import EpaSupervisionBlend from "@/CTS/components/EpaSupervisionBlend.vue"
import { getLevelLabel } from "@/CTS/utils/level-labels"

type DisplayStyle = 1 | 2 | 3 | 4 | 5

const userStore = useUserStore()
const { get } = useFetch()
const { formatDate } = useDateFunctions()

const apiUrl = inject("apiURL")
const person = ref(null) as Ref<Person | null>
let studentUserId = userStore.userInfo.userId
const showPersonName = ref(false)
const epaAssessments = ref([]) as Ref<Assessment[]>
const epaAssessment = ref() as Ref<Assessment>
const epas = ref([]) as Ref<Epa[]>
const levels = ref([]) as Ref<Level[]>
const loaded = ref(false)
const showDetails = ref([]) as Ref<boolean[]>

const showAssessmentDetail = ref(false)
const displayStyle = ref<DisplayStyle>(2)

const styleOptions = [
    { label: "1. Original circles", value: 1 as DisplayStyle },
    { label: "2. Current bubbles", value: 2 as DisplayStyle },
    { label: "3. Abbreviations", value: 3 as DisplayStyle },
    { label: "4. Timeline", value: 4 as DisplayStyle },
    { label: "5. Bar", value: 5 as DisplayStyle },
]

const bubbleDisplayStyle = computed<"legacy" | "current" | "abbrev">(() => {
    if (displayStyle.value === 1) return "legacy"
    if (displayStyle.value === 3) return "abbrev"
    return "current"
})

const isTimelineView = computed(() => displayStyle.value === 4)
const isBlendView = computed(() => displayStyle.value === 5)
const isCardListView = computed(() => isTimelineView.value || isBlendView.value)

const anyExpanded = computed(() => showDetails.value.some((s) => s))

async function load() {
    const $q = useQuasar()
    $q.loading.show({
        message: "Loading",
        delay: 250, // ms
    })

    const route = useRoute()
    const params = route.query
    if (params.student !== null && params.student !== undefined) {
        const result = await get(apiUrl + "cts/permissions?access=ViewStudentAssessments&studentId=" + params.student)
        const canAccess = result.result.hasAccess
        const studentParam = parseInt(params.student.toString())
        if (!Number.isNaN(studentParam) && canAccess) {
            studentUserId = studentParam
            showPersonName.value = true
        }
    }
    let p1 = get(apiUrl + "cts/epas").then((r) => (epas.value = r.result))
    let p2 = get(apiUrl + "cts/levels").then((r) => (levels.value = r.result))
    let p3 = get(apiUrl + "people/" + studentUserId).then((r) => (person.value = r.result))
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
    epaAssessments.value = r.result.sort((e1: Assessment, e2: Assessment) => {
        if (e1.epaName === null || e2.epaName === null || e1.epaName === e2.epaName) {
            return new Date(e2.encounterDate).getTime() - new Date(e1.encounterDate).getTime()
        }
        return e1.epaName.localeCompare(e2.epaName)
    })
}

function getAssessmentsForEpa(epaId: number | null) {
    return epaAssessments.value.filter((e) => e.epaId === epaId)
}

function getText(date: Date, enteredBy: string, levelName: string, comment: string | null, serviceName: string | null) {
    return (
        levelName +
        "\n" +
        (comment !== null ? comment : "") +
        "\n" +
        formatDate(date.toString()) +
        " " +
        enteredBy +
        " " +
        "\n" +
        serviceName +
        "\n"
    )
}

function toggleExpandAll() {
    const expand = !anyExpanded.value
    for (let i = 0; i < showDetails.value.length; i++) {
        showDetails.value[i] = expand
    }
}

function handleAssessmentClick(id: number) {
    epaAssessment.value = epaAssessments.value.find((a) => a.encounterId === id)!
    showAssessmentDetail.value = true
}

load()
</script>
<template>
    <div v-if="loaded">
        <h1>
            <span v-if="showPersonName && person != null"
                >Assessments for {{ person.firstName }} {{ person.lastName }}</span
            >
            <span v-else>My Assessments</span>
        </h1>

        <q-dialog
            v-model="showAssessmentDetail"
            aria-labelledby="assessment-detail-title"
        >
            <q-card style="width: 700px; max-width: 80vw">
                <q-card-section class="row items-center q-pb-none">
                    <div
                        id="assessment-detail-title"
                        class="text-h6"
                    >
                        Assessment Details
                    </div>
                    <q-space />
                    <q-btn
                        icon="close"
                        flat
                        round
                        dense
                        aria-label="Close dialog"
                        v-close-popup
                    />
                </q-card-section>
                <q-card-section>
                    <div class="row">
                        <div class="col-12"><strong>EPA:</strong> {{ epaAssessment.epaName }}</div>
                    </div>
                    <div class="row">
                        <div class="col-12">
                            <strong>Rating:</strong>
                            <span :class="['levelChip', 'levelChip--' + epaAssessment.levelValue, 'q-ml-sm']">
                                {{ epaAssessment.levelName }}
                            </span>
                        </div>
                    </div>
                    <div class="row q-mt-xs">
                        <div class="col-12"><strong>Comment:</strong> {{ epaAssessment.comment }}</div>
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
                        <div class="col-12"><strong>Service:</strong> {{ epaAssessment.serviceName }}</div>
                    </div>
                </q-card-section>
            </q-card>
        </q-dialog>

        <div class="q-mb-md">
            <q-btn-toggle
                v-model="displayStyle"
                :options="styleOptions"
                color="white"
                text-color="primary"
                toggle-color="primary"
                toggle-text-color="white"
                unelevated
                no-caps
                dense
                spread
                aria-label="Assessment display style"
                class="assessmentStyleToggle"
            />
        </div>

        <template v-if="!isCardListView">
            <div class="row items-center">
                <div class="expandToggleCol">
                    <q-btn
                        dense
                        color="secondary"
                        :icon="anyExpanded ? 'expand_less' : 'expand_more'"
                        :aria-label="anyExpanded ? 'Collapse all EPAs' : 'Expand all EPAs'"
                        @click="toggleExpandAll()"
                    ></q-btn>
                </div>
                <div class="col col-md-10 col-lg-7 q-ml-md">
                    <h2>Entrustable Professional Activities</h2>
                </div>
            </div>
            <div
                v-for="(epa, index) in epas"
                :key="epa.epaId ?? `epa-${index}`"
                class="row q-mt-sm q-pt-sm assessmentGroup"
            >
                <div class="expandToggleCol">
                    <q-btn
                        dense
                        :icon="showDetails[index] ? 'expand_less' : 'expand_more'"
                        :aria-label="`${showDetails[index] ? 'Collapse' : 'Expand'} details for ${epa.name}`"
                        :aria-expanded="showDetails[index]"
                        @click="showDetails[index] = !showDetails[index]"
                        v-if="getAssessmentsForEpa(epa.epaId).length > 0"
                    />
                </div>
                <div class="col col-md-10 col-lg-7 q-ml-md">
                    <div class="row items-center">
                        <div class="col-12 col-sm q-mr-sm">
                            {{ epa.name }}
                        </div>
                        <div class="col-12 col-sm-auto">
                            <AssessmentBubble
                                :max-value="5"
                                :value="a.levelValue"
                                :level-name="a.levelName"
                                :display-style="bubbleDisplayStyle"
                                :abbreviation="getLevelLabel(a.levelValue).abbreviation"
                                :text="
                                    getText(a.encounterDate, a.enteredByName, a.levelName, a?.comment, a?.serviceName)
                                "
                                :id="a.encounterId"
                                @bubble-click="handleAssessmentClick"
                                v-for="a in getAssessmentsForEpa(epa.epaId)"
                                :key="a.encounterId"
                            />
                        </div>
                    </div>
                    <q-slide-transition>
                        <div
                            class="q-mb-md"
                            v-if="showDetails[index]"
                            :key="'epadetails' + index"
                        >
                            <div
                                v-for="a in getAssessmentsForEpa(epa.epaId)"
                                :key="a.encounterId"
                                class="row q-mb-sm items-center q-col-gutter-sm"
                            >
                                <div class="col-12 col-sm-6 col-md-4">
                                    {{ formatDate(a.encounterDate.toString()) }}
                                    {{ a.enteredByName }}
                                </div>
                                <div class="col-12 col-sm-6 col-md-4">
                                    {{ a.serviceName }}
                                </div>
                                <div class="col-12 col-sm-auto">
                                    <span
                                        v-if="displayStyle === 1"
                                        class="text-grey-8"
                                    >
                                        {{ a.levelName }}
                                    </span>
                                    <span
                                        v-else
                                        :class="['levelChip', 'levelChip--' + a.levelValue]"
                                    >
                                        {{ a.levelName }}
                                    </span>
                                </div>
                                <div
                                    v-if="a.comment"
                                    class="col-12 q-mt-xs text-grey-8 assessmentComment"
                                >
                                    {{ a.comment }}
                                </div>
                            </div>
                        </div>
                    </q-slide-transition>
                </div>
            </div>
        </template>

        <template v-else>
            <h2 class="q-mb-md">Entrustable Professional Activities</h2>
            <template v-if="isTimelineView">
                <EpaVoiceThread
                    v-for="epa in epas"
                    :key="epa.epaId ?? epa.name"
                    :epa="epa"
                    :assessments="getAssessmentsForEpa(epa.epaId)"
                />
            </template>
            <template v-else>
                <EpaSupervisionBlend
                    v-for="epa in epas"
                    :key="epa.epaId ?? epa.name"
                    :epa="epa"
                    :assessments="getAssessmentsForEpa(epa.epaId)"
                />
            </template>
        </template>
    </div>
</template>
