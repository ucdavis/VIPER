/**
 * Shared mapping of schedule-audit actions to semantic Quasar colors, used by both the
 * standalone Audit Trail page and the inline per-week history popover so the colored
 * action badges stay consistent across the feature.
 */
const ACTION_COLORS: Record<string, string> = {
    "Added to rotation": "positive",
    "Removed from rotation": "negative",
    "Made primary evaluator": "primary",
    "Primary evaluator flag removed": "warning",
}

function getAuditActionColor(action: string): string {
    return ACTION_COLORS[action] ?? "grey-8"
}

export { getAuditActionColor }
