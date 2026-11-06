<template>
    <h2>Summary</h2>
    <template
        v-for="(section, index) in sections"
        :key="section.title"
    >
        <q-separator
            v-if="index > 0"
            class="q-my-md"
        />
        <h3 class="sr-only">{{ section.title }}</h3>
        <EisFactList :rows="section.rows" />
    </template>
</template>

<script setup lang="ts">
import { computed } from "vue"
import EisFactList from "../components/EisFactList.vue"
import { useEisHeader } from "../composables/use-eis-header"
import { summaryFacts } from "../utils/eis-facts"

/**
 * The legacy EIS home page details in the legacy order, with a separator between personal
 * details, the position's units, and service credit and leave. Reads the header EisPerson.vue
 * has already loaded.
 */
const header = useEisHeader()

const sections = computed(() => (header?.value ? summaryFacts(header.value) : []))
</script>
