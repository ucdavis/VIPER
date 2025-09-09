<template>
    <q-banner
        v-if="shouldShowBanner"
        :class="bannerClass"
        rounded
        dense
    >
        <template #avatar>
            <q-icon
                :name="bannerIcon"
                :color="iconColor"
            />
        </template>

        <div class="row items-center q-gutter-x-sm">
            <span class="text-weight-medium">{{ bannerTitle }}:</span>
            <span>{{ bannerMessage }}</span>
        </div>
    </q-banner>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { usePermissionsStore } from "../stores/permissions"

// No props needed - banner shows automatically based on permissions

// Store
const permissionsStore = usePermissionsStore()

// Computed
const shouldShowBanner = computed(() => {
    // Show for any user with limited permissions (service-specific, own-schedule, or both)
    return permissionsStore.hasLimitedPermissions
})

const bannerClass = computed(() => {
    const classes = ["q-mb-md"]

    // Determine primary color based on permission type
    if (permissionsStore.hasOnlyServiceSpecificPermissions) {
        classes.push("text-positive")
    } else if (permissionsStore.hasOnlyOwnSchedulePermission) {
        classes.push("text-info")
    } else if (permissionsStore.editableServiceCount > 0) {
        // User has both service-specific AND own-schedule permissions
        classes.push("text-positive")
    } else {
        classes.push("text-info")
    }

    return classes
})

const bannerIcon = computed(() => {
    if (permissionsStore.hasOnlyServiceSpecificPermissions) {
        return "business"
    }

    if (permissionsStore.hasOnlyOwnSchedulePermission) {
        return "person"
    }

    if (permissionsStore.editableServiceCount > 0) {
        // User has both service-specific AND own-schedule permissions
        return "business"
    }

    return "info"
})

const iconColor = computed(() => {
    if (permissionsStore.hasOnlyServiceSpecificPermissions) {
        return "positive"
    }

    if (permissionsStore.hasOnlyOwnSchedulePermission) {
        return "info"
    }

    if (permissionsStore.editableServiceCount > 0) {
        // User has both service-specific AND own-schedule permissions
        return "positive"
    }

    return "info"
})

const bannerTitle = computed(() => {
    if (permissionsStore.hasOnlyServiceSpecificPermissions) {
        return "Rotation-Specific Access"
    }

    if (permissionsStore.hasOnlyOwnSchedulePermission) {
        return "Own Schedule Access"
    }

    if (permissionsStore.editableServiceCount > 0 && permissionsStore.hasEditOwnSchedulePermission) {
        // User has both service-specific AND own-schedule permissions
        return "Limited Access"
    }

    return "Limited Access"
})

const bannerMessage = computed(() => {
    if (permissionsStore.hasOnlyServiceSpecificPermissions) {
        const services = permissionsStore.getEditableServicesDisplay()
        const serviceCount = permissionsStore.editableServiceCount
        const plural = serviceCount > 1 ? "services" : "service"

        return `You can manage schedules for ${serviceCount} ${plural}: ${services}.`
    }

    if (permissionsStore.hasOnlyOwnSchedulePermission) {
        return "You can only edit your own schedule entries."
    }

    if (permissionsStore.editableServiceCount > 0 && permissionsStore.hasEditOwnSchedulePermission) {
        // User has both service-specific AND own-schedule permissions
        const services = permissionsStore.getEditableServicesDisplay()
        const serviceCount = permissionsStore.editableServiceCount
        const plural = serviceCount > 1 ? "services" : "service"

        return `You can manage schedules for ${serviceCount} ${plural} (${services}) and edit your own schedule entries.`
    }

    return "You have limited access to clinical scheduling features."
})
</script>
