import type { RouteLocationNormalized } from "vue-router"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { useUserStore } from "@/store/UserStore"
import { setAccessNotice } from "@/Students/composables/use-access-notice"
import { CAREER_SELECTION_ACCESS_MESSAGES } from "../constants/access-messages"
import { CAREER_SELECTION_PERMISSIONS } from "../constants/permissions"

const { ADMIN, READ_ONLY, STUDENT } = CAREER_SELECTION_PERMISSIONS
// A mentor sees a roster, but only of the students they mentor. Which students those are is
// data the client does not hold, so the guards let faculty through and the API decides.
const { FACULTY } = CAREER_SELECTION_PERMISSIONS
// Those in the STUDENTS_DVM role have this permission.
// The app open/close switch flips only STUDENT, so read access survives a closed app.
const { VIEW_OWN } = CAREER_SELECTION_PERMISSIONS

const HOME = { name: "StudentsHome" }

/**
 * Turns away a user with no Career Selection access at all. They land on Students Home, which
 * says why rather than leaving them to wonder how they got there.
 */
function denyAccess() {
    setAccessNotice(CAREER_SELECTION_ACCESS_MESSAGES.NO_ACCESS)
    return HOME
}

function viewRoute(pidm: string | number) {
    return { name: "CareerSelectionView", params: { pidm } }
}

function editRoute(pidm: string | number) {
    return { name: "CareerSelectionEdit", params: { pidm } }
}

function isAdmin(): boolean {
    return checkHasOnePermission([ADMIN])
}

function canViewAllRecords(): boolean {
    return checkHasOnePermission([ADMIN, READ_ONLY])
}

function isFaculty(): boolean {
    return checkHasOnePermission([FACULTY])
}

function hasOwnRecord(): boolean {
    return checkHasOnePermission([VIEW_OWN, STUDENT])
}

function ownRecordId(): number | null {
    const userStore = useUserStore()
    return userStore.userInfo.userId ?? null
}

// Viewing your own record is not gated on the edit permission, so the ownership check has
// to come before any permission check rather than after one.
function requireCareerViewAccess(pidm: string | number) {
    if (canViewAllRecords()) {
        return true
    }

    // Whether this student is one of their mentees is a question only the server can answer, so
    // a mentor is let through and the record request is left to decide it. A record that is not
    // theirs is refused there, which the page surfaces as its record-not-found notice.
    if (isFaculty()) {
        return true
    }

    const ownId = ownRecordId()
    if (ownId === null || !hasOwnRecord()) {
        return denyAccess()
    }

    return Number(pidm) === ownId ? true : viewRoute(ownId)
}

// Whether this record can be edited is the server's call: the record carries canEdit, the same
// rule its save enforces, and the form sends anyone it refuses on to the view page. Deciding it
// here from the client's permissions could disagree with the Edit button the server showed, so
// the edit page admits exactly who the view page does.
function requireCareerEditAccess(pidm: string | number) {
    return requireCareerViewAccess(pidm)
}

function requireCareerListAccess() {
    if (canViewAllRecords() || isFaculty()) {
        return true
    }

    const ownId = ownRecordId()
    if (ownId === null || !hasOwnRecord()) {
        return denyAccess()
    }

    // Edit rather than View, because the form downgrades to View when the app is closed and
    // a student who can edit should not have to click through a read-only page.
    return editRoute(ownId)
}

/**
 * The report is for the same staff as the roster. Anyone else is handled exactly as the roster
 * handles them, since the legacy app served both from one address: a student goes to their own
 * record, and a user with no access at all is turned away.
 */
function requireCareerReportAccess() {
    return canViewAllRecords() || isFaculty() ? true : requireCareerListAccess()
}

/**
 * Only admins manage the dropdown options. Legacy refused everyone else outright on this page;
 * here a user who has other Career Selection access goes where the roster would send them, with
 * a notice saying why, rather than being left at a dead end.
 */
function requireCareerOptionsAccess() {
    if (isAdmin()) {
        return true
    }

    const hasOtherAccess = canViewAllRecords() || isFaculty() || (ownRecordId() !== null && hasOwnRecord())
    if (!hasOtherAccess) {
        return denyAccess()
    }

    setAccessNotice(CAREER_SELECTION_ACCESS_MESSAGES.MANAGE_OPTIONS)
    // The roster's own guard sends each role on from there: staff stay, a student goes to
    // their record.
    return { name: "CareerSelectionList" }
}

const careerSelectionGuards = {
    list: requireCareerListAccess,
    view: (to: RouteLocationNormalized) => requireCareerViewAccess(to.params.pidm as string),
    edit: (to: RouteLocationNormalized) => requireCareerEditAccess(to.params.pidm as string),
    report: requireCareerReportAccess,
    options: requireCareerOptionsAccess,
}

export {
    careerSelectionGuards,
    requireCareerViewAccess,
    requireCareerEditAccess,
    requireCareerListAccess,
    requireCareerReportAccess,
    requireCareerOptionsAccess,
}
