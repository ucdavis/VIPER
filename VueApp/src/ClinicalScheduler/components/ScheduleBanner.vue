<template>
    <q-banner
        v-if="shouldShow"
        :class="bannerClass"
        rounded
    >
        <template #avatar>
            <q-icon :name="iconName" />
        </template>
        <div class="text-body2">
            <strong v-if="title">{{ title }}</strong>
            {{ message }}
        </div>
        <template
            v-if="$slots.action"
            #action
        >
            <slot name="action" />
        </template>
    </q-banner>
</template>

<script setup lang="ts">
import { computed } from "vue"

interface Props {
    type: "read-only" | "instructions" | "no-entries" | "error" | "info"
    show?: boolean
    currentYear?: number
    entityName?: string // rotation name, clinician name, etc.
    errorMessage?: string
    customMessage?: string // required for 'instructions', 'no-entries', and 'info' types
}

const props = withDefaults(defineProps<Props>(), {
    show: true,
    currentYear: 0,
    entityName: "",
    errorMessage: "",
    customMessage: "",
})

const shouldShow = computed(() => props.show)

const bannerClass = computed(() => {
    switch (props.type) {
        case "read-only": {
            return "text-dark bg-info q-mb-md"
        }
        case "instructions": {
            return "text-dark bg-blue-grey-1 q-mb-md"
        }
        case "no-entries": {
            return "text-dark bg-amber-2 q-mb-md"
        }
        case "error": {
            return "text-white bg-negative q-mb-md"
        }
        case "info": {
            return "text-dark bg-blue-grey-2 q-mb-md"
        }
        default: {
            return "text-dark bg-blue-grey-1 q-mb-md"
        }
    }
})

const iconName = computed(() => {
    switch (props.type) {
        case "read-only": {
            return "book"
        }
        case "instructions": {
            return "info"
        }
        case "no-entries": {
            return "warning"
        }
        case "error": {
            return "error"
        }
        case "info": {
            return "info"
        }
        default: {
            return "info"
        }
    }
})

const title = computed(() => {
    switch (props.type) {
        case "read-only": {
            return "Read-Only Mode:"
        }
        case "error": {
            return "Error:"
        }
        default: {
            return ""
        }
    }
})

const message = computed(() => {
    switch (props.type) {
        case "read-only": {
            return "You are viewing historical schedule data. Past schedules cannot be edited."
        }
        case "instructions": {
            return props.customMessage || "Instructions not provided."
        }
        case "no-entries": {
            return props.customMessage || `${props.entityName || "No entries found"} for ${props.currentYear}.`
        }
        case "error": {
            return props.errorMessage || "An error occurred."
        }
        case "info": {
            return props.customMessage || "Information not provided."
        }
        default: {
            return ""
        }
    }
})
</script>
