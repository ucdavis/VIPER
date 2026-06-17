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

            <!-- Scale questions -->
            <EvalScaleQuestion
                v-for="q in QUESTIONS"
                :key="q.id"
                :question-id="q.id"
                :question-text="q.text"
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
import { ref, computed } from "vue"
import EvalScaleQuestion from "../components/EvalScaleQuestion.vue"

const { studentId } = defineProps<{
    studentId: string
}>()

const PHOTO_BASE_URL = "https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid="

// TODO: replace with real API lookup by studentId
const students = [
    { id: "1", name: "Alice Nguyen", mailId: "alinguyen" },
    { id: "2", name: "Ben Okafor", mailId: "beokafor" },
    { id: "3", name: "Carmen Silva", mailId: "casilva" },
]

const QUESTIONS = [
    {
        id: 17071,
        text: "The student's ability to apply and integrate knowledge and understanding of disease mechanisms is",
    },
    { id: 17076, text: "The student's ability to obtain and document a patient history is" },
    { id: 17082, text: "The student's ability to obtain physical examination findings to determine patient status is" },
    { id: 17087, text: "The student's ability to apply clinical problem solving and judgment is" },
    {
        id: 17093,
        text: "The student's ability to synthesize and prioritize problems to create a precision care plan is",
    },
    {
        id: 17098,
        text: "The student's ability to perform/demonstrate technical skills needed in the diagnosis and management of diseases is",
    },
    { id: 17104, text: "The student's ability to demonstrate effective oral and written communication skills is" },
    { id: 17111, text: "The student's ability to access, organize and prioritize medical data and records is" },
    {
        id: 17117,
        text: "The student's ability to demonstrate personal, interpersonal and professional behaviors expected of a veterinarian is",
    },
    { id: 17124, text: "The student's ability to implement life-long learning skills is" },
    { id: 17130, text: "The student's ability to demonstrate knowledge and understanding of ethical principles is" },
    {
        id: 17137,
        text: "The student's ability to perform efficiently, utilize their healthcare team, consider animal welfare and client expectations and limitations is",
    },
    {
        id: 17142,
        text: "The student's ability to demonstrate knowledge and understanding of infectious disease control is",
    },
]

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

