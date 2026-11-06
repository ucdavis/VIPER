import type { EisCampusListing, EisPersonHeader } from "../types/eis-types"
import { formatEisDate, formatHours, formatServiceCredit } from "./eis-format"

/** One labelled value in an EisFactList. A null value shows as blank. */
interface EisFact {
    label: string
    value: string | number | null
    href?: string
}

/**
 * One line of an EisFactList: one or two related facts side by side, as the legacy EIS header
 * paired them (date of birth with age, home with alternate department). A lone fact leaves the
 * second column empty rather than pulling the next pair up beside it.
 */
type EisFactRow = EisFact[]

/** The email address, linked when there is one. */
function emailFact(email: string | null): EisFact {
    return email ? { label: "Email", value: email, href: `mailto:${email}` } : { label: "Email", value: email }
}

/** As in the legacy header, PTO shows only for people who have a balance. */
function ptoFacts(hours: number | null): EisFact[] {
    return hours !== null && hours > 0 ? [{ label: "PTO balance", value: formatHours(hours) }] : []
}

function staffRows(header: EisPersonHeader): EisFactRow[] {
    return header.isStaff
        ? [
              [
                  { label: "Staff program", value: header.staffProgram },
                  { label: "Staff type", value: header.staffStatus },
              ],
          ]
        : []
}

function facultyRows(header: EisPersonHeader): EisFactRow[] {
    return header.isFaculty
        ? [
              [
                  { label: "Faculty program", value: header.facultyProgram },
                  { label: "Ladder rank", value: header.ladderRank },
              ],
          ]
        : []
}

/** The short header above every EIS page, plus the staff or faculty program the person holds. */
function headerFacts(header: EisPersonHeader): EisFactRow[] {
    return [
        [{ label: "Employee ID", value: header.employeeId }],
        [{ label: "Primary affiliation", value: header.primaryAffiliation }],
        [
            { label: "Primary title", value: header.primaryTitle },
            { label: "Job group ID", value: header.jobGroup },
        ],
        [
            { label: "Home department", value: header.homeDepartment },
            { label: "Alternate department", value: header.alternateDepartment },
        ],
        ...staffRows(header),
        ...facultyRows(header),
    ]
}

/** One titled group of rows in the Summary, set apart from the next by a separator. */
interface EisFactSection {
    title: string
    rows: EisFactRow[]
}

/**
 * The legacy EIS home page details, in the legacy order, split where the subject changes:
 * personal and contact details, the position's units, then service credit and leave.
 */
function summaryFacts(person: EisPersonHeader): EisFactSection[] {
    return [
        {
            title: "Personal",
            rows: [
                [emailFact(person.email)],
                [
                    { label: "Date of birth", value: person.dateOfBirth },
                    { label: "Age", value: person.age },
                ],
                [
                    { label: "Gender", value: person.gender },
                    { label: "Ethnicity", value: person.ethnicity },
                ],
                [
                    { label: "Hire date", value: formatEisDate(person.hireDate) },
                    { label: "Employment status", value: person.employmentStatus },
                ],
                [
                    { label: "US citizen", value: person.citizenship },
                    { label: "Visa type", value: person.visa },
                ],
            ],
        },
        {
            title: "Position",
            rows: [
                [
                    { label: "Bargaining unit", value: person.bargainingUnit },
                    { label: "Labor relations unit", value: person.laborRelationsUnit },
                ],
            ],
        },
        {
            title: "Service and leave",
            rows: [
                [
                    { label: "Service credit", value: formatServiceCredit(person.serviceCreditMonths) },
                    { label: "Vacation balance", value: formatHours(person.vacationHours) },
                ],
                [{ label: "Sick leave balance", value: formatHours(person.sickHours) }, ...ptoFacts(person.ptoHours)],
            ],
        },
    ]
}

/** One campus directory listing. */
function listingFacts(listing: EisCampusListing): EisFactRow[] {
    return [
        [
            { label: "Title", value: listing.title },
            { label: "Department", value: listing.department },
        ],
        [
            { label: "Address", value: listing.address },
            { label: "Phone", value: listing.phone },
        ],
    ]
}

export { headerFacts, listingFacts, summaryFacts }
export type { EisFact, EisFactRow, EisFactSection }
