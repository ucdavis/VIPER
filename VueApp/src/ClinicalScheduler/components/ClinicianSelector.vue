<template>
    <div class="clinician-selector-with-toggle">
        <q-select
            ref="clinicianSelect"
            v-model="selectedClinician"
            :options="props.includeAllAffiliates ? clinicians : filteredClinicians"
            :loading="loading"
            :error="!!error"
            :error-message="error || undefined"
            placeholder="Search for a clinician..."
            :emit-value="!props.includeAllAffiliates"
            :map-options="!props.includeAllAffiliates"
            :use-input="!props.includeAllAffiliates"
            :fill-input="!props.includeAllAffiliates"
            :hide-selected="!props.includeAllAffiliates"
            :option-label="props.includeAllAffiliates ? getOptionLabel : 'fullName'"
            :option-value="props.includeAllAffiliates ? getOptionValue : 'mothraId'"
            clearable
            dense
            :input-debounce="100"
            @filter="onFilter"
            @popup-show="onPopupShow"
            @update:model-value="onClinicianChange"
            :virtual-scroll-slice-size="50"
        >
            <template #prepend>
                <q-icon name="search" />
            </template>

            <template #no-option>
                <q-item>
                    <q-item-section class="text-grey">
                        {{ searchQuery ? "No clinicians found" : "Loading clinicians..." }}
                    </q-item-section>
                </q-item>
            </template>

            <template #option="scope">
                <q-item v-bind="scope.itemProps">
                    <q-item-section>
                        <q-item-label>{{
                            props.includeAllAffiliates
                                ? `${scope.opt.lastName}, ${scope.opt.firstName}`
                                : scope.opt.fullName
                        }}</q-item-label>
                    </q-item-section>
                </q-item>
            </template>

            <template #selected-item="scope">
                <span v-if="scope.opt">{{
                    props.includeAllAffiliates ? `${scope.opt.lastName}, ${scope.opt.firstName}` : scope.opt.fullName
                }}</span>
            </template>

            <template #error>
                <div class="q-field__bottom text-negative">
                    {{ error }}
                    <q-btn
                        flat
                        dense
                        size="sm"
                        label="Retry"
                        @click="fetchClinicians"
                        class="q-ml-sm"
                    />
                </div>
            </template>
        </q-select>

        <!-- Include All Affiliates Toggle -->
        <div
            v-if="showAffiliatesToggle"
            class="affiliates-toggle-under-field"
        >
            <q-checkbox
                :model-value="includeAllAffiliates"
                @update:model-value="onAffiliatesToggle"
                :label="affiliatesToggleLabel"
                :disable="isPastYear"
                color="primary"
                size="xs"
                dense
            />
            <q-tooltip v-if="isPastYear"> This option is only available for the current year </q-tooltip>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick } from "vue"
import { ClinicianService, type Clinician } from "../services/clinician-service"
import type { QSelect } from "quasar"

interface Props {
    modelValue?: Clinician | null
    year?: number | null
    includeAllAffiliates?: boolean
    showAffiliatesToggle?: boolean
    affiliatesToggleLabel?: string
    isPastYear?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    modelValue: null,
    year: null,
    includeAllAffiliates: false,
    showAffiliatesToggle: true,
    affiliatesToggleLabel: "Include all affiliates",
    isPastYear: false,
})

const emit = defineEmits<{
    "update:modelValue": [value: Clinician | null]
    change: [value: Clinician | null]
    "update:include-all-affiliates": [value: boolean]
    "clinicians-loaded": [clinicians: Clinician[]]
}>()

