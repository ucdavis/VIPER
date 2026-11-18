<template>
    <section
        aria-labelledby="report-admin-preview"
        class="q-mb-lg"
    >
        <h2 id="report-admin-preview">Preview</h2>
        <p class="text-grey">
            Runs the report as the form now stands, without saving it, {{ PREVIEW_ROWS }} rows at a time. Parameters use
            their defaults.
        </p>
        <q-btn
            outline
            no-caps
            color="primary"
            icon="visibility"
            label="Preview"
            :loading="running"
            @click="preview(1)"
        >
            <template #loading>
                <q-spinner
                    size="1em"
                    class="q-mr-sm"
                />
                Preview
            </template>
        </q-btn>
        <StatusBanner
            v-if="errors.length > 0"
            type="error"
            class="q-mt-md"
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
        <ReportBuilderTable
            v-if="result !== null"
            class="q-mt-md"
            :columns="result.columns"
            :rows="result.rows"
            :pagination="pagination"
            :paginate="true"
            title="Preview"
            :loading="running"
            @request="changePage"
        />
    </section>
</template>

<script setup lang="ts">
import { ref } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import ReportBuilderTable from "@/shared/report-builder/ReportBuilderTable.vue"
import type { ReportPagination } from "@/shared/report-builder/use-report-builder"
import type { ReportRunResult } from "@/shared/report-builder/report-builder-types"
import { reportAdminService } from "../admin/report-admin-service"
import type { ReportEditRequest } from "../admin/report-admin-types"

/** A page of the unsaved report, so the admin can check it before saving. */
const { report } = defineProps<{ report: ReportEditRequest }>()

// The server's preview page size (ReportLimits.PreviewRows).
const PREVIEW_ROWS = 50

const result = ref<ReportRunResult | null>(null)
const errors = ref<string[]>([])
const running = ref(false)
const pagination = ref<ReportPagination>({
    page: 1,
    rowsPerPage: PREVIEW_ROWS,
    sortBy: null,
    descending: false,
    rowsNumber: 0,
})

async function preview(page: number): Promise<void> {
    running.value = true
    const { sortBy, descending } = pagination.value
    const outcome = await reportAdminService.preview(report, {
        parameters: {},
        page,
        perPage: null,
        sortBy,
        sortDescending: descending,
        search: null,
    })
    running.value = false
    result.value = outcome.result
    errors.value = outcome.errors
    pagination.value = {
        ...pagination.value,
        page,
        rowsNumber: outcome.result?.totalRows ?? (page - 1) * PREVIEW_ROWS + (outcome.result?.rows.length ?? 0),
    }
}

async function changePage(next: Omit<ReportPagination, "rowsNumber">): Promise<void> {
    pagination.value = { ...pagination.value, sortBy: next.sortBy, descending: next.descending }
    await preview(next.page)
}
</script>
