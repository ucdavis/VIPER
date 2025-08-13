<template>
    <q-select
        v-model="internalValue"
        :options="yearOptions"
        :loading="isLoading"
        :error="!!error"
        :error-message="error || undefined"
        :placeholder="isLoading ? 'Loading years...' : 'Select year...'"
        emit-value
        map-options
        dense
        option-label="label"
        option-value="value"
        class="year-selector"
        :disable="isLoading && yearOptions.length === 0"
    >
        <template v-slot:no-option>
            <q-item>
                <q-item-section class="text-grey">
                    {{ isLoading ? 'Loading years...' : (error ? 'Failed to load years' : 'No years available') }}
                </q-item-section>
            </q-item>
        </template>
    </q-select>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { AcademicYearService } from '../services/AcademicYearService'

interface Props {
    modelValue: number | null
}

interface Emits {
    (e: 'update:modelValue', value: number): void
    (e: 'yearChanged', value: number): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

// Reactive state
const availableYears = ref<number[]>([])
const isLoading = ref(true) // Start with loading state to show skeleton
const error = ref<string | null>(null)

// Internal value for v-model binding with QSelect
const internalValue = computed({
    get: () => props.modelValue,
    set: (value: number | null) => {
        if (value !== null) {
            emit('update:modelValue', value)
            emit('yearChanged', value)
        }
    }
})

// Transform years into QSelect options format
const yearOptions = computed(() =>
    availableYears.value.map(year => ({
        label: year.toString(),
        value: year
    }))
)

// Initialize component with academic year and available years
const initializeYearSelector = async () => {
    isLoading.value = true
    error.value = null

    try {
        // Load both current academic year and available years
        const [currentAcademicYear, availableYearsList] = await Promise.all([
            AcademicYearService.getCurrentAcademicYear(),
            AcademicYearService.getAvailableYears()
        ])

        availableYears.value = availableYearsList

        // Initialize with current academic year if parent hasn't set a value (null)
        if (props.modelValue === null) {
            // Parent wants us to initialize - set to academic year
            emit('update:modelValue', currentAcademicYear)
            emit('yearChanged', currentAcademicYear)
        }
    } catch (err) {
        console.error('Failed to load year data:', err)
        error.value = 'Failed to load year data'

        // Fallback to calendar year based years
        const thisYear = new Date().getFullYear()
        const years = []
        for (let i = 0; i <= 5; i++) {
            years.push(thisYear - i)
        }
        availableYears.value = years
        
        // Don't change the parent's year value on error - keep their preference
    } finally {
        isLoading.value = false
    }
}

// Initialize component
onMounted(async () => {
    await initializeYearSelector()
})
</script>

<style scoped>
.year-selector {
    min-width: 120px;
    font-weight: bold;
}

/* Override Quasar styles to match project theme */
.year-selector :deep(.q-field__control) {
    color: #8B0000;
}

.year-selector :deep(.q-field__native) {
    font-weight: bold;
    font-size: 16px;
}
</style>