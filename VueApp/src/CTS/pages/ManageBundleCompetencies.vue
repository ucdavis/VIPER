<script setup lang="ts">
    import { ref, inject } from 'vue'
    import type { Ref } from 'vue'
    import type { QTableProps } from 'quasar'
    import { useRoute } from 'vue-router'
    import { useFetch } from '@/composables/ViperFetch'
    import type { Bundle, BundleCompetency, BundleCompetencyAddUpdate, BundleCompetencyGroup, Competency, Level, Role } from '@/CTS/types'

    const { get, post, put, del } = useFetch()
    const apiUrl = inject('apiURL')
    const route = useRoute()
    const params = route.query

    const bundleId = route.query.bundleId == null ? 0 : parseInt(route.query.bundleId.toString())
    const bundle = ref(null) as Ref<Bundle | null>

    //for viewing/editing competencies
    const bundleCompetencies = ref([]) as Ref<BundleCompetency[]>
    const editingBundleCompetency = ref(null) as Ref<BundleCompetency | null>
    const defaultBundleCompetency = { bundleCompetencyId: null, bundleId: bundleId, competencyId: null, levelIds: [], roleId: null, bundleCompetencyGroupId: null, order: 1 } as BundleCompetencyAddUpdate
    const bundleCompetency = ref(structuredClone(defaultBundleCompetency)) as Ref<BundleCompetencyAddUpdate>
    const showCompForm = ref(false)
    const columns = ref([
        { name: "order", label: "Order", field: "order", align: "left", sortable: true },
        { name: "competencyname", label: "Competency", field: "competencyName", align: "left", sortable: true },
        { name: "role", label: "Role", field: "roleName", align: "left", sortable: true },
        { name: "action", label: "", field: "", align: "left" },
    ]) as Ref<QTableProps['columns']>

    //for editing groups
    const defaultGroup: BundleCompetencyGroup = { bundleCompetencyGroupId: null, name: "", order: 1 }
    const bundleCompetencyGroups = ref([]) as Ref<BundleCompetencyGroup[]>
    const bundleCompetencyGroup = ref(structuredClone(defaultGroup)) as Ref<BundleCompetencyGroup>
    const showGroupForm = ref(false)

    //lookups
    const competencies = ref([]) as Ref<Competency[]>
    const competencyOptions = ref([]) as Ref<Competency[]>
    const levels = ref([]) as Ref<Level[]>
    const roles = ref([]) as Ref<Role[]>

    //get bundle, bundle competencies and bundle groups, and all competencies, roles and levels for lookups
    async function load() {
        await get(apiUrl + "cts/bundles/" + bundleId).then(r => bundle.value = r.result)
        loadBundleComps()


        get(apiUrl + "cts/competencies").then(r => {
            competencies.value = r.result
            competencyOptions.value = competencies.value
        })
        get(apiUrl + "cts/roles").then(r => roles.value = r.result)

        let levelTypes = "?";
        if (bundle.value?.assessment) {
            levelTypes += "dops=true&"
        }
        if (bundle.value?.clinical) {
            levelTypes += "clinical=true&"
        }
        if (bundle.value?.milestone) {
            levelTypes += "milestone=true&"
        }
        if (levelTypes = "?") {
            levelTypes += "course=true"
        }
        await get(apiUrl + "cts/levels" + levelTypes).then(r => levels.value = r.result)
        if (columns.value) {
            let lastRow = columns.value.pop()
            levels.value.forEach(l => columns.value?.push(
                { name: "level_" + l.levelId, label: l.levelName, field: "", align: "left", sortable: true },
            ))
            if (lastRow != undefined) {
                columns.value.push(lastRow)
            }
        }

    }
    //need to reload bundle comps when changes are made
    function loadBundleComps() {
        get(apiUrl + "cts/bundles/" + bundleId + "/competencies").then(r => bundleCompetencies.value = r.result)
        get(apiUrl + "cts/bundles/" + bundleId + "/groups")
            .then(r => bundleCompetencyGroups.value = [defaultGroup, ...r.result])
    }
    //filter bundle comps to put them in tables by group
    function getCompsByGroup(groupId: number | null) {
        return bundleCompetencies.value.filter(c => c.bundleCompetencyGroupId == groupId)
    }
    //search/filter in comp search box
    function filterComps(val: any, update: any, abort: any) {
        update(() => {
            const srch = val.toLowerCase()
            competencyOptions.value = competencies.value.filter(v => (v.number.toLowerCase() + " " + v.name.toLowerCase()).indexOf(srch) > -1)
        })
    }

    //add/update/delete a bundle competency
    async function addBundleCompetency() {
        bundleCompetency.value.order = bundleCompetencies.value.filter(bc => bc.bundleCompetencyGroupId == bundleCompetency.value.bundleCompetencyGroupId).length + 1
        var result = await post(apiUrl + "cts/bundles/" + bundleId + "/competencies", bundleCompetency.value)
        if (result.success) {
            await loadBundleComps()
        }
        clearComp()
    }
    async function updateBundleCompetency() {
        var result = await put(apiUrl + "cts/bundles/" + bundleId + "/competencies/" + bundleCompetency.value.bundleCompetencyId, bundleCompetency.value)
        if (result.success) {
            await loadBundleComps()
            clearComp()
        }
    }
    async function removeBundleCompetency(comp: BundleCompetency) {
        var result = await del(apiUrl + "cts/bundles/" + bundleId + "/competencies/" + comp.bundleCompetencyId)
        if (result.success) {
            loadBundleComps()
        }
    }
    function clearComp() {
        bundleCompetency.value = defaultBundleCompetency
        showCompForm.value = false
    }
    function editComp(comp: BundleCompetency) {
        showCompForm.value = true
        editingBundleCompetency.value = comp
        bundleCompetency.value = {
            bundleCompetencyGroupId: comp.bundleCompetencyGroupId,
            bundleCompetencyId: comp.bundleCompetencyId,
            bundleId: comp.bundleId,
            competencyId: comp.competencyId,
            levelIds: comp.levels.map(l => l.levelId),
            order: comp.order,
            roleId: comp.roleId
        }
    }

    //add/update/delete a bundle competency group
    async function saveBundleCompetencyGroup() {
        var u = apiUrl + "cts/bundles/" + bundleId + "/groups"
        let success = false
        if (bundleCompetencyGroup.value?.bundleCompetencyGroupId != null) {
            success = (await put(u + "/" + bundleCompetencyGroup.value.bundleCompetencyGroupId, bundleCompetencyGroup.value)).success
        }
        else {
            success = (await post(u, bundleCompetencyGroup.value)).success
        }

        if (success) {
            loadBundleComps()
            clearBundleCompetencyGroup()
        }
    }
    function clearBundleCompetencyGroup() {
        bundleCompetencyGroup.value = defaultGroup
        showGroupForm.value = false
    }
    async function removeBundleCompetencyGroup() {
        let success = (await (del(apiUrl + "cts/bundles/" + bundleId + "/groups/" + bundleCompetencyGroup.value.bundleCompetencyGroupId))).success
        if (success) {
            loadBundleComps()
            clearBundleCompetencyGroup()
        }
    }

    load()
