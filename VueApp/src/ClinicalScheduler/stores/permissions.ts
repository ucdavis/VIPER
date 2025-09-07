import { defineStore } from "pinia"
import { ref, computed, readonly } from "vue"
import {
    hasFullAccess,
    hasAnyEditing,
    hasOnlyServicePermissions,
    hasOnlyOwnPermission,
    hasLimitedPermissions,
    hasClinicianViewReadOnly as helperHasClinicianViewReadOnly,
} from "./permissions-helpers"
import { createPermissionActions } from "./permissions-actions"
import type { PermissionsState } from "./permissions-actions"
import { createPermissionUtils } from "./permissions-utils"
import type { UserPermissions, PermissionSummary, User, Service } from "../types"

// Helper functions extracted to reduce main store function size
function createPermissionsState(): PermissionsState {
    return {
        userPermissions: ref<UserPermissions | null>(null),
        permissionSummary: ref<PermissionSummary | null>(null),
        isLoading: ref(false),
        error: ref<string | null>(null),
    }
}

/**
 * Creates basic permission getters for individual permission flags and user data
 */
function createBasicPermissionGetters(state: PermissionsState) {
    return {
        user: computed<User | null>(() => state.userPermissions.value?.user || null),
        hasAdminPermission: computed<boolean>(
            () => state.userPermissions.value?.permissions.hasAdminPermission || false,
        ),
        hasManagePermission: computed<boolean>(
            () => state.userPermissions.value?.permissions.hasManagePermission || false,
        ),
        hasEditClnSchedulesPermission: computed<boolean>(
            () => state.userPermissions.value?.permissions.hasEditClnSchedulesPermission || false,
        ),
        hasEditOwnSchedulePermission: computed<boolean>(
            () => state.userPermissions.value?.permissions.hasEditOwnSchedulePermission || false,
        ),
        editableServices: computed<Service[]>(() => state.userPermissions.value?.editableServices || []),
        editableServiceCount: computed<number>(() => state.userPermissions.value?.editableServices?.length || 0),
        servicePermissions: computed<Record<number, boolean>>(
            () => state.userPermissions.value?.permissions.servicePermissions || {},
        ),
    }
}

/**
 * Creates permission level getters that combine multiple permission flags
 */
function createPermissionLevelGetters(state: PermissionsState) {
    return {
        hasFullAccessPermission: computed<boolean>(() => {
            const perms = state.userPermissions.value?.permissions
            return hasFullAccess(perms)
        }),

        hasAnyEditPermission: computed<boolean>(() => {
            const perms = state.userPermissions.value?.permissions
            const serviceCount = state.userPermissions.value?.editableServices?.length || 0
            return hasAnyEditing(perms, serviceCount)
        }),

        hasOnlyServiceSpecificPermissions: computed<boolean>(() => {
            const perms = state.userPermissions.value?.permissions
            const serviceCount = state.userPermissions.value?.editableServices?.length || 0
            return hasOnlyServicePermissions(perms, serviceCount)
        }),

        hasOnlyOwnSchedulePermission: computed<boolean>(() => {
            const perms = state.userPermissions.value?.permissions
            const serviceCount = state.userPermissions.value?.editableServices?.length || 0
            return hasOnlyOwnPermission(perms, serviceCount)
        }),

        hasLimitedPermissions: computed<boolean>(() => {
            const perms = state.userPermissions.value?.permissions
            const serviceCount = state.userPermissions.value?.editableServices?.length || 0
            return hasLimitedPermissions(perms, serviceCount)
        }),

        hasClinicianViewReadOnly: computed<boolean>(() => {
            const perms = state.userPermissions.value?.permissions
            const serviceCount = state.userPermissions.value?.editableServices?.length || 0
            return helperHasClinicianViewReadOnly(perms, serviceCount)
        }),
    }
}

/**
 * Creates view access getters that determine which parts of the application users can access
 */
