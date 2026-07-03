/**
 * Shared audit-badge palette: every audit trail colors the same verb classes the same way
 * (create-like green, edits blue, deletes red, restores/reopens teal, closes amber,
 * imports cyan, denials amber, anything else neutral). Used by the CMS file audit and the
 * Effort audit list; new areas with an audit surface should reuse it, extending the verb
 * prefixes here rather than growing a local copy.
 */

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
    if (action === "AccessFileDenied") {
        return "warning"
    }
    const match = VERB_COLOR_RULES.find(([prefixes]) => prefixes.some((prefix) => action.startsWith(prefix)))
    return match ? match[1] : "grey-8"
}
