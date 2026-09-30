import { buildFrequentNumberColumns } from "./svm-phone-columns"
import { filterRows } from "./use-mobile-table-rows"
import type { QTableColumn } from "quasar"
import type { JumpTarget } from "../components/SectionJumpLinks.vue"
import type { PhoneListUnit } from "../types/phone-list-phone-types"
import type { SVMFrequentNumberRecord, SVMPhoneSection } from "../types/svm-phone-types"

/**
 * The section jump links for the phone list pages, shared by the read-only and maintain pages of
 * each list so the two build their links, and the heading ids those links point at, the same way.
 */

/** A section as far as a jump link is concerned: where it lands, and what the search sees. */
type JumpSection = { id: string; label: string; cols: QTableColumn[] | undefined; rows: unknown[] }

const FREQUENT_NUMBERS_ANCHOR_ID = "phone-section-frequent-numbers"

// Matched on the data columns only, which are the same whether or not the list can be edited.
const frequentNumberColumns = buildFrequentNumberColumns(false)

function unitAnchorId(unitId: number): string {
    return `phone-unit-${unitId}`
}

function sectionAnchorId(sectionId: number): string {
    return `phone-section-${sectionId}`
}

function phoneListJumpSections(units: PhoneListUnit[]): JumpSection[] {
    return units.map((unit) => ({ id: unitAnchorId(unit.id), label: unit.name, cols: unit.cols, rows: unit.rows }))
}

/** Every SVM section, followed by the frequently called numbers, in page order. */
function svmJumpSections(sections: SVMPhoneSection[], frequentNumbers: SVMFrequentNumberRecord[]): JumpSection[] {
    return [
        ...sections.map((section) => ({
            id: sectionAnchorId(section.id),
            label: section.title,
            cols: section.cols,
            rows: section.rows,
        })),
        {
            id: FREQUENT_NUMBERS_ANCHOR_ID,
            label: "Frequently Called Numbers",
            cols: frequentNumberColumns,
            rows: frequentNumbers,
        },
    ]
}

/** For a maintain page, where every section stays shown, with its add button, whatever the search. */
function allJumpTargets(sections: JumpSection[]): JumpTarget[] {
    return sections.map(({ id, label }) => ({ id, label }))
}

/**
 * For a read-only page, which hides a section the search empties: a link to one would go nowhere.
 * Uses the same filterRows the lists themselves do, so a link is offered if and only if that
 * section still has rows.
 */
function matchingJumpTargets(sections: JumpSection[], search: string): JumpTarget[] {
    return allJumpTargets(sections.filter((section) => filterRows(section.cols, section.rows, search).length > 0))
}

export {
    FREQUENT_NUMBERS_ANCHOR_ID,
    allJumpTargets,
    matchingJumpTargets,
    phoneListJumpSections,
    sectionAnchorId,
    svmJumpSections,
    unitAnchorId,
}
