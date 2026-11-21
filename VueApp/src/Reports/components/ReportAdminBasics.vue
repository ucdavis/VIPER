<template>
    <section
        aria-labelledby="report-admin-basics"
        class="q-mb-lg"
    >
        <h2 id="report-admin-basics">Report</h2>
        <div class="row q-col-gutter-md">
            <q-input
                :model-value="model.title"
                label="Title *"
                :rules="[(value: string) => value.trim() !== '' || 'Enter a title.']"
                maxlength="500"
                outlined
                dense
                class="col-12 col-md-6"
                @update:model-value="update({ title: String($event ?? '') })"
            />
            <q-input
                :model-value="model.area"
                label="Area"
                hint="Groups the report in the list, such as Personnel or Effort"
                maxlength="500"
                outlined
                dense
                class="col-12 col-md-6"
                @update:model-value="update({ area: text($event) })"
            />
            <q-input
                :model-value="model.description"
                label="Description"
                type="textarea"
                autogrow
                outlined
                dense
                class="col-12"
                @update:model-value="update({ description: text($event) })"
            />
            <q-select
                :model-value="model.source || null"
                label="Data source *"
                :options="sourceOptions"
                option-value="value"
                option-label="label"
                emit-value
                map-options
                outlined
                dense
                options-dense
                class="col-12 col-md-6"
                @update:model-value="emit('source', $event ?? '')"
            >
                <template #option="scope">
                    <q-item v-bind="scope.itemProps">
                        <q-item-section>
                            <q-item-label>{{ scope.opt.label }}</q-item-label>
                            <q-item-label caption>{{ scope.opt.caption }}</q-item-label>
                        </q-item-section>
                    </q-item>
                </template>
            </q-select>
        </div>
        <StatusBanner
            v-if="sourcesFailed"
            type="error"
            class="q-mt-md"
        >
            The list of data sources couldn't be loaded.
        </StatusBanner>
        <StatusBanner
            v-if="columnsError"
            type="warning"
            class="q-mt-md"
        >
            SQL Server can't describe this procedure's columns ({{ columnsError }}). Enter the column names yourself.
        </StatusBanner>
    </section>
</template>

<script setup lang="ts">
import { computed } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import type { ReportAdminForm } from "../admin/report-admin-form"
import type { ReportSourceSummary } from "../admin/report-admin-types"

/** The report's title, description, area and data source. */
const {
    sources,
    sourcesFailed = false,
    columnsError = null,
} = defineProps<{
    sources: ReportSourceSummary[]
    sourcesFailed?: boolean
    /** Why the chosen procedure's columns are unknown, if they are. */
    columnsError?: string | null
}>()

const emit = defineEmits<{ (e: "source", name: string): void }>()

const model = defineModel<ReportAdminForm>({ required: true })

const sourceOptions = computed(() =>
    sources.map((source) => ({
        value: source.name,
        label: source.name,
        caption: source.description ? `${source.kind}: ${source.description}` : source.kind,
    })),
)

function text(value: string | number | null): string | null {
    const trimmed = String(value ?? "").trim()
    return trimmed === "" ? null : String(value)
}

function update(change: Partial<ReportAdminForm>): void {
    model.value = { ...model.value, ...change }
}
</script>
