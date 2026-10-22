<template>
    <h2>Summary</h2>
    <EisFactList
        v-if="header"
        :facts="facts"
    />
</template>

<script setup lang="ts">
import { computed, inject } from "vue"
import EisFactList from "../components/EisFactList.vue"
import { eisHeaderKey } from "../composables/eis-header"
import { summaryFacts } from "../services/eis-facts"

/**
 * The legacy EIS home page details: contact, demographics, employment, service credit and
 * leave balances. Reads the header EisPerson.vue has already loaded.
 */
const header = inject(eisHeaderKey, null)

const facts = computed(() => (header?.value ? summaryFacts(header.value) : []))
</script>
