<template>
    <h1>Person Collector</h1>
    <div
        v-if="form === undefined"
        role="status"
    >
        <q-spinner-dots
            size="2rem"
            color="primary"
            aria-hidden="true"
        />
        <span class="sr-only">Loading the Person Collector</span>
    </div>
    <StatusBanner
        v-else-if="form === null"
        type="error"
    >
        The Person Collector could not be loaded. It needs the SVMSecure.Personnel.PersonCollector permission.
    </StatusBanner>
    <div
        v-else
        class="row q-col-gutter-md"
    >
        <div class="col-12 col-md-4">
            <PersonCollectorFilters
                v-model="request"
                :form="form"
            />
        </div>
        <div class="col-12 col-md-8">
            <q-card
                flat
                bordered
            >
                <q-card-section class="row items-center q-pb-sm">
                    <h2 class="text-h6 q-my-none col">Lists</h2>
                    <ExportToolbar
                        v-if="result"
                        :excel-export="exportExcel"
                        :busy="loading"
                    />
                </q-card-section>
                <q-linear-progress
                    :indeterminate="loading"
                    :value="0"
                    :class="{ invisible: !loading }"
                    color="primary"
                    aria-hidden="true"
                />
                <q-card-section :class="{ 'person-collector-stale': loading }">
                    <div
                        role="status"
                        class="sr-only"
                    >
                        {{ statusText }}
                    </div>
                    <StatusBanner
                        v-if="failed"
                        type="error"
                    >
                        The lists could not be loaded. Change a choice to try again.
                    </StatusBanner>
                    <PersonCollectorResults
                        v-else-if="result"
                        :result="result"
                    />
                    <div
                        v-else
                        class="text-center text-grey-8 q-pa-lg"
                    >
                        <q-icon
                            name="groups"
                            size="3rem"
                            aria-hidden="true"
                        />
                        <p class="q-mt-sm q-mb-none">
                            Choose faculty, staff or students on the left. The lists update as you choose.
                        </p>
                    </div>
                </q-card-section>
            </q-card>
        </div>
    </div>
</template>

<script setup lang="ts">
import { useDebounceFn } from "@vueuse/core"
import { inflect } from "inflection"
import { computed, onMounted, ref, watch } from "vue"
import ExportToolbar from "@/components/ExportToolbar.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import PersonCollectorFilters from "../components/PersonCollectorFilters.vue"
import PersonCollectorResults from "../components/PersonCollectorResults.vue"
import { emptyRequest, hasChoice, toApiRequest } from "../services/person-collector-request"
import { personCollectorService } from "../services/person-collector-service"
import type {
    PersonCollectorForm,
    PersonCollectorRequest,
    PersonCollectorResult,
} from "../types/person-collector-types"

/**
 * The legacy Person Collector as a single page: choices on the left, lists on the right. The
 * lists reload shortly after each change; a response that arrives after a newer change is
 * dropped, so a slow request can't overwrite newer lists.
 */
const RELOAD_DELAY_MS = 500

const form = ref<PersonCollectorForm | null | undefined>(undefined)
const request = ref<PersonCollectorRequest>(emptyRequest())
const result = ref<PersonCollectorResult | null>(null)
const loading = ref(false)
const failed = ref(false)
let loadId = 0

/** Read to screen readers when the lists change. */
const statusText = computed(() => {
    if (loading.value || result.value === null) {
        return ""
    }
    const total = result.value.sections.reduce((sum, section) => sum + section.people.length, 0)
    return `${total} ${inflect("person", total)} in ${result.value.sections.length} ${inflect("list", result.value.sections.length)}`
})

async function load(current: number, choices: PersonCollectorRequest) {
    const loaded = await personCollectorService.getResults(toApiRequest(choices))
    if (current !== loadId) {
        return
    }
    result.value = loaded
    failed.value = loaded === null
    loading.value = false
}

const loadSoon = useDebounceFn(load, RELOAD_DELAY_MS)

watch(request, (choices) => {
    loadId += 1
    failed.value = false
    if (!hasChoice(choices)) {
        result.value = null
        loading.value = false
        return
    }
    loading.value = true
    void loadSoon(loadId, choices)
})

function exportExcel(): Promise<void> {
    return personCollectorService.exportExcel(toApiRequest(request.value))
}

onMounted(async () => {
    form.value = await personCollectorService.getForm()
})
</script>

<style scoped>
.person-collector-stale {
    opacity: 0.6;
    transition: opacity 0.2s;
}
</style>