</script>
<template>
    <div v-if="bundle != null">
        <h2>Competencies for Bundle {{ bundle?.name }}</h2>
        <q-btn dense no-caps label="Add Group" @click="showGroupForm = true" class="q-mt-lg q-px-sm" color="green" icon="add"></q-btn>
        <div v-if="showGroupForm" class="q-mt-sm q-mb-md">
            <q-form @submit="saveBundleCompetencyGroup()">
                <div class="row">
                    <q-input dense outlined label="Name" class="col-12 col-md-6 col-lg-4" v-model="bundleCompetencyGroup.name"></q-input>
                </div>
                <div class="row">
                    <q-input type="number" label="Order" dense outlined class="col-4 col-md-2 col-lg-1" v-model="bundleCompetencyGroup.order"></q-input>
                </div>
                <div class="row q-mt-sm">
                    <q-btn type="submit" color="primary" dense class="col-5 col-sm-2 col-md-1 q-px-sm q-mr-md" label="Submit Group"></q-btn>
                    <q-btn type="button" @click="clearBundleCompetencyGroup()" color="secondary" dense class="col-5 col-sm-2 col-md-1 q-px-sm q-mr-md" label="Cancel"></q-btn>
                    <q-btn v-if="bundleCompetencyGroup?.bundleCompetencyGroupId" type="button" @click="removeBundleCompetencyGroup()" color="red-5" dense class="col-5 col-sm-2 col-md-1 q-px-sm q-mr-md" label="Delete"></q-btn>
                </div>
            </q-form>
        </div>

        <q-form @submit="addBundleCompetency()" class="col-12 q-mt-md">
            <div class="row">
                <q-select dense options-dense outlined behavior="dialog" class="col-10 col-sm-6 col-md-4" label="Add Competency"
                          v-model="bundleCompetency.competencyId" emit-value map-options
                          :options="competencyOptions" option-value="competencyId" :option-label="opt => (opt.number + ' ' + opt.name)"
                          use-input input-debounce="0" @filter="filterComps"></q-select>
                <q-select dense options-dense outlined class="col-12 col-sm-6 col-md-4" label="Group"
                          v-model="bundleCompetency.bundleCompetencyGroupId" emit-value map-options
                          :options="bundleCompetencyGroups" option-value="bundleCompetencyGroupId" option-label="name"></q-select>
                <q-btn dense no-caps size="md" type="submit" label="Add" icon="add" color="green" class="col-auto q-ml-md q-px-sm"></q-btn>
            </div>
        </q-form>
    </div>

    <q-dialog v-model="showCompForm">
        <q-card class="q-pa-lg">
            <q-form @submit="updateBundleCompetency()">
                <q-card-section>
                    <div class="row">
                        <div class="col-12">
                            {{ editingBundleCompetency?.competencyNumber }} {{ editingBundleCompetency?.competencyName }}
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-12">
                            <q-select dense options-dense outlined v-model="bundleCompetency.roleId" label="Role"
                                      :options="roles" option-value="roleId" option-label="name" emit-value map-options>
                            </q-select>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-12">
                            <q-select dense options-dense outlined v-model="bundleCompetency.levelIds" multiple label="Levels"
                                      :options="levels" option-value="levelId" option-label="levelName" emit-value map-options></q-select>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-12 col-lg-4">
                            <q-input type="number" dense outlined v-model="bundleCompetency.order" label="Order"></q-input>
                        </div>
                    </div>
                </q-card-section>
                <q-card-actions align="evenly">
                    <q-btn type="submit" dense no-caps label="Submit" color="primary"></q-btn>
                    <q-btn dense no-caps label="Cancel" @click="clearComp()" color="secondary"></q-btn>
                </q-card-actions>
            </q-form>
        </q-card>
    </q-dialog>

    <q-table dense
             :rows="getCompsByGroup(group.bundleCompetencyGroupId)"
             :columns="columns"
             class="q-my-md"
             v-for="group in bundleCompetencyGroups">
        <template v-slot:top="props">
            <div v-if="group.bundleCompetencyGroupId != null">
                <q-btn dense flat size="sm" icon="edit" color="primary" class="q-mr-md" @click="bundleCompetencyGroup=group;showGroupForm=true;"></q-btn>
                <strong>{{ group.name }}</strong>
            </div>
        </template>
        <template v-slot:body-cell-competencyname="props">
            <q-td :props="props">
                {{ props.row.competencyNumber }} {{ props.row.competencyName }}
            </q-td>
        </template>
        <template v-for="level in levels" v-slot:['body-cell-level_'+level.levelId]="props">
            <q-td :props="props">
                <q-icon v-if="props.row.levels.findIndex((l: any) => l.levelId == level.levelId) > -1" name="check" color="green"></q-icon>
            </q-td>
        </template>
        <template v-slot:body-cell-action="props">
            <q-td :props="props">
                <q-btn dense size="sm" icon="edit" color="primary" @click="editComp(props.row)" class="q-mr-sm"></q-btn>
                <q-btn dense size="sm" icon="delete" color="red-5" @click="removeBundleCompetency(props.row)"></q-btn>
            </q-td>
        </template>
    </q-table>
</template>