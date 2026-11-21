<template>
    <div
        v-if="loading"
        role="status"
        class="q-my-lg"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading report</span>
    </div>

    <template v-else-if="definition === null">
        <slot
            name="heading"
            :title="null"
        />
        <StatusBanner type="warning">
            This report isn't available. It may have been removed, or you may not have access to it.
        </StatusBanner>
    </template>

    <template v-else>
        <slot
            name="heading"
            :title="definition.title"
        >
            <h2>{{ definition.title }}</h2>
        </slot>
        <ReportViewerLoaded
            v-model="values"
            :definition="definition"
            :result="result"
            :errors="errors"
            :running="running"
            :ready="ready"
            :pagination="pagination"
            :search="search"
            :can-export="canExport"
            :exporting="exporting"
            :nothing-to-export="nothingToExport"
            :export-as="exportAs"
            :run="run"
            :change-page="changePage"
            :change-search="changeSearch"
        />
    </template>
</template>

<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"
import ReportViewerLoaded from "./ReportViewerLoaded.vue"
import { useReportBuilder } from "./use-report-builder"

/**
 * An admin-built report: its form, its rows a page at a time, and the downloads it allows.
 * Pages can embed it with their own heading in the `heading` slot (given the report's title, or
 * null when it isn't available) and starting values in `params`. On the report's own page
 * (`syncQuery`) the form's values live in the URL so the page can be bookmarked.
 */
const {
    slug,
    params = undefined,
    syncQuery = false,
} = defineProps<{
    slug: string
    params?: Record<string, string | string[]>
    syncQuery?: boolean
}>()

defineSlots<{ heading?: (props: { title: string | null }) => unknown }>()

const {
    definition,
    values,
    result,
    errors,
    loading,
    running,
    exporting,
    nothingToExport,
    search,
    pagination,
    canExport,
    ready,
    run,
    changePage,
    changeSearch,
    exportAs,
} = useReportBuilder(() => slug, { params: () => params, syncQuery })
</script>
