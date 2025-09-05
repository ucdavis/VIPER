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
                        {{ searchQuery ? "No rotations found" : "Loading rotations..." }}
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

        <!-- Permission feedback messages using Quasar components -->
        <div
            v-if="shouldShowPermissionInfo"
            class="rotation-permission-info"
        >
            <q-chip
                v-if="permissionsStore.hasOnlyServiceSpecificPermissions"
                icon="business"
                color="positive"
                text-color="white"
                size="sm"
                dense
            >
                Showing {{ filteredRotations.length }} of {{ totalRotations }} rotations ({{
                    permissionsStore.getEditableServicesDisplay()
                }}
                only)
            </q-chip>

            <q-chip
                v-else-if="permissionsStore.hasFullAccessPermission && hasFilteredRotations"
                icon="check_circle"
                color="primary"
                text-color="white"
                size="sm"
                dense
            >
                Showing all {{ filteredRotations.length }} available rotations
            </q-chip>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"
import { RotationService } from "../services/rotation-service"
import { usePermissionsStore } from "../stores/permissions"
import type { RotationWithService } from "../types/rotation-types"

// Constants
const INPUT_DEBOUNCE_MS = 300
const EXCLUDED_ROTATION_NAMES = ["vacation"] // Rotation names to exclude (case-insensitive)

// Props
interface Props {
    modelValue?: number | null
    serviceFilter?: number | null
    year?: number | null
    onlyWithScheduledWeeks?: boolean
    excludeRotationNames?: string[] // For filtering out already assigned rotations
}

const props = withDefaults(defineProps<Props>(), {
    modelValue: null,
    serviceFilter: null,
    year: null,
    onlyWithScheduledWeeks: false,
    excludeRotationNames: () => [],
})

// Emits
/* eslint-disable no-unused-vars */
interface Emits {
    (e: "update:modelValue", value: number | null): void
    (e: "rotation-selected", rotation: RotationWithService | null): void
}
/* eslint-enable no-unused-vars */

const emit = defineEmits<Emits>()

// Store
const permissionsStore = usePermissionsStore()

// Reactive data
const rotations = ref<RotationWithService[]>([])
const filteredRotations = ref<RotationWithService[]>([])
const isLoading = ref(false)
const error = ref<string | null>(null)
const searchQuery = ref("")

// Computed
const selectedRotation = computed({
    get: () => {
        if (!props.modelValue) return null
        return rotations.value.find((r) => r.rotId === props.modelValue) || null
    },
    set: (value) => {
        emit("update:modelValue", value?.rotId || null)
    },
})

// Filter rotations based on user permissions
const permissionFilteredRotations = computed(() => {
    return rotations.value.filter((rotation) => {
        // If user has admin or manage permissions, show all rotations
        if (
            permissionsStore.hasAdminPermission ||
            permissionsStore.hasManagePermission ||
            permissionsStore.hasEditClnSchedulesPermission
        ) {
            return true
        }

        // Check if user has permission to edit this rotation's service
        return permissionsStore.canEditService(rotation.serviceId)
    })
})

// Permission info computed properties
const totalRotations = computed(() => rotations.value.length)

const hasFilteredRotations = computed(() => filteredRotations.value.length > 0 && rotations.value.length > 0)

const shouldShowPermissionInfo = computed(() => {
    // Show permission info when rotations are loaded and not in error state
    return !isLoading.value && !error.value && rotations.value.length > 0
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
                includeService: true,
            })
        } else {
            // Use the original API that returns all rotations
            result = await RotationService.getRotations({
                serviceId: props.serviceFilter || undefined,
                includeService: true,
            })
        }

        if (result.success) {
            // Filter out excluded rotation names and system-excluded rotations
            const filteredResult = result.result
                .filter((rotation) => {
                    const rotationName = getRotationDisplayName(rotation)
                    // Check system-excluded rotations (case-insensitive)
                    if (
                        EXCLUDED_ROTATION_NAMES.some(
                            (excluded) => rotationName.toLowerCase() === excluded.toLowerCase(),
                        )
                    ) {
                        return false
                    }
                    // Check prop-excluded rotations
                    return !props.excludeRotationNames || !props.excludeRotationNames.includes(rotationName)
                })
                .sort((a, b) => getRotationDisplayName(a).localeCompare(getRotationDisplayName(b)))

            rotations.value = filteredResult
            // Use permission-filtered rotations as the base for filtering
            filteredRotations.value = permissionFilteredRotations.value
        } else {
            error.value = result.errors.join(", ") || "Failed to load rotations"
        }
    } catch {
        error.value = "An unexpected error occurred while loading rotations"
    } finally {
        isLoading.value = false
    }
}

function getRotationDisplayName(rotation: RotationWithService): string {
    if (!rotation.name) {
        return `[ERROR: Rotation ${rotation.rotId} missing name]`
    }
    return rotation.name
}

function filterRotations(items: RotationWithService[], searchTerm: string): RotationWithService[] {
    const search = searchTerm.toLowerCase()
    return items.filter(
        (rotation) =>
            getRotationDisplayName(rotation).toLowerCase().includes(search) ||
            rotation.abbreviation?.toLowerCase().includes(search) ||
            rotation.service?.serviceName?.toLowerCase().includes(search),
    )
}

// eslint-disable-next-line no-unused-vars
function onFilter(val: string, update: (fn: () => void) => void) {
    searchQuery.value = val
    update(() => {
        const baseRotations = permissionFilteredRotations.value
        filteredRotations.value = val === "" ? baseRotations : filterRotations(baseRotations, val)
    })
}

function onRotationChange(rotationId: number | null) {
    const rotation = rotations.value.find((r) => r.rotId === rotationId) || null
    emit("rotation-selected", rotation)
}

// Watchers
watch(
    () => props.serviceFilter,
    () => {
        void loadRotations()
    },
)

watch(
    () => props.year,
    () => {
        if (props.onlyWithScheduledWeeks) {
            void loadRotations()
        }
    },
)

watch(
    () => props.onlyWithScheduledWeeks,
    () => {
        void loadRotations()
    },
)

// Watch for model value changes to clear search
watch(
    () => props.modelValue,
    (newValue) => {
        if (newValue === null) {
            searchQuery.value = ""
            // Reset filtered rotations when cleared
            filteredRotations.value = permissionFilteredRotations.value
        }
    },
)

// Watch for permission changes to update filtered rotations
watch(
    () => permissionsStore.servicePermissions,
    () => {
        // Update filtered rotations when permissions change
        if (searchQuery.value === "") {
            filteredRotations.value = permissionFilteredRotations.value
        } else {
            filteredRotations.value = filterRotations(permissionFilteredRotations.value, searchQuery.value)
        }
    },
    { deep: true },
)

// Lifecycle
onMounted(async () => {
    // Initialize permissions store first
    if (!permissionsStore.userPermissions) {
        await permissionsStore.initialize()
    }
    void loadRotations()
})
</script>

<style scoped>
.rotation-selector {
    max-width: 400px;
    position: relative;
}

/* Permission info positioned absolutely like ClinicianSelector checkbox */
.rotation-permission-info {
    position: absolute;
    top: 100%;
    left: 0;
    margin-top: -18px;
    margin-left: 12px;
    padding: 2px 4px;
    font-size: 12px;
    z-index: 1;
    background: white;
}

/* Responsive adjustments */
@media (width <= 600px) {
    .rotation-selector {
        max-width: 100%;
    }
}
</style>
