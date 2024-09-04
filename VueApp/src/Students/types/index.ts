
export type Student = {
    personId: number,
    mailId: string,
    lastName: string,
    firstName: string,
    middleName: string | null,
    fullName: string,
    classLevel: string | null,
    termCode: number | null,
    classYear: number | null,
    email: string,
    currentClassYear: boolean,
    active: boolean,
    classYears: StudentClassYear[],
    
}

export type StudentClassYear = {
    studentClassYearId: number,
    personId: number,
    classYear: number,
    active: boolean,
    graduated: boolean,
    ross: boolean,
    leftTerm: number | null,
    leftReason: number | null,
    added: Date,
    addedBy: number | null,
    updated: Date | null,
    updatedBy: number | null,
    comment: string | null,
    leftReasonText: string | null,
}

export type StudentClassYearProblem = {
    personId: number,
    mailId: string,
    lastName: string,
    firstName: string,
    middleName: string | null,
    fullName: string,
    classLevel: string | null,
    termCode: number | null,
    classYear: number | null,
    email: string,
    currentClassYear: boolean,
    active: boolean,
    classYears: StudentClassYear[],
    expectedClassYear: number | null,
    problems: string,
}

export type StudentClassYearUpdate = {
    studentClassYearId: number,
    classYear: number | null,
    personId: number | null,
    ross: boolean | null,
    leftReason: number | null,
    leftTerm: number | null,
    comment: string | null,
    active: boolean,
}