<template>
    <div class="clinician-selector-with-toggle">
        <q-select
            v-model="selectedClinician"
            :options="filteredClinicians"
            :loading="loading"
            :error="!!error"
            :error-message="error || undefined"
            placeholder="Search for a clinician..."
            emit-value
            map-options
            use-input
            fill-input
            hide-selected
            clearable
            :input-debounce="300"
            @filter="onFilter"
            @update:model-value="onClinicianChange"
            option-label="fullName"
            option-value="mothraId"
        >
            <template v-slot:prepend>
                <q-icon name="search" />
            </template>

            <template v-slot:no-option>
                <q-item>
                    <q-item-section class="text-grey">
                        {{ searchQuery ? 'No clinicians found' : 'Loading clinicians...' }}
                    </q-item-section>
                </q-item>
            </template>

            <template v-slot:option="scope">
                <q-item v-bind="scope.itemProps">
                    <q-item-section>
                        <q-item-label>{{ scope.opt.fullName }}</q-item-label>
                    </q-item-section>
                </q-item>
            </template>

            <template v-slot:selected-item="scope">
                <span v-if="scope.opt">{{ scope.opt.fullName }}</span>
            </template>

            <template v-slot:error>
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
        <div v-if="showAffiliatesToggle" class="affiliates-toggle-inline">
            <q-checkbox
                :model-value="includeAllAffiliates"
                @update:model-value="onAffiliatesToggle"
                :label="affiliatesToggleLabel"
                :disable="isPastYear"
                color="primary"
                size="xs"
                dense
            />
            <q-tooltip v-if="isPastYear">
                This option is only available for the current year
            </q-tooltip>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { ClinicianService, type Clinician } from '../services/ClinicianService'

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
    affiliatesToggleLabel: 'Include all affiliates',
    isPastYear: false
})

const emit = defineEmits<{
    'update:modelValue': [value: Clinician | null]
    'change': [value: Clinician | null]
    'update:includeAllAffiliates': [value: boolean]
}>()

// Reactive data
const clinicians = ref<Clinician[]>([])
const filteredClinicians = ref<Clinician[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const searchQuery = ref('')

// Computed
const selectedClinician = computed({
    get: () => props.modelValue?.mothraId || null,
    set: (value) => {
        const clinician = clinicians.value.find(c => c.mothraId === value) || null
        emit('update:modelValue', clinician)
    }
})

// Methods
const fetchClinicians = async () => {
    loading.value = true
    error.value = null

    try {
        const result = await ClinicianService.getClinicians({
            year: props.year || undefined,
            includeAllAffiliates: props.includeAllAffiliates
        })
        if (result.success) {
            clinicians.value = result.result
            filteredClinicians.value = result.result
        } else {
            error.value = result.errors.join(', ') || 'Failed to load clinicians'
        }
    } catch (err) {
        error.value = err instanceof Error ? err.message : 'Failed to load clinicians'
        console.error('Error loading clinicians:', err)
    } finally {
        loading.value = false
    }
}

const filterClinicians = (items: Clinician[], searchTerm: string): Clinician[] => {
    const search = searchTerm.toLowerCase()
    return items.filter(clinician =>
        clinician.fullName.toLowerCase().includes(search) ||
        clinician.firstName.toLowerCase().includes(search) ||
        clinician.lastName.toLowerCase().includes(search) ||
        clinician.mothraId.toLowerCase().includes(search)
    )
}

function onFilter(val: string, update: (fn: () => void) => void) {
    searchQuery.value = val
    update(() => {
        filteredClinicians.value = val === ''
            ? clinicians.value
            : filterClinicians(clinicians.value, val)
    })
}

const onClinicianChange = (mothraId: string | null) => {
    const clinician = clinicians.value.find(c => c.mothraId === mothraId) || null
    emit('change', clinician)
}

const onAffiliatesToggle = (value: boolean) => {
    emit('update:includeAllAffiliates', value)
}

// Watchers
watch(() => props.year, () => {
    fetchClinicians()
})

watch(() => props.includeAllAffiliates, () => {
    fetchClinicians()
})

// Lifecycle
onMounted(() => {
    fetchClinicians()
})
</script>

<style scoped>
.clinician-selector-with-toggle {
    max-width: 400px;
}

.affiliates-toggle-inline {
    margin-top: 2px;
    margin-left: 0px;
    padding: 2px 4px;
    font-size: 12px;
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