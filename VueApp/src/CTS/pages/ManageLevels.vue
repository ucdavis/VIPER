<script setup lang="ts">
import type { Ref } from "vue"
import { ref, inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"

type Level = {
    levelId: number
    levelName: string
    description: string
    active: boolean
    order: number
    epa: boolean
    course: boolean
    clinical: boolean
    milestone: boolean
    dops: boolean
}

const emptyLevel = {
    levelId: 0,
    levelName: "",
    description: "",
    active: true,
    order: 0,
    epa: false,
    course: false,
    clinical: false,
    milestone: false,
    dops: false,
}
const level = ref(emptyLevel) as Ref<Level>
const levels = ref([]) as Ref<Level[]>
const type = ref("EPA")
const levelTypes = [
    { label: "EPA", value: "EPA" },
    { label: "DOPS", value: "DOPS" },
    { label: "Clinical", value: "Clinical" },
    { label: "Course", value: "Course" },
    { label: "Milestone", value: "Milestone" },
]
const showForm = ref(false)
const levelUrl = inject("apiURL") + "cts/levels"

async function loadLevels() {
    const { get } = useFetch()
    get(levelUrl).then(({ result }) => (levels.value = result))
    level.value = emptyLevel
}

async function submitLevel() {
    const { post, put } = useFetch()
    let success
    if (level.value.levelId) {
        const r = await put(levelUrl + "/" + level.value.levelId, level.value)
        success = r.success
    } else {
        level.value.epa = type.value === "EPA"
        level.value.clinical = type.value === "Clinical"
        level.value.course = type.value === "Course"
        level.value.dops = type.value === "DOPS"
        level.value.milestone = type.value === "Milestone"
        const r = await post(levelUrl, level.value)
        success = r.success
    }

    if (success) {
        loadLevels()
    }
}

async function deleteLevel() {
    const { del } = useFetch()
    const r = await del(levelUrl + "/" + level.value.levelId)
    if (r.success) {
        loadLevels()
    }
}

function filterLevels() {
    switch (type.value) {
        case "EPA":
            return levels.value.filter((l) => l.epa)
        case "Milestone":
            return levels.value.filter((l) => l.milestone)
        case "DOPS":
            return levels.value.filter((l) => l.dops)
        case "Clinical":
            return levels.value.filter((l) => l.clinical)
        case "Course":
            return levels.value.filter((l) => l.course)
        default:
            return []
    }
}

function editLevel(l: Level) {
    level.value = l
    showForm.value = true
}

loadLevels()
</script>

<template>
    <h2>Manage Levels</h2>
    <q-form
        action=""
        @submit="submitLevel"
    >
        <div class="row items-start">
            <q-btn-toggle
                name="type"
                v-model="type"
                :options="levelTypes"
                size="large"
                outlined
                no-caps
                push
                toggle-color="primary"
                class="q-mb-md"
            ></q-btn-toggle>
            <q-btn
                no-caps
                dense
                color="primary"
                icon="add"
                label="Add level"
                class="q-ml-md q-mt-xs q-px-md q-py-sm"
                @click="showForm = true"
            ></q-btn>
        </div>
        <div v-if="Object.keys(level).length">
            {{ level?.levelId ? "Update" : "Add" }} Level
            <div class="row">
                <q-toggle
                    outlined
                    dense
                    label="Active"
                    type="checkbox"
                    class="col col-md-8 col-lg-4"
                    v-model="level.active"
                ></q-toggle>
            </div>
            <div class="row">
                <q-input
                    outlined
                    dense
                    label="Order"
                    type="number"
                    step="1"
                    class="col col-md-2 col-lg-1"
                    v-model="level.order"
                ></q-input>
            </div>
            <div class="row">
                <q-input
                    outlined
                    dense
                    label="Name"
                    type="text"
                    class="col col-md-8 col-lg-4"
                    v-model="level.levelName"
                ></q-input>
            </div>
            <div class="row">
                <q-input
                    outlined
                    dense
                    label="Description "
                    type="textarea"
                    class="col col-md-8 col-lg-4"
                    v-model="level.description"
                ></q-input>
            </div>
            <div class="row">
                <q-btn
                    dense
                    no-caps
                    type="submit"
                    :label="(level?.levelId ? 'Update' : 'Add') + ' Level'"
                    color="primary"
                    class="q-mt-sm col col-4 col-md-1"
                ></q-btn>
                <q-btn
                    dense
                    no-caps
                    type="button"
                    label="Delete Level"
                    color="red"
                    class="q-mt-sm q-ml-lg col col-4 col-md-1"
                    @click="deleteLevel"
                ></q-btn>
            </div>
        </div>

        <div></div>

        <q-list v-if="type !== ''">
            <q-item-label
                header
                class="text-dark text-h6"
            >
                {{ type }} Levels
            </q-item-label>
            <q-item
                v-for="l in filterLevels()"
                :key="l.levelId"
            >
                <q-item-section
                    side
                    top
                >
                    <q-btn
                        dense
                        no-caps
                        size="md"
                        color="primary"
                        icon="edit"
                        @click="editLevel(l)"
                    ></q-btn>
                </q-item-section>
                <q-item-section
                    side
                    top
                >
                    <q-icon
                        :name="l.active ? 'check' : 'close'"
                        :color="l.active ? 'green' : 'red'"
                    ></q-icon>
                </q-item-section>
                <q-item-section
                    side
                    top
                >
                    {{ l.order }}
                </q-item-section>
                <q-item-section top>
                    {{ l.levelName }}
                </q-item-section>
                <q-item-section top>
                    {{ l.description }}
                </q-item-section>
            </q-item>
        </q-list>
    </q-form>
</template>
