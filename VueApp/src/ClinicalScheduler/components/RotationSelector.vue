<template>
  <div class="rotation-selector">
    <label for="rotation-select" class="form-label">Select Rotation:</label>
    <div class="position-relative">
      <select 
        id="rotation-select"
        v-model="selectedRotationId" 
        @change="onRotationChange"
        class="form-select"
        :disabled="isLoading"
      >
        <option value="">
          {{ isLoading ? 'Loading rotations...' : 'Select a rotation' }}
        </option>
        <optgroup 
          v-for="service in groupedRotations" 
          :key="service.serviceId"
          :label="service.serviceName"
        >
          <option 
            v-for="rotation in service.rotations" 
            :key="rotation.rotId"
            :value="rotation.rotId"
          >
            {{ rotation.name }} ({{ rotation.abbreviation }})
          </option>
        </optgroup>
      </select>
      
      <!-- Loading spinner -->
      <div v-if="isLoading" class="position-absolute top-50 end-0 translate-middle-y me-3">
        <div class="spinner-border spinner-border-sm text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
      </div>
    </div>

    <!-- Error display -->
    <div v-if="error" class="alert alert-warning mt-2" role="alert">
      <i class="bi bi-exclamation-triangle"></i>
      {{ error }}
      <button 
        @click="loadRotations" 
        class="btn btn-sm btn-outline-warning ms-2"
        :disabled="isLoading"
      >
        Retry
      </button>
    </div>

    <!-- Selected rotation info -->
    <div v-if="selectedRotation" class="mt-2">
      <div class="card card-body bg-light">
        <h6 class="card-title mb-1">{{ selectedRotation.name }}</h6>
        <div class="text-muted small">
          <div>Service: {{ selectedRotation.service?.serviceName }}</div>
          <div v-if="selectedRotation.subjectCode || selectedRotation.courseNumber">
            Course: {{ selectedRotation.subjectCode }} {{ selectedRotation.courseNumber }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { RotationService, type RotationWithService } from '../services/RotationService'

// Props
interface Props {
  modelValue?: number | null
  serviceFilter?: number | null
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: null,
  serviceFilter: null
})

// Emits
interface Emits {
  (e: 'update:modelValue', value: number | null): void
  (e: 'rotationSelected', rotation: RotationWithService | null): void
}

const emit = defineEmits<Emits>()

// Reactive data
const rotations = ref<RotationWithService[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)
const selectedRotationId = ref<number | string>(props.modelValue || '')

// Computed
const groupedRotations = computed(() => {
  const groups: Record<number, { serviceId: number, serviceName: string, rotations: RotationWithService[] }> = {}
  
  rotations.value.forEach(rotation => {
    if (rotation.service) {
      const serviceId = rotation.service.serviceId
      if (!groups[serviceId]) {
        groups[serviceId] = {
          serviceId,
          serviceName: rotation.service.serviceName,
          rotations: []
        }
      }
      groups[serviceId].rotations.push(rotation)
    }
  })

  // Sort rotations within each service
  Object.values(groups).forEach(group => {
    group.rotations.sort((a, b) => a.name.localeCompare(b.name))
  })

  // Return sorted by service name
  return Object.values(groups).sort((a, b) => a.serviceName.localeCompare(b.serviceName))
})

const selectedRotation = computed(() => {
  if (!selectedRotationId.value) return null
  return rotations.value.find(r => r.rotId === Number(selectedRotationId.value)) || null
})

// Methods
async function loadRotations() {
  isLoading.value = true
  error.value = null

  try {
    const result = await RotationService.getRotations({ 
      serviceId: props.serviceFilter || undefined, 
      includeService: true 
    })
    
    if (result.success) {
      rotations.value = result.result
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

function onRotationChange() {
  const rotationId = selectedRotationId.value ? Number(selectedRotationId.value) : null
  emit('update:modelValue', rotationId)
  emit('rotationSelected', selectedRotation.value)
}

// Watchers
watch(() => props.modelValue, (newValue) => {
  selectedRotationId.value = newValue || ''
})

watch(() => props.serviceFilter, () => {
  loadRotations()
})

// Lifecycle
onMounted(() => {
  loadRotations()
})
</script>

<style scoped>
.rotation-selector {
  max-width: 500px;
}

.spinner-border-sm {
  width: 1rem;
  height: 1rem;
}

.card-body {
  padding: 0.75rem;
}

.bi {
  margin-right: 0.25rem;
}
</style>