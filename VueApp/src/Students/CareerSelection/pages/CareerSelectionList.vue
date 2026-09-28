<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { CAREER_SELECTION_PERMISSIONS } from "../constants/permissions"
import CompletenessIcon from "../components/CompletenessIcon.vue"
import CareerSelectionPageHeading from "../components/CareerSelectionPageHeading.vue"
import CareerSelectionTable from "../components/CareerSelectionTable.vue"
import { careerSelectionService } from "../services/career-selection-service.ts"
import { useReportExports } from "@/Students/composables/use-report-exports"
import AccessNoticeBanner from "@/Students/components/AccessNoticeBanner.vue"
import { useAccessNotice } from "@/Students/composables/use-access-notice"
import { CAREER_FIELDS } from "../utils/career-fields"
import { OVERVIEW_COLUMNS } from "../utils/career-columns"
import type { StudentCareerListItem } from "../types"

// Set when the user was sent here from a page they may not use, such as the options page.
const accessNotice = useAccessNotice()

const loading = ref(false)
const rows = ref<StudentCareerListItem[]>([])

const isAdmin = computed(() => checkHasOnePermission([CAREER_SELECTION_PERMISSIONS.ADMIN]))

// Fields shown as a completeness icon; the rest are plain text.
const completenessFields = CAREER_FIELDS.filter((f) => f.completedField)

// CSV is left off for now, as Excel covers it; the service and endpoints still serve it.
const { handleExcelExport, handlePdfExport } = useReportExports(
    rows,
    {
        downloadExcel: careerSelectionService.downloadOverviewExcel,
        downloadPdf: careerSelectionService.downloadOverviewPdf,
    },
    { exportWhenEmpty: true },
)

async function load(): Promise<void> {
    loading.value = true
    rows.value = await careerSelectionService.getList()
    loading.value = false
}

onMounted(load)
</script>

<template>
    <div class="q-pa-md">
        <q-breadcrumbs class="q-mb-sm">
            <q-breadcrumbs-el label="Career Selection" />
            <q-breadcrumbs-el label="Overview" />
        </q-breadcrumbs>

        <CareerSelectionPageHeading title="Career Selection Overview" />

        <AccessNoticeBanner :notice="accessNotice" />

        <CareerSelectionTable
            :rows="rows"
            :columns="OVERVIEW_COLUMNS"
            :loading="loading"
            :rows-per-page="50"
            :can-edit="isAdmin"
            label="Career selection overview table"
            :cell-fields="completenessFields"
            column-storage-key="career-selection-hidden-columns"
            :excel-export="handleExcelExport"
            :pdf-export="handlePdfExport"
            :report-route="{ name: 'CareerSelectionReport' }"
        >
            <template #field-cell="{ field, cell }">
                <q-td :props="cell">
                    <CompletenessIcon
                        :complete="cell.row[field.completedField!] ?? false"
                        :show-missing="!field.optional"
                        :label="field.tooltipLabel"
                    />
                </q-td>
            </template>

            <template #card-field="{ field, row, value }">
                <div
                    v-if="field.completedField"
                    class="row items-center q-gutter-md q-mb-xs"
                >
                    <span class="row items-center q-gutter-xs">
                        <CompletenessIcon
                            :complete="row[field.completedField] === true"
                            :show-missing="!field.optional"
                            :label="field.tooltipLabel"
                        />
                        <span class="text-caption">{{ field.label }}</span>
                    </span>
                </div>
                <div
                    v-else-if="value"
                    class="text-caption q-mb-xs"
                >
                    {{ field.label }}: {{ value }}
                </div>
            </template>
        </CareerSelectionTable>
    </div>
</template>
