<template>
    <h2>Manage Domains</h2>
    <q-form method="post" @submit="save">
        <q-label class="row items-center">
            <span class="col col-md-2 col-lg-1">Domain:</span>
            <q-input outlined dense type="text" name="name" v-model="domain.name" size="30" maxlength="250" class="col col-md-5 col-lg-3" />
        </q-label>
        <q-label class="row items-center">
            <span class="col col-md-2 col-lg-1">Order:</span>
            <q-input outlined dense type="number" name="order" v-model="domain.order" step="1" class="col col-1" />
        </q-label>
        <q-label class="row items-start">
            <span class="col col-md-2 col-lg-1 q-pt-sm">Description:</span>
            <q-input outlined dense type="textarea" name="description" v-model="domain.description" class="col col-md-5 col-lg-3"></q-input>
        </q-label>
        <div class="row q-mt-sm">
            <q-btn type="submit" label="Submit" no-caps dense color="primary" class="col col-4 col-lg-1 offset-md-2 offset-lg-1"></q-btn>
            <q-btn v-if="domain.domainId > 0" @click="clearDomain" label="Clear" no-caps dense color="red-5" class="col col-4 col-lg-1 offset-md-2 offset-lg-1"></q-btn>
        </div>
    </q-form>

    <div class="row">
        <q-list class="col col-12 col-md-10 col-lg-6">
            <q-item-label header class="text-primary"><h3>Domains</h3></q-item-label>
            <q-item v-for="d in domains">
                <q-item-section top side>
                    <q-btn icon="edit" dense flat class="secondary" @click="domain = d"></q-btn>
                </q-item-section>
                <q-item-section top>{{ d.order }}. {{ d.name }}</q-item-section>
                <q-item-section>{{ d.description }}</q-item-section>
            </q-item>
        </q-list>
    </div>
</template>

<script lang="ts">
    type DomainData = {
        domainId: number
        order: number | null
        name: string
        description: string
    }
    import type { Ref } from "vue"
    import { defineComponent, ref } from "vue"
    import { useFetch } from '@/composables/ViperFetch'
    export default defineComponent({
        name: "ManageDomains",
        data() {
            return {
                domain: ref({
                    domainId: 0,
                    order: null,
                    name: '',
                    description: '',
                }) as Ref<DomainData>,
                domains: [] as DomainData[],
            }
        },
        async mounted() {
            await this.getDomains()
        },
        methods: {
            save: async function () {
                let u = import.meta.env.VITE_API_URL + "cts/domains"
                if (this.domain.domainId > 0)
                    u += "/" + this.domain.domainId
                const { vfetch } =
                    useFetch(u,
                        {
                            method: this.domain.domainId > 0 ? "PUT" : "POST",
                            body: JSON.stringify(this.domain),
                            headers: { "Content-Type": "application/json" }
                        })
                await vfetch()
                this.clearDomain()
            },
            getDomains: async function () {
                const { result, errors, vfetch } = useFetch(import.meta.env.VITE_API_URL + "cts/domains")
                await vfetch()
                this.domains = result.value
            },
            clearDomain: function () {
                this.domain = { domainId: 0, order: null, name: '', description: '' }
            }
        }
    })
</script>