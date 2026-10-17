import type { EisCampusListing, EisPersonHeader } from "../types/eis-types"
import { formatEisDate, formatHours, formatServiceCredit } from "./eis-format"

/** One labelled value in an EisFactList. A null value shows as blank. */
interface EisFact {
    label: string
    value: string | number | null
    href?: string
}

/** The email address, linked when there is one. */
function emailFact(email: string | null): EisFact {
    return email ? { label: "Email", value: email, href: `mailto:${email}` } : { label: "Email", value: email }
}

/** As in the legacy header, PTO shows only for people who have a balance. */
function ptoFacts(hours: number | null): EisFact[] {
    return hours !== null && hours > 0 ? [{ label: "PTO balance", value: formatHours(hours) }] : []
}

function staffFacts(header: EisPersonHeader): EisFact[] {
    return header.isStaff
        ? [
              { label: "Staff program", value: header.staffProgram },
              { label: "Staff type", value: header.staffStatus },
          ]
        : []
}

function facultyFacts(header: EisPersonHeader): EisFact[] {
    return header.isFaculty
        ? [
              { label: "Faculty program", value: header.facultyProgram },
              { label: "Ladder rank", value: header.ladderRank },
          ]
        : []
}

/** The short header above every EIS page, plus the staff or faculty program the person holds. */
function headerFacts(header: EisPersonHeader): EisFact[] {
    return [
        { label: "Employee ID", value: header.employeeId },
        { label: "Primary affiliation", value: header.primaryAffiliation },
        { label: "Primary title", value: header.primaryTitle },
        { label: "Job group ID", value: header.jobGroup },
        { label: "Home department", value: header.homeDepartment },
        { label: "Alternate department", value: header.alternateDepartment },
        ...staffFacts(header),
        ...facultyFacts(header),
    ]
}

/** The legacy EIS home page details: contact, demographics, employment, service credit and leave. */
function summaryFacts(person: EisPersonHeader): EisFact[] {
    return [
        emailFact(person.email),
        { label: "Date of birth", value: person.dateOfBirth },
        { label: "Age", value: person.age },
        { label: "Gender", value: person.gender },
        { label: "Ethnicity", value: person.ethnicity },
        { label: "Hire date", value: formatEisDate(person.hireDate) },
        { label: "Employment status", value: person.employmentStatus },
        { label: "US citizen", value: person.citizenship },
        { label: "Visa type", value: person.visa },
        { label: "Bargaining unit", value: person.bargainingUnit },
        { label: "Labor relations unit", value: person.laborRelationsUnit },
        { label: "Service credit", value: formatServiceCredit(person.serviceCreditMonths) },
        { label: "Vacation balance", value: formatHours(person.vacationHours) },
        { label: "Sick leave balance", value: formatHours(person.sickHours) },
        ...ptoFacts(person.ptoHours),
    ]
}

/** One campus directory listing. */
function listingFacts(listing: EisCampusListing): EisFact[] {
    return [
        { label: "Title", value: listing.title },
        { label: "Department", value: listing.department },
        { label: "Address", value: listing.address },
        { label: "Phone", value: listing.phone },
    ]
}

export { headerFacts, listingFacts, summaryFacts }
export type { EisFact }
