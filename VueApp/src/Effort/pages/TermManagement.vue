<template>
    <div class="q-pa-md">
        <h2>Term Management</h2>

        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading terms...
        </div>

        <template v-else>
            <!-- Add Future Term Section -->
            <div
                v-if="availableTerms.length > 0"
                class="q-mb-lg"
            >
                <div class="text-subtitle1 q-mb-sm">Add Future Term</div>
                <div class="row items-center q-gutter-sm">
                    <q-select
                        v-model="selectedNewTerm"
                        :options="availableTerms"
                        option-label="termName"
                        option-value="termCode"
                        label="Select a term to add"
                        dense
                        options-dense
                        outlined
                        style="min-width: 250px"
                    />
                    <q-btn
                        label="Add Term"
                        color="primary"
                        dense
                        :disable="!selectedNewTerm"
                        @click="confirmAddTerm"
                    />
                </div>
            </div>
            <!-- Desktop: Table view -->
            <q-table
                :rows="terms"
                :columns="columns"
                row-key="termCode"
                dense
                flat
                bordered
                :pagination="{ rowsPerPage: 0 }"
                hide-pagination
                class="gt-xs"
            >
                <template #body-cell-termName="props">
                    <q-td :props="props">
                        <div class="row items-center no-wrap">
                            <router-link
                                :to="`/Effort/${props.row.termCode}`"
                                class="text-primary"
                            >
                                {{ props.row.termName }}
                            </router-link>
                            <q-btn
                                v-if="props.row.canDelete"
                                icon="delete"
                                color="negative"
                                dense
                                flat
                                round
                                size="sm"
                                class="q-ml-sm"
                                @click="confirmDeleteTerm(props.row)"
                                @keyup.enter="confirmDeleteTerm(props.row)"
                                @keyup.space="confirmDeleteTerm(props.row)"
                            >
                                <q-tooltip>Delete term</q-tooltip>
                            </q-btn>
                        </div>
                    </q-td>
                </template>
                <template #body-cell-harvestedDate="props">
                    <q-td :props="props">
                        {{ formatDate(props.row.harvestedDate) }}
                    </q-td>
                </template>
                <template #body-cell-openedDate="props">
                    <q-td :props="props">
                        <div class="row items-center no-wrap">
                            <span>{{ formatDate(props.row.openedDate) }}</span>
                            <div class="q-ml-sm">
                                <q-btn
                                    v-if="props.row.canOpen"
                                    icon="add_circle"
                                    label="Open"
                                    color="positive"
                                    dense
                                    flat
                                    no-caps
                                    size="sm"
                                    @click="confirmOpenTerm(props.row)"
                                    @keyup.enter="confirmOpenTerm(props.row)"
                                    @keyup.space="confirmOpenTerm(props.row)"
                                />
                                <q-btn
                                    v-if="props.row.canUnopen"
                                    icon="history"
                                    label="Revert to Unopened"
                                    color="warning"
                                    dense
                                    flat
                                    no-caps
                                    size="sm"
                                    @click="confirmUnopenTerm(props.row)"
                                    @keyup.enter="confirmUnopenTerm(props.row)"
                                    @keyup.space="confirmUnopenTerm(props.row)"
                                />
                            </div>
                        </div>
                    </q-td>
                </template>
                <template #body-cell-closedDate="props">
                    <q-td :props="props">
                        <div class="row items-center no-wrap">
                            <span>{{ formatDate(props.row.closedDate) }}</span>
                            <div class="q-ml-sm">
                                <q-btn
                                    v-if="props.row.canClose"
                                    icon="lock"
                                    label="Close"
                                    color="negative"
                                    dense
                                    flat
                                    no-caps
                                    size="sm"
                                    @click="confirmCloseTerm(props.row)"
                                    @keyup.enter="confirmCloseTerm(props.row)"
                                    @keyup.space="confirmCloseTerm(props.row)"
                                />
                                <q-btn
                                    v-if="props.row.canReopen"
                                    icon="lock_open"
                                    label="Reopen"
                                    color="grey-7"
                                    dense
                                    flat
                                    no-caps
                                    size="sm"
                                    @click="confirmReopenTerm(props.row)"
                                    @keyup.enter="confirmReopenTerm(props.row)"
                                    @keyup.space="confirmReopenTerm(props.row)"
                                />
                            </div>
                        </div>
                    </q-td>
                </template>
            </q-table>

            <!-- Mobile: Card view -->
            <div class="lt-sm">
                <q-card
                    v-for="term in terms"
                    :key="term.termCode"
                    flat
                    bordered
                    class="q-mb-sm"
                >
                    <q-card-section class="q-py-sm">
                        <div class="text-subtitle1">
                            <router-link
                                :to="`/Effort/${term.termCode}`"
                                class="text-primary"
                            >
                                {{ term.termName }}
                            </router-link>
                        </div>
                        <div class="text-caption text-grey-7 q-mt-xs">
                            <span v-if="term.harvestedDate">Harvested: {{ formatDate(term.harvestedDate) }}</span>
                            <span
                                v-if="term.openedDate"
                                class="q-ml-md"
                                >Opened: {{ formatDate(term.openedDate) }}</span
                            >
                            <span
                                v-if="term.closedDate"
                                class="q-ml-md"
                                >Closed: {{ formatDate(term.closedDate) }}</span
                            >
                        </div>
                    </q-card-section>
                    <q-card-actions
                        align="left"
                        class="q-pt-none"
                    >
                        <q-btn
                            v-if="term.canOpen"
                            icon="add_circle"
                            label="Open"
                            color="positive"
                            dense
                            flat
                            no-caps
                            size="sm"
                            @click="confirmOpenTerm(term)"
                            @keyup.enter="confirmOpenTerm(term)"
                            @keyup.space="confirmOpenTerm(term)"
                        />
                        <q-btn
                            v-if="term.canUnopen"
                            icon="history"
                            label="Revert"
                            color="warning"
                            dense
                            flat
                            no-caps
                            size="sm"
                            @click="confirmUnopenTerm(term)"
                            @keyup.enter="confirmUnopenTerm(term)"
                            @keyup.space="confirmUnopenTerm(term)"
                        />
                        <q-btn
                            v-if="term.canClose"
                            icon="lock"
                            label="Close"
                            color="negative"
                            dense
                            flat
                            no-caps
                            size="sm"
                            @click="confirmCloseTerm(term)"
                            @keyup.enter="confirmCloseTerm(term)"
                            @keyup.space="confirmCloseTerm(term)"
                        />
                        <q-btn
                            v-if="term.canReopen"
                            icon="lock_open"
                            label="Reopen"
                            color="grey-7"
                            dense
                            flat
                            no-caps
                            size="sm"
                            @click="confirmReopenTerm(term)"
                            @keyup.enter="confirmReopenTerm(term)"
                            @keyup.space="confirmReopenTerm(term)"
                        />
                        <q-btn
                            v-if="term.canDelete"
                            icon="delete"
                            label="Delete"
                            color="negative"
                            dense
                            flat
                            no-caps
                            size="sm"
                            @click="confirmDeleteTerm(term)"
                            @keyup.enter="confirmDeleteTerm(term)"
                            @keyup.space="confirmDeleteTerm(term)"
                        />
                    </q-card-actions>
                </q-card>
            </div>

            <div class="q-mt-lg">
                <router-link
                    to="/Effort/terms"
                    class="text-primary"
                >
                    Back to Term Selection
                </router-link>
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue"
import { useQuasar } from "quasar"
import { effortService } from "../services/effort-service"
import { useDateFunctions } from "@/composables/DateFunctions"
import type { TermDto, AvailableTermDto } from "../types"
import type { QTableColumn } from "quasar"

