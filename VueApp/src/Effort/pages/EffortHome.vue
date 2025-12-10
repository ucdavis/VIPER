<template>
    <div class="q-pa-md">
        <h1 class="text-h4">Effort Tracking</h1>
        <p v-if="term">Term: {{ term.termName }}</p>
        <p
            v-else-if="isLoading"
            class="text-grey"
        >
            Loading...
        </p>
        <p
            v-else
            class="text-grey"
        >
            No term selected
        </p>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useRoute } from "vue-router"
import { effortService } from "../services/effort-service"
import type { TermDto } from "../types"

const route = useRoute()

const term = ref<TermDto | null>(null)
const isLoading = ref(false)

const termCode = computed(() => {
    const param = route.params.termCode
    if (param && typeof param === "string") {
        return Number(param)
    }
    return null
})

watch(
    termCode,
    async (code) => {
        if (code) {
            isLoading.value = true
            try {
                term.value = await effortService.getTerm(code)
            } finally {
                isLoading.value = false
            }
        } else {
            term.value = null
        }
    },
    { immediate: true },
)
</script>
