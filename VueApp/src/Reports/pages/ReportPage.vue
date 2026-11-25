<template>
    <ReportViewer
        :slug="slug"
        sync-query
    >
        <template #heading="{ title }">
            <BreadcrumbHeading
                :label="title ?? 'Report not available'"
                parent-label="Reports"
                :parent-to="{ name: 'ReportsHome' }"
            />
        </template>
    </ReportViewer>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useRoute } from "vue-router"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import ReportViewer from "@/shared/report-builder/ReportViewer.vue"

/** One report on its own page, with its form's values in the URL so it can be bookmarked. */
const route = useRoute()

const slug = computed(() => String(route.params.slug))
</script>
