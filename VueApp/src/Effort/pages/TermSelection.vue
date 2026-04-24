<template>
    <div class="q-pa-md">
        <h1>Select a Term</h1>

        <div
            v-if="isLoading"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading terms...</div>
        </div>

        <template v-else>
            <!-- Unopened Terms - only visible to ManageTerms users -->
            <TermTable
                v-if="hasManageTerms && unopenedTerms.length > 0"
                title="Unopened Terms"
                :rows="unopenedTerms"
                :columns="unopenedColumns"
            >
                <template #caption="{ term }">
                    <span v-if="term.harvestedDate">Harvested: {{ formatDate(term.harvestedDate) }}</span>
                    <span v-if="term.expectedCloseDate">
                        {{ term.harvestedDate ? " · " : "" }}Expected Close:
                        {{ formatDate(term.expectedCloseDate) }}
                    </span>
                </template>
            </TermTable>

            <!-- Open Terms -->
            <TermTable
                title="Open Terms"
                :rows="openTerms"
                :columns="openColumns"
                empty-message="No terms are currently open for editing"
            >
                <template #caption="{ term }">
                    Opened: {{ formatDate(term.openedDate ?? "") }}
                    <span v-if="term.expectedCloseDate">
                        · Expected Close: {{ formatDate(term.expectedCloseDate) }}
                    </span>
                </template>
            </TermTable>

            <!-- Closed Terms -->
            <TermTable
                title="Closed Terms"
                :rows="closedTerms"
                :columns="closedColumns"
            >
                <template #caption="{ term }"> Closed: {{ formatDate(term.closedDate ?? "") }} </template>
            </TermTable>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { termService } from "../services/term-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import { useDateFunctions } from "@/composables/DateFunctions"
import TermTable from "../components/TermTable.vue"
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
    { name: "expectedCloseDate", label: "Expected Close", field: "expectedCloseDate", align: "center" },
]

const openColumns: QTableColumn[] = [
    { name: "termName", label: "Term", field: "termName", align: "left" },
    { name: "harvestedDate", label: "Harvested", field: "harvestedDate", align: "center" },
    { name: "openedDate", label: "Opened", field: "openedDate", align: "center" },
    { name: "expectedCloseDate", label: "Expected Close", field: "expectedCloseDate", align: "center" },
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
