<script setup lang="ts">
import { useVModel } from "@vueuse/core"
import { ref } from "vue"
import type { RouteLocationRaw } from "vue-router"

const props = defineProps<{
    filter: string
    excelExport?: () => Promise<void>
    pdfExport?: () => Promise<void>
    reportRoute?: RouteLocationRaw
}>()

const emit = defineEmits<{
    (e: "update:filter", value: string): void
}>()

const filterModel = useVModel(props, "filter", emit)
const exportingExcel = ref(false)
const exportingPdf = ref(false)

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
            v-if="reportRoute"
            flat
            dense
            no-caps
            icon="assessment"
            label="Report"
            class="q-mr-sm"
            :to="reportRoute"
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
            label="Print/PDF"
            class="q-mr-sm"
            :loading="exportingPdf"
            @click="handlePdfExport"
        >
            <template #loading>
                <q-spinner
                    size="1em"
                    class="q-mr-sm"
                />
                Print/PDF
            </template>
        </q-btn>
        <q-input
            v-model="filterModel"
            dense
            outlined
            debounce="300"
            placeholder="Search"
            class="q-ml-sm"
            clearable
            clear-icon="close"
        >
            <template #append>
                <q-icon
                    v-if="!filterModel"
                    name="search"
                />
            </template>
        </q-input>
    </div>
</template>
