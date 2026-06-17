<template>
    <div class="q-pa-md">
        <h2 class="q-mt-none q-mb-md">Clinical Student Evaluation</h2>

        <!-- Success banner -->
        <q-banner
            v-if="success"
            class="bg-positive text-white q-mb-md"
            rounded
        >
            Evaluation submitted successfully.
            <template #action>
                <q-btn
                    flat
                    label="Dismiss"
                    @click="success = false"
                />
            </template>
        </q-banner>

        <template v-if="student">
            <!-- Student header -->
            <q-card
                flat
                bordered
                class="q-mb-md"
            >
                <q-item role="presentation">
                    <q-item-section
                        avatar
                        class="q-pr-md"
                    >
                        <q-avatar
                            size="90px"
                            square
                            class="fit"
                        >
                            <q-img
                                :src="`${PHOTO_BASE_URL}${student.mailId}&altphoto=1`"
                                :alt="`${student.name}'s photo`"
                            />
                        </q-avatar>
                    </q-item-section>
                    <q-item-section>
                        <q-item-label class="text-h6">{{ student.name }}</q-item-label>
                        <q-item-label caption>Clinical Rotation Evaluation</q-item-label>
                    </q-item-section>
                </q-item>
            </q-card>

            <q-banner
                dense
                rounded
                class="bg-ucdavis-blue-10 text-ucdavis-blue-100 q-mb-md"
            >
                <template #avatar>
                    <q-icon
                        name="info"
                        color="ucdavis-blue-60"
                    />
                </template>
                For each of the following competencies, please assess the student's current progression by providing the
                target milestone they have achieved. If you feel you have not observed a competency, mark NA.
            </q-banner>

            <!-- Scale questions -->
            <EvalScaleQuestion
                v-for="q in QUESTIONS"
                :key="q.id"
                :question-id="q.id"
                :question-text="q.text"
                :levels="q.levels"
                v-model="answers[q.id]"
            />

            <!-- Overall evaluation -->
            <q-card
                flat
                bordered
                class="q-mt-sm q-pa-md"
            >
                <div class="text-subtitle1 text-weight-bold q-mb-xs">Overall Rotation Evaluation</div>
                <p class="text-caption text-grey-7 q-mb-sm">
                    Based on the ability to apply and integrate knowledge and demonstrate skills and behaviors during
                    this rotation:
                </p>

                <div class="row q-mb-md">
                    <div class="col-12 col-sm-5 col-md-3">
                        <q-select
                            label="Overall Grade"
                            dense
                            options-dense
                            outlined
                            v-model="overallGrade"
                            :options="GRADE_OPTIONS"
                            option-label="label"
                            option-value="value"
                            emit-value
                            map-options
                        />
                    </div>
                </div>

                <q-input
                    type="textarea"
                    outlined
                    dense
                    autogrow
                    v-model="feedbackWell"
                    label="Things the student did well"
                    class="q-mb-sm"
                />
                <q-input
                    type="textarea"
                    outlined
                    dense
                    autogrow
                    v-model="feedbackImprove"
                    label="Opportunities for improvement"
                    class="q-mb-sm"
                />
                <q-input
                    type="textarea"
                    outlined
                    dense
                    autogrow
                    v-model="feedbackOther"
                    label="Any other feedback"
                    class="q-mb-md"
                />

                <div class="row justify-center">
                    <q-btn
                        no-caps
                        label="Submit Evaluation"
                        color="primary"
                        padding="sm xl"
                        @click="submit"
                    />
                </div>
            </q-card>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, inject, onMounted } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import type { MilestoneLevel } from "@/CTS/types"
import EvalScaleQuestion from "../components/EvalScaleQuestion.vue"

const { studentId } = defineProps<{
    studentId: string
}>()

const apiUrl = inject("apiURL") as string
const { get } = useFetch()

const PHOTO_BASE_URL = "https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid="

// TODO: replace with real API lookup by studentId
const students = [
    { id: "1", name: "Alice Nguyen", mailId: "alinguyen" },
    { id: "2", name: "Ben Okafor", mailId: "beokafor" },
    { id: "3", name: "Carmen Silva", mailId: "casilva" },
]

// TODO: milestoneId values are placeholders until questions are mapped to real CTS milestone bundles
const QUESTIONS = ref([
    {
        id: 17071,
        milestoneId: 1012,
        text: "1.1 Gathers and assimilates relevant information about animals",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17076,
        milestoneId: 1013,
        text: "1.2 Synthesizes and prioritizes problems to arrive at differential diagnoses",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17082,
        milestoneId: 1014,
        text: "1.3 Creates and adjusts a diagnostic and/or treatment plan based on available evidence",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17087,
        milestoneId: 1020,
        text: "2.2 Promotes comprehensive wellness and preventive care",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17093,
        milestoneId: 1023,
        text: "3.3 Advises stakeholders on practices that promote animal welfare",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17098,
        milestoneId: 1025,
        text: "4.2 Promotes the health and safety of people and the environment",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17104,
        milestoneId: 1026,
        text: "5.1 Listens attentively and communicates professionally",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17111,
        milestoneId: 1027,
        text: "5.2 Adapts communication style to diverse audiences",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17117,
        milestoneId: 1028,
        text: "5.3 Prepares documentation/forms appropriate for the intended audience",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17124,
        milestoneId: 1029,
        text: "6.1 Solicits, respects and integrates contributions from others",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17130,
        milestoneId: 1030,
        text: "6.2 Functions as leader or team member based on experience, skills and context",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17137,
        milestoneId: 1036,
        text: "7.4 Engages in self- directed learning",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17142,
        milestoneId: 1041,
        text: "8.3 Advocates for the health and safety of patients, clients, and members of the team within the workplace",
        levels: [] as MilestoneLevel[],
    },
    {
        id: 17143,
        milestoneId: 1042,
        text: "9.1 Practices evidence-based veterinary medicine (EBVM)",
        levels: [] as MilestoneLevel[],
    },
])

async function loadMilestoneLevels() {
    await Promise.all(
        QUESTIONS.value.map((q) =>
            get(apiUrl + "cts/milestones/" + q.milestoneId + "/levels").then((r) => {
                q.levels = r.result ?? []
            }),
        ),
    )
}

onMounted(loadMilestoneLevels)

const GRADE_OPTIONS = [
    { label: "A+ (Outstanding)", value: "A+" },
    { label: "A (Outstanding)", value: "A" },
    { label: "A- (High Quality)", value: "A-" },
    { label: "B+ (High Quality)", value: "B+" },
    { label: "B (Satisfactory)", value: "B" },
    { label: "B- (Satisfactory)", value: "B-" },
    { label: "C+ (Satisfactory)", value: "C+" },
    { label: "D (Marginal)", value: "D" },
    { label: "I (Incomplete)", value: "I" },
    { label: "F (Unsatisfactory)", value: "F" },
]

const answers = ref<Record<number, number | null>>({})
const overallGrade = ref<string | null>(null)
const feedbackWell = ref("")
const feedbackImprove = ref("")
const feedbackOther = ref("")
const success = ref(false)

const student = computed(() => students.find((s) => s.id === studentId) ?? null)

function submit() {
    success.value = true
    answers.value = {}
    overallGrade.value = null
    feedbackWell.value = ""
    feedbackImprove.value = ""
    feedbackOther.value = ""
}
</script>

<style scoped>
h2 {
    font-size: 1.4rem;
    font-weight: 400;
}
</style>

