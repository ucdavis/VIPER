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
import { formatEisDate, formatHours, formatServiceCredit } from "../services/eis-format"

/**
 * The legacy EIS home page details: contact, demographics, employment, service credit and
 * leave balances. Reads the header EisPerson.vue has already loaded.
 */
const header = inject(eisHeaderKey, null)

const facts = computed(() => {
    const person = header?.value
    if (!person) {
        return []
    }
    return [
        { label: "Email", value: person.email ?? "", href: person.email ? `mailto:${person.email}` : undefined },
        { label: "Date of birth", value: person.dateOfBirth ?? "" },
        { label: "Age", value: person.age === null ? "" : String(person.age) },
        { label: "Gender", value: person.gender ?? "" },
        { label: "Ethnicity", value: person.ethnicity ?? "" },
        { label: "Hire date", value: formatEisDate(person.hireDate) },
        { label: "Employment status", value: person.employmentStatus ?? "" },
        { label: "US citizen", value: person.citizenship ?? "" },
        { label: "Visa type", value: person.visa ?? "" },
        { label: "Bargaining unit", value: person.bargainingUnit ?? "" },
        { label: "Labor relations unit", value: person.laborRelationsUnit ?? "" },
        { label: "Service credit", value: formatServiceCredit(person.serviceCreditMonths) },
        { label: "Vacation balance", value: formatHours(person.vacationHours) },
        { label: "Sick leave balance", value: formatHours(person.sickHours) },
        // As in the legacy header, PTO shows only for people who have a balance.
        ...((person.ptoHours ?? 0) > 0 ? [{ label: "PTO balance", value: formatHours(person.ptoHours) }] : []),
    ]
})
</script>