const $q = useQuasar()
const { formatDate } = useDateFunctions()

const terms = ref<TermDto[]>([])
const availableTerms = ref<AvailableTermDto[]>([])
const selectedNewTerm = ref<AvailableTermDto | null>(null)
const isLoading = ref(false)

const columns: QTableColumn[] = [
    { name: "termName", label: "Term", field: "termName", align: "left" },
    { name: "harvestedDate", label: "Harvested", field: "harvestedDate", align: "left" },
    { name: "openedDate", label: "Opened", field: "openedDate", align: "left" },
    { name: "closedDate", label: "Closed", field: "closedDate", align: "left" },
]

async function loadTerms() {
    isLoading.value = true
    try {
        const [termsResult, availableResult] = await Promise.all([
            effortService.getTerms(),
            effortService.getAvailableTerms(),
        ])
        terms.value = termsResult
        availableTerms.value = availableResult
        selectedNewTerm.value = null
    } finally {
        isLoading.value = false
    }
}

function confirmAddTerm() {
    if (!selectedNewTerm.value) return
    const term = selectedNewTerm.value
    $q.dialog({
        title: "Add Term",
        message: `Add "${term.termName}" to the Effort system?`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await addTerm(term.termCode)
    })
}

async function addTerm(termCode: number) {
    const result = await effortService.createTerm(termCode)
    if (result) {
        $q.notify({ type: "positive", message: "Term added successfully" })
        await loadTerms()
    }
}

function confirmOpenTerm(term: TermDto) {
    $q.dialog({
        title: "Open Term",
        message: `Are you sure you want to open "${term.termName}" for effort entry?`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await openTerm(term.termCode)
    })
}

async function openTerm(termCode: number) {
    const result = await effortService.openTerm(termCode)
    if (result) {
        $q.notify({ type: "positive", message: "Term opened successfully" })
        await loadTerms()
    }
}

function confirmCloseTerm(term: TermDto) {
    $q.dialog({
        title: "Close Term",
        message: `Are you sure you want to close "${term.termName}"?`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await closeTerm(term.termCode)
    })
}

async function closeTerm(termCode: number) {
    const result = await effortService.closeTerm(termCode)
    if (result.success) {
        $q.notify({ type: "positive", message: "Term closed successfully" })
        await loadTerms()
    }
    // Errors are handled by the global error handler (GenericError.vue)
}

function confirmReopenTerm(term: TermDto) {
    $q.dialog({
        title: "Reopen Term",
        message: `Are you sure you want to reopen "${term.termName}"?`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await reopenTerm(term.termCode)
    })
}

async function reopenTerm(termCode: number) {
    const result = await effortService.reopenTerm(termCode)
    if (result) {
        $q.notify({ type: "positive", message: "Term reopened successfully" })
        await loadTerms()
    }
}

function confirmUnopenTerm(term: TermDto) {
    $q.dialog({
        title: "Revert Term",
        message: `Are you sure you want to revert "${term.termName}" to unopened status?`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await unopenTerm(term.termCode)
    })
}

async function unopenTerm(termCode: number) {
    const result = await effortService.unopenTerm(termCode)
    if (result) {
        $q.notify({ type: "positive", message: "Term reverted to unopened status" })
        await loadTerms()
    }
}

function confirmDeleteTerm(term: TermDto) {
    $q.dialog({
        title: "Delete Term",
        message: `Are you sure you want to delete "${term.termName}"? This cannot be undone.`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await deleteTerm(term.termCode)
    })
}

async function deleteTerm(termCode: number) {
    const success = await effortService.deleteTerm(termCode)
    if (success) {
        $q.notify({ type: "positive", message: "Term deleted successfully" })
        await loadTerms()
    }
}

onMounted(loadTerms)
</script>
