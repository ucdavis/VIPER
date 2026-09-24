import type { StudentCareerListItem, StudentCareerReport } from "../types"

/**
 * The career selection fields the overview and report show, in column order.
 */
type CareerField = {
    name: string // Column name, which is also the table's body-cell slot name.
    label: string // Column header and mobile card caption.
    valueField: keyof StudentCareerReport // The report's value for this field.
    completedField?: keyof StudentCareerListItem // The overview's completeness flag, for non-text fields.
    tooltipLabel?: string // Fuller name for the overview's completeness icon, which is also its accessible label.
    optional?: boolean // Unanswered is not flagged as missing on the overview.
    statement?: boolean // Free-text statement, shown as an excerpt on the report.
}

const CAREER_FIELDS: CareerField[] = [
    {
        name: "direction",
        label: "Career",
        valueField: "direction",
        completedField: "directionCompleted",
        tooltipLabel: "Career Direction",
    },
    {
        name: "primarySpecies",
        label: "Species 1",
        valueField: "primaryFocus",
        completedField: "primaryFocusCompleted",
        tooltipLabel: "Primary Focus",
    },
    {
        name: "secondarySpecies",
        label: "Species 2",
        valueField: "secondaryFocus",
        completedField: "secondaryFocusCompleted",
        tooltipLabel: "Secondary Focus",
        optional: true,
    },
    {
        name: "postGrad",
        label: "Post Grad",
        valueField: "postGrad",
        completedField: "postGradCompleted",
        tooltipLabel: "Post-Graduation Plans",
    },
    { name: "mentor", label: "Mentor", valueField: "mentorName" },
    {
        name: "shortTerm",
        label: "Short Term",
        valueField: "shortTermPlans",
        completedField: "shortTermPlansCompleted",
        tooltipLabel: "Short Term Plans",
        statement: true,
    },
    {
        name: "longTerm",
        label: "Long Term",
        valueField: "longTermPlans",
        completedField: "longTermPlansCompleted",
        tooltipLabel: "Long Term Plans",
        statement: true,
    },
]

export { CAREER_FIELDS }
export type { CareerField }
