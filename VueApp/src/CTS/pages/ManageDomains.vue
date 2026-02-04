<script setup lang="ts">
type DomainData = {
    domainId: number
    order: number | null
    name: string
    description: string
}
import type { Ref } from "vue"
import { ref, inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"

const domain = ref({
    domainId: 0,
    order: null,
    name: "",
    description: "",
}) as Ref<DomainData>
const domains = ref([]) as Ref<DomainData[]>
const baseUrl = inject("apiURL") + "cts/domains"

async function getDomains() {
    const { get } = useFetch()
    const r = await get(baseUrl)
    domains.value = r.result
}
async function save() {
    const { put, post } = useFetch()
    let r = { success: false }
    if (domain.value.domainId > 0) {
        r = await put(baseUrl + "/" + domain.value.domainId, domain.value)
    } else {
        r = await post(baseUrl, domain.value)
    }

    if (r.success) {
        clearDomain()
        await getDomains()
    }
}
function clearDomain() {
    domain.value = { domainId: 0, order: null, name: "", description: "" }
}
async function deleteDomain() {
    const { del } = useFetch()
    const r = await del(baseUrl + "/" + domain.value.domainId)
    if (r.success) {
        clearDomain()
        await getDomains()
    }
}

getDomains()
</script>

<template>
    <h2>Manage Domains</h2>
    <q-form
        method="post"
        @submit="save"
    >
        <q-label class="row items-center">
            <span class="col col-md-2 col-lg-1">Domain:</span>
            <q-input
                outlined
                dense
                type="text"
                name="name"
                v-model="domain.name"
                size="30"
                maxlength="250"
                class="col col-md-5 col-lg-3"
            />
        </q-label>
        <q-label class="row items-center">
            <span class="col col-md-2 col-lg-1">Order:</span>
            <q-input
                outlined
                dense
                type="number"
                name="order"
                v-model="domain.order"
                step="1"
                class="col col-1"
            />
        </q-label>
        <q-label class="row items-start">
            <span class="col col-md-2 col-lg-1 q-pt-sm">Description:</span>
            <q-input
                outlined
                dense
                type="textarea"
                name="description"
                v-model="domain.description"
                class="col col-md-5 col-lg-3"
            ></q-input>
        </q-label>
        <div class="row q-mt-sm">
            <q-btn
                type="submit"
                label="Submit"
                no-caps
                dense
                color="primary"
                class="col col-4 col-lg-1 offset-md-2 offset-lg-1"
            ></q-btn>
            <q-btn
                v-if="domain.domainId > 0"
                @click="deleteDomain"
                label="Delete"
                no-caps
                dense
                color="red-7"
                class="col col-4 col-lg-1 q-ml-md"
            ></q-btn>
            <q-btn
                v-if="domain.domainId > 0"
                @click="clearDomain"
                label="Clear"
                no-caps
                dense
                color="info"
                class="col col-4 col-lg-1 q-ml-md"
            ></q-btn>
        </div>
    </q-form>

    <div class="row">
        <q-list class="col col-12 col-md-10 col-lg-6">
            <q-item-label
                header
                class="text-primary"
                ><h3>Domains</h3></q-item-label
            >
            <q-item
                v-for="d in domains"
                :key="d.domainId"
            >
                <q-item-section
                    top
                    side
                >
                    <q-btn
                        icon="edit"
                        dense
                        flat
                        class="secondary"
                        @click="domain = d"
                    ></q-btn>
                </q-item-section>
                <q-item-section top>{{ d.order }}. {{ d.name }}</q-item-section>
                <q-item-section>{{ d.description }}</q-item-section>
            </q-item>
        </q-list>
    </div>
</template>
