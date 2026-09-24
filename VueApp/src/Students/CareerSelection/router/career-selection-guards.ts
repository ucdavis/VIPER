import type { RouteLocationNormalized } from "vue-router"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { useUserStore } from "@/store/UserStore"
import { CAREER_SELECTION_PERMISSIONS } from "../constants/permissions"

const { ADMIN, READ_ONLY, STUDENT } = CAREER_SELECTION_PERMISSIONS
// A mentor sees a roster, but only of the students they mentor. Which students those are is
// data the client does not hold, so the guards let faculty through and the API decides.
const { FACULTY } = CAREER_SELECTION_PERMISSIONS
// Those in the STUDENTS_DVM role have this permission.
// The app open/close switch flips only STUDENT, so read access survives a closed app.
const { VIEW_OWN } = CAREER_SELECTION_PERMISSIONS

const HOME = { name: "StudentsHome" }

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

function canEditOwnRecord(): boolean {
    return checkHasOnePermission([STUDENT])
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
        return HOME
    }

    return Number(pidm) === ownId ? true : viewRoute(ownId)
}

function requireCareerEditAccess(pidm: string | number) {
    if (isAdmin()) {
        return true
    }

    const ownId = ownRecordId()
    if (Number(pidm) !== ownId) {
        // Do not disclose the existence of the record to unauthorized users.
        if (canViewAllRecords() || isFaculty()) {
            return viewRoute(pidm)
        }
        return ownId !== null && hasOwnRecord() ? viewRoute(ownId) : HOME
    }

    if (canEditOwnRecord()) {
        return true
    }
    // App closed: the record stays readable even though it can no longer be edited.
    return hasOwnRecord() ? viewRoute(ownId) : HOME
}

function requireCareerListAccess() {
    if (canViewAllRecords() || isFaculty()) {
        return true
    }

    const ownId = ownRecordId()
    if (ownId === null || !hasOwnRecord()) {
        return HOME
    }

    // Edit rather than View, because the edit guard downgrades to View when the app is
    // closed and a student who can edit should not have to click through a read-only page.
    return editRoute(ownId)
}

const careerSelectionGuards = {
    list: requireCareerListAccess,
    view: (to: RouteLocationNormalized) => requireCareerViewAccess(to.params.pidm as string),
    edit: (to: RouteLocationNormalized) => requireCareerEditAccess(to.params.pidm as string),
}

export { careerSelectionGuards, requireCareerViewAccess, requireCareerEditAccess, requireCareerListAccess }
