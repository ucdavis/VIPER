<script setup lang="ts">
    import { inject, ref, watch } from 'vue'
    import type { Ref } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import type { Level, Milestone, MilestoneLevel, MilestoneLevelUpdate } from '@/CTS/types'

    const { get, post, put, del } = useFetch()
    const apiUrl = inject('apiURL')
    const levels = ref([]) as Ref<Level[]>
    const milestones = ref([]) as Ref<Milestone[]>
    const milestone = ref(null) as Ref<Milestone | null>
    const milestoneLevels = ref([]) as Ref<MilestoneLevel[]>
    const milestoneLevelUpdate = ref([]) as Ref<MilestoneLevelUpdate[]>
    const loaded = ref(false)

    async function load() {
        await Promise.resolve([
            get(apiUrl + "cts/levels?milestone=true").then(r => levels.value = r.result),
            get(apiUrl + "cts/milestones").then(r => milestones.value = r.result)
        ])

        loaded.value = true
    }

    watch(milestone, () => {
        if (milestone.value != null) {
            get(apiUrl + "cts/milestones/" + milestone.value.milestoneId + "/levels")
                .then(r => milestoneLevels.value = r.result)
                .then(() => {
                    levels.value.forEach(l => {
                        if (milestoneLevels.value.findIndex(ml => ml.levelId == l.levelId) == -1) {
                            milestoneLevels.value.push({ levelId: l.levelId, levelName: l.levelName, levelOrder: l.order, milestoneId: milestone.value!.milestoneId, description: "", milestoneLevelId: null })
                        }
                    })
                })
        }
    })

    async function submitLevels() {
        if (milestone.value != null) {
            var result = await put(apiUrl + "cts/milestones/" + milestone.value.milestoneId + "/levels",
                milestoneLevels.value.map((ml: MilestoneLevel) => ({ levelId: ml.levelId, description: ml.description })))
            if (result.success) {
                milestone.value = null
                milestoneLevels.value = []
                load()
            }
        }
    }

    load()
</script>
<template>
    <h2>Edit Milestones</h2>
    <div class="row">
        <q-select dense outlined label="Select Milestone" class="col-12 col-md-6"
                  v-model="milestone" :options="milestones" option-label="name"></q-select>
    </div>
    <span>To create new milestones, use the Manage Bundles page to create a Milestone bundle.</span>

    <div v-if="milestone?.milestoneId" class="q-mt-md">
        <h3>Levels for {{ milestone.name }} {{ milestone.competencyName }}</h3>
        <q-form @submit="submitLevels">
            <div class="row" v-for="milestoneLevel in milestoneLevels">
                <div class="col-12 col-sm-6 col-lg-3">
                    {{ milestoneLevel.levelOrder }}. {{ milestoneLevel.levelName }}
                </div>
                <div class="col-12 col-sm-6 col-lg-4">
                    <q-editor v-model="milestoneLevel.description" outlined label="Description"></q-editor>
                </div>
            </div>
            <q-btn dense type="submit" label="submit" color="primary" class="q-px-sm"></q-btn>
        </q-form>
    </div>
</template>