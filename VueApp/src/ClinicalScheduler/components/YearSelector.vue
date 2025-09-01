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
        <template #no-option>
            <q-item>
                <q-item-section class="text-grey">
                    {{ isLoading ? "Loading years..." : error ? "Failed to load years" : "No years available" }}
                </q-item-section>
            </q-item>
        </template>
    </q-select>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { PageDataService } from "../services/page-data-service"

interface Props {
    modelValue: number | null
}

interface Emits {
    (e: "update:modelValue", value: number): void
    (e: "year-changed", value: number): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const availableYears = ref<number[]>([])
const isLoading = ref(true) // Start with loading state to show skeleton
const error = ref<string | null>(null)

const internalValue = computed({
    get: () => props.modelValue,
    set: (value: number | null) => {
        if (value !== null) {
            emit("update:modelValue", value)
            emit("year-changed", value)
        }
    },
})

const yearOptions = computed(() =>
    availableYears.value.map((year) => ({
        label: year.toString(),
        value: year,
    })),
)

const initializeYearSelector = async () => {
    isLoading.value = true
    error.value = null

    try {
        // Load initial page data (single API call)
        const pageData = await PageDataService.getPageData()
        const currentGradYear = pageData.currentGradYear
        const availableYearsList = pageData.availableGradYears

        availableYears.value = availableYearsList

        if (props.modelValue === null) {
            emit("update:modelValue", currentGradYear)
            emit("year-changed", currentGradYear)
        }
    } catch {
        error.value = "Failed to load year data"

        // Fallback to calendar year based years
        const thisYear = new Date().getFullYear()
        const years = []
        for (let i = 0; i <= 5; i++) {
            years.push(thisYear - i)
        }
        availableYears.value = years
    } finally {
        isLoading.value = false
    }
}

onMounted(async () => {
    await initializeYearSelector()
})
</script>

<style scoped>
.year-selector {
    min-width: 120px;
    font-weight: bold;
}

.year-selector :deep(.q-field__control) {
    color: var(--ucdavis-blue-100); /* UC Davis Aggie Blue */
}

.year-selector :deep(.q-field__native) {
    font-weight: bold;
    font-size: 16px;
}
</style>
