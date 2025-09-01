<script setup lang="ts">
import type { Ref } from "vue"
import { ref, inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import type { Epa, Service } from "@/CTS/types"

const showForm = ref(false)
const epas = ref([]) as Ref<Epa[]>
const services = ref([]) as Ref<Service[]>
const epaServices = ref([]) as Ref<number[]>
const epa = ref({ epaId: null, order: null, name: "", description: "", active: false, services: [] }) as Ref<Epa>
const epaUrl = inject("apiURL") + "cts/epas"
const serviceUrl = inject("apiURL") + "cts/clinicalservices"

async function getServices() {
    const { get } = useFetch()
    const r = await get(serviceUrl)
    services.value = r.result.map((r: any) => ({ label: r.serviceName, value: r.serviceId }))
}
async function getEpas() {
    const { get } = useFetch()
    const r = await get(epaUrl)
    epas.value = r.result
    clearEpa()
}
function selectEpa(e: Epa) {
    epa.value = e
    epaServices.value = epa.value.services.map((s) => s.serviceId)
    showForm.value = true
    window.scrollTo(0, 0)
}
async function submitEpa() {
    const { put, post } = useFetch()
    // Create EPA object without services (handled separately via API)
    const epaObj = {
        epaId: epa.value.epaId,
        order: epa.value.order,
        name: epa.value.name,
        description: epa.value.description,
        active: epa.value.active,
    }
    let r = { success: false, result: null as Epa | null }
    if (epa.value?.epaId) {
        r = await put(epaUrl + "/" + epa.value.epaId, epaObj)
    } else {
        epaObj.epaId = 0
        r = await post(epaUrl, epaObj)
        if (r.result != null) {
            epa.value.epaId = r.result.epaId
        }
    }

    if (r.success) {
        await put(epaUrl + "/" + epa.value.epaId + "/services", epaServices.value)
    }

    if (r.success) {
        getEpas()
    }
}
async function deleteEpa() {
    const { del } = useFetch()
    const r = await del(epaUrl + "/" + epa.value.epaId)
    if (r.success) {
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
    <q-form
        @submit="submitEpa"
        v-if="showForm"
        class="q-mb-md"
    >
        <h2>{{ epa.epaId ? "Updating EPA" : "Creating EPA" }}</h2>

        <div class="row">
            <q-input
                outlined
                dense
                type="text"
                label="Name"
                v-model="epa.name"
                class="col col-lg-6"
            />
        </div>
        <div class="row">
            <q-editor
                v-model="epa.description"
                min-height="15rem"
                class="col col-lg-6"
                :toolbar="[
                    ['left', 'center', 'right', 'justify'],
                    ['bold', 'italic', 'underline', 'strike'],
                    ['quote', 'unordered', 'ordered', 'outdent', 'indent'],
                    ['undo', 'redo'],
                    ['viewsource'],
                ]"
            />
        </div>
        <div class="row">
            <q-toggle
                label="Active"
                v-model="epa.active"
            />
        </div>
        <div class="row">
            <q-select
                outlined
                dense
                options-dense
                label="Services"
                class="col col-lg-4"
                emit-value
                map-options
                v-model="epaServices"
                use-chips
                multiple
                :options="services"
            />
        </div>
        <div class="row">
            <q-btn
                dense
                no-caps
                type="submit"
                :label="(epa?.epaId ? 'Update' : 'Create') + ' EPA'"
                color="primary"
                class="q-mt-sm col col-4 col-md-4 col-lg-1"
            />
            <q-btn
                dense
                no-caps
                type="button"
                label="Clear"
                color="secondary"
                @click="clearEpa()"
                v-if="epa?.epaId"
                class="q-mt-sm q-ml-lg col col-4 col-md-4 col-lg-1"
            />
            <q-btn
                dense
                no-caps
                type="button"
                label="Delete EPA"
                color="red"
                @click="deleteEpa()"
                v-if="epa?.epaId"
                class="q-mt-sm q-ml-lg col col-4 col-md-4 col-lg-1"
            />
        </div>
        <hr />
    </q-form>

    <div class="q-mb-md">
        <q-btn
            no-caps
            size="md"
            label="Add EPA"
            color="green"
            @click="showForm = true"
        />
    </div>

    <q-list separator>
        <q-item-label
            header
            class="text-h6 text-dark"
        >
            Entrustable Professional Activities
        </q-item-label>
        <q-item>
            <q-item-section
                top
                side
                class="text-dark col-5 col-md-4 col-lg-2"
            >
                <span class="row fit">
                    <span class="col">Edit</span>
                    <span class="col">Active</span>
                </span>
            </q-item-section>
            <q-item-section
                top
                side
                class="text-dark col-7 col-md-8 col-lg-3"
            >
                Name
            </q-item-section>
            <q-item-section
                top
                class="text-dark col-12 col-md-6 col-lg-4"
            >
                Description
            </q-item-section>
            <q-item-section
                top
                class="text-dark col-12 col-md-6 col-lg-3"
            >
                Services
            </q-item-section>
        </q-item>
        <q-item
            v-for="epa in epas"
            :key="epa.epaId || `epa-${epa.name}`"
        >
            <q-item-section
                top
                side
                class="text-dark col-5 col-md-4 col-lg-2"
            >
                <template #default>
                    <span class="row fit">
                        <span class="col">
                            <q-btn
                                dense
                                no-caps
                                size="md"
                                icon="edit"
                                color="primary"
                                @click="selectEpa(epa)"
                            />
                        </span>
                        <span class="col">
                            <q-icon
                                :name="epa.active ? 'check' : 'close'"
                                :color="epa.active ? 'green' : 'red'"
                                size="md"
                            />
                        </span>
                    </span>
                </template>
            </q-item-section>
            <q-item-section
                top
                side
                class="text-dark col-7 col-md-8 col-lg-3"
            >
                {{ epa.name }}
            </q-item-section>
            <!--v-html - this is sanitized in EpaController-->
            <!-- eslint-disable vue/no-v-html, vue/no-v-text-v-html-on-component -->
            <q-item-section
                top
                class="text-dark col-12 col-md-6 col-lg-4"
                v-html="epa.description"
            />
            <!-- eslint-enable vue/no-v-html, vue/no-v-text-v-html-on-component -->
            <q-item-section
                top
                class="text-dark col-12 col-md-6 col-lg-3"
            >
                <q-expansion-item
                    expand-separator
                    :label="epa.services.length + ' services'"
                >
                    <span
                        v-for="epaService in epa.services"
                        :key="epaService.serviceId"
                    >
                        {{ epaService.serviceName }}<br />
                    </span>
                </q-expansion-item>
            </q-item-section>
        </q-item>
    </q-list>
</template>
