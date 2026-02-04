<script setup lang="ts">
import { inject, ref, watch } from "vue"
import type { Ref } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import type { Competency, Level, Milestone, MilestoneLevel } from "@/CTS/types"

const { get, post, put } = useFetch()
const apiUrl = inject("apiURL")
const levels = ref([]) as Ref<Level[]>
const competencies = ref([]) as Ref<Competency[]>
const competencyId = ref()
const milestones = ref([]) as Ref<Milestone[]>
const milestone = ref(null) as Ref<Milestone | null>
const milestoneLevels = ref([]) as Ref<MilestoneLevel[]>
const loaded = ref(false)

async function load() {
    await Promise.resolve([
        get(apiUrl + "cts/levels?milestone=true").then((r) => (levels.value = r.result)),
        get(apiUrl + "cts/milestones").then((r) => (milestones.value = r.result)),
        get(apiUrl + "cts/competencies").then((r) => (competencies.value = r.result)),
    ])

    //filter competencies to just those without a milestone already defined
    competencies.value = competencies.value.filter(
        (c) => !milestones.value.some((m) => m.competencyId === c.competencyId),
    )

    loaded.value = true
}

watch(milestone, () => {
    if (milestone.value !== null) {
        get(apiUrl + "cts/milestones/" + milestone.value.milestoneId + "/levels")
            .then((r) => (milestoneLevels.value = r.result))
            .then(() => {
                levels.value.forEach((l) => {
                    if (milestoneLevels.value.findIndex((ml) => ml.levelId === l.levelId) === -1) {
                        milestoneLevels.value.push({
                            levelId: l.levelId,
                            levelName: l.levelName,
                            levelOrder: l.order,
                            milestoneId: milestone.value!.milestoneId,
                            description: "",
                            milestoneLevelId: null,
                        })
                    }
                })
            })
    }
})

async function submitLevels() {
    if (milestone.value !== null) {
        var putResult = await put(
            apiUrl + "cts/milestones/" + milestone.value.milestoneId + "/levels",
            milestoneLevels.value.map((ml: MilestoneLevel) => ({ levelId: ml.levelId, description: ml.description })),
        )
        if (putResult.success) {
            milestone.value = null
            milestoneLevels.value = []
            load()
        }
    }
}

async function createMilestone() {
    if (competencyId.value > 0) {
        let comp = competencies.value.find((c) => c.competencyId === competencyId.value)
        if (comp !== undefined) {
            let bundle = {
                bundleId: null,
                assessment: false,
                clinical: false,
                milestone: true,
                name: "Milestone " + comp.number + " " + comp.name,
                roles: [],
            }
            let r = await post(apiUrl + "cts/bundles/", bundle)
            if (r.success) {
                let bundleCompetency = {
                    bundleCompetencyId: null,
                    bundleId: r.result.bundleId,
                    competencyId: competencyId.value,
                    levelIds: levels.value.reduce((levelIds: number[], l) => {
                        levelIds.push(l.levelId)
                        return levelIds
                    }, []),
                    roleId: null,
                    bundleCompetencyGroupId: null,
                    order: 1,
                }
                await post(apiUrl + "cts/bundles/" + r.result.bundleId + "/competencies", bundleCompetency)
                get(apiUrl + "cts/milestones").then((res) => (milestones.value = res.result))
                get(apiUrl + "cts/competencies").then((res) => (competencies.value = res.result))
            }
        }
    }
}

load()
</script>
<template>
    <h2>Edit Milestones</h2>

    <q-form @submit="createMilestone">
        <div class="row items-center">
            <div class="col-12 col-md-6 col-lg-4">
                <q-select
                    dense
                    options-dense
                    outlined
                    label="Select Competency"
                    :options="competencies"
                    :option-label="(opt) => opt.number + ' ' + opt.name"
                    option-value="competencyId"
                    v-model="competencyId"
                    emit-value
                    map-options
                >
                </q-select>
            </div>
            <div class="col q-ml-md">
                <q-btn
                    icon="add"
                    label="Add Milestone"
                    type="submit"
                    color="green"
                ></q-btn>
            </div>
        </div>
    </q-form>

    <q-separator spaced="lg" />

    <div class="row">
        <q-select
            dense
            options-dense
            outlined
            label="Select Milestone"
            class="col-12 col-md-6"
            v-model="milestone"
            :options="milestones"
            option-label="name"
        ></q-select>
    </div>

    <div
        v-if="milestone?.milestoneId"
        class="q-mt-md"
    >
        <h3>Levels for {{ milestone.name }}</h3>
        <q-form @submit="submitLevels">
            <div
                class="row"
                v-for="milestoneLevel in milestoneLevels"
                :key="milestoneLevel.levelId"
            >
                <div class="col-12 col-sm-6 col-lg-2">
                    {{ milestoneLevel.levelOrder }}. {{ milestoneLevel.levelName }}
                </div>
                <div class="col-12 col-sm-6 col-lg-6">
                    <q-editor
                        v-model="milestoneLevel.description"
                        outlined
                        label="Description"
                        :toolbar="[
                            ['left', 'center', 'right', 'justify'],
                            ['bold', 'italic', 'underline', 'strike'],
                            ['quote', 'unordered', 'ordered', 'outdent', 'indent'],
                            ['undo', 'redo'],
                            ['viewsource'],
                        ]"
                    />
                </div>
            </div>
            <q-btn
                dense
                type="submit"
                label="submit"
                color="primary"
                class="q-px-sm"
            ></q-btn>
        </q-form>
    </div>
</template>
