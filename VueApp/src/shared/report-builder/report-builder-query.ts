import type { LocationQuery, LocationQueryRaw } from "vue-router"
import type { ReportParameterValue, ReportParameterValues, ReportViewerParameter } from "./report-builder-types"

/**
 * Moves parameter values between the form, the URL query and the run request, so a report page
 * can be bookmarked or shared with its parameters filled in.
 */

type QueryValue = LocationQuery[string]

function queryStrings(value: QueryValue | undefined): string[] {
    const list = Array.isArray(value) ? value : [value]
    return list.filter((item): item is string => typeof item === "string" && item !== "")
}

/** True for a value the user hasn't filled in; such values are left out of queries and requests. */
function isEmptyValue(value: ReportParameterValue | undefined): boolean {
    return value === null || value === undefined || value === "" || (Array.isArray(value) && value.length === 0)
}

/** A list of strings as the parameter holds it: all of them for multi-choice, otherwise the first. */
function toParameterValue(parameter: ReportViewerParameter, values: string[]): ReportParameterValue {
    if (parameter.type === "MultiChoice") {
        return values
    }
    return values[0] ?? null
}

/**
 * The form's starting values. Each comes from the first source that has it: the URL query, the
 * values the embedding page passed, then the report's default.
 */
function initialValues(
    parameters: ReportViewerParameter[],
    query: LocationQuery,
    given: Record<string, string | string[]> = {},
): ReportParameterValues {
    return Object.fromEntries(
        parameters.map((parameter) => {
            const fromQuery = queryStrings(query[parameter.name])
            const fromGiven = queryStrings(given[parameter.name])
            const fromDefault = parameter.defaultValue === null ? [] : [parameter.defaultValue]
            const first = [fromQuery, fromGiven, fromDefault].find((values) => values.length > 0) ?? []
            return [parameter.name, toParameterValue(parameter, first)]
        }),
    )
}

/** True when every required parameter has a value, so the report can run without the user. */
function hasRequiredValues(parameters: ReportViewerParameter[], values: ReportParameterValues): boolean {
    return parameters.every((parameter) => !parameter.required || !isEmptyValue(values[parameter.name]))
}

/** The filled-in values, each as a list. Names the report doesn't have are never sent. */
function toRequestParameters(
    parameters: ReportViewerParameter[],
    values: ReportParameterValues,
): Record<string, string[]> {
    return Object.fromEntries(
        parameters
            .map((parameter) => [parameter.name, values[parameter.name]] as const)
            .filter(([, value]) => !isEmptyValue(value))
            .map(([name, value]) => [name, Array.isArray(value) ? value : [String(value)]]),
    )
}

/** The filled-in values as query parameters; lists repeat the key. */
function toQuery(parameters: ReportViewerParameter[], values: ReportParameterValues): LocationQueryRaw {
    return Object.fromEntries(
        Object.entries(toRequestParameters(parameters, values)).map(([name, list]) => [
            name,
            list.length === 1 ? list[0] : list,
        ]),
    )
}

export { hasRequiredValues, initialValues, isEmptyValue, toQuery, toRequestParameters }
