<script setup lang="ts">
    import { ref, inject } from 'vue'
    import type { Ref } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    const { get, post, put, del } = useFetch()

    const apiUrl = inject('apiURL')
    const role = ref({}) as Ref<any>
    const roles = ref([]) as Ref<any[]>

    async function load() {
        get(apiUrl + "cts/roles").then(r => roles.value = r.result)
    }

    async function save() {
        let r = (role.value?.roleId)
            ? (await put(apiUrl + "cts/roles/" + role.value.roleId, role.value))
            : (await post(apiUrl + "cts/roles", role.value))
        if (r.success) {
            clearRole()
            load()
        }
    }

    async function removeRole() {
        let r = await del(apiUrl + "cts/roles/" + role.value.roleId)
        if (r.success) {
            clearRole()
        }
    }

    function clearRole() {
        role.value = {}
    }

    load()
</script>
<template>
    <h2>Add/Edit Roles</h2>
    <q-form @submit="save()" class="q-mb-md">
        <div class="row">
            <q-input dense outlined v-model="role.name" label="Role Name" class="col-12 col-sm-6 col-lg-4"></q-input>
        </div>
        <div class="row q-mt-sm">
            <q-btn type="submit" color="primary" dense class="col-5 col-sm-2 col-md-1 q-px-sm q-mr-md" label="Submit"></q-btn>
            <q-btn v-if="role?.roleId" type="button" @click="clearRole()" color="primary" dense class="col-5 col-sm-2 col-md-1 q-px-sm q-mr-md" label="Cancel"></q-btn>
            <q-btn v-if="role?.roleId" type="button" @click="removeRole()" color="red-5" dense class="col-5 col-sm-2 col-md-1 q-px-sm q-mr-md" label="Delete"></q-btn>
        </div>
    </q-form>

    <div class="row items-center" v-for="r in roles">
        <q-btn dense size="sm" icon="edit" color="primary" @click="role=r" class="q-mt-xs q-mr-md"></q-btn>
        {{ r.name }}
    </div>
</template>