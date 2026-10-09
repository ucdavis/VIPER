import { createMemoryHistory, createRouter } from "vue-router"
import { routes } from "../router/routes"

/**
 * EIS pages are addressed by employee ID, so a bookmarked or shared link opens the same tab for
 * the same person.
 */

describe("eis routes", () => {
    it("resolves the picker and each tab for an employee", () => {
        expect.hasAssertions()
        const router = createRouter({ history: createMemoryHistory(), routes })

        expect(router.resolve("/Personnel/EIS").name).toBe("EisSelectPerson")
        expect(router.resolve("/Personnel/EIS/10123456").name).toBe("EisSummary")
        expect(
            ["Academics", "Appointments", "Category", "History", "Address"].map(
                (tab) => router.resolve(`/Personnel/EIS/10123456/${tab}`).name,
            ),
        ).toStrictEqual(["EisAcademics", "EisAppointments", "EisAppointmentCategory", "EisHistory", "EisAddress"])
        expect(router.resolve({ name: "EisHistory", params: { employeeId: "10123456" } }).path).toBe(
            "/Personnel/EIS/10123456/History",
        )
    })
})
