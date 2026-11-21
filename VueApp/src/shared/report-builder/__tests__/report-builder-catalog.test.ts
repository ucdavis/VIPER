import { OTHER_AREA, groupByArea } from "../report-builder-catalog"
import type { ReportCatalogItem } from "../report-builder-types"

function report(title: string, area: string | null): ReportCatalogItem {
    return { slug: title, title, description: null, area }
}

describe("grouping by area", () => {
    it("groups reports by area, in alphabetical order, keeping each group's order", () => {
        expect.hasAssertions()

        const groups = groupByArea([
            report("Roster", "Personnel"),
            report("Courses", "Effort"),
            report("Leave", "Personnel"),
        ])

        expect(groups.map((group) => [group.area, group.reports.map((r) => r.title)])).toStrictEqual([
            ["Effort", ["Courses"]],
            ["Personnel", ["Roster", "Leave"]],
        ])
    })

    it("lists reports without an area last", () => {
        expect.hasAssertions()

        const groups = groupByArea([report("Misc", null), report("Blank", " "), report("Roster", "Personnel")])

        expect(groups.map((group) => group.area)).toStrictEqual(["Personnel", OTHER_AREA])
    })

    it("has no groups for no reports", () => {
        expect.hasAssertions()

        expect(groupByArea([])).toStrictEqual([])
    })
})
