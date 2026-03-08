import { computed } from "vue"
import type { Ref } from "vue"
import type { QTableColumn } from "quasar"
import type { EffortByType } from "../types"
import { effortTypeService } from "../services/effort-type-service"

const ALWAYS_SHOW = ["CLI", "VAR", "LEC", "LAB", "DIS", "PBL", "CBL", "TBL", "PRS", "JLC", "EXM"]

const SPACING_COLUMNS = new Set(["VAR", "EXM"])

// Effort type labels loaded from the API and cached at module level
let effortTypeLabelCache: Record<string, string> | null = null
let loadPromise: Promise<void> | null = null

async function fetchAndCacheLabels(): Promise<void> {
    const types = await effortTypeService.getEffortTypes()
    if (types.length === 0) {
        loadPromise = null
        return
    }
    effortTypeLabelCache = {}
    for (const t of types) {
        effortTypeLabelCache[t.id] = t.description
    }
}

async function loadEffortTypeLabels(): Promise<void> {
    if (!loadPromise) {
        loadPromise = fetchAndCacheLabels()
        try {
            await loadPromise
        } catch (error) {
            loadPromise = null
            throw error
        }
    }
    return loadPromise
}

function getEffortTypeLabel(code: string): string {
    // Lazily kick off the load on first use (no-op if already started)
    void loadEffortTypeLabels().catch(() => {})
    return effortTypeLabelCache?.[code] ?? code
}

interface EffortColumnOptions {
    /** Show "0" instead of blank for zero values (grouped report = true, individual = false) */
    showZero?: boolean
    /** Apply legacy always-show column ordering and VAR/EXM spacing (grouped only) */
    legacyColumnOrder?: boolean
}

function getAverageValue(averages: EffortByType, type: string): string {
    const val = averages[type] ?? 0
    return val.toFixed(1)
}

/**
 * Composable that generates dynamic QTable columns for effort type data.
 * Only shows effort types that have non-zero values in the report.
 */
function useEffortTypeColumns(effortTypes: Ref<string[]>, options?: EffortColumnOptions) {
    const showZero = options?.showZero ?? false
    const legacyColumnOrder = options?.legacyColumnOrder ?? false

    function formatValue(val: number): string {
        if (val > 0) {
            return val.toString()
        }
        return showZero ? "0" : ""
    }

    function buildColumn(type: string): QTableColumn {
        const spacer = legacyColumnOrder && SPACING_COLUMNS.has(type)
        return {
            name: `effort_${type}`,
            label: type,
            field: (row: { effortByType?: EffortByType }) => row.effortByType?.[type] ?? 0,
            align: "right" as const,
            sortable: false,
            format: (val: number) => formatValue(val),
            style: spacer ? "width: 5rem; min-width: 5rem; padding-right: 20px" : "width: 5rem; min-width: 5rem",
            headerStyle: "width: 5rem; min-width: 5rem",
        }
    }

    const effortColumns = computed<QTableColumn[]>(() => {
        if (!legacyColumnOrder) {
            return effortTypes.value.map(buildColumn)
        }

        // Legacy column order: ALWAYS_SHOW types first (in defined order),
        // then remaining types that appear in effortTypes, alphabetically
        const inputSet = new Set(effortTypes.value)
        const ordered: string[] = [...ALWAYS_SHOW]

        const remaining = effortTypes.value.filter((t) => !ALWAYS_SHOW.includes(t)).toSorted()
        ordered.push(...remaining)

        // For ALWAYS_SHOW types, include even if not in input effortTypes.
        // For remaining types, they are already from effortTypes.
        return ordered.filter((t) => ALWAYS_SHOW.includes(t) || inputSet.has(t)).map((t) => buildColumn(t))
    })

    function getTotalValue(totals: EffortByType, type: string): string {
        const val = totals[type] ?? 0
        return formatValue(val)
    }

    return {
        effortColumns,
        getTotalValue,
        getAverageValue,
    }
}

export { useEffortTypeColumns, ALWAYS_SHOW, SPACING_COLUMNS, getEffortTypeLabel, loadEffortTypeLabels }
export type { EffortColumnOptions }
