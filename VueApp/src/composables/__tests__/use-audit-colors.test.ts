import { getAuditActionColor } from "@/composables/use-audit-colors"

/**
 * Unit tests for the shared audit-badge palette: all three current vocabularies (CMS file
 * audit, Effort audit list, Clinical Scheduler audit trail) must keep resolving to the same
 * verb-class colors, and unknown actions fall back to the neutral grey.
 */

describe("getAuditActionColor - shared verb palette", () => {
    it.each([
        ["AddFile", "positive"],
        ["UploadFile", "positive"],
        ["EditFile", "primary"],
        ["DeleteFile", "negative"],
        ["CancelDelete", "secondary"],
        ["ImportFile", "info"],
        ["AccessFileDenied", "warning"],
    ])("colors the CMS action %s as %s", (action, color) => {
        expect(getAuditActionColor(action)).toBe(color)
    })

    it.each([
        ["CreateRecord", "positive"],
        ["UpdateRecord", "primary"],
        ["DeleteRecord", "negative"],
        ["OpenTerm", "secondary"],
        ["ReopenTerm", "secondary"],
        ["CloseTerm", "warning"],
        ["ImportRecords", "info"],
        ["VerifyRecords", "positive"],
    ])("colors the Effort action %s as %s", (action, color) => {
        expect(getAuditActionColor(action)).toBe(color)
    })

    // Sentence-style actions from ScheduleAuditActions.cs. Three of the four put the verb
    // somewhere the prefix rules cannot see, so they are exact-match entries.
    it.each([
        ["Added to rotation", "positive"],
        ["Removed from rotation", "negative"],
        ["Made primary evaluator", "primary"],
        ["Primary evaluator flag removed", "warning"],
    ])("colors the Clinical Scheduler action %s as %s", (action, color) => {
        expect(getAuditActionColor(action)).toBe(color)
    })

    it("falls back to neutral grey for unknown actions", () => {
        expect(getAuditActionColor("AccessFile")).toBe("grey-8")
        expect(getAuditActionColor("SomethingNew")).toBe("grey-8")
    })
})
