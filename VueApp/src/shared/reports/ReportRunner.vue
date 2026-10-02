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

    <template v-else-if="notFound || definition === null">
        <BreadcrumbHeading
            label="Report not available"
            :parent-label="parentLabel"
            :parent-to="parentTo"
        />
        <StatusBanner type="warning">
            This report isn't available. It may not be built yet, or you may not have access to it.
        </StatusBanner>
    </template>

    <template v-else>
        <BreadcrumbHeading
            :label="definition.title"
            :parent-label="parentLabel"
            :parent-to="parentTo"
        />
        <p>{{ definition.description }}</p>

        <ReportParameterForm
            v-if="definition.parameters.length > 0"
            v-model="values"
            :parameters="definition.parameters"
            :running="running"
            @submit="run"
        />

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

        <div
            v-if="running && result === null"
            role="status"
            class="q-my-lg"
        >
            <q-spinner-dots
                size="2rem"
                color="primary"
                aria-hidden="true"
            />
            <span class="sr-only">Running report</span>
        </div>

        <template v-if="result !== null">
            <ExportToolbar
                class="justify-end q-mb-sm"
                :excel-export="() => exportAs('xlsx')"
                :pdf-export="() => exportAs('pdf')"
                :busy="exporting !== null"
            >
                <template #append>
                    <q-btn
                        flat
                        dense
                        no-caps
                        icon="text_snippet"
                        label="CSV"
                        :disable="exporting !== null"
                        :loading="exporting === 'csv'"
                        @click="exportAs('csv')"
                    >
                        <template #loading>
                            <q-spinner
                                size="1em"
                                class="q-mr-sm"
                            />
                            CSV
                        </template>
                    </q-btn>
                </template>
            </ExportToolbar>
            <ReportView :result="result" />
        </template>
    </template>
</template>

<script setup lang="ts">
import type { RouteLocationRaw } from "vue-router"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"
import ExportToolbar from "@/components/ExportToolbar.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import ReportParameterForm from "./ReportParameterForm.vue"
import ReportView from "./ReportView.vue"
import { useReport } from "./use-report"

/**
 * A complete report page for any area: the report's parameters, its result and its exports.
 * The area's route supplies the key and where the breadcrumb leads back to.
 */
const { reportKey, parentLabel, parentTo } = defineProps<{
    reportKey: string
    parentLabel: string
    parentTo: RouteLocationRaw
}>()

const { definition, values, result, errors, loading, notFound, running, exporting, run, exportAs } = useReport(
    () => reportKey,
)
</script>
