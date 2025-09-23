<script setup lang="ts">
    import { ref, inject } from 'vue'
    import type { Ref } from 'vue'
    import type { QTableProps } from 'quasar'
    import { useFetch } from '@/composables/ViperFetch'
    const { get, post, put, del } = useFetch()

    import type { Bundle, Role } from '@/CTS/types'

    const apiUrl = inject('apiURL')

    //form props
    const emptyBundle = { bundleId: null, assessment: false, clinical: false, milestone: false, name: "", roles: [] } as Bundle
    const bundle = ref(structuredClone(emptyBundle)) as Ref<Bundle>
    const roles = ref([]) as Ref<Role[]>
    const bundleRoles = ref([]) as Ref<number[]>

    //bundle table props
    const bundles = ref([]) as Ref<Bundle[]>
    const paging = ref({ page: 1, sortBy: "enteredOn", descending: true, rowsPerPage: 0 }) as Ref<any>
    const columns: QTableProps['columns'] = [
        { name: "action", label: "", field: "id", align: "left" },
        { name: "name", label: "Name", field: "name", align: "left", sortable: true },
        { name: "compcount", label: "Competency Count", field: "competencyCount", align: "left", sortable: true },
        { name: "roles", label: "Roles", field: "roles", align: "left", sortable: true },
        { name: "clinical", label: "Clinical", field: "clinical", align: "left", sortable: true },
        { name: "assessment", label: "Assessment", field: "assessment", align: "left", sortable: true },
        { name: "milestone", label: "Milestone", field: "milestone", align: "left", sortable: true },
    ]
    const filter = ref("")
    const loading = ref(false)

    async function load() {
        get(apiUrl + "cts/bundles").then(r => bundles.value = r.result)
        get(apiUrl + "cts/roles").then(r => roles.value = r.result)
    }

    async function save() {
        let success = false
        if (bundle.value.bundleId == null) {
            let r = await post(apiUrl + "cts/bundles/", bundle.value)
            if (r.success) {
                success = r.success
                bundle.value = r.result
            }
        }
        else {
            let r = await put(apiUrl + "cts/bundles/" + bundle.value.bundleId, bundle.value)
            success = r.success
        }

        console.log(bundle.value, emptyBundle)
        //update roles and then reload
        if (success) {
            await put(apiUrl + "cts/bundles/" + bundle.value.bundleId + "/roles", bundleRoles.value)
            clearBundle()
        }
    }

    function selectBundle(b: Bundle) {
        bundle.value = b
        bundleRoles.value = b.roles.map(r => r.roleId)
    }
    function clearBundle() {
        bundle.value = emptyBundle
        bundleRoles.value = []
        load()
    }

    async function removeBundle() {
        let r = await del(apiUrl + "cts/bundles/" + bundle.value.bundleId)
        if (r.success) {
            clearBundle()
        }
    }

    load()
</script>
<template>
    <h2>Manage Bundles</h2>
    <q-card flat>
        <q-form @submit="save()">
            <h3>{{ bundle?.bundleId == null ? 'Add Bundle' : 'Update Bundle' }}</h3>
            <div class="row">
                <q-input dense outlined class="col-12 col-md-6 col-lg-4" label="Name" v-model="bundle.name"></q-input>
            </div>
            <div class="row">
                <q-select dense options-dense outlined multiple
                          label="Roles" :options="roles"
                          class="col-12 col-md-6 col-lg-4"
                          map-options emit-value
                          option-value="roleId" option-label="name"
                          v-model="bundleRoles"></q-select>
            </div>
            <div class="row">
                <q-toggle label="Assessment" v-model="bundle.assessment"></q-toggle>
            </div>
            <div class="row">
                <q-toggle label="Show for Clinical Encounters" v-model="bundle.clinical"></q-toggle>
            </div>
            <div class="row">
                <q-toggle label="Milestone" v-model="bundle.milestone"></q-toggle>
            </div>
            <div class="row q-mt-sm">
                <q-btn type="submit" color="primary" dense class="col-5 col-sm-2 col-md-1 q-px-sm q-mr-md" label="Submit"></q-btn>
                <q-btn v-if="bundle?.bundleId" type="button" @click="clearBundle()" color="primary" dense class="col-5 col-sm-2 col-md-1 q-px-sm q-mr-md" label="Cancel"></q-btn>
                <q-btn v-if="bundle?.bundleId" type="button" @click="removeBundle()" color="red-5" dense class="col-5 col-sm-2 col-md-1 q-px-sm q-mr-md" label="Delete"></q-btn>
            </div>
        </q-form>
    </q-card>

    <q-table rowKey="bundleId"
             :rows="bundles"
             :columns="columns"
             dense
             v-model:pagination="paging"
             :rows-per-page-options="[5, 10, 15, 25, 50, 100]"
             :filter="filter"
             :loading="loading"
             class="q-mt-md">
        <template v-slot:body-cell-action="props">
            <q-td :props="props">
                <q-btn dense size="sm" icon="edit" @click="selectBundle(props.row)" color="primary"></q-btn>
            </q-td>
        </template>
        <template v-slot:body-cell-roles="props">
            <q-td :props="props">
                <div v-for="r in props.row.roles" :key="r.roleId">{{ r.name }}</div>
            </q-td>
        </template>
        <template v-slot:body-cell-compcount="props">
            <q-td :props="props">
                <q-btn dense size="sm" class="q-mr-md" icon="list" color="primary"
                       :to="'ManageBundleCompetencies?bundleId=' + props.row.bundleId"></q-btn>
                {{ props.row.competencyCount }}
            </q-td>
        </template>
        <template v-slot:body-cell-clinical="props">
            <q-td :props="props">
                <q-icon v-if="props.row.clinical" name="check" color="green" />
            </q-td>
        </template>
        <template v-slot:body-cell-assessment="props">
            <q-td :props="props">
                <q-icon v-if="props.row.assessment" name="check" color="green" />
            </q-td>
        </template>
        <template v-slot:body-cell-milestone="props">
            <q-td :props="props">
                <q-icon v-if="props.row.milestone" name="check" color="green" />
            </q-td>
        </template>
    </q-table>
</template>