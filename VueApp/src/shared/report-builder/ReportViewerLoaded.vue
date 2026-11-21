<template>
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

<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"
import ReportBuilderForm from "./ReportBuilderForm.vue"
import ReportBuilderResults from "./ReportBuilderResults.vue"
import type { ReportPagination } from "./use-report-builder"
import type {
    ReportExportFormat,
    ReportParameterValues,
    ReportRunResult,
    ReportViewerDefinition,
} from "./report-builder-types"

/**
 * A loaded report's form, status and results - everything ReportViewer shows once the report's
 * definition has come back, split out so each stays under the Vue template complexity threshold.
 */
defineProps<{
    definition: ReportViewerDefinition
    result: ReportRunResult | null
    errors: string[]
    running: boolean
    ready: boolean
    pagination: ReportPagination
    search: string
    canExport: boolean
    exporting: ReportExportFormat | null
    nothingToExport: boolean
    exportAs: (format: ReportExportFormat) => Promise<void>
    run: () => Promise<void>
    changePage: (next: Omit<ReportPagination, "rowsNumber">) => Promise<void>
    changeSearch: (text: string | null) => Promise<void>
}>()

const values = defineModel<ReportParameterValues>({ required: true })
</script>
