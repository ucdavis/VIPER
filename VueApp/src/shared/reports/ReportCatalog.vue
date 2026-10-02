<template>
    <!-- Hidden once loaded if the user may run none of the area's reports, so pages that most
         visitors reach for other reasons don't show an empty section. -->
    <section
        v-if="loading || failed || reports.length > 0"
        :aria-labelledby="headingId"
        class="q-mt-md"
    >
        <h2 :id="headingId">{{ heading }}</h2>
        <div
            v-if="loading"
            role="status"
        >
            <q-spinner-dots
                size="2rem"
                color="primary"
                aria-hidden="true"
            />
            <span class="sr-only">Loading reports</span>
        </div>
        <StatusBanner
            v-else-if="failed"
            type="error"
        >
            The report list could not be loaded. Please try again later.
        </StatusBanner>
        <!-- QItem hardcodes role="listitem", which hides the link role of its anchor; the
             explicit role restores it, as in LeftNav. -->
        <q-list
            v-else
            bordered
            separator
        >
            <q-item
                v-for="report in reports"
                :key="report.key"
                :to="{ name: reportRoute, params: { key: report.key } }"
                clickable
                role="link"
            >
                <q-item-section>
                    <q-item-label>{{ report.title }}</q-item-label>
                    <q-item-label caption>{{ report.description }}</q-item-label>
                </q-item-section>
                <q-item-section
                    v-if="!report.available"
                    side
                >
                    <StatusBadge
                        color="grey-7"
                        label="Coming soon"
                    />
                </q-item-section>
            </q-item>
        </q-list>
    </section>
</template>

<script setup lang="ts">
import { onMounted, ref, useId } from "vue"
import StatusBadge from "@/components/StatusBadge.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { reportService } from "./report-service"
import type { ReportCatalogItem } from "./report-types"

/**
 * Lists the reports in one area that the signed-in user may run, each linking to the area's
 * report route. Planned reports are listed too, marked as coming soon.
 */
const {
    area,
    reportRoute,
    heading = "Reports",
} = defineProps<{
    area: string
    /** Name of the area's route that takes the report key as its `key` param. */
    reportRoute: string
    heading?: string
}>()

const headingId = useId()
const reports = ref<ReportCatalogItem[]>([])
const loading = ref(true)
const failed = ref(false)

async function loadCatalog(): Promise<void> {
    const catalog = await reportService.getCatalog(area)
    failed.value = catalog === null
    reports.value = catalog ?? []
    loading.value = false
}

onMounted(() => {
    loadCatalog()
})
</script>
