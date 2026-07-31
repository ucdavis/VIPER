<template>
    <q-select
        :model-value="modelValue"
        multiple
        use-chips
        use-input
        input-debounce="100"
        dense
        options-dense
        clearable
        :label="label"
        :options="filteredOptions"
        :loading="loading"
        :hint="hint ?? undefined"
        @update:model-value="emit('update:modelValue', $event ?? [])"
        @filter="filterOptions"
    />
</template>

<script setup lang="ts">
import { ref } from "vue"
import { getPermissionOptions } from "@/CMS/services/cms-options-service"

// hint accepts null to opt out entirely (no reserved bottom space), since passing
// undefined would fall back to the default hint.
withDefaults(
    defineProps<{
        modelValue: string[]
        label?: string
        hint?: string | null
    }>(),
    { label: "Permissions", hint: "Users need any one of the selected permissions" },
)

const emit = defineEmits<{ "update:modelValue": [value: string[]] }>()

const allPermissions = ref<string[]>([])
const filteredOptions = ref<string[]>([])
const loading = ref(false)
let loaded = false

async function loadPermissions() {
    loading.value = true
    const result = await getPermissionOptions()
    if (result !== null) {
        allPermissions.value = result
        loaded = true
    }
    loading.value = false
}

async function filterOptions(val: string, update: (fn: () => void) => void) {
    if (!loaded) {
        await loadPermissions()
    }
    update(() => {
        const needle = val.toLowerCase()
        filteredOptions.value =
            needle === "" ? allPermissions.value : allPermissions.value.filter((p) => p.toLowerCase().includes(needle))
    })
}
</script>
