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
        <p v-if="definition.description">{{ definition.description }}</p>
        <StatusBanner
            v-if="definition.sensitiveNotice"
            type="info"
            icon="lock"
            live="off"
        >
            {{ definition.sensitiveNotice }}
        </StatusBanner>

        <ReportBuilderForm
            v-if="definition.parameters.length > 0"
            v-model="values"
            :parameters="definition.parameters"
            @submit="run"
        />

        <p
            v-if="!ready"
            class="text-grey-8"
        >
            Fill in the required fields to see the report.
        </p>
        <div
            v-else-if="running && result === null"
            role="status"
            class="q-my-md"
        >
            <q-spinner-dots
                size="2rem"
                color="primary"
                aria-hidden="true"
            />
            <span class="sr-only">Running report</span>
        </div>

        <StatusBanner
            v-if="errors.length > 0"
            type="error"
        >
            <ul class="q-my-none q-pl-md">
                <li
                    v-for="error in errors"
                    :key="error"
                >
                    {{ error }}
                </li>
            </ul>
        </StatusBanner>

        <ReportBuilderResults
            v-if="result !== null"
            :result="result"
            :pagination="pagination"
            :paginate="definition.paginate"
            :title="definition.title"
            :search="search"
            :allow-excel="definition.allowExcel"
            :allow-pdf="definition.allowPdf"
            :can-export="canExport"
            :running="running"
            :exporting="exporting"
            :nothing-to-export="nothingToExport"
            :export-as="exportAs"
            @request="changePage"
            @search="changeSearch"
        />
    </template>
</template>

<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"
import ReportBuilderForm from "./ReportBuilderForm.vue"
import ReportBuilderResults from "./ReportBuilderResults.vue"
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
