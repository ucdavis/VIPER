<template>
    <div
        v-if="shouldShow"
        class="permission-feedback-chip"
    >
        <q-chip
            v-if="
                showServiceSpecific &&
                permissionsStore.editableServiceCount > 0 &&
                !permissionsStore.hasFullAccessPermission
            "
            icon="business"
            color="positive"
            text-color="white"
            size="sm"
            dense
        >
            {{ computedServiceSpecificMessage }}
        </q-chip>

        <q-chip
            v-else-if="permissionsStore.hasFullAccessPermission && showFullAccess"
            icon="check_circle"
            color="primary"
            text-color="white"
            size="sm"
            dense
        >
            {{ computedFullAccessMessage }}
        </q-chip>

        <q-chip
            v-else-if="customMessage"
            :icon="customIcon || 'info'"
            :color="customColor || 'info'"
            text-color="white"
            size="sm"
            dense
        >
            {{ customMessage }}
        </q-chip>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { usePermissionsStore } from "../stores/permissions"

interface Props {
    // Control which types of feedback to show
    showServiceSpecific?: boolean
    showFullAccess?: boolean

    // Custom messages for different contexts
    serviceSpecificMessage?: string
    fullAccessMessage?: string

    // For custom permission feedback
    customMessage?: string
    customIcon?: string
    customColor?: string

    // Item counts for dynamic messages
    filteredCount?: number
    totalCount?: number

    // Control visibility
    visible?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    showServiceSpecific: true,
    showFullAccess: true,
    visible: true,
    serviceSpecificMessage: "",
    fullAccessMessage: "",
    customMessage: "",
    customIcon: "",
    customColor: "",
    filteredCount: undefined,
    totalCount: undefined,
})

const permissionsStore = usePermissionsStore()

// Computed message for service-specific permissions
const computedServiceSpecificMessage = computed(() => {
    if (props.serviceSpecificMessage) {
        return props.serviceSpecificMessage
    }

    if (props.filteredCount !== undefined && props.totalCount !== undefined) {
        return `Showing ${props.filteredCount} of ${props.totalCount} items (${permissionsStore.getEditableServicesDisplay()})`
    }

    return `Limited to ${permissionsStore.getEditableServicesDisplay()}`
})

// Computed message for full access
const computedFullAccessMessage = computed(() => {
    if (props.fullAccessMessage) {
        return props.fullAccessMessage
    }

    if (props.filteredCount !== undefined) {
        return `Showing all ${props.filteredCount} available items`
    }

    return "Full access"
})

// Determine if the component should be shown
const shouldShow = computed(() => {
    if (!props.visible) return false

    if (props.customMessage) return true

    // Show service-specific chip if user has service permissions (regardless of other permissions)
    if (
        props.showServiceSpecific &&
        permissionsStore.editableServiceCount > 0 &&
        !permissionsStore.hasFullAccessPermission
    ) {
        return true
    }

    if (props.showFullAccess && permissionsStore.hasFullAccessPermission) {
        return props.filteredCount !== undefined && props.filteredCount > 0
    }

    return false
})
</script>

<style scoped>
.permission-feedback-chip {
    margin-top: -6px;
    margin-left: 12px;
    padding: 2px 4px;
    background: inherit;
}
</style>
