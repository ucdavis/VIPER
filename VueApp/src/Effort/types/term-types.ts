/**
 * Term and person types for the Effort system.
 */

type TermDto = {
    termCode: number
    termName: string
    status: string
    harvestedDate: string | null
    openedDate: string | null
    closedDate: string | null
    isOpen: boolean
    canEdit: boolean
    // State transition properties for term management UI
    canOpen: boolean
    canClose: boolean
    canReopen: boolean
    canUnopen: boolean
    canDelete: boolean
    canHarvest: boolean
    canImportClinical: boolean
}

type PersonDto = {
    personId: number
    termCode: number
    mailId: string | null
    firstName: string
    lastName: string
    middleInitial: string | null
    fullName: string
    effortTitleCode: string
    effortDept: string
    percentAdmin: number
    jobGroupId: string | null
    title: string | null
    adminUnit: string | null
    effortVerified: string | null
    reportUnit: string | null
    volunteerWos: boolean
    percentClinical: number | null
    isVerified: boolean
    recordCount: number
    hasZeroHourRecords: boolean
    canSendVerificationEmail: boolean
    lastEmailedDate: string | null
    lastEmailedBy: string | null
    // Percentage summaries for instructor list display
    percentAdminSummary: string | null
    percentClinicalSummary: string | null
    percentOtherSummary: string | null
}

type AvailableTermDto = {
    termCode: number
    termName: string
    startDate: string
}

type TermOptionDto = {
    termCode: number
    termName: string
}

export type { TermDto, PersonDto, AvailableTermDto, TermOptionDto }
