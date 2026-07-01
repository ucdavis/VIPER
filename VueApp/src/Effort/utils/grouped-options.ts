type GroupedOption = { label: string; isHeader?: boolean }

/**
 * Type-ahead filter for grouped select options: keep section headers and items
 * whose label matches the search, then drop headers left with no items beneath
 * them. An empty search returns every option (headers included).
 */
function filterGroupedOptions<T extends GroupedOption>(options: T[], search: string): T[] {
    // An empty or whitespace-only search returns every option (headers included);
    // the header pass below would otherwise drop headers for sections with no items.
    if (!search.trim()) {
        return [...options]
    }
    const needle = search.toLowerCase()
    return options
        .filter((opt) => opt.isHeader || opt.label.toLowerCase().includes(needle))
        .filter((opt, index, arr) => {
            if (!opt.isHeader) {
                return true
            }
            const next = arr[index + 1]
            return Boolean(next && !next.isHeader)
        })
}

export { filterGroupedOptions }
