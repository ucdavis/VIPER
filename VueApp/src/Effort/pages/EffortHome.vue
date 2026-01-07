<template>
    <div class="q-pa-md">
        <template v-if="isLoading">
            <div class="text-grey q-my-md">Loading...</div>
        </template>
        <template v-else>
            <h2 class="text-grey-7">Select your action from the menu</h2>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { termService } from "../services/term-service"

const route = useRoute()
const router = useRouter()

const isLoading = ref(true)

onMounted(async () => {
    const termCode = route.params.termCode ? parseInt(route.params.termCode as string, 10) : null

    if (termCode) {
        try {
            await termService.getTerm(termCode)
        } catch {
            router.replace({ name: "TermSelection" })
        } finally {
            isLoading.value = false
        }
    } else {
        router.replace({ name: "TermSelection" })
    }
})
</script>
