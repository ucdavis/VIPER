<script setup lang="ts">
type DomainData = {
    domainId: number
    order: number | null
    name: string
    description: string
}
import type { Ref } from "vue"
import { ref, inject } from "vue"
import { useQuasar } from "quasar"
import { useFetch } from "@/composables/ViperFetch"

const $q = useQuasar()

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
    const r =
        domain.value.domainId > 0
            ? await put(baseUrl + "/" + domain.value.domainId, domain.value)
            : await post(baseUrl, domain.value)

    if (r.success) {
        clearDomain()
        await getDomains()
    }
}
function clearDomain() {
    domain.value = { domainId: 0, order: null, name: "", description: "" }
}
function deleteDomain() {
    $q.dialog({
        title: "Confirm Delete",
        message: "Are you sure you want to delete this domain?",
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        const { del } = useFetch()
        const r = await del(baseUrl + "/" + domain.value.domainId)
        if (r.success) {
            clearDomain()
            await getDomains()
        }
    })
}

getDomains()
</script>

<template>
    <h1>Manage Domains</h1>
    <q-form
        method="post"
        @submit="save"
    >
        <div class="row q-gutter-sm q-mb-sm">
            <q-input
                outlined
                dense
                label="Domain"
                v-model="domain.name"
                maxlength="250"
                class="col-12 col-md-5 col-lg-3"
            />
            <q-input
                outlined
                dense
                label="Order"
                type="number"
                v-model="domain.order"
                step="1"
                class="col-4 col-md-1"
            />
        </div>
        <div class="row q-mb-sm">
            <q-input
                outlined
                dense
                label="Description"
                type="textarea"
                v-model="domain.description"
                class="col-12 col-md-5 col-lg-3"
            />
        </div>
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
                color="negative"
                class="col col-4 col-lg-1 q-ml-md"
            ></q-btn>
            <q-btn
                v-if="domain.domainId > 0"
                @click="clearDomain"
                label="Clear"
                no-caps
                dense
                color="info"
                text-color="dark"
                class="col col-4 col-lg-1 q-ml-md"
            ></q-btn>
        </div>
    </q-form>

    <div class="row">
        <div class="col col-12 col-md-10 col-lg-6">
            <h2 class="text-primary q-px-md">Domains</h2>
            <q-list>
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
                            :aria-label="`Edit domain: ${d.name}`"
                            @click="domain = d"
                        ></q-btn>
                    </q-item-section>
                    <q-item-section top>{{ d.order }}. {{ d.name }}</q-item-section>
                    <q-item-section>{{ d.description }}</q-item-section>
                </q-item>
            </q-list>
        </div>
    </div>
</template>
