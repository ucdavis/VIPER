<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @update:model-value="emit('update:modelValue', $event)"
    >
        <q-card style="min-width: 36rem; max-width: 56rem">
            <q-card-section>
                <div class="text-h6">Edit Leave Data</div>
            </q-card-section>

            <q-card-section
                class="q-pt-none scroll"
                style="max-height: 60vh"
            >
                <table class="leave-table">
                    <thead>
                        <tr>
                            <th class="text-left">Term</th>
                            <th class="text-center">Exclude Didactic</th>
                            <th class="text-center">Exclude Clinical</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr
                            v-for="term in allTerms"
                            :key="term.termCode"
                        >
                            <td>{{ term.termName }}</td>
                            <td class="text-center">
                                <q-checkbox
                                    v-model="didacticChecked"
                                    :val="String(term.termCode)"
                                    dense
                                />
                            </td>
                            <td class="text-center">
                                <q-checkbox
                                    v-model="clinicalChecked"
                                    :val="String(term.termCode)"
                                    dense
                                />
                            </td>
                        </tr>
                    </tbody>
                </table>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    flat
                    label="Cancel"
                    @click="emit('update:modelValue', false)"
                />
                <q-btn
                    color="primary"
                    label="Save"
                    :loading="saving"
                    @click="save"
                >
                    <template #loading>
                        <q-spinner
                            size="1em"
                            class="q-mr-sm"
                        />
                        Save
                    </template>
                </q-btn>
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import { useQuasar } from "quasar"
import { reportService } from "../services/report-service"
import type { TermDto, SabbaticalDto } from "../types"

const props = defineProps<{
    modelValue: boolean
    personId: number
    allTerms: TermDto[]
    sabbaticalData: SabbaticalDto | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    saved: [data: SabbaticalDto]
}>()

const $q = useQuasar()
const saving = ref(false)
const clinicalChecked = ref<string[]>([])
const didacticChecked = ref<string[]>([])

// Initialize checkboxes when dialog opens
watch(
    () => props.modelValue,
    (open) => {
        if (open) {
            clinicalChecked.value = parseTermCodes(props.sabbaticalData?.excludeClinicalTerms)
            didacticChecked.value = parseTermCodes(props.sabbaticalData?.excludeDidacticTerms)
        }
    },
)

function parseTermCodes(csv: string | null | undefined): string[] {
    if (!csv) return []
    return csv
        .split(",")
        .map((s) => s.trim())
        .filter(Boolean)
}

async function save() {
    saving.value = true
    try {
        const result = await reportService.saveSabbatical(
            props.personId,
            clinicalChecked.value.length > 0 ? clinicalChecked.value.join(",") : null,
            didacticChecked.value.length > 0 ? didacticChecked.value.join(",") : null,
        )
        if (result) {
            emit("saved", result)
            emit("update:modelValue", false)
            $q.notify({ type: "positive", message: "Leave data saved." })
        } else {
            $q.notify({ type: "negative", message: "Failed to save leave data." })
        }
    } finally {
        saving.value = false
    }
}
</script>

<style scoped>
.leave-table {
    width: 100%;
    border-collapse: collapse;
}

.leave-table th,
.leave-table td {
    padding: 0.25rem 0.5rem;
    border-bottom: 1px solid var(--ucdavis-black-20, #ddd);
}

.leave-table th {
    font-weight: 600;
    font-size: 0.85rem;
}
</style>
