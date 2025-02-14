<script setup lang="ts">
    import type { Ref } from 'vue'
    import type { Level, MilestoneLevel } from '@/CTS/types'
    import { ref, defineProps, defineEmits, watch } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'

    const props = defineProps({
        levelType: {
            type: String,
            required: true
        },
        clearLevel: {
            type: Boolean
        },
        levelId: {
            type: Number
        },
        milestoneLevels: {
            type: Array<MilestoneLevel>
        }
    })
    const emit = defineEmits(["levelChange"])

    //levels
    const levels = ref([]) as Ref<Level[]>
    const selectedLevel = ref({}) as Ref<Level>

    const baseUrl = import.meta.env.VITE_API_URL + "cts/levels"

    async function getLevels() {
        const { get } = useFetch()
        const r = await get(baseUrl + "?" + props.levelType + "=true")
        levels.value = r.result
        if (props.levelId) {
            const l = levels.value.find((l) => l.levelId == props.levelId)
            if (l) {
                selectedLevel.value = l
            }
        }
    }

    watch(selectedLevel, () => {
        emit("levelChange", selectedLevel.value.levelId)
    })
    watch(props, () => {
        if (props.clearLevel) {
            selectedLevel.value = {} as Level
        }
        if (props.levelId) {
            const l = levels.value.find((l) => l.levelId == props.levelId)
            if (l) {
                selectedLevel.value = l
            }
        }
    })

    getLevels()
</script>

<template>
    <div class="row q-mb-sm text-center gt-sm">
        <div class="col" v-for="level in levels">
            {{level.levelName}}
        </div>
    </div>
    <div class="row q-mb-md gt-sm">
        <div class="col q-mx-sm levelSelection" v-for="level in levels">
            <q-btn :label="level.order"
                   push
                   unelevated
                   flat
                   size="md"
                   dense
                   :class="selectedLevel.levelId == level.levelId ? 'selectedLevel q-py-sm' : 'q-py-sm'"
                   @click="selectedLevel = level">
                <q-tooltip v-if="props.levelType == 'epa'" class="text-dark bg-light-blue-3">
                    <template v-slot:default>
                        <span class="levelHover">{{ level.description }}</span>
                    </template>
                </q-tooltip>
            </q-btn>
            <template v-if="milestoneLevels != undefined">
                <div v-for="ml in milestoneLevels.filter(ml => ml.levelId == level.levelId)" class="q-px-sm">
                    {{ ml.description }}
                </div>
            </template>
        </div>
    </div>
    <div class="q-mb-sm text-center lt-md">
        <div class="q-mx-sm levelSelection" v-for="level in levels">
            <q-btn push
                   unelevated
                   flat
                   no-caps
                   size="md"
                   dense
                   :class="selectedLevel.levelId == level.levelId ? 'selectedLevel' : ''"
                   @click="selectedLevel = level">
                <template v-slot:default>
                    {{level.order}}. {{level.levelName}}
                </template>
            </q-btn>
            <template v-if="milestoneLevels != undefined">
                <div v-for="ml in milestoneLevels.filter(ml => ml.levelId == level.levelId)"
                     v-if="selectedLevel.levelId == level.levelId"
                     class="q-px-sm q-mb-md">
                    {{ ml.description }}
                </div>
            </template>
            <q-tooltip v-if="props.levelType == 'epa'" class="text-dark bg-light-blue-3">
                <template v-slot:default>
                    <span class="levelHover">{{ level.description }}</span>
                </template>
            </q-tooltip>
            <!--<template v-if="props.levelType == 'epa'">
        <div v-if="selectedLevel.levelId == level.levelId"
             class="q-px-sm q-mb-md">
            {{ level.description }}
        </div>
    </template>-->
        </div>
    </div>
</template>


<style scoped>
    div.levelSelection button {
        border: 1px solid rgb(30, 136, 229);
        color: rgb(30, 136, 229);
        width: 100%;
        margin-bottom: .2rem;
    }

    div.levelSelection button.selectedLevel {
        background-color: rgb(30, 136, 229);
        color: white;
    }

    .levelHover {
        font-size: .8rem;
    }
</style>