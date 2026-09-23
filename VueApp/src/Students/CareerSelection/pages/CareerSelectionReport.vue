<script setup lang="ts">
import { ref, onMounted, computed } from "vue"
import { careerSelectionService } from "../services/career-selection-service"
import CareerSelectionPageHeading from "../components/CareerSelectionPageHeading.vue"
import CareerSelectionTable from "../components/CareerSelectionTable.vue"
import { useReportExports } from "@/Students/composables/use-report-exports"
import { CAREER_FIELDS } from "../utils/career-fields"
import { REPORT_COLUMNS, previewStatement } from "../utils/career-columns"
import type { StudentCareerReport } from "../types"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { CAREER_SELECTION_PERMISSIONS } from "../constants/permissions"

const loading = ref(false)
const rows = ref<StudentCareerReport[]>([])

const statementFields = CAREER_FIELDS.filter((f) => f.statement)

const { handleExcelExport, handlePdfExport, handleCsvExport } = useReportExports(rows, {
    downloadExcel: careerSelectionService.downloadExcel,
    openPdf: careerSelectionService.openPdf,
    downloadCsv: careerSelectionService.downloadCsv,
})

async function load(): Promise<void> {
    loading.value = true
    rows.value = await careerSelectionService.getReport()
    loading.value = false
}

const isAdmin = computed(() => checkHasOnePermission([CAREER_SELECTION_PERMISSIONS.ADMIN]))

onMounted(load)
</script>

<template>
    <div class="q-pa-md">
        <q-breadcrumbs class="q-mb-sm">
            <q-breadcrumbs-el
                label="Career Selection"
                :to="{ name: 'CareerSelectionList' }"
            />
            <q-breadcrumbs-el label="Report" />
        </q-breadcrumbs>

        <CareerSelectionPageHeading title="Career Selection Report" />

        <CareerSelectionTable
            :rows="rows"
            :columns="REPORT_COLUMNS"
            :loading="loading"
            :rows-per-page="25"
            :can-edit="isAdmin"
            label="Career selection report table"
            :cell-fields="statementFields"
            :excel-export="handleExcelExport"
            :csv-export="handleCsvExport"
            :pdf-export="handlePdfExport"
            :overview-route="{ name: 'CareerSelectionList' }"
        >
            <!-- The excerpt is applied here rather than as a column format, so search still
                 reaches the whole statement (see career-columns). -->
            <template #field-cell="{ field, cell }">
                <q-td :props="cell">
                    {{ previewStatement(cell.row[field.valueField]) }}
                </q-td>
            </template>

            <template #card-field="{ field, value }">
                <div
                    v-if="value"
                    class="row items-center q-gutter-md q-mb-xs"
                >
                    <span class="row items-center q-gutter-xs">
                        {{ field.label }}:
                        {{ field.statement ? previewStatement(value) : value }}
                    </span>
                </div>
            </template>
        </CareerSelectionTable>
    </div>
</template>
