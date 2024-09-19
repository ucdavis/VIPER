<script setup lang="ts">
    import { inject, ref } from 'vue'
    import type { Ref } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import type { Competency, Domain } from '@/CTS/types'

    const { get, post, put, del } = useFetch()
    const apiUrl = inject('apiURL')
    const domains = ref([]) as Ref<Domain[]>
    const competencies = ref([]) as Ref<Competency[]>
    const emptyComp = { name: "", number: "", description: "", canLinkToStudent: false, domainId: 0, parentId: null, competencyId: null, domain: null, children: null } as Competency
    const selectedComp = ref(structuredClone(emptyComp)) as Ref<Competency>
    const loaded = ref(false)
    const showForm = ref(false)

    async function load() {
        Promise.resolve([
            get(apiUrl + "cts/domains").then(r => domains.value = r.result),
            get(apiUrl + "cts/competencies").then(r => competencies.value = r.result)
        ])
        loaded.value = true
    }

    async function submitComp() {
        let success = false
        if (selectedComp.value.competencyId) {
            const r = await put(apiUrl + "cts/competencies/" + selectedComp.value.competencyId, selectedComp.value)
            success = r.success
        }
        else {
            const r = await post(apiUrl + "cts/competencies", selectedComp.value)
            success = r.success
        }

        if (success) {
            await load()
            clearComp()
        }
    }

    async function deleteComp() {
        const r = await del(apiUrl + "cts/competencies/" + selectedComp.value.competencyId, selectedComp.value)
        if (r.success) {
            await load()
            clearComp()
        }
    }

    function clearComp() {
        selectedComp.value = emptyComp
        showForm.value = false
    }

    load()
</script>
<template>
    <h2>Manage Competencies</h2>
    <h3>Add/Edit Competency <q-btn dense no-caps icon="add" label="Add Competency" color="primary" class="q-ml-md q-mt-xs q-px-md q-py-sm" @click="showForm = true"></q-btn></h3>
    <q-form @submit="submitComp" v-if="showForm" v-model="selectedComp">
        <div class="row">
            <q-input dense outlined v-model="selectedComp.number" label="Number" class="col-12 col-sm-6 col-md-3 col-lg-1"></q-input>
            <q-input dense outlined v-model="selectedComp.name" label="Name" class="col-12 col-md-6 col-lg-3"></q-input>
        </div>
        <div class="row">
            <q-select dense options-dense outlined 
                      v-model="selectedComp.domainId" 
                      label="Domain" 
                      map-options 
                      emit-value 
                      :option-label="opt => opt.order + '. ' + opt.name" 
                      option-value="domainId" 
                      :options="domains" 
                      class="col-12 col-md-9 col-lg-4"></q-select>
        </div>
        <div class="row">
            <q-select dense options-dense outlined 
                      v-model="selectedComp.parentId" 
                      label="Parent" 
                      map-options 
                      emit-value 
                      :option-label="opt => opt.number + ' ' + opt.name" 
                      option-value="competencyId" 
                      :options="competencies" 
                      class="col-12 col-md-9 col-lg-4"></q-select>
        </div>
        <div class="row">
            <q-toggle v-model="selectedComp.canLinkToStudent" label="Can link to student"></q-toggle>
        </div>
        <div class="row">
            <q-input type="textarea" dense outlined v-model="selectedComp.description" label="Description" class="col-12 col-md-8 col-lg-4"></q-input>
        </div>
        <div class="row q-mt-md">
            <q-btn type="submit" dense no-caps label="Submit" color="primary" class="q-px-md q-mx-md col-2 col-md-1"></q-btn>
            <q-btn type="button" dense no-caps label="Cancel" color="secondary" class="q-px-md q-mx-md col-2 col-md-1" @click="clearComp()"></q-btn>
            <q-btn type="button" dense no-caps label="Delete" v-if="selectedComp.competencyId != null" color="red-5" class="q-px-md q-mx-md col-2 col-md-1" @click="deleteComp()"></q-btn>
        </div>
    </q-form>
    <div v-if="loaded" class="q-mt-md">
        <h3>Existing Competencies</h3>
        <div class="row items-center">
            <div class="col-1">
                &nbsp;
            </div>
            <div class="col-2 col-sm-1">Number</div>
            <div class="col-9 col-md-4 col-lg-3">Name</div>
            <div class="col-11 col-md-5 col-lg-3">Description</div>
            <div class="col-1">Std</div>
        </div>
        <div class="row items-start q-mb-sm" v-for="comp in competencies">
            <div class="col-1">
                <q-btn dense no-caps size="sm" icon="edit" color="primary" @click="selectedComp = comp;showForm = true;"></q-btn>
            </div>
            <div class="col-2 col-sm-1">
                {{ comp.number }}
            </div>
            <div class="col-9 col-md-4 col-lg-3">
                {{ comp.name }}
            </div>
            <div class="col-11 col-md-5 col-lg-3">
                {{ comp.description }}
            </div>
            <div class="col-1">
                <q-icon name="check" color="green" v-if="comp.canLinkToStudent"></q-icon>
            </div>
        </div>
    </div>
</template>