<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'

    const users = ref([]) as Ref<any[]>
    const { get } = useFetch()

    async function loadUsers() {
        let userList = await get("/api/fakeUsers")
        users.value = userList.result
    }

    loadUsers()
</script>
<template>
    <h2>Fake User Management</h2>
    {{ users }}
</template>