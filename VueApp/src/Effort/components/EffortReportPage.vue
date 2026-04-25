<script setup lang="ts">
import ExportToolbar from "@/components/ExportToolbar.vue"
import ReportFilterForm from "./ReportFilterForm.vue"
import ReportLayout from "./ReportLayout.vue"
import type { ReportFilterParams } from "../types"

type FilterField = "department" | "faculty" | "role" | "title"

defineProps<{
    title: string
    subtitle?: string
    termCode: number
    loading: boolean
    hasReport: boolean
    isEmpty: boolean
    emptyMessage?: string
    initialFilters?: ReportFilterParams
    visibleFields?: FilterField[]
    meritOnly?: boolean
    onPdfExport: () => Promise<void>
    onExcelExport: () => Promise<void>
}>()

defineEmits<{
    (e: "generate", params: ReportFilterParams): void
}>()
</script>

<template>
    <div class="q-pa-md">
        <h1>{{ title }}</h1>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="visibleFields"
            :merit-only="meritOnly"
            @generate="$emit('generate', $event)"
        />

        <div
            v-if="loading"
            role="status"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading report...</div>
        </div>

        <ReportLayout v-else-if="hasReport">
            <template #header>
                <div class="col text-h6">
                    <slot name="header">{{ subtitle ?? title }}</slot>
                </div>
                <div class="col-auto no-print">
                    <ExportToolbar
                        :pdf-export="onPdfExport"
                        :excel-export="onExcelExport"
                    />
                </div>
            </template>

            <div
                v-if="isEmpty"
                role="status"
                class="text-grey-7 text-center q-pa-lg"
            >
                {{ emptyMessage ?? "No data found for the selected filters." }}
            </div>

            <slot v-else />
        </ReportLayout>

        <div
            v-else
            class="text-grey-7 text-center q-pa-lg"
        >
            Select filters and click "Generate Report" to view data.
        </div>
    </div>
</template>
