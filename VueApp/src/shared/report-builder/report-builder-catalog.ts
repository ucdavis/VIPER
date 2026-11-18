import type { ReportCatalogItem } from "./report-builder-types"

/** Reports without an area are listed last, under this heading. */
const OTHER_AREA = "Other reports"

interface ReportArea {
    area: string
    reports: ReportCatalogItem[]
}

/**
 * The reports grouped by area, areas in alphabetical order with "Other reports" last. The server
 * already sorts by area then title, so each group keeps that order.
 */
function groupByArea(reports: ReportCatalogItem[]): ReportArea[] {
    const groups = new Map<string, ReportCatalogItem[]>()
    for (const report of reports) {
        const area = report.area?.trim() || OTHER_AREA
        groups.set(area, [...(groups.get(area) ?? []), report])
    }
    const named = [...groups.keys()].filter((area) => area !== OTHER_AREA)
    named.sort((a, b) => a.localeCompare(b))
    const order = groups.has(OTHER_AREA) ? [...named, OTHER_AREA] : named
    return order.map((area) => ({ area, reports: groups.get(area) ?? [] }))
}

export { OTHER_AREA, groupByArea }
export type { ReportArea }
