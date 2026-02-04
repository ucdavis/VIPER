<script setup lang="ts">
import type { Ref } from "vue"
import { ref, inject, watch } from "vue"
import { useRoute } from "vue-router"
import { useFetch } from "@/composables/ViperFetch"

import type { Course, Session, Term, Role } from "@/CTS/types"
import { useStorage } from "@vueuse/core"

const apiUrl = inject("apiURL")
const { get, put } = useFetch()
const route = useRoute()
const courseId = parseInt(route.query.courseId as string)
const terms = ref([])
const storedTerm = useStorage("course-competencies-term", {} as Term | null)
const selectedTerm = ref()
const courses = ref([]) as Ref<Course[]>
const selectedCourse = ref() as Ref<Course>
const sessions = ref([]) as Ref<Session[]>
const roles = ref([]) as Ref<Role[]>
const courseRoles = ref([]) as Ref<Role[]>
const courseRolesUpdate = ref([]) as Ref<number[]>
const showRolesForm = ref(false)

async function getCourses() {
    if (selectedTerm.value?.termCode) {
        storedTerm.value = selectedTerm.value
        var r = await get(apiUrl + "cts/courses/?termCode=" + selectedTerm.value?.termCode + "&subjectCode=VET")
        courses.value = r.result

        if (courseId) {
            var c = courses.value.find((crs) => crs.courseId === courseId)
            if (c !== undefined) {
                selectedCourse.value = c
            }
        }
    }
}

function getSessions() {
    if (selectedCourse.value.courseId) {
        get(apiUrl + "cts/courses/" + selectedCourse.value.courseId + "/sessions?supportedSessionTypes=true").then(
            (r) => (sessions.value = r.result),
        )
        getCourseRoles()
    }
}

function getTerms() {
    get(apiUrl + "curriculum/terms")
        .then((r) => (terms.value = r.result))
        .then(() => (selectedTerm.value = storedTerm.value || terms.value[0]))
}

function getRoles() {
    get(apiUrl + "cts/roles").then((r) => (roles.value = r.result))
}

function getCourseRoles() {
    get(apiUrl + "cts/courses/" + selectedCourse.value.courseId + "/roles")
        .then((r) => (courseRoles.value = r.result))
        .then(
            () =>
                (courseRolesUpdate.value = courseRoles.value.reduce<number[]>((result: number[], v: Role) => {
                    result.push(v.roleId)
                    return result
                }, [])),
        )
}

async function updateCourseRoles() {
    var result = await put(apiUrl + "cts/courses/" + selectedCourse.value.courseId + "/roles", courseRolesUpdate.value)
    if (result.success) {
        getCourseRoles()
        showRolesForm.value = false
    }
}

watch(selectedTerm, () => getCourses())
watch(selectedCourse, () => getSessions())

getTerms()
getRoles()
</script>
<template>
    <div class="row q-mb-md">
        <div class="col-12 col-sm-6 col-md-3 col-lg-2">
            <q-select
                dense
                options-dense
                outlined
                label="Term"
                v-model="selectedTerm"
                :options="terms"
                option-label="description"
                option-value="termCode"
            ></q-select>
        </div>
        <div class="col-12 col-sm-6 col-lg-4">
            <q-select
                dense
                options-dense
                outlined
                label="Course"
                v-model="selectedCourse"
                :options="courses"
                :option-label="(c) => c.title + ' - ' + c.competencyCount + ' Comps'"
                option-value="courseId"
            ></q-select>
        </div>
    </div>

    <div
        v-if="showRolesForm"
        class="q-mb-md"
    >
        <q-form @submit="updateCourseRoles">
            <div
                v-for="r in roles"
                :key="r.roleId"
            >
                <q-checkbox
                    v-model="courseRolesUpdate"
                    :val="r.roleId"
                    :label="r.name"
                >
                </q-checkbox>
            </div>
            <q-btn
                dense
                no-caps
                label="Update Roles"
                type="submit"
                class="q-px-md"
                color="primary"
            ></q-btn>
        </q-form>
    </div>

    <div
        v-if="selectedCourse !== null"
        class="q-mb-md"
    >
        <h4>
            Roles
            <q-btn
                dense
                outline
                no-caps
                label="Manage"
                color="secondary"
                icon="edit"
                @click="showRolesForm = true"
                class="q-px-md"
            ></q-btn>
        </h4>
        <div v-if="courseRoles.length">
            <div
                v-for="cr in courseRoles"
                :key="cr.roleId"
            >
                <q-icon name="role"></q-icon>{{ cr.name }}
            </div>
        </div>
        <div v-else>No roles assigned</div>
    </div>
    <div v-if="sessions.length">
        <h4>Sessions in {{ selectedCourse.courseNum }}</h4>
        <table
            cellspacing="0"
            cellpadding="3"
        >
            <thead>
                <tr>
                    <th class="text-left">Type</th>
                    <th class="text-left">Title</th>
                    <th class="text-left">Competencies</th>
                </tr>
            </thead>
            <tbody>
                <tr
                    v-for="s in sessions"
                    :key="s.sessionId"
                >
                    <td>{{ s.type }} {{ s.typeOrder }}</td>
                    <td>{{ s.title }}</td>
                    <td>
                        <RouterLink
                            :to="
                                'ManageSessionCompetencies?courseId=' +
                                selectedCourse.courseId +
                                '&sessionId=' +
                                s.sessionId
                            "
                        >
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
