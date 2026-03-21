<script setup lang="ts">
import { useVModel } from "@vueuse/core"
import { ref } from "vue"
import type { QTableProps } from "quasar"
import { exportTable } from "@/composables/QuasarTableUtilities"

const props = defineProps<{
    filter: string
    columns?: QTableProps["columns"]
    rows?: unknown[]
    excelExport?: () => Promise<void>
    pdfExport?: () => Promise<void>
}>()

const emit = defineEmits<{
    (e: "update:filter", value: string): void
}>()

const filterModel = useVModel(props, "filter", emit)
const exportingExcel = ref(false)
const exportingPdf = ref(false)

function handleCsvExport(): void {
    if (props.columns && props.rows) {
        exportTable(props.columns, props.rows)
    }
}

async function handleExcelExport(): Promise<void> {
    if (!props.excelExport) {
        return
    }
    exportingExcel.value = true
    await props.excelExport()
    exportingExcel.value = false
}

async function handlePdfExport(): Promise<void> {
    if (!props.pdfExport) {
        return
    }
    exportingPdf.value = true
    await props.pdfExport()
    exportingPdf.value = false
}
</script>

<template>
    <div class="row items-center no-wrap">
        <q-btn
            flat
            dense
            no-caps
            icon="description"
            label="CSV"
            class="q-mr-sm"
            @click="handleCsvExport"
        />
        <q-btn
            flat
            dense
            no-caps
            icon="table_chart"
            label="Excel"
            class="q-mr-sm"
            :loading="exportingExcel"
            @click="handleExcelExport"
        >
            <template #loading>
                <q-spinner
                    size="1em"
                    class="q-mr-sm"
                />
                Excel
            </template>
        </q-btn>
        <q-btn
            flat
            dense
            no-caps
            icon="picture_as_pdf"
            label="PDF"
            class="q-mr-sm"
            :loading="exportingPdf"
            @click="handlePdfExport"
        >
            <template #loading>
                <q-spinner
                    size="1em"
                    class="q-mr-sm"
                />
                PDF
            </template>
        </q-btn>
        <q-input
            v-model="filterModel"
            dense
            outlined
            debounce="300"
            placeholder="Filter results"
            class="q-ml-sm"
            clearable
            clear-icon="close"
        >
            <template #append>
                <q-icon
                    v-if="!filterModel"
                    name="filter_alt"
                />
            </template>
        </q-input>
    </div>
</template>
