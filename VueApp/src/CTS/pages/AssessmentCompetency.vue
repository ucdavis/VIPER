<script setup lang="ts">
import { ref } from "vue"
import type { Ref } from "vue"
import { useRoute } from "vue-router"
import type { Competency, MilestoneLevel } from "@/CTS/types"
import StudentSelect from "@/components/StudentSelect.vue"
import LevelSelect from "@/CTS/components/LevelSelect.vue"

const route = useRoute()

const competency = ref({
    competencyId: 0,
    name: "Listens attentively and communicates professionally",
    number: "5.1.",
}) as Ref<Competency>
const clearStudent = ref(false)
const autoSelectedStudent = ref(
    route.query.studentId !== undefined && route.query.studentId !== null
        ? parseInt(route.query.studentId.toString())
        : 0,
)
const selectedStudentId = ref(autoSelectedStudent)
const studentMilestone = ref({}) as Ref<any>
const submitErrors = ref()
const levelId = ref(0)
const clearLevel = ref(false)
const milestoneLevels = ref([
    {
        milestoneLevelId: 0,
        milestoneId: 0,
        levelId: 9,
        levelName: "Novice",
        levelOrder: 1,
        description:
            "Communicates primarily unidirectionally with limited active listening. Communicates well with scripted plan but may falter when confronted with unexpected variables in the workplace.",
    },
    {
        milestoneLevelId: 0,
        milestoneId: 0,
        levelId: 10,
        levelName: "Advanced Beginner",
        levelOrder: 2,
        description: "Actively listens and fosters bidirectional communication in most situations.",
    },
    {
        milestoneLevelId: 0,
        milestoneId: 0,
        levelId: 12,
        levelName: "Competent",
        levelOrder: 3,
        description: "Consistently communicates bidirectionally and professionally.",
    },
    {
        milestoneLevelId: 0,
        milestoneId: 0,
        levelId: 13,
        levelName: "Proficient",
        levelOrder: 4,
        description: "Communicates with confidence and ease regardless of situation.",
    },
]) as Ref<MilestoneLevel[]>

function submitMilestone() {}
</script>
<template>
    <h2>Competency Assessment</h2>
    <div class="row justify-between items-center q-mb-lg">
        <div class="col-12 col-md-6">
            <h2 class="epa text-weight-regular">{{ competency.number }} {{ competency.name }}</h2>
        </div>
        <div class="col-12 col-md-6 text-right">
            <StudentSelect
                @student-change="(s: number) => (selectedStudentId = s)"
                selected-filter="All"
                :clear-student="clearStudent"
                :borderless="false"
                :outlined="true"
                :auto-select-student="autoSelectedStudent"
            />
        </div>
    </div>
    <q-form
        @submit="submitMilestone"
        v-bind="studentMilestone"
        v-show="selectedStudentId > 0"
    >
        <div
            class="bg-red-5 text-white q-pa-sm rounded q-mb-md"
            v-if="submitErrors?.message?.length > 0"
        >
            {{ submitErrors.message }}
            Please make sure you have selected a service, EPA, student, and a level on the entrustment scale.
        </div>
        <LevelSelect
            level-type="milestone"
            @level-change="(selectedLevelId: number) => (levelId = selectedLevelId)"
            :clear-level="clearLevel"
            :milestone-levels="milestoneLevels"
        />
        <div class="row">
            <div class="col-12 q-mx-sm">
                <q-input
                    type="textarea"
                    outlined
                    dense
                    v-model="studentMilestone.comment"
                    class="q-mb-md"
                    label="Comments: What should the student keep doing? How can they improve performance?"
                ></q-input>
            </div>
        </div>
        <div class="column">
            <q-btn
                no-caps
                label="Submit Milestone"
                type="submit"
                padding="sm xl"
                color="primary"
                size="md"
                class="self-center"
            ></q-btn>
        </div>
    </q-form>
</template>
