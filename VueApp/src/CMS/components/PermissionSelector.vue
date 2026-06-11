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
        hint="Users need any one of the selected permissions"
        @update:model-value="emit('update:modelValue', $event ?? [])"
        @filter="filterOptions"
    />
</template>

<script setup lang="ts">
import { inject, ref } from "vue"
import { useFetch } from "@/composables/ViperFetch"

withDefaults(
    defineProps<{
        modelValue: string[]
        label?: string
    }>(),
    { label: "Permissions" },
)

const emit = defineEmits<{ "update:modelValue": [value: string[]] }>()

const apiURL = inject("apiURL") + "cms/options/"
const allPermissions = ref<string[]>([])
const filteredOptions = ref<string[]>([])
const loading = ref(false)
let loaded = false

async function loadPermissions() {
    loading.value = true
    const { get } = useFetch()
    const res = await get(apiURL + "permissions")
    if (res.success) {
        allPermissions.value = res.result
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
