<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref, inject, watch } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import type { QTableProps } from 'quasar'
    import { useDateFunctions } from '@/composables/DateFunctions'
    import type { SessionCompetency, LegacyComptency, Course, Session, Term } from '@/CTS/types'
    import { useStorage } from '@vueuse/core'

    const apiUrl = inject('apiURL')
    const { get, createUrlSearchParams } = useFetch()
    const terms = ref([])
    const storedTerm = useStorage('course-competencies-term', {} as Term | null)
    const selectedTerm = ref()
    const courses = ref([]) as Ref<Course[]>
    const selectedCourse = ref() as Ref<Course>
    const sessions = ref([]) as Ref<Session[]>

    const loading = ref(false)

    function getCourses() {
        if (selectedTerm.value?.termCode) {
            storedTerm.value = selectedTerm.value
            get(apiUrl + "cts/courses/?termCode=" + selectedTerm.value?.termCode + "&subjectCode=VET")
                .then(r => courses.value = r.result)
        }
    }

    function getSessions() {
        if (selectedCourse.value.courseId) {
            get(apiUrl + "cts/courses/" + selectedCourse.value.courseId + "/sessions")
                .then(r => sessions.value = r.result)
        }
    }

    function getTerms() {
        get(apiUrl + "curriculum/terms")
            .then(r => terms.value = r.result)
            .then(() => selectedTerm.value = storedTerm.value || terms.value[0])
    }

    watch(selectedTerm, () => getCourses())
    watch(selectedCourse, () => getSessions())

    getTerms()
</script>
<template>
    <div class="row">
        <div class="col-12 col-sm-6 col-md-3 col-lg-2">
            <q-select dense options-dense outlined label="Term"
                      v-model="selectedTerm" :options="terms" option-label="description" option-value="termCode"></q-select>
        </div>
        <div class="col-12 col-sm-6 col-lg-4">
            <q-select dense options-dense outlined label="Course"
                      v-model="selectedCourse" :options="courses"
                      :option-label="(c) => c.title + ' - ' + c.competencyCount + ' Comps'"
                      option-value="courseId"></q-select>
        </div>
    </div>
    <div v-if="sessions.length" class="q-mt-md">
        <h4>Sessions in {{ selectedCourse.courseNum }}</h4>
        <table cellspacing="0" cellpadding="3">
            <thead>
                <tr>
                    <th class="text-left">Type</th>
                    <th class="text-left">Title</th>
                    <th class="text-left">Competencies</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="s in sessions">
                    <td>{{ s.type }} {{ s.typeOrder }}</td>
                    <td>{{ s.title }}</td>
                    <td>
                        <RouterLink :to="'ManageSesionCompetencies?sessionId=' + s.sessionId">
                            {{ s.competencyCount }}
                        </RouterLink>
                    </td>
                </tr>
            </tbody>
        </table>
        <!--
        <div v-for="s in sessions">
            {{ s.type }} {{ s.typeOrder }} {{ s.title }}
        </div>
        -->
    </div>
</template>