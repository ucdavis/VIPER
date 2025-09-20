/**
 * Rotation names to exclude from display (case-insensitive)
 * These rotations will be filtered out from dropdowns and schedule displays
 */
const EXCLUDED_ROTATION_NAMES = ["vacation"]

/**
 * Check if a rotation name should be excluded
 * @param rotationName The rotation name to check
 * @returns True if the rotation should be excluded
 */
function isRotationExcluded(rotationName: string | undefined | null): boolean {
    if (!rotationName) {
        return false
    }
    return EXCLUDED_ROTATION_NAMES.some((excluded) => rotationName.toLowerCase() === excluded.toLowerCase())
}

export { EXCLUDED_ROTATION_NAMES, isRotationExcluded }
