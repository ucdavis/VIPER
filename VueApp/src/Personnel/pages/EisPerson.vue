<template>
    <div
        v-if="loading"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading employee</span>
    </div>
    <template v-else-if="header === null">
        <BreadcrumbHeading
            label="Employee not available"
            parent-label="EIS"
            :parent-to="{ name: 'EisSelectPerson' }"
        />
        <StatusBanner type="warning">
            This employee isn't available. They may not exist, or may be outside the units you can view.
        </StatusBanner>
    </template>
    <template v-else>
        <BreadcrumbHeading
            :label="header.name"
            parent-label="EIS"
            :parent-to="{ name: 'EisSelectPerson' }"
        />
        <EisPersonHeader :header="header" />
        <nav
            aria-label="Employee information"
            class="eis-section-nav q-mb-md"
        >
            <q-tabs
                align="left"
                no-caps
                active-color="primary"
                indicator-color="primary"
                class="tabs-no-fade"
            >
                <q-route-tab
                    v-for="tab in TABS"
                    :key="tab.name"
                    :to="{ name: tab.name, params: { employeeId } }"
                    :label="tab.label"
                    exact
                />
            </q-tabs>
        </nav>
        <router-view />
    </template>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { useRoute } from "vue-router"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import EisPersonHeader from "../components/EisPersonHeader.vue"
import { provideEisHeader } from "../composables/use-eis-header"
import { eisService } from "../services/eis-service"
import type { EisPersonHeader as EisPersonHeaderData } from "../types/eis-types"

/**
 * One employee in EIS: the header, loaded once, and a tab per legacy page. The selected employee
 * lives in the URL rather than the session, so pages can be bookmarked and opened side by side.
 */
const TABS = [
    { name: "EisSummary", label: "Summary" },
    { name: "EisAcademics", label: "Awards & degrees" },
    { name: "EisAppointments", label: "Appointments" },
    { name: "EisAppointmentCategory", label: "Appointment category" },
    { name: "EisHistory", label: "Appointment history" },
    { name: "EisAddress", label: "Address" },
] as const

const route = useRoute()
const employeeId = computed(() => String(route.params.employeeId ?? ""))
const header = ref<EisPersonHeaderData | null>(null)
const loading = ref(true)
let loadId = 0

provideEisHeader(header)

watch(
    employeeId,
    async (id) => {
        loadId += 1
        const current = loadId
        loading.value = true
        const loaded = await eisService.getHeader(id)
        if (current !== loadId) {
            return
        }
        header.value = loaded
        loading.value = false
    },
    { immediate: true },
)
</script>

<style scoped>
/* A tinted, bordered band so the tabs read as navigation rather than page text. The active tab
   takes the blue-10 tint the left nav uses for its active row. */
.eis-section-nav {
    background-color: var(--surface-tint);
    border: 1px solid var(--ucdavis-black-10);
    border-radius: 0.25rem;
}

.eis-section-nav :deep(.q-tab--active) {
    background-color: var(--ucdavis-blue-10);
    font-weight: 500;
}
</style>
