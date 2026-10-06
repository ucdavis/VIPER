import { inflect } from "inflection"
import type { EisAppointment, EisPersonOption } from "../types/eis-types"

/**
 * Display formatting for the EIS pages, matching the legacy pages: US dates, dollar amounts
 * with cents, and service credit in months and years.
 */

const LOCALE = "en-US"
const ISO_DATE = /^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})/u
const MONTHS_PER_YEAR = 12
const CENTS = 2
const MAX_PERCENT_DECIMALS = 6

const moneyFormat = new Intl.NumberFormat(LOCALE, { minimumFractionDigits: CENTS, maximumFractionDigits: CENTS })
const decimalFormat = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: MAX_PERCENT_DECIMALS })

/** "2026-07-01" becomes "07/01/2026"; null becomes "". Other text is left as it is. */
function formatEisDate(value: string | null): string {
    if (value === null) {
        return ""
    }
    const parts = ISO_DATE.exec(value)?.groups
    return parts ? `${parts.month}/${parts.day}/${parts.year}` : value
}

/** 1234.5 becomes "1,234.50"; null becomes "". */
function formatMoney(value: number | null): string {
    return value === null ? "" : moneyFormat.format(value)
}

/** A distribution percent with two decimals, as the legacy page showed it; null becomes "". */
function formatPercent(value: number | null): string {
    return formatMoney(value)
}

/** A percent or step as the system stores it, without trailing zeros; null becomes "". */
function formatDecimal(value: number | null): string {
    return value === null ? "" : decimalFormat.format(value)
}

/** Leave balances in hours: "120.50 hrs". */
function formatHours(value: number | null): string {
    return value === null ? "" : `${moneyFormat.format(value)} hrs`
}

/** "360 months (30.00 yrs)", as the legacy header showed service credit. */
function formatServiceCredit(months: number): string {
    const years = (months / MONTHS_PER_YEAR).toFixed(CENTS)
    return `${decimalFormat.format(months)} ${inflect("month", months)} (${years} yrs)`
}

/**
 * The heading of one appointment: "Appt. 1 — PROF-AY (001100) — Grade: 5 — EXEMPT — VM: VME —
 * 07/01/2020 to 06/30/2026", leaving out the parts it doesn't have.
 */
function describeAppointment(appointment: EisAppointment): string {
    const dates = [appointment.beginDate, appointment.endDate].filter((date) => date !== null).join(" to ")
    return [
        `Appt. ${appointment.number ?? "?"}`,
        `${appointment.title} (${appointment.titleCode})`,
        appointment.grade === null ? "" : `Grade: ${appointment.grade}`,
        appointment.exemptStatus,
        appointment.department,
        dates,
    ]
        .filter((part) => part !== "")
        .join(" — ")
}

/** People whose name or employee ID contains the search text, ignoring case. */
function filterPeople(people: EisPersonOption[], search: string): EisPersonOption[] {
    const needle = search.trim().toLowerCase()
    if (needle === "") {
        return people
    }
    return people.filter((person) => person.name.toLowerCase().includes(needle) || person.employeeId.includes(needle))
}

export {
    describeAppointment,
    filterPeople,
    formatDecimal,
    formatEisDate,
    formatHours,
    formatMoney,
    formatPercent,
    formatServiceCredit,
}
