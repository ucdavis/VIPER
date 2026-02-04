<template>
    <div class="q-pa-md">
        <h2>Select a Term</h2>

        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading terms...
        </div>

        <template v-else>
            <!-- Unopened Terms - only visible to ManageTerms users -->
            <div
                v-if="hasManageTerms && unopenedTerms.length > 0"
                class="q-mb-lg"
            >
                <!-- Desktop: Table -->
                <q-table
                    title="Unopened Terms"
                    :rows="unopenedTerms"
                    :columns="unopenedColumns"
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
                            <router-link
                                :to="`/Effort/${props.row.termCode}`"
                                class="text-primary"
                            >
                                {{ props.row.termName }}
                            </router-link>
                        </q-td>
                    </template>
                    <template #body-cell-harvestedDate="props">
                        <q-td :props="props">
                            {{ formatDate(props.row.harvestedDate) }}
                        </q-td>
                    </template>
                </q-table>
                <!-- Mobile: List -->
                <div class="lt-sm">
                    <div class="text-subtitle2 q-mb-xs">Unopened Terms</div>
                    <q-list
                        bordered
                        separator
                        dense
                    >
                        <q-item
                            v-for="term in unopenedTerms"
                            :key="term.termCode"
                            clickable
                            v-ripple
                            :to="`/Effort/${term.termCode}`"
                        >
                            <q-item-section>
                                <q-item-label>{{ term.termName }}</q-item-label>
                                <q-item-label
                                    caption
                                    v-if="term.harvestedDate"
                                >
                                    Harvested: {{ formatDate(term.harvestedDate) }}
                                </q-item-label>
                            </q-item-section>
                            <q-item-section side>
                                <q-icon name="chevron_right" />
                            </q-item-section>
                        </q-item>
                    </q-list>
                </div>
            </div>

            <!-- Open Terms -->
            <div class="q-mb-lg">
                <!-- Desktop: Table -->
                <q-table
                    title="Open Terms"
                    :rows="openTerms"
                    :columns="openColumns"
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
                            <router-link
                                :to="`/Effort/${props.row.termCode}`"
                                class="text-primary"
                            >
                                {{ props.row.termName }}
                            </router-link>
                        </q-td>
                    </template>
                    <template #body-cell-harvestedDate="props">
                        <q-td :props="props">
                            {{ formatDate(props.row.harvestedDate) }}
                        </q-td>
                    </template>
                    <template #body-cell-openedDate="props">
                        <q-td :props="props">
                            {{ formatDate(props.row.openedDate) }}
                        </q-td>
                    </template>
                    <template #no-data>
                        <div class="text-grey q-pa-sm">No terms are currently open for editing</div>
                    </template>
                </q-table>
                <!-- Mobile: List -->
                <div class="lt-sm">
                    <div class="text-subtitle2 q-mb-xs">Open Terms</div>
                    <q-list
                        bordered
                        separator
                        dense
                    >
                        <q-item
                            v-for="term in openTerms"
                            :key="term.termCode"
                            clickable
                            v-ripple
                            :to="`/Effort/${term.termCode}`"
                        >
                            <q-item-section>
                                <q-item-label>{{ term.termName }}</q-item-label>
                                <q-item-label caption> Opened: {{ formatDate(term.openedDate ?? "") }} </q-item-label>
                            </q-item-section>
                            <q-item-section side>
                                <q-icon name="chevron_right" />
                            </q-item-section>
                        </q-item>
                        <q-item v-if="openTerms.length === 0">
                            <q-item-section class="text-grey"> No terms are currently open for editing </q-item-section>
                        </q-item>
                    </q-list>
                </div>
            </div>

            <!-- Closed Terms -->
            <div class="q-mb-lg">
                <!-- Desktop: Table -->
                <q-table
                    title="Closed Terms"
                    :rows="closedTerms"
                    :columns="closedColumns"
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
                            <router-link
                                :to="`/Effort/${props.row.termCode}`"
                                class="text-primary"
                            >
                                {{ props.row.termName }}
                            </router-link>
                        </q-td>
                    </template>
                    <template #body-cell-harvestedDate="props">
                        <q-td :props="props">
                            {{ formatDate(props.row.harvestedDate) }}
                        </q-td>
                    </template>
                    <template #body-cell-openedDate="props">
                        <q-td :props="props">
                            {{ formatDate(props.row.openedDate) }}
                        </q-td>
                    </template>
                    <template #body-cell-closedDate="props">
                        <q-td :props="props">
                            {{ formatDate(props.row.closedDate) }}
                        </q-td>
                    </template>
                </q-table>
                <!-- Mobile: List -->
                <div class="lt-sm">
                    <div class="text-subtitle2 q-mb-xs">Closed Terms</div>
                    <q-list
                        bordered
                        separator
                        dense
                    >
                        <q-item
                            v-for="term in closedTerms"
                            :key="term.termCode"
                            clickable
                            v-ripple
                            :to="`/Effort/${term.termCode}`"
                        >
                            <q-item-section>
                                <q-item-label>{{ term.termName }}</q-item-label>
                                <q-item-label caption> Closed: {{ formatDate(term.closedDate ?? "") }} </q-item-label>
                            </q-item-section>
                            <q-item-section side>
                                <q-icon name="chevron_right" />
                            </q-item-section>
                        </q-item>
                    </q-list>
                </div>
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { termService } from "../services/term-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import { useDateFunctions } from "@/composables/DateFunctions"
import type { TermDto } from "../types"
import type { QTableColumn } from "quasar"

const { hasManageTerms } = useEffortPermissions()
const { formatDate } = useDateFunctions()

const terms = ref<TermDto[]>([])
const isLoading = ref(false)

// Filter terms by date fields to match legacy system logic (ChangeTerms.cfm)
// Legacy uses: open = opened IS NOT NULL AND closed IS NULL
//              closed = closed IS NOT NULL
//              unopened = opened IS NULL
const unopenedTerms = computed(() => terms.value.filter((t) => !t.openedDate && !t.closedDate))
const openTerms = computed(() => terms.value.filter((t) => t.openedDate && !t.closedDate))
const closedTerms = computed(() => terms.value.filter((t) => t.closedDate))

// Column definitions
const unopenedColumns: QTableColumn[] = [
    { name: "termName", label: "Term", field: "termName", align: "left" },
    { name: "harvestedDate", label: "Harvested", field: "harvestedDate", align: "center" },
]

const openColumns: QTableColumn[] = [
    { name: "termName", label: "Term", field: "termName", align: "left" },
    { name: "harvestedDate", label: "Harvested", field: "harvestedDate", align: "center" },
    { name: "openedDate", label: "Opened", field: "openedDate", align: "center" },
]

const closedColumns: QTableColumn[] = [
    { name: "termName", label: "Term", field: "termName", align: "left" },
    { name: "harvestedDate", label: "Harvested", field: "harvestedDate", align: "center" },
    { name: "openedDate", label: "Opened", field: "openedDate", align: "center" },
    { name: "closedDate", label: "Closed", field: "closedDate", align: "center" },
]

async function loadTerms() {
    isLoading.value = true
    try {
        terms.value = await termService.getTerms()
    } finally {
        isLoading.value = false
    }
}

onMounted(loadTerms)
</script>
