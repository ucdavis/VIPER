<script setup lang="ts">
import { useVModel } from "@vueuse/core"
import { computed, ref } from "vue"
import { useQuasar } from "quasar"
import type { RouteLocationRaw } from "vue-router"

const props = withDefaults(
    defineProps<{
        filter?: string
        showSearch?: boolean
        excelExport?: () => Promise<void>
        csvExport?: () => Promise<void>
        pdfExport?: () => void | Promise<void>
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
        csvExport: undefined,
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

// On a phone a row of export buttons plus the search does not fit: the exports fold into one
// menu, and the row wraps so the search takes a line of its own.
const $q = useQuasar()
const compact = computed(() => $q.screen.xs)

const running = ref<string | null>(null) // The export currently running, if any.
const isBusy = computed(() => props.busy || running.value !== null)

/** Some reports with export options have both a report and an overview view that are included
 * alongside export options. Examples include Emergency Contact and Career Selection.
 */
const navButtons = computed(() =>
    [
        { key: "report", icon: "assessment", label: "Report", to: props.reportRoute },
        { key: "overview", icon: "list_alt", label: "Overview", to: props.overviewRoute },
    ].filter((button) => button.to !== undefined),
)

/**
 * The file export options, in the order they appear. Each one runs the handler its page passed and
 * spins until it settles; a page that passes no handler for a format shows no button for it.
 */
const exportButtons = computed(() =>
    [
        { key: "excel", icon: "table_chart", label: "Excel", class: "export-excel", run: props.excelExport },
        { key: "csv", icon: "grid_on", label: "CSV", class: "export-csv", run: props.csvExport },
        { key: "word", icon: "description", label: "Word", class: "export-word", run: props.wordExport },
        { key: "pdf", icon: "picture_as_pdf", label: "Print/PDF", class: "export-pdf", run: props.pdfExport },
    ].filter((button) => button.run !== undefined),
)

async function runExport(button: { key: string; run?: () => void | Promise<void> }): Promise<void> {
    if (!button.run) return
    running.value = button.key
    try {
        await button.run()
    } finally {
        running.value = null
    }
}
</script>

<template>
    <div
        class="row items-center"
        :class="{ 'no-wrap': !compact }"
    >
        <slot name="prepend" />
        <q-btn
            v-for="button in navButtons"
            :key="button.key"
            flat
            dense
            no-caps
            :icon="button.icon"
            :label="button.label"
            class="q-mr-sm"
            :to="button.to"
        />
        <q-btn-dropdown
            v-if="compact && exportButtons.length > 0"
            flat
            dense
            no-caps
            icon="download"
            label="Export"
            class="q-mr-sm export-menu"
            :disable="isBusy"
            :loading="running !== null"
        >
            <template #loading>
                <q-spinner
                    size="1em"
                    class="q-mr-sm"
                />
                Export
            </template>
            <q-list role="menu">
                <q-item
                    v-for="button in exportButtons"
                    :key="button.key"
                    v-close-popup
                    clickable
                    role="menuitem"
                    :class="button.class"
                    @click="runExport(button)"
                >
                    <q-item-section avatar>
                        <q-icon :name="button.icon" />
                    </q-item-section>
                    <q-item-section>{{ button.label }}</q-item-section>
                </q-item>
            </q-list>
        </q-btn-dropdown>
        <q-btn
            v-for="button in compact ? [] : exportButtons"
            :key="button.key"
            flat
            dense
            no-caps
            :icon="button.icon"
            :label="button.label"
            class="q-mr-sm"
            :class="button.class"
            :disable="isBusy"
            :loading="running === button.key"
            @click="runExport(button)"
        >
            <template #loading>
                <q-spinner
                    size="1em"
                    class="q-mr-sm"
                />
                {{ button.label }}
            </template>
        </q-btn>
        <q-btn
            v-if="printAction"
            flat
            dense
            no-caps
            icon="print"
            label="Print"
            class="q-mr-sm export-print"
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
            aria-label="Search"
            class="bg-white"
            :class="compact ? 'col-12 q-mt-sm' : 'q-ml-sm'"
            clearable
            :clear-value="''"
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

<style scoped>
.export-pdf {
    color: #b30b00;
}

.export-excel {
    color: #217346;
}

.export-csv {
    color: var(--q-primary);
}

.export-word {
    color: #2b579a;
}

.export-print {
    color: var(--q-primary);
}
</style>
