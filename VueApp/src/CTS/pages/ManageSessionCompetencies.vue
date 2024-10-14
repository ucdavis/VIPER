<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref, inject, watch, computed } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import { useRoute, useRouter } from 'vue-router'

    import type { SessionCompetency, LegacyComptency, SessionCompetencyAddUpdate, Session, Role, Competency, Level } from '@/CTS/types'
    import { useStorage } from '@vueuse/core'

    const apiUrl = inject('apiURL')
    const { get, post, put, del } = useFetch()
    const route = useRoute()
    const router = useRouter()
    const courseId = parseInt(route.query.courseId as string)
    const sessionId = parseInt(route.query.sessionId as string)
    const sessionCompetencies = ref([]) as Ref<SessionCompetency[]>
    const legacyCompetencies = ref([]) as Ref<LegacyComptency[]>
    const session = ref({}) as Ref<Session>
    const roles = ref([]) as Ref<Role[]>
    const showLegacyComps = ref(false)
    const showCompForm = ref(false)
    const compAddUpdate = ref({ sessionCompetencyId: null, sessionId: sessionId, competencyId: null, order: null, levelIds: [], roleId: null }) as Ref<SessionCompetencyAddUpdate>
    const compAddUpdateRolesAndLevels = ref([]) as Ref<string[]>
    const allComps = ref([]) as Ref<Competency[]>
    const filteredComps = ref([]) as Ref<Competency[]>
    const levels = ref([]) as Ref<Level[]>
    const multiRole = computed(() => roles.value.length && session.value.multiRole)

    function getRoles() {
        get(apiUrl + "cts/courses/" + courseId + "/roles").then(r => roles.value = r.result)
    }

    function getSession() {
        get(apiUrl + "cts/courses/" + courseId + "/sessions/" + sessionId)
            .then(r => session.value = r.result)
    }

    function getSessionCompetencies() {
        get(apiUrl + "cts/courses/" + courseId + "/sessions/" + sessionId + "/competencies")
            .then(r => sessionCompetencies.value = r.result)

    }

    function getLegacySessionCompetencies() {
        get(apiUrl + "cts/legacyCompetencies/session/" + sessionId)
            .then(r => legacyCompetencies.value = r.result.filter((c: LegacyComptency) => c.dvmCompetencyId != null))
    }

    function getComps() {
        get(apiUrl + "cts/competencies")
            .then(r => allComps.value = r.result)
    }

    function getLevels() {
        get(apiUrl + "cts/levels?course=true")
            .then(r => levels.value = r.result)
    }

    //search/filter in comp search box
    function filterComps(val: any, update: any, abort: any) {
        update(() => {
            const srch = val.toLowerCase()
            filteredComps.value = allComps.value.filter(v => (v.number.toLowerCase() + " " + v.name.toLowerCase()).indexOf(srch) > -1)
        })
    }

    async function submitSessionComp() {
        var postOrPut = compAddUpdate.value.sessionCompetencyId == null ? post : put
        var success = false
        if (multiRole.value) {
            success = true
            //submit levels for each role
            for (var role of roles.value) {
                //get level ids for this role
                compAddUpdate.value.levelIds = compAddUpdateRolesAndLevels.value.reduce<number[]>((result: number[], val: string) => {
                    var rid = val.split("-")[0]
                    if (parseInt(rid) == role.roleId) {
                        result.push(parseInt(val.split("-")[1]))

                    }
                    return result
                }, [])
                if (compAddUpdate.value.levelIds.length) {
                    compAddUpdate.value.roleId = role.roleId
                    var r = await postOrPut(apiUrl + "cts/courses/" + courseId + "/sessions/" + sessionId + "/competencies", compAddUpdate.value)
                    success = success && r.success
                }
            }
        }
        else {
            var r = await postOrPut(apiUrl + "cts/courses/" + courseId + "/sessions/" + sessionId + "/competencies", compAddUpdate.value)
            success = r.success
        }

        console.log(success)
        if (success) {
            compAddUpdate.value = { sessionCompetencyId: null, sessionId: sessionId, competencyId: null, order: null, levelIds: [], roleId: null }
            compAddUpdateRolesAndLevels.value = []
            showCompForm.value = false
            getSessionCompetencies()
        }
    }

    function selectComp(sc: SessionCompetency) {
        compAddUpdate.value.sessionCompetencyId = sc.sessionCompetencyId
        compAddUpdate.value.competencyId = sc.competencyId

        var compValues = sessionCompetencies.value
            .filter((c: SessionCompetency) => c.competencyId == sc.competencyId)

        if (multiRole.value) {
            //get all levelids for all roles for this comp
            compAddUpdateRolesAndLevels.value = compValues
                .reduce<string[]>((result, c) => {
                    c.levels?.forEach(level => result.push(c.roleId + "-" + level.levelId))
                    return result
                }, [])
        }
        else {
            //get all level ids currently selected for this comp
            compAddUpdate.value.levelIds = compValues.length
                ? compValues[0].levels
                    .reduce<number[]>((result, l) => { result.push(l.levelId); return result; }, [])
                : []
        }
        showCompForm.value = true
    }

    async function delComp(sc: SessionCompetency) {
        var u = apiUrl + "cts/courses/" + courseId + "/sessions/" + sessionId + "/competencies/" + sc.competencyId
        if (sc.roleId != null) {
            u += "?roleId=" + sc.roleId
        }
        var r = await del(u)
        getSessionCompetencies()
    }

    if (!courseId || !sessionId) {
        router.push("ManageCourseCompetencies")
    }
    else {
        watch(showLegacyComps, () => getLegacySessionCompetencies())
        getRoles()
        getSession()
        getComps()
        getLevels()
        getSessionCompetencies()
    }
