<script setup lang="ts">
import { useVModel } from "@vueuse/core"
import { computed, ref } from "vue"
import type { RouteLocationRaw } from "vue-router"

const props = withDefaults(
    defineProps<{
        filter?: string
        showSearch?: boolean
        excelExport?: () => Promise<void>
        pdfExport?: () => Promise<void>
        wordExport?: () => Promise<void>
        printAction?: () => void
        busy?: boolean
        reportRoute?: RouteLocationRaw
        overviewRoute?: RouteLocationRaw
    }>(),
    {
        filter: "",
        showSearch: false,
        excelExport: undefined,
        pdfExport: undefined,
        wordExport: undefined,
        printAction: undefined,
        busy: false,
        reportRoute: undefined,
        overviewRoute: undefined,
    },
)

const emit = defineEmits<{
    (e: "update:filter", value: string): void
}>()

const filterModel = useVModel(props, "filter", emit)
const exportingExcel = ref(false)
const exportingPdf = ref(false)
const exportingWord = ref(false)
const isBusy = computed(() => props.busy || exportingExcel.value || exportingPdf.value || exportingWord.value)

async function handleExcelExport(): Promise<void> {
    if (!props.excelExport) return
    exportingExcel.value = true
    try {
        await props.excelExport()
    } finally {
        exportingExcel.value = false
    }
}

async function handlePdfExport(): Promise<void> {
    if (!props.pdfExport) return
    exportingPdf.value = true
    try {
        await props.pdfExport()
    } finally {
        exportingPdf.value = false
    }
}

async function handleWordExport(): Promise<void> {
    if (!props.wordExport) return
    exportingWord.value = true
    try {
        await props.wordExport()
    } finally {
        exportingWord.value = false
    }
}
</script>

<template>
    <div class="row items-center no-wrap">
        <slot name="prepend" />
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
            v-if="overviewRoute"
            flat
            dense
            no-caps
            icon="list_alt"
            label="Overview"
            class="q-mr-sm"
            :to="overviewRoute"
        />
        <q-btn
            v-if="excelExport"
            flat
            dense
            no-caps
            icon="table_chart"
            label="Excel"
            class="q-mr-sm"
            :disable="isBusy"
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
            v-if="wordExport"
            flat
            dense
            no-caps
            icon="description"
            label="Word"
            class="q-mr-sm"
            :disable="isBusy"
            :loading="exportingWord"
            @click="handleWordExport"
        >
            <template #loading>
                <q-spinner
                    size="1em"
                    class="q-mr-sm"
                />
                Word
            </template>
        </q-btn>
        <q-btn
            v-if="pdfExport"
            flat
            dense
            no-caps
            icon="picture_as_pdf"
            label="Print/PDF"
            class="q-mr-sm"
            :disable="isBusy"
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
        <q-btn
            v-if="printAction"
            flat
            dense
            no-caps
            icon="print"
            label="Print"
            class="q-mr-sm"
            @click="printAction"
        />
        <slot name="append" />
        <q-input
            v-if="showSearch"
            v-model="filterModel"
            dense
            outlined
            debounce="300"
            placeholder="Search"
            class="q-ml-sm bg-white"
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
