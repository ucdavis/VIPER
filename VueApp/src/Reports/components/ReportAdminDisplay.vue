<template>
    <section
        aria-labelledby="report-admin-display"
        class="q-mb-lg"
    >
        <h2 id="report-admin-display">Display and downloads</h2>
        <div class="row q-col-gutter-md items-start">
            <div class="col-12 col-sm-6 col-md-3">
                <q-toggle
                    :model-value="model.paginate"
                    label="Show a page at a time"
                    @update:model-value="update({ paginate: $event })"
                />
            </div>
            <q-input
                :model-value="model.pageSize"
                label="Rows per page"
                :hint="`Empty for ${DEFAULT_PAGE_SIZE}`"
                type="number"
                :min="1"
                :max="MAX_PAGE_SIZE"
                :disable="!model.paginate"
                outlined
                dense
                class="col-12 col-sm-6 col-md-3"
                @update:model-value="update({ pageSize: toNumber($event) })"
            />
            <q-select
                :model-value="model.orderBy"
                label="Sort by"
                :options="sortOptions"
                clearable
                outlined
                dense
                options-dense
                class="col-12 col-sm-6 col-md-3"
                @update:model-value="update({ orderBy: $event ?? null })"
            />
            <div class="col-12 col-sm-6 col-md-3">
                <q-toggle
                    :model-value="model.orderByDescending"
                    label="Largest or latest first"
                    :disable="model.orderBy === null"
                    @update:model-value="update({ orderByDescending: $event })"
                />
            </div>
            <div class="col-12">
                <q-checkbox
                    :model-value="model.allowExcel"
                    label="Excel download"
                    class="q-mr-lg"
                    @update:model-value="update({ allowExcel: $event })"
                />
                <q-checkbox
                    :model-value="model.allowPdf"
                    label="PDF download"
                    class="q-mr-lg"
                    @update:model-value="update({ allowPdf: $event })"
                />
                <q-checkbox
                    :model-value="model.pdfLandscape"
                    label="PDF in landscape"
                    :disable="!model.allowPdf"
                    @update:model-value="update({ pdfLandscape: $event })"
                />
            </div>
            <q-input
                :model-value="model.sensitiveNotice"
                label="Confidentiality notice"
                hint="Shown above the table and on every download, for confidential data"
                maxlength="500"
                outlined
                dense
                class="col-12"
                @update:model-value="update({ sensitiveNotice: emptyAsNull($event) })"
            />
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { ReportAdminForm } from "../admin/report-admin-form"

/** Paging, the default sort, the downloads offered, and the confidentiality notice. */
const { sourceColumns } = defineProps<{
    /** The source's columns, any of which the report can be sorted by. */
    sourceColumns: string[]
}>()

const model = defineModel<ReportAdminForm>({ required: true })

// The server's defaults (ReportLimits).
const DEFAULT_PAGE_SIZE = 25
const MAX_PAGE_SIZE = 500

/** The source's columns, or the report's own when SQL Server can't describe the source. */
const sortOptions = computed(() =>
    sourceColumns.length > 0
        ? sourceColumns
        : model.value.columns.map((column) => column.sourceColumn).filter((name) => name !== ""),
)

function toNumber(value: string | number | null): number | null {
    const number = Number(value)
    return value === null || value === "" || !Number.isFinite(number) ? null : number
}

function emptyAsNull(value: string | number | null): string | null {
    return value === null || String(value).trim() === "" ? null : String(value)
}

function update(change: Partial<ReportAdminForm>): void {
    model.value = { ...model.value, ...change }
}
</script>
