/**
 * Shared audit-badge palette: every audit trail colors the same verb classes the same way
 * (create-like green, edits blue, deletes red, restores/reopens teal, closes amber,
 * imports cyan, denials amber, anything else neutral). Used by the CMS file audit, the
 * Effort audit list and the Clinical Scheduler audit trail; new areas with an audit
 * surface should reuse it, extending the tables here rather than growing a local copy.
 */

// Actions whose color the verb prefixes cannot derive, checked before them. Most audit
// vocabularies are verb-first PascalCase codes, but the Clinical Scheduler logs
// human-readable sentences that sometimes put the verb last ("Primary evaluator flag
// removed"), which no prefix can express.
const EXACT_ACTION_COLORS: Record<string, string> = {
    AccessFileDenied: "warning",
    "Added to rotation": "positive",
    "Removed from rotation": "negative",
    "Made primary evaluator": "primary",
    "Primary evaluator flag removed": "warning",
}

// Ordered: first matching prefix wins (CancelDelete must resolve before Delete).
const VERB_COLOR_RULES: [prefixes: string[], color: string][] = [
    [["Add", "Upload", "Create", "Verif"], "positive"],
    [["Edit", "Update"], "primary"],
    [["CancelDelete", "Open", "Reopen"], "secondary"],
    [["Delete"], "negative"],
    [["Close"], "warning"],
    [["Import"], "info"],
]

export function getAuditActionColor(action: string): string {
    const exact = EXACT_ACTION_COLORS[action]
    if (exact !== undefined) {
        return exact
    }
    const match = VERB_COLOR_RULES.find(([prefixes]) => prefixes.some((prefix) => action.startsWith(prefix)))
    return match ? match[1] : "grey-8"
}
