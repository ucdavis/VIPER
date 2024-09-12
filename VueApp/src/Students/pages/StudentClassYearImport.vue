<script setup lang="ts">
    import { ref, inject, watch } from 'vue'
    import { useRoute } from 'vue-router'
    import type { Ref } from 'vue'
    import type { QTableProps } from 'quasar'
    import { useFetch } from '@/composables/ViperFetch'
    import { exportTable } from '@/composables/QuasarTableUtilities'
    import type { StudentClassYear, Student, StudentClassYearUpdate, StudentClassYearProblem } from '../types'

    type Term = { label: string, value: number }
    type ClassLevel = { label: string, value: string }
    const route = useRoute()
    const { get, post, put, del } = useFetch()
    const apiUrl = inject('apiURL')
    const viperUrl = inject('viperOneUrl')

    const classYear = ref(0)
    const term = ref({ label: "Spring 2024", value: 202402 }) as Ref<Term>
    const terms = ref([]) as Ref<Term[]>
    const classLevel =  ref({ label: "V4", value: "V4" }) as Ref<ClassLevel>
    const classLevels = [
        { label: "V1", value: "V1" },
        { label: "V2", value: "V2" },
        { label: "V3", value: "V3" },
        { label: "V4", value: "V4" }
    ] as ClassLevel[]
    const students = ref([]) as Ref<Student[]>
    const selectedStudents = ref([]) as Ref<number[]>
    const noStudentsFound = ref(false)

    async function getStudentsByTermAndClassLevel() {
        students.value = []
        noStudentsFound.value = false
        if (term.value?.value && classLevel.value?.value) {
            students.value = (await get(apiUrl + "students/dvm/byTermAndClassLevel/" + term.value.value + "/" + classLevel.value.value)).result
            noStudentsFound.value = (students.value.length == 0)
        }
    }
    async function importStudents() {
        await post(
            apiUrl + "students/dvm/" + classYear.value + "/import",
            selectedStudents.value
        )
        getStudentsByTermAndClassLevel()
        selectedStudents.value = []
    }
    function selectAll() {
        selectedStudents.value = students.value.reduce((ids, s) => {
            if (s.classYears.length == 0)
                ids.push(s.personId)
            return ids
        }, [] as number[])
    }

    function load() {
        classYear.value = parseInt(route.query.classYear as string)
        get(apiUrl + "curriculum/terms")
            .then(data => {
                terms.value = data.result
                    .map((t: any) => ({ label: t.description, value: t.termCode }))
            })
    }

    load()
</script>
<template>
    <div class="row">
        <div class="col">
            <h2>Student Class Year Import</h2>
        </div>
    </div>

    <q-form>
        <div class="row q-mb-md">
            <q-select label="Term Code" outlined dense options-dense :options="terms" v-model="term"
                      class="col col-md-2">
            </q-select>
            <q-select label="Class Level" outlined dense options-dense :options="classLevels" v-model="classLevel"
                      class="col col-md-1 q-ml-sm">
            </q-select>
            <q-btn label="Load student list" dense no-caps class="q-ml-sm q-px-sm"
                   :disabled="term.value == null || classLevel.value == ''"
                   :color="term.value == null || classLevel.value == '' ? 'white' : 'secondary'"
                   :text-color="term.value == null || classLevel.value == '' ? 'dark' : 'white'"
                   @click="getStudentsByTermAndClassLevel()"></q-btn>
            <q-btn label="Return to class list" dense no-caps class="q-ml-xl q-px-sm" color="secondary"
                   :href="'StudentClassYear?classYear=' + classYear"></q-btn>
        </div>

        <div v-if="students.length">
            <q-btn dense no-caps class="q-px-md q-mr-md" color="primary" label="Select All"
                   @click="selectAll()"></q-btn>
            <q-btn dense no-caps class="q-px-md" color="primary" label="Import Selected Students"
                   @click="importStudents()"></q-btn>
            <q-list dense>
                <q-item-label header background-color="secondary">
                    {{students.length}} Students {{classLevel.value}} in {{term.label}}
                </q-item-label>
                <q-item dense clickable v-ripple v-for="student in students">
                    <q-item-section side>
                        <q-checkbox v-model="selectedStudents" :val="student.personId" :disable="student.classYears.length != 0"></q-checkbox>
                    </q-item-section>
                    <q-item-section avatar>
                        <q-avatar square>
                            <img :src="viperUrl + 'public/utilities/getbase64image.cfm?altphoto=1&mailId=' + student.mailId" class="smallPhoto" />
                        </q-avatar>
                    </q-item-section>
                    <q-item-section>
                        <q-item-label>
                            {{student.lastName}}, {{student.firstName}}
                        </q-item-label>
                    </q-item-section>
                    <q-item-section v-if="student.classYears.length != 0">
                        Current: Class of {{student.classYear}}
                    </q-item-section>
                </q-item>
            </q-list>
        </div>
        <div v-if="noStudentsFound">
            No students found
        </div>
    </q-form>

</template>

