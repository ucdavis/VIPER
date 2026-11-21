<template>
    <div class="row items-center q-mb-md">
        <h1 class="q-ma-none">Reports</h1>
        <q-space />
        <q-btn
            v-if="isAdmin"
            flat
            no-caps
            color="primary"
            icon="settings"
            label="Manage reports"
            :to="{ name: 'ReportAdmin' }"
        />
    </div>
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
        The report list couldn't be loaded. Please try again later.
    </StatusBanner>
    <StatusBanner
        v-else-if="areas.length === 0"
        type="info"
    >
        There are no reports for you to view yet.
    </StatusBanner>
    <template v-else>
        <section
            v-for="group in areas"
            :key="group.area"
            :aria-label="group.area"
            class="q-mb-lg"
        >
            <h2>{{ group.area }}</h2>
            <!-- QItem hardcodes role="listitem", which hides the link role of its anchor; the
                 explicit role restores it, as in LeftNav. -->
            <q-list
                bordered
                separator
            >
                <q-item
                    v-for="report in group.reports"
                    :key="report.slug"
                    :to="{ name: 'Report', params: { slug: report.slug } }"
                    clickable
                    role="link"
                >
                    <q-item-section>
                        <q-item-label>{{ report.title }}</q-item-label>
                        <q-item-label
                            v-if="report.description"
                            caption
                        >
                            {{ report.description }}
                        </q-item-label>
                    </q-item-section>
                </q-item>
            </q-list>
        </section>
    </template>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { REPORT_ADMIN_PERMISSION } from "@/Reports/admin/report-admin-permissions"
import { groupByArea } from "@/shared/report-builder/report-builder-catalog"
import type { ReportArea } from "@/shared/report-builder/report-builder-catalog"
import { reportBuilderService } from "@/shared/report-builder/report-builder-service"

/** Every admin-built report the user may view, grouped by area. */
const areas = ref<ReportArea[]>([])
const loading = ref(true)
const failed = ref(false)
const isAdmin = checkHasOnePermission([REPORT_ADMIN_PERMISSION])

async function loadCatalog(): Promise<void> {
    const catalog = await reportBuilderService.getCatalog()
    failed.value = catalog === null
    areas.value = groupByArea(catalog ?? [])
    loading.value = false
}

onMounted(() => {
    loadCatalog()
})
</script>
