import AuditLogResultsTable from "../components/AuditLogResultsTable.vue"
import StatusBadge from "@/components/StatusBadge.vue"
import { createTestWrapper } from "./test-utils"
import type { AuditLogEntry } from "../types/audit-types"

function makeEntry(overrides: Partial<AuditLogEntry> = {}): AuditLogEntry {
    return {
        scheduleAuditId: 1,
        area: "Clinicians",
        mothraId: "abc123",
        personName: "Dr. Example",
        action: "Added to rotation",
        rotationId: 5,
        rotationName: "Cardiology",
        weekId: 10,
        weekNum: 3,
        weekStart: "2026-01-05",
        term: "Spring 2026",
        modifiedBy: "mgr456",
        modifiedByName: "Manager Person",
        timeStamp: "2026-01-06T08:30:00",
        ...overrides,
    }
}

function mountTable(entries: AuditLogEntry[], isLoading = false) {
    return createTestWrapper({
        component: AuditLogResultsTable,
        props: { entries, isLoading },
    })
}

describe("auditLogResultsTable", () => {
    it("renders audit entries", () => {
        expect.assertions(4)

        const wrapper = mountTable([makeEntry()])

        expect(wrapper.text()).toContain("Dr. Example")
        expect(wrapper.text()).toContain("Added to rotation")
        expect(wrapper.text()).toContain("Cardiology")
        expect(wrapper.text()).toContain("Manager Person")
    })

    it("renders one row per entry", () => {
        expect.assertions(3)

        const wrapper = mountTable([
            makeEntry(),
            makeEntry({ scheduleAuditId: 2, personName: "Dr. Second", action: "Removed from rotation" }),
        ])

        // One action badge renders per entry across every responsive table variant.
        expect(wrapper.findAllComponents(StatusBadge)).toHaveLength(2)
        expect(wrapper.text()).toContain("Dr. Second")
        expect(wrapper.text()).toContain("Removed from rotation")
    })

    it("marks the table as loading", () => {
        expect.assertions(1)

        const wrapper = mountTable([], true)

        expect(wrapper.findComponent({ name: "QTable" }).props("loading")).toBeTruthy()
    })
})
