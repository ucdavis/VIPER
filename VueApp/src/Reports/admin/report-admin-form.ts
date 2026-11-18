import type {
    ReportChoice,
    ReportColumnFormat,
    ReportParameterType,
} from "@/shared/report-builder/report-builder-types"
import type {
    ReportColumnEdit,
    ReportEditRequest,
    ReportFilterOperator,
    ReportParameterEdit,
    ReportSourceDetail,
    ReportValueKind,
} from "./report-admin-types"

/**
 * The admin form's state and the conversions to and from the API's shape. Columns and
 * parameters carry a key for the sortable lists, and a choice parameter's fixed list is edited as
 * text, one "value | label" per line.
 */

type ChoicesFrom = "list" | "view"

interface FormColumn extends ReportColumnEdit {
    key: number
}

interface FormParameter extends Omit<ReportParameterEdit, "choicesList"> {
    key: number
    choicesFrom: ChoicesFrom
    choicesText: string
}

interface ReportAdminForm extends Omit<ReportEditRequest, "columns" | "parameters"> {
    columns: FormColumn[]
    parameters: FormParameter[]
}

const CHOICE_SEPARATOR = "|"

/** Which operators suit each parameter type, the first being the default. */
const OPERATORS: Record<ReportParameterType, ReportFilterOperator[]> = {
    Text: ["Equals", "Contains"],
    Number: ["Equals", "From", "To"],
    Date: ["Equals", "From", "To"],
    Boolean: ["Equals"],
    Choice: ["Equals"],
    MultiChoice: ["In"],
}

const FORMATS_BY_KIND: Partial<Record<ReportValueKind, ReportColumnFormat>> = {
    WholeNumber: "Number",
    FractionalNumber: "Number",
    Date: "Date",
    DateTime: "DateTime",
    Boolean: "YesNo",
}

let lastKey = 0

function nextKey(): number {
    lastKey += 1
    return lastKey
}

function emptyForm(): ReportAdminForm {
    return {
        title: "",
        description: null,
        area: null,
        source: "",
        paginate: true,
        pageSize: null,
        allowExcel: true,
        allowPdf: false,
        pdfLandscape: false,
        sensitiveNotice: null,
        orderBy: null,
        orderByDescending: false,
        permissions: [],
        columns: [],
        parameters: [],
        lastModifiedOn: null,
    }
}

/** "VME | Medicine" per line; a line without a label uses the value. Blank lines are skipped. */
function parseChoices(text: string): ReportChoice[] {
    return text
        .split(/\r?\n/u)
        .map((line) => line.trim())
        .filter((line) => line !== "")
        .map((line) => {
            const at = line.indexOf(CHOICE_SEPARATOR)
            const value = (at === -1 ? line : line.slice(0, at)).trim()
            const label = at === -1 ? "" : line.slice(at + 1).trim()
            return { value, label: label === "" ? value : label }
        })
}

function formatChoices(choices: ReportChoice[] | null): string {
    return (choices ?? [])
        .map((choice) =>
            choice.label === choice.value ? choice.value : `${choice.value} ${CHOICE_SEPARATOR} ${choice.label}`,
        )
        .join("\n")
}

function toFormParameter(parameter: ReportParameterEdit): FormParameter {
    const { choicesList, ...rest } = parameter
    const hasList = (choicesList?.length ?? 0) > 0
    return {
        ...rest,
        key: nextKey(),
        choicesFrom: hasList || parameter.choicesSource === null ? "list" : "view",
        choicesText: formatChoices(choicesList),
    }
}

function toForm(report: ReportEditRequest): ReportAdminForm {
    return {
        ...report,
        permissions: [...report.permissions],
        columns: report.columns.map((column) => ({ ...column, key: nextKey() })),
        parameters: report.parameters.map(toFormParameter),
    }
}

function toRequestParameter(parameter: FormParameter): ReportParameterEdit {
    const { key: _key, choicesFrom, choicesText, ...rest } = parameter
    const isChoice = parameter.type === "Choice" || parameter.type === "MultiChoice"
    return {
        ...rest,
        choicesList: isChoice && choicesFrom === "list" ? parseChoices(choicesText) : null,
        choicesSource: isChoice && choicesFrom === "view" ? parameter.choicesSource : null,
    }
}

function toRequest(form: ReportAdminForm): ReportEditRequest {
    return {
        ...form,
        columns: form.columns.map(({ key: _key, ...column }) => column),
        parameters: form.parameters.map(toRequestParameter),
    }
}

/** A format to start a column with, from the kind of value it holds. */
function formatFor(kind: ReportValueKind): ReportColumnFormat {
    return FORMATS_BY_KIND[kind] ?? "Text"
}

function newColumn(name = "", kind: ReportValueKind = "Text"): FormColumn {
    return { key: nextKey(), sourceColumn: name, label: null, format: formatFor(kind), sortable: true, visible: true }
}

/** The source's showable columns the form doesn't have yet, as new columns. */
function missingColumns(source: ReportSourceDetail, columns: FormColumn[]): FormColumn[] {
    const have = new Set(columns.map((column) => column.sourceColumn.toLowerCase()))
    return source.columns
        .filter((column) => column.isSupported && !have.has(column.name.toLowerCase()))
        .map((column) => newColumn(column.name, column.kind))
}

function newParameter(isView: boolean): FormParameter {
    return {
        key: nextKey(),
        name: "",
        label: null,
        type: "Text",
        operator: isView ? "Equals" : null,
        required: false,
        defaultValue: null,
        choicesSource: null,
        binding: "UserInput",
        choicesFrom: "list",
        choicesText: "",
    }
}

function operatorsFor(type: ReportParameterType): ReportFilterOperator[] {
    return OPERATORS[type]
}

/** The parameter with its type changed, keeping its operator if it still suits the type. */
function withType(parameter: FormParameter, type: ReportParameterType, isView: boolean): FormParameter {
    if (!isView) {
        return { ...parameter, type, operator: null }
    }
    const allowed = OPERATORS[type]
    const keep = parameter.operator !== null && allowed.includes(parameter.operator)
    return { ...parameter, type, operator: keep ? parameter.operator : (allowed[0] ?? null) }
}

/** What a parameter can be bound to: a view's showable columns, or a procedure's parameters. */
function parameterTargets(source: ReportSourceDetail | null): string[] {
    if (source === null) {
        return []
    }
    const list = source.kind === "View" ? source.columns : source.parameters
    return list.filter((item) => item.isSupported).map((item) => item.name)
}

export {
    emptyForm,
    formatChoices,
    missingColumns,
    newColumn,
    newParameter,
    operatorsFor,
    parameterTargets,
    parseChoices,
    toForm,
    toRequest,
    withType,
}
export type { ChoicesFrom, FormColumn, FormParameter, ReportAdminForm }