// Reactive data
const clinicianSelect = ref<QSelect | null>(null)
const clinicians = ref<Clinician[]>([])
const filteredClinicians = ref<Clinician[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const searchQuery = ref("")

// Computed
const selectedClinician = computed({
    get: () => {
        // When includeAllAffiliates is true, we work with the full clinician object
        // When false, we work with mothraId string
        if (props.includeAllAffiliates) {
            return props.modelValue || null
        } else {
            return props.modelValue?.mothraId || null
        }
    },
    set: (value: Clinician | string | null) => {
        if (props.includeAllAffiliates) {
            // Value is already the full clinician object
            emit("update:modelValue", value as Clinician | null)
        } else {
            // Value is mothraId string, find the clinician
            const clinician = clinicians.value.find((c) => c.mothraId === (value as string)) || null
            emit("update:modelValue", clinician)
        }
    },
})

// Methods
const fetchClinicians = async () => {
    loading.value = true
    error.value = null

    try {
        const result = await ClinicianService.getClinicians({
            year: props.year || undefined,
            includeAllAffiliates: props.includeAllAffiliates,
        })
        if (result.success) {
            // Filter out records with empty/blank names and sort by lastName, firstName
            const validClinicians = result.result
                .filter(
                    (clinician) =>
                        clinician.fullName &&
                        clinician.fullName.trim().length > 1 &&
                        clinician.firstName &&
                        clinician.lastName,
                )
                .sort((a, b) => {
                    const lastNameCompare = a.lastName.localeCompare(b.lastName)
                    return lastNameCompare !== 0 ? lastNameCompare : a.firstName.localeCompare(b.firstName)
                })

            clinicians.value = validClinicians
            filteredClinicians.value = validClinicians

            // Emit event to notify parent that clinicians are loaded
            emit("clinicians-loaded", result.result)

            // Reset search query when affiliates toggle changes
            searchQuery.value = ""
        } else {
            error.value = result.errors.join(", ") || "Failed to load clinicians"
        }
    } catch (err) {
        error.value = err instanceof Error ? err.message : "Failed to load clinicians"
    } finally {
        loading.value = false
    }
}

const filterClinicians = (items: Clinician[], searchTerm: string): Clinician[] => {
    const search = searchTerm.toLowerCase()
    return items.filter(
        (clinician) =>
            `${clinician.lastName}, ${clinician.firstName}`.toLowerCase().includes(search) ||
            clinician.firstName.toLowerCase().includes(search) ||
            clinician.lastName.toLowerCase().includes(search) ||
            clinician.mothraId.toLowerCase().includes(search),
    )
}

// Helper functions for QSelect options when includeAllAffiliates is true
const getOptionLabel = (clinician: Clinician) => `${clinician.lastName}, ${clinician.firstName}`
const getOptionValue = (clinician: Clinician) => clinician.mothraId

// eslint-disable-next-line no-unused-vars
function onFilter(val: string, update: (fn: () => void) => void) {
    // Only used when use-input is true (i.e., when includeAllAffiliates is false)
    searchQuery.value = val
    update(() => {
        if (val === "") {
            filteredClinicians.value = clinicians.value
        } else {
            filteredClinicians.value = filterClinicians(clinicians.value, val)
        }
    })
}

const onClinicianChange = (value: string | Clinician | null) => {
    if (props.includeAllAffiliates) {
        // Value is already the full clinician object
        emit("change", value as Clinician | null)
    } else {
        // Value is mothraId string, find the clinician
        const clinician = clinicians.value.find((c) => c.mothraId === value) || null
        emit("change", clinician)
    }
}

const onAffiliatesToggle = (value: boolean) => {
    emit("update:include-all-affiliates", value)
}

const onPopupShow = () => {
    // When includeAllAffiliates is true (use-input is false), QSelect will show options automatically
    // When includeAllAffiliates is false (use-input is true), we need to populate filteredClinicians
    if (!props.includeAllAffiliates && searchQuery.value === "" && clinicians.value.length > 0) {
        // Show all clinicians when not using affiliates mode and search is empty
        filteredClinicians.value = clinicians.value
    }

    // Ensure the input is focused and ready for searching when use-input is enabled
    if (!props.includeAllAffiliates) {
        nextTick(() => {
            clinicianSelect.value?.focus()
        })
    }
}

// Watchers
watch(
    () => props.includeAllAffiliates,
    () => {
        if (props.year !== null && !loading.value) {
            void fetchClinicians()
        }
    },
)

// Watch for year changes and refetch clinicians
watch(
    () => props.year,
    (newYear) => {
        if (newYear !== null && !loading.value) {
            void fetchClinicians()
        }
    },
)

// Lifecycle
onMounted(() => {
    // Only fetch if we have a year, otherwise the watcher will handle it
    if (props.year !== null) {
        void fetchClinicians()
    }
})
</script>

<style scoped>
.clinician-selector-with-toggle {
    max-width: 400px;
    position: relative;
    margin-top: -20px;
}

.affiliates-toggle-under-field {
    position: absolute;
    top: 100%;
    left: 0;
    margin-top: 2px;
    margin-left: 12px;
    padding: 2px 4px;
    font-size: 12px;
    z-index: 1;
    background: white;
}

/* Reduce spacing around separators in the dropdown menu */
.clinician-selector-with-toggle :deep(.q-menu .q-separator) {
    margin: 4px 0 !important;
    padding: 0 !important;
}

/* Alternative selector for Quasar separator in dropdown */
.clinician-selector-with-toggle :deep(.q-item.q-separator) {
    margin: 4px 0 !important;
    padding: 0 !important;
    min-height: 1px !important;
}

/* Target the separator specifically in QSelect dropdown */
.clinician-selector-with-toggle :deep(.q-virtual-scroll__content .q-separator) {
    margin: 4px 0 !important;
}

/* Reduce spacing around the field with bottom area (like error messages, helper text) */
.clinician-selector-with-toggle :deep(.q-field--with-bottom) {
    padding-bottom: 0 !important;
    margin-bottom: 0 !important;
}
</style>
