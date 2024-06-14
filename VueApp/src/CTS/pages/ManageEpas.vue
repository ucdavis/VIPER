<script setup lang="ts">
    import type { Ref } from "vue"
    import { ref } from "vue"
    import { useFetch } from '@/composables/ViperFetch'
    import type { Epa, Service } from '@/CTS/types'    

    const showForm = ref(false)
    const epas = ref([]) as Ref<Epa[]>
    const services = ref([]) as Ref<Service[]>
    const epaServices = ref([]) as Ref<number[]>
    const epa = ref({ epaId: null, order: null, name: "", description: "", active: false, services: [] }) as Ref<Epa>
    const epaUrl = import.meta.env.VITE_API_URL + "cts/epas"
    const serviceUrl = import.meta.env.VITE_API_URL + "cts/clinicalservices"

    async function getServices() {
        const { get, result } = useFetch()
        await get(serviceUrl)
        services.value = result.value.map((r : any) => ({ label: r.serviceName, value: r.serviceId }))
    }
    async function getEpas() {
        const { get, result } = useFetch()
        await get(epaUrl)
        epas.value = result.value
        clearEpa()
    }
    function selectEpa(e: Epa) {
        epa.value = e
        epaServices.value = epa.value.services.map(s => s.serviceId)
        showForm.value = true
    }
    async function submitEpa() {
        const { success, result, put, post } = useFetch()
        let { services: _, ...epaObj } = epa.value
        if (epa.value?.epaId) {
            await put(epaUrl + "/" + epa.value.epaId, epaObj)
        }
        else {
            epaObj.epaId = 0
            await post(epaUrl, epaObj)
            epa.value.epaId = result.value.epaId
        }

        if (success.value) {
            await put(epaUrl + "/" + epa.value.epaId + "/services", epaServices.value)
        }

        if (success.value) {
            getEpas()
        }
    }
    async function deleteEpa() {
        const { remove, success } = useFetch()
        await remove(epaUrl + "/" + epa.value.epaId)
        if (success) {
            getEpas()
        }
    }
    async function clearEpa() {
        epa.value = { epaId: null, order: null, name: "", description: "", active: false, services: [] }
        epaServices.value = []
    }

    getServices()
    getEpas()
</script>

<template>
    <q-form @submit="submitEpa" v-if="showForm" class="q-mb-md">
        <h2>{{ epa.epaId ? 'Updating EPA' : 'Creating EPA' }}</h2>
        
        <div class="row">
            <q-input outlined dense type="text" label="Name" v-model="epa.name" class="col col-lg-6"></q-input>
        </div>
        <div class="row">
            <q-editor v-model="epa.description" min-height="15rem" class="col col-lg-6"
                      :toolbar="[
                        [ 'left', 'center', 'right', 'justify' ],
                        [ 'bold', 'italic', 'underline', 'strike' ],
                        ['quote', 'unordered', 'ordered', 'outdent', 'indent'],
                        [ 'undo', 'redo' ],
                        ['viewsource'] ]"></q-editor>
        </div>
        <div class="row">
            <q-toggle label="Active" v-model="epa.active"></q-toggle>
        </div>
        <div class="row">
            <q-select outlined dense options-dense label="Services" class="col col-lg-4" emit-value map-options
                      v-model="epaServices" use-chips multiple :options="services"></q-select>
        </div>
        <div class="row">
            <q-btn dense no-caps type="submit" :label="(epa?.epaId ? 'Update' : 'Create') + ' EPA'" color="primary"
                   class="q-mt-sm col col-4 col-md-4 col-lg-1"></q-btn>
            <q-btn dense no-caps type="button" label="Clear" color="secondary" @click="clearEpa()"
                   v-if="epa?.epaId"
                   class="q-mt-sm q-ml-lg col col-4 col-md-4 col-lg-1"></q-btn>
            <q-btn dense no-caps type="button" label="Delete EPA" color="red" @click="deleteEpa()"
                   v-if="epa?.epaId"
                   class="q-mt-sm q-ml-lg col col-4 col-md-4 col-lg-1"></q-btn>
        </div>
        <hr />
    </q-form>

    <h3>
        <q-btn no-caps size="md" label="Add EPA" color="green" @click="showForm=true"></q-btn>
    </h3>

    <q-list separator>
        <q-item-label header class="text-h6 text-dark">
            Entrustable Professional Activities
        </q-item-label>
        <q-item>
            <q-item-section top side class="text-dark col-5 col-md-4 col-lg-2">
                <span class="row fit">
                    <span class="col">Edit</span>
                    <span class="col">Active</span>
                </span>
            </q-item-section>
            <q-item-section top side class="text-dark col-7 col-md-8 col-lg-3">Name</q-item-section>
            <q-item-section top class="text-dark col-12 col-md-6 col-lg-4">Description</q-item-section>
            <q-item-section top class="text-dark col-12 col-md-6 col-lg-3">Services</q-item-section>
        </q-item>
        <q-item v-for="epa in epas">
            <q-item-section top side class="text-dark col-5 col-md-4 col-lg-2">
                <template v-slot:default>
                    <span class="row fit">
                        <span class="col">
                            <q-btn dense no-caps size="md" icon="edit" color="primary" @click="selectEpa(epa)"></q-btn>
                        </span>
                        <span class="col">
                            <q-icon :name="epa.active ? 'check' : 'close'" :color="epa.active ? 'green' : 'red'" size="md"></q-icon>
                        </span>
                    </span>
                </template>
            </q-item-section>
            <q-item-section top side class="text-dark col-7 col-md-8 col-lg-3">
                {{epa.name}}
            </q-item-section>
            <!--v-html - this is sanitized in EpaController-->
            <q-item-section top class="text-dark col-12 col-md-6 col-lg-4" v-html="epa.description"></q-item-section>
            <q-item-section top class="text-dark col-12 col-md-6 col-lg-3">
                <span v-for="epaService in epa.services">
                    {{epaService.serviceName}}
                </span>
            </q-item-section>
        </q-item>
    </q-list>
</template>