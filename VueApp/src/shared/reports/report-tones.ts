import type { ReportFlag, ReportRowResult, ReportTone } from "./report-types"

/**
 * Maps report highlight and badge tones to page styling. Tones are semantic, never raw colors,
 * and every flag also carries a text label, so no meaning depends on color alone.
 */

const HIGHLIGHT_CLASSES: Record<ReportTone, string | null> = {
    Neutral: null,
    Info: "report-tone-info",
    Positive: "report-tone-positive",
    Warning: "report-tone-warning",
    Negative: "report-tone-negative",
    Muted: "report-tone-muted",
}

// Quasar palette names; StatusBadge picks a readable text color for each.
const BADGE_COLORS: Record<ReportTone, string> = {
    Neutral: "grey-7",
    Info: "info",
    Positive: "positive",
    Warning: "warning",
    Negative: "negative",
    Muted: "grey-7",
}

/**
 * The highlight class for one cell, following the exports: highlights apply in declared order,
 * so the last one covering the cell wins. A highlight covers a cell when it targets the whole
 * row, this column, or a column the user can't see. Pass null for cells outside any column.
 */
function highlightClass(
    flags: ReportFlag[],
    columnKey: string | null,
    visibleKeys: ReadonlySet<string>,
): string | null {
    const classes = flags
        .filter(
            (flag) =>
                flag.kind === "Highlight" &&
                (flag.columnKey === null || flag.columnKey === columnKey || !visibleKeys.has(flag.columnKey)),
        )
        .map((flag) => HIGHLIGHT_CLASSES[flag.tone])
        .filter((name): name is string => name !== null)
    return classes.at(-1) ?? null
}

/** Badges shown beside one cell. Whole-row badges go beside the first column. */
function cellBadges(flags: ReportFlag[], columnKey: string, isFirstColumn: boolean): ReportFlag[] {
    return flags.filter(
        (flag) => flag.kind === "Badge" && (flag.columnKey === columnKey || (isFirstColumn && flag.columnKey === null)),
    )
}

function badgeColor(tone: ReportTone): string {
    return BADGE_COLORS[tone]
}

/** Every flag label on a row, once each, as the exports' Notes column lists them. */
function flagText(row: ReportRowResult): string {
    return [...new Set(row.flags.map((flag) => flag.label))].join("; ")
}

export { badgeColor, cellBadges, flagText, highlightClass }
