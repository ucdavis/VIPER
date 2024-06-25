<script setup lang="ts">
    import type { Ref } from 'vue'
    import type { Level } from '@/CTS/types'
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
    })
    const emit = defineEmits(["levelChange"])

    //levels
    const levels = ref([]) as Ref<Level[]>
    const selectedLevel = ref({}) as Ref<Level>

    const baseUrl = import.meta.env.VITE_API_URL + "cts/levels"

    async function getLevels() {
        const { get, result } = useFetch()
        await get(baseUrl + "?" + props.levelType + "=true")
        levels.value = result.value
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
            </q-btn>
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
</style>