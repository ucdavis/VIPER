type CareerDropdownOption = {
    label: string
    value: number | null
    // The catch-all option. Set by the server so a renamed "Other" still behaves like one.
    isOther: boolean
}

/**
 * One SVM affiliate offered by the mentor picker. `iamId` is what the shared person picker keys
 * its options on; `personId` is what a career selection is saved with.
 */
type MentorOption = {
    personId: number
    iamId: string
    fullName: string
    loginId: string | null
    mailId: string | null
}

type StudentInfo = {
    direction: CareerDropdownOption | null
    directionOther: string | null
    primaryFocus: CareerDropdownOption | null
    primaryFocusOther: string | null
    secondaryFocus: CareerDropdownOption | null
    secondaryFocusOther: string | null
    postGrad: CareerDropdownOption | null
    mentorId?: number | null
    mentorName?: string | null
    // The picker keys its options on the IAM id, so a saved mentor needs one to
    // render. The server saves the mentor from mentorId and ignores this.
    mentorIamId?: string | null
    shortTermPlans: string
    longTermPlans: string
}

type StudentCareerListItem = {
    personId: number
    rowKey: string
    hasDetailRoute: boolean
    fullName: string
    classLevel: string
    email: string
    directionCompleted: boolean
    primaryFocusCompleted: boolean
    secondaryFocusCompleted: boolean
    postGradCompleted: boolean
    shortTermPlansCompleted: boolean
    longTermPlansCompleted: boolean
    mentorName?: string
    lastUpdated: string | null
}

type StudentCareerDetail = {
    personId: number
    fullName: string
    classLevel: string
    studentInfo: StudentInfo
    canEdit: boolean
    canViewStudentList: boolean
    lastUpdated: string | null
}

type StudentCareerReport = {
    personId: number
    rowKey: string
    hasDetailRoute: boolean
    fullName: string
    classLevel: string
    email: string
    direction: string
    primaryFocus: string
    secondaryFocus: string
    postGrad: string
    shortTermPlans: string
    longTermPlans: string
    mentorName: string
    lastUpdated: string | null
}

// What the shared roster table needs of a row, which the overview and report rows both carry.
type CareerTableRow = {
    personId: number
    rowKey: string
    hasDetailRoute: boolean
    fullName: string
    classLevel: string
    email?: string | null
    lastUpdated?: string | null
}

// The three option tables behind the form's dropdowns.
type CareerOptionType = "career" | "species" | "postGrad"

// One dropdown option as the server sends it, to the form and the admin page alike.
type CareerSelectionOption = {
    id: number
    label: string
    isOther: boolean
    usageCount: number // Only admins get a real count; it is 0 for everyone else.
}

type CareerOptionSaveResult = {
    success: boolean
    errors: string[]
}

export type {
    CareerDropdownOption,
    CareerOptionType,
    CareerSelectionOption,
    CareerOptionSaveResult,
    CareerTableRow,
    MentorOption,
    StudentInfo,
    StudentCareerListItem,
    StudentCareerDetail,
    StudentCareerReport,
}