</script>
<template>
    <h2>Competencies for {{ session.courseTitle }} {{ session.type }} {{ session.typeOrder}} {{ session.title }}</h2>

    <q-dialog v-model="showCompForm">
        <q-card style="width: 800px;max-width: 80vw;" class="q-pa-md">
            <h3>{{ compAddUpdate?.sessionCompetencyId ? 'Update' : 'Add' }} Session Competency</h3>
            <q-form @submit="submitSessionComp">
                <div class="row">
                    <div class="col-12">
                        <q-select dense options-dense outlined label="Competency"
                                  v-model="compAddUpdate.competencyId" :readonly="compAddUpdate.sessionCompetencyId != null"
                                  :options="allComps" :option-label="opt => opt.number + ' ' + opt.name" option-value="competencyId"
                                  emit-value map-options
                                  use-input input-debounce="0" @filter="filterComps"></q-select>
                    </div>
                </div>
                <div class="row" v-if="!multiRole">
                    <div class="col" v-for="l in levels">
                        <q-checkbox v-model="compAddUpdate.levelIds" :label="l.levelName" :val="l.levelId"></q-checkbox>
                    </div>
                </div>
                <div class="row items-center roleLevelSelect" v-if="multiRole" v-for="r in roles">
                    <div class="col">
                        {{ r.name }}
                    </div>
                    <div class="col" v-for="l in levels">
                        <q-checkbox v-model="compAddUpdateRolesAndLevels" :label="l.levelName" :val="r.roleId + '-' + l.levelId"></q-checkbox>
                    </div>
                </div>
                <div class="row">
                    <q-btn dense no-caps label="Submit" type="submit" color="primary" class="q-px-md"></q-btn>
                </div>
            </q-form>
        </q-card>
    </q-dialog>

    <div class="row">
        <div class="col">
            <h3>
                Existing Competencies
            </h3>
        </div>
        <div class="col">
            <span class="text-h6 q-mx-xl">
                <q-checkbox size="sm" dense label="Show legacy" v-model="showLegacyComps"></q-checkbox>
            </span>
        </div>
        <div class="col">
            <q-btn dense no-caps color="green" icon="add" label="Add Competency" class="q-px-md" @click="showCompForm = true"></q-btn>
        </div>
    </div>
    <div class="row">
        <div class="col-12">
            <table cellspacing="0" cellpadding="3" border="0" style="width:100%;" class="sessionComps">
                <thead>
                    <tr class="bg-blue-grey-2">
                        <th class="text-left">Competency</th>
                        <th class="text-left" v-if="multiRole">Role</th>
                        <th class="text-center">Know</th>
                        <th class="text-center">Obs</th>
                        <th class="text-center">Perf</th>
                        <th class="text-center">Assess</th>
                        <th class="text-left">&nbsp;</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="lc in legacyCompetencies" class="bg-yellow-2" v-if="showLegacyComps">
                        <td style="max-width:50vw;">{{ lc.dvmCompetencyName }}</td>
                        <td v-if="multiRole">{{ lc.dvmRoleName }}</td>
                        <template v-for="level in ['Know', 'Obs', 'Perf', 'Assess']">
                            <td>
                                <q-icon name="check" color="green" v-if="lc.levels.findIndex((l: Level) => l.levelName == level) >= 0"></q-icon>
                            </td>
                        </template>
                        <td class="text-right">
                        </td>
                    </tr>
                    <tr v-for="c in sessionCompetencies">
                        <td>{{ c.competencyNumber }} {{ c.competencyName }}</td>
                        <td v-if="multiRole">{{ c.roleName }}</td>
                        <template v-for="level in ['Knowledge', 'Observed', 'Performed', 'Assessed']">
                            <td class="text-center">
                                <q-icon name="check" color="green" v-if="c.levels.findIndex((l: Level) => l.levelName == level) >= 0"></q-icon>
                            </td>
                        </template>
                        <td class="text-right">
                            <q-btn dense size="sm" icon="edit" color="secondary" class="q-mr-md" @click="selectComp(c)"></q-btn>
                            <q-btn dense size="sm" icon="delete" color="red-5" @click="delComp(c)"></q-btn>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</template>

<style type="text/css">
    table.sessionComps {
        border: 1px solid silver;
        border-collapse: collapse;
    }

        table.sessionComps th,
        table.sessionComps td {
            border: 1px solid silver;
        }

    .roleLevelSelect {
        border-bottom: 1px solid silver;
    }
</style>