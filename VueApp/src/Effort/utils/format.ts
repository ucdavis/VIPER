/**
 * Formats a percent assignment type name with its modifier appended in parentheses.
 * Used in both RolloverAssignmentTable and PercentAssignmentTable.
 */
export function formatTypeWithModifier(typeName: string, modifier: string | null): string {
    return modifier ? `${typeName} (${modifier})` : typeName
}

/** Badge color for percent assignment type class (Admin/Clinical/Other). */
export function getTypeClassColor(typeClass: string): string {
    const lc = typeClass.toLowerCase()
    if (lc === "admin") return "blue"
    if (lc === "clinical") return "green"
    return "orange"
}