function createViewAccessGetters(state: PermissionsState) {
    return {
        /**
         * Determines if user can access the clinician view of the scheduler.
         * Clinician view shows schedules from the perspective of individual instructors.
         * Access granted to: full access users + users who can edit their own schedule.
         */
        canAccessClinicianView: computed<boolean>(() => {
            const perms = state.userPermissions.value?.permissions
            if (!perms) {
                return false
            }

            // Full access users and own schedule users can access clinician view
            return hasFullAccess(perms) || Boolean(perms.hasEditOwnSchedulePermission)
        }),

        /**
         * Determines if user can access the rotation view of the scheduler.
         * Rotation view shows schedules organized by clinical rotations.
         * Access granted to: full access users + users with service-specific permissions.
         */
        canAccessRotationView: computed<boolean>(() => {
            const perms = state.userPermissions.value?.permissions
            if (!perms) {
                return false
            }

            // Full access users and service-specific users can access rotation view
            const serviceCount = state.userPermissions.value?.editableServices?.length || 0
            return hasFullAccess(perms) || serviceCount > 0
        }),

        /**
         * Returns a string representation of the user's highest permission level.
         * Used for routing, UI display, and access control decisions.
         * Hierarchy: admin > manage > edit_all > edit_own > service_specific > none
         */
        permissionLevel: computed<string>(() => {
            const perms = state.userPermissions.value?.permissions
            if (!perms) {
                return "none"
            }

            const serviceCount = state.userPermissions.value?.editableServices?.length || 0
            if (perms.hasAdminPermission) {
                return "admin"
            }
            if (perms.hasManagePermission) {
                return "manage"
            }
            if (perms.hasEditClnSchedulesPermission) {
                return "edit_all"
            }
            if (perms.hasEditOwnSchedulePermission) {
                return "edit_own"
            }
            if (serviceCount > 0) {
                return "service_specific"
            }

            return "none"
        }),
    }
}

/**
 * Creates all permission getters by combining the sub-getter functions
 */
function createPermissionsGetters(state: PermissionsState) {
    return {
        ...createBasicPermissionGetters(state),
        ...createPermissionLevelGetters(state),
        ...createViewAccessGetters(state),
    }
}

/**
 * Clinical Scheduler permissions store.
 *
 * Manages user permissions for the clinical scheduling system, providing:
 * - User permission data and state management
 * - Computed properties for different permission levels and access checks
 * - Actions for fetching permissions and performing dynamic permission validation
 * - Utility functions for permission-related operations
 *
 * The store is organized into modular sections (helpers, actions, utils) for maintainability.
 */
export const usePermissionsStore = defineStore("permissions", () => {
    // State
    const state = createPermissionsState()
    const { userPermissions, permissionSummary, isLoading, error } = state

    // Getters
    const getters = createPermissionsGetters(state)
    const {
        user,
        hasAdminPermission,
        hasManagePermission,
        hasEditClnSchedulesPermission,
        hasEditOwnSchedulePermission,
        editableServices,
        editableServiceCount,
        servicePermissions,
        hasFullAccessPermission,
        hasAnyEditPermission,
        hasOnlyServiceSpecificPermissions,
        hasOnlyOwnSchedulePermission,
        hasLimitedPermissions,
        hasClinicianViewReadOnly,
        canAccessClinicianView,
        canAccessRotationView,
        permissionLevel,
    } = getters

    // Actions
    const actions = createPermissionActions(state)
    const utils = createPermissionUtils(state, getters)

    // Initialize permissions on store creation
    const initialize = async (): Promise<void> => {
        await actions.fetchUserPermissions()
    }

    return {
        // State
        userPermissions: readonly(userPermissions),
        permissionSummary: readonly(permissionSummary),
        isLoading: readonly(isLoading),
        error: readonly(error),

        // Getters
        user,
        hasAdminPermission,
        hasManagePermission,
        hasEditClnSchedulesPermission,
        hasEditOwnSchedulePermission,
        editableServices,
        editableServiceCount,
        servicePermissions,
        hasFullAccessPermission,
        hasAnyEditPermission,
        hasOnlyServiceSpecificPermissions,
        hasOnlyOwnSchedulePermission,
        hasLimitedPermissions,
        hasClinicianViewReadOnly,
        canAccessClinicianView,
        canAccessRotationView,
        permissionLevel,

        // Actions
        ...actions,
        ...utils,
        initialize,
    }
})
