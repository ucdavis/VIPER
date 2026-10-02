import type { LocationQuery, LocationQueryRaw } from "vue-router"
import type {
    ReportParameterMetadata,
    ReportParameterType,
    ReportParameterValue,
    ReportParameterValues,
} from "./report-types"

/**
 * Moves report parameter values between the form, the URL query and the request body, so a
 * report page can be bookmarked or shared with its parameters filled in.
 */

type QueryValue = LocationQuery[string]

const PARSERS: Record<ReportParameterType, (values: string[]) => ReportParameterValue> = {
    Choice: (values) => values[0] ?? null,
    MultiChoice: (values) => values,
    Date: (values) => values[0] ?? null,
    Text: (values) => values[0] ?? null,
    Number: (values) => {
        const number = Number(values[0])
        return values[0] !== undefined && values[0] !== "" && Number.isFinite(number) ? number : null
    },
    Boolean: (values) => (values[0] === undefined ? null : values[0] === "true"),
}

function queryStrings(value: QueryValue): string[] {
    const list = Array.isArray(value) ? value : [value]
    return list.filter((item): item is string => typeof item === "string")
}

/** True for a value the user hasn't filled in; such values are left out of queries and requests. */
function isEmptyValue(value: ReportParameterValue | undefined): boolean {
    return value === null || value === undefined || value === "" || (Array.isArray(value) && value.length === 0)
}

function emptyValue(parameter: ReportParameterMetadata): ReportParameterValue {
    return parameter.type === "MultiChoice" ? [] : null
}

/** True when the query carries at least one of the report's parameters. */
function hasQueryParameters(parameters: ReportParameterMetadata[], query: LocationQuery): boolean {
    return parameters.some((parameter) => query[parameter.name] !== undefined)
}

/**
 * The form's starting values: from the query when it has the parameter, otherwise the report's
 * default, otherwise empty.
 */
function initialValues(parameters: ReportParameterMetadata[], query: LocationQuery): ReportParameterValues {
    return Object.fromEntries(
        parameters.map((parameter) => {
            const fromQuery = query[parameter.name]
            const value =
                fromQuery === undefined
                    ? (parameter.defaultValue ?? emptyValue(parameter))
                    : PARSERS[parameter.type](queryStrings(fromQuery))
            return [parameter.name, value]
        }),
    )
}

/** The non-empty values as query parameters; lists repeat the key. */
function toQuery(parameters: ReportParameterMetadata[], values: ReportParameterValues): LocationQueryRaw {
    const entries = parameters
        .map((parameter) => [parameter.name, values[parameter.name]] as const)
        .filter(([, value]) => !isEmptyValue(value))
        .map(([name, value]) => [name, Array.isArray(value) ? value : String(value)])
    return Object.fromEntries(entries)
}

/**
 * The run request body. Empty values are left out, so the server applies its defaults and its
 * own required checks, and names it doesn't know are never sent.
 */
function toRequestBody(parameters: ReportParameterMetadata[], values: ReportParameterValues): ReportParameterValues {
    return Object.fromEntries(
        parameters
            .map((parameter) => [parameter.name, values[parameter.name] ?? null] as const)
            .filter(([, value]) => !isEmptyValue(value)),
    )
}

export { hasQueryParameters, initialValues, isEmptyValue, toQuery, toRequestBody }
