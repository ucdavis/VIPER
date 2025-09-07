import type { UserPermissions } from "../types"

/**
 * Permission helper functions for cleaner permission logic evaluation.
 * These functions abstract common permission checks used throughout the clinical scheduler.
 */

/**
 * Checks if user has full access permissions (admin, manage, or edit all schedules).
 * Users with full access can perform all scheduling operations without restrictions.
 * @param perms - User permissions object
 * @returns True if user has admin, manage, or edit all schedules permission
 */
function hasFullAccess(perms?: UserPermissions["permissions"]): boolean {
    return Boolean(perms?.hasAdminPermission || perms?.hasManagePermission || perms?.hasEditClnSchedulesPermission)
}

/**
 * Checks if user has any editing capability (full access, own schedule, or service-specific).
 * This is useful for determining if the user should see editing UI elements.
 * @param perms - User permissions object
 * @param serviceCount - Number of services the user can edit (default: 0)
 * @returns True if user has any form of editing permission
 */
function hasAnyEditing(perms?: UserPermissions["permissions"], serviceCount = 0): boolean {
    return Boolean(
        perms?.hasAdminPermission ||
            perms?.hasManagePermission ||
            perms?.hasEditClnSchedulesPermission ||
            perms?.hasEditOwnSchedulePermission ||
            serviceCount > 0,
    )
}

/**
 * Checks if user has only service-specific permissions (no other editing rights).
 * These users can only edit schedules for specific services they're assigned to.
 * @param perms - User permissions object
 * @param serviceCount - Number of services the user can edit (default: 0)
 * @returns True if user only has service-specific permissions
 */
function hasOnlyServicePermissions(perms?: UserPermissions["permissions"], serviceCount = 0): boolean {
    if (!perms) {
        return false
    }

    return (
        !perms.hasAdminPermission &&
        !perms.hasManagePermission &&
        !perms.hasEditClnSchedulesPermission &&
        !perms.hasEditOwnSchedulePermission &&
        serviceCount > 0
    )
}

/**
 * Checks if user has only own schedule permission (no service-specific or full access).
 * These users can only edit their own instructor schedule entries.
 * @param perms - User permissions object
 * @param serviceCount - Number of services the user can edit (default: 0)
 * @returns True if user only has own schedule permission
 */
function hasOnlyOwnPermission(perms?: UserPermissions["permissions"], serviceCount = 0): boolean {
    if (!perms) {
        return false
    }

    return (
        !perms.hasAdminPermission &&
        !perms.hasManagePermission &&
        !perms.hasEditClnSchedulesPermission &&
        perms.hasEditOwnSchedulePermission &&
        serviceCount === 0
    )
}

/**
 * Checks if user has limited permissions (any combination of service-specific and/or own-schedule).
 * This identifies users who have some editing capabilities but not full system access.
 * @param perms - User permissions object
 * @param serviceCount - Number of services the user can edit (default: 0)
 * @returns True if user has limited (non-full-access) permissions
 */
function hasLimitedPermissions(perms?: UserPermissions["permissions"], serviceCount = 0): boolean {
    if (!perms) {
        return false
    }

    // User has limited permissions if they don't have full access
    // but do have either service-specific or own-schedule permissions
    const hasFullAccess = perms.hasAdminPermission || perms.hasManagePermission || perms.hasEditClnSchedulesPermission

    const hasLimitedAccess = perms.hasEditOwnSchedulePermission || serviceCount > 0

    return !hasFullAccess && hasLimitedAccess
}

/**
 * Checks if user should see read-only clinician selector (in clinician view).
 * This applies to users with EditOwnSchedule permission AND service-specific permissions.
 * Users with only EditOwnSchedule (no service permissions) still see all clinicians.
 * @param perms - User permissions object
 * @param serviceCount - Number of services the user can edit (default: 0)
 * @returns True if user should see read-only clinician selector in clinician view
 */
function hasClinicianViewReadOnly(perms?: UserPermissions["permissions"], serviceCount = 0): boolean {
    if (!perms) {
        return false
    }

    // Users with full access permissions can see all clinicians
    const hasFullAccess = perms.hasAdminPermission || perms.hasManagePermission || perms.hasEditClnSchedulesPermission

    // Users with EditOwnSchedule permission AND service-specific permissions should see read-only selector in clinician view
    return !hasFullAccess && perms.hasEditOwnSchedulePermission && serviceCount > 0
}

export {
    hasFullAccess,
    hasAnyEditing,
    hasOnlyServicePermissions,
    hasOnlyOwnPermission,
    hasLimitedPermissions,
    hasClinicianViewReadOnly,
}
