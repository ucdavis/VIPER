<template>
    <h1>Employee Information System</h1>
    <div
        v-if="loading"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading employees</span>
    </div>
    <StatusBanner
        v-else-if="people === null"
        type="error"
    >
        The employee list could not be loaded. You may not have access to EIS.
    </StatusBanner>
    <StatusBanner
        v-else-if="people.length === 0"
        type="info"
    >
        No employees are available to you. If you should see your units' employees, ask for your login to be added to
        the EIS department list.
    </StatusBanner>
    <q-select
        v-else
        :model-value="null"
        :options="options"
        option-value="employeeId"
        option-label="name"
        emit-value
        map-options
        use-input
        input-debounce="0"
        label="Find an employee by name or ID"
        outlined
        class="eis-person-select"
        @filter="onFilter"
        @update:model-value="open"
    >
        <template #option="scope">
            <q-item v-bind="scope.itemProps">
                <q-item-section>
                    <q-item-label>{{ scope.opt.name }}</q-item-label>
                    <q-item-label caption>{{ scope.opt.employeeId }}</q-item-label>
                </q-item-section>
            </q-item>
        </template>
        <template #no-option>
            <q-item>
                <q-item-section class="text-grey">No matching employees</q-item-section>
            </q-item>
        </template>
    </q-select>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue"
import { useRouter } from "vue-router"
import StatusBanner from "@/components/StatusBanner.vue"
import { filterPeople } from "../services/eis-format"
import { eisService } from "../services/eis-service"
import type { EisPersonOption } from "../types/eis-types"

/**
 * Picks the employee to view. The server lists only the people this user may see: everyone for
 * EIS users, or the people their units pay for department users.
 */
const router = useRouter()
const people = ref<EisPersonOption[] | null>(null)
const options = ref<EisPersonOption[]>([])
const loading = ref(true)

function onFilter(search: string, update: (apply: () => void) => void): void {
    update(() => {
        options.value = filterPeople(people.value ?? [], search)
    })
}

async function open(employeeId: string | null): Promise<void> {
    if (employeeId !== null) {
        await router.push({ name: "EisSummary", params: { employeeId } })
    }
}

onMounted(async () => {
    people.value = await eisService.getPeople()
    options.value = people.value ?? []
    loading.value = false
})
</script>

<style scoped>
.eis-person-select {
    max-width: 32rem;
}
</style>
