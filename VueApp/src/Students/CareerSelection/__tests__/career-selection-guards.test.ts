import { setActivePinia, createPinia } from "pinia"
import { useUserStore } from "@/store/UserStore"
import {
    requireCareerViewAccess,
    requireCareerEditAccess,
    requireCareerListAccess,
} from "../router/career-selection-guards"

/**
 * Tests for the Career Selection route guards.
 * Faculty (SVMSecure.CareerSelection.Faculty) is deliberately absent: whether a given
 * student is one of a faculty member's mentees is not answerable from the permission
 * array, so it is enforced by the API rather than here.
 */

const ADMIN = "SVMSecure.CareerSelection.Admin"
const READ_ONLY = "SVMSecure.CareerSelection.ReadOnly"
const STUDENT = "SVMSecure.CareerSelection.Student"
const VIEW_OWN = "SVMSecure.CareerSelection.ViewOwn"

const OWN_ID = 100
const OTHER_ID = 200
const HOME = { name: "StudentsHome" }

// A guard returns true to admit, or a route location to redirect. Both are truthy, so
// compare against true rather than asserting truthiness on the raw result.
function admits(result: unknown): boolean {
    return result === true
}

function ownView(pidm: number = OWN_ID) {
    return { name: "CareerSelectionView", params: { pidm } }
}

function setUser(permissions: string[], userId: number | null = OWN_ID): void {
    setActivePinia(createPinia())
    const userStore = useUserStore()
    userStore.userInfo.userId = userId
    userStore.setPermissions(permissions)
}

describe("career selection route guards", () => {
    describe("view access", () => {
        it("allows an admin to view any record", () => {
            expect.hasAssertions()
            setUser([ADMIN])
            expect(admits(requireCareerViewAccess(OTHER_ID))).toBeTruthy()
        })

        it("allows a read-only user to view any record", () => {
            expect.hasAssertions()
            setUser([READ_ONLY])
            expect(admits(requireCareerViewAccess(OTHER_ID))).toBeTruthy()
        })

        it("allows a student to view their own record while the app is open", () => {
            expect.hasAssertions()
            setUser([VIEW_OWN, STUDENT])
            expect(admits(requireCareerViewAccess(OWN_ID))).toBeTruthy()
        })

        it("allows a student to view their own record while the app is closed", () => {
            expect.hasAssertions()
            // Closing the app strips Student from the role but leaves ViewOwn in place.
            setUser([VIEW_OWN])
            expect(admits(requireCareerViewAccess(OWN_ID))).toBeTruthy()
        })

        it("allows a student holding only an individual grant to view their own record", () => {
            expect.hasAssertions()
            // An individual grant surfaces as Student without the ViewOwn role membership.
            setUser([STUDENT])
            expect(admits(requireCareerViewAccess(OWN_ID))).toBeTruthy()
        })

        it("redirects a student away from another student's record", () => {
            expect.hasAssertions()
            setUser([VIEW_OWN, STUDENT])
            expect(requireCareerViewAccess(OTHER_ID)).toStrictEqual(ownView())
        })

        it("sends a user with no career selection access home", () => {
            expect.hasAssertions()
            setUser(["SVMSecure.Students"])
            expect(requireCareerViewAccess(OWN_ID)).toStrictEqual(HOME)
        })

        it("sends a user with no resolved id home", () => {
            expect.hasAssertions()
            setUser([VIEW_OWN], null)
            expect(requireCareerViewAccess(OWN_ID)).toStrictEqual(HOME)
        })
    })

    describe("edit access", () => {
        it("allows an admin to edit any record", () => {
            expect.hasAssertions()
            setUser([ADMIN])
            expect(admits(requireCareerEditAccess(OTHER_ID))).toBeTruthy()
        })

        // canEditOwnRecord no longer lists ADMIN, so the early return in requireCareerEditAccess
        // is the only thing admitting an admin to their own record.
        it("allows an admin to edit their own record", () => {
            expect.hasAssertions()
            setUser([ADMIN])
            expect(admits(requireCareerEditAccess(OWN_ID))).toBeTruthy()
        })

        it("allows a student to edit their own record while the app is open", () => {
            expect.hasAssertions()
            setUser([VIEW_OWN, STUDENT])
            expect(admits(requireCareerEditAccess(OWN_ID))).toBeTruthy()
        })

        it("redirects a student to their own view while the app is closed", () => {
            expect.hasAssertions()
            setUser([VIEW_OWN])
            expect(requireCareerEditAccess(OWN_ID)).toStrictEqual(ownView())
        })

        it("redirects a student away from another student's edit page", () => {
            expect.hasAssertions()
            setUser([VIEW_OWN, STUDENT])
            expect(requireCareerEditAccess(OTHER_ID)).toStrictEqual(ownView())
        })

        it("redirects a read-only user to the record they asked for, not their own", () => {
            expect.hasAssertions()
            setUser([READ_ONLY])
            expect(requireCareerEditAccess(OTHER_ID)).toStrictEqual(ownView(OTHER_ID))
        })

        it("does not let a read-only user edit", () => {
            expect.hasAssertions()
            setUser([READ_ONLY])
            expect(admits(requireCareerEditAccess(OTHER_ID))).toBeFalsy()
        })

        it("sends a user with no career selection access home", () => {
            expect.hasAssertions()
            setUser(["SVMSecure.Students"])
            expect(requireCareerEditAccess(OWN_ID)).toStrictEqual(HOME)
        })
    })

    describe("list access", () => {
        it("allows an admin onto the list", () => {
            expect.hasAssertions()
            setUser([ADMIN])
            expect(admits(requireCareerListAccess())).toBeTruthy()
        })

        it("allows a read-only user onto the list", () => {
            expect.hasAssertions()
            setUser([READ_ONLY])
            expect(admits(requireCareerListAccess())).toBeTruthy()
        })

        it("redirects a student to their own record", () => {
            expect.hasAssertions()
            setUser([VIEW_OWN, STUDENT])
            expect(requireCareerListAccess()).toStrictEqual({
                name: "CareerSelectionEdit",
                params: { pidm: OWN_ID },
            })
        })

        it("sends a user with no career selection access home", () => {
            expect.hasAssertions()
            setUser(["SVMSecure.Students"])
            expect(requireCareerListAccess()).toStrictEqual(HOME)
        })
    })
})
