<template>
  <div class="rotation-selector">
    <q-select
      v-model="selectedRotation"
      :options="filteredRotations"
      :loading="isLoading"
      :error="!!error"
      :error-message="error || undefined"
      placeholder="Search for a rotation..."
      emit-value
      map-options
      use-input
      fill-input
      hide-selected
      clearable
      dense
      :input-debounce="INPUT_DEBOUNCE_MS"
      @filter="onFilter"
      @update:model-value="onRotationChange"
      option-label="name"
      option-value="rotId"
    >
      <template #prepend>
        <q-icon name="search" />
      </template>
            
      <template #no-option>
        <q-item>
          <q-item-section class="text-grey">
            {{ searchQuery ? 'No rotations found' : 'Loading rotations...' }}
          </q-item-section>
        </q-item>
      </template>

      <template #option="scope">
        <q-item v-bind="scope.itemProps">
          <q-item-section>
            <q-item-label>{{ getRotationDisplayName(scope.opt) }}</q-item-label>
          </q-item-section>
        </q-item>
      </template>

      <template #selected-item="scope">
        <span v-if="scope.opt">{{ getRotationDisplayName(scope.opt) }}</span>
      </template>

      <template #error>
        <div class="q-field__bottom text-negative">
          {{ error }}
          <q-btn
            flat
            dense
            size="sm"
            label="Retry"
            @click="loadRotations"
            class="q-ml-sm"
          />
        </div>
      </template>
    </q-select>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { RotationService, type RotationWithService } from '../services/RotationService'

// Constants
const INPUT_DEBOUNCE_MS = 300

// Props
interface Props {
  modelValue?: number | null
  serviceFilter?: number | null
  year?: number | null
  onlyWithScheduledWeeks?: boolean
  excludeRotationNames?: string[]  // For filtering out already assigned rotations
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: null,
  serviceFilter: null,
  year: null,
  onlyWithScheduledWeeks: false,
  excludeRotationNames: () => []
})

// Emits
interface Emits {
  (e: 'update:modelValue', value: number | null): void
  (e: 'rotation-selected', rotation: RotationWithService | null): void
}

const emit = defineEmits<Emits>()

// Reactive data
const rotations = ref<RotationWithService[]>([])
const filteredRotations = ref<RotationWithService[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)
const searchQuery = ref('')

// Computed
const selectedRotation = computed({
    get: () => {
        if (!props.modelValue) return null
        return rotations.value.find(r => r.rotId === props.modelValue) || null
    },
    set: (value) => {
        emit('update:modelValue', value?.rotId || null)
    }
})

// Methods
async function loadRotations() {
  isLoading.value = true
  error.value = null

  try {
    let result
    
    if (props.onlyWithScheduledWeeks) {
      // Use the new API that only returns rotations with scheduled weeks
      result = await RotationService.getRotationsWithScheduledWeeks({ 
        year: props.year || undefined,
        includeService: true 
      })
    } else {
      // Use the original API that returns all rotations
      result = await RotationService.getRotations({ 
        serviceId: props.serviceFilter || undefined, 
        includeService: true 
      })
    }
    
    if (result.success) {
      // Deduplicate by rotation name and filter out excluded names
      const uniqueRotations = new Map<string, RotationWithService>()
      
      result.result.forEach(rotation => {
        const rotationName = getRotationDisplayName(rotation)
        // Skip if this rotation name is in the exclusion list
        if (props.excludeRotationNames && props.excludeRotationNames.includes(rotationName)) {
          return
        }
        // Keep only the first occurrence of each rotation name
        if (!uniqueRotations.has(rotationName)) {
          uniqueRotations.set(rotationName, rotation)
        }
      })
      
      const deduplicatedRotations = Array.from(uniqueRotations.values())
        .sort((a, b) => getRotationDisplayName(a).localeCompare(getRotationDisplayName(b)))
      
      rotations.value = deduplicatedRotations
      filteredRotations.value = deduplicatedRotations
    } else {
      error.value = result.errors.join(', ') || 'Failed to load rotations'
    }
  } catch (err) {
    error.value = 'An unexpected error occurred while loading rotations'
    console.error('Error loading rotations:', err)
  } finally {
    isLoading.value = false
  }
}

function getRotationDisplayName(rotation: RotationWithService): string {
  // Remove everything after and including the first parenthesis
  const beforeParenthesis = rotation.name.split('(')[0].trim()
  return beforeParenthesis || rotation.name
}

function filterRotations(items: RotationWithService[], searchTerm: string): RotationWithService[] {
    const search = searchTerm.toLowerCase()
    return items.filter(rotation => 
        getRotationDisplayName(rotation).toLowerCase().includes(search) ||
        rotation.abbreviation?.toLowerCase().includes(search) ||
        rotation.service?.serviceName?.toLowerCase().includes(search)
    )
}

function onFilter(val: string, update: (fn: () => void) => void) {
    searchQuery.value = val
    update(() => {
        filteredRotations.value = val === '' 
            ? rotations.value 
            : filterRotations(rotations.value, val)
    })
}

function onRotationChange(rotationId: number | null) {
    const rotation = rotations.value.find(r => r.rotId === rotationId) || null
    emit('rotation-selected', rotation)
}

// Watchers
watch(() => props.serviceFilter, () => {
    loadRotations()
})

watch(() => props.year, () => {
    if (props.onlyWithScheduledWeeks) {
        loadRotations()
    }
})

watch(() => props.onlyWithScheduledWeeks, () => {
    loadRotations()
})

// Lifecycle
onMounted(() => {
    loadRotations()
})
</script>

<style scoped>
.rotation-selector {
    max-width: 400px;
}
</style>