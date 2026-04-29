/**
 * Display labels for EPA supervision levels (cts.Level rows with order 1–5).
 *
 * The Level entity carries only a full name. These labels are presentation
 * concerns kept on the frontend so we can iterate without a schema change.
 *
 * - `chartLabel`: y-axis label for the progression chart ("Indirect")
 * - `abbreviation`: 3-letter pill text for Style 3 ("TIS")
 */
type LevelLabel = {
    chartLabel: string
    abbreviation: string
}

const LABELS_BY_VALUE: Record<number, LevelLabel> = {
    1: { chartLabel: "Knowledge", abbreviation: "DNP" },
    2: { chartLabel: "Direct", abbreviation: "TDS" },
    3: { chartLabel: "Indirect", abbreviation: "TIS" },
    4: { chartLabel: "Independent", abbreviation: "IRS" },
    5: { chartLabel: "Mentoring", abbreviation: "SJL" },
}

const FALLBACK: LevelLabel = { chartLabel: "", abbreviation: "" }

function getLevelLabel(levelValue: number): LevelLabel {
    return LABELS_BY_VALUE[levelValue] ?? FALLBACK
}

export type { LevelLabel }
export { getLevelLabel }
