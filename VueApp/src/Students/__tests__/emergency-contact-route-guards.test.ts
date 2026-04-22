import { setActivePinia, createPinia } from "pinia"
import { useUserStore } from "@/store/UserStore"
import { requireEditAccess, requireOwnStudentRecord } from "../router/routes"

const STUDENT_USER_ID = 100
const OTHER_USER_ID = 200

function setUserPermissions(permissions: string[], userId: number | null = STUDENT_USER_ID): void {
    const userStore = useUserStore()
    userStore.userInfo.userId = userId
    userStore.setPermissions(permissions)
}

describe("Emergency Contact route guards", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    describe("own-record ownership check", () => {
        it("allows admin to access any student record", () => {
            setUserPermissions(["SVMSecure.Students.EmergencyContactAdmin"])
            expect(requireOwnStudentRecord(OTHER_USER_ID)).toBeTruthy()
        })

        it("allows SIS user to access any student record", () => {
            setUserPermissions(["SVMSecure.SIS.AllStudents"])
            expect(requireOwnStudentRecord(OTHER_USER_ID)).toBeTruthy()
        })

        it("allows student to access their own record", () => {
            setUserPermissions(["SVMSecure.Students.EmergencyContactStudent"])
            expect(requireOwnStudentRecord(STUDENT_USER_ID)).toBeTruthy()
        })

        it("redirects student to their own view when accessing someone else", () => {
            setUserPermissions(["SVMSecure.Students.EmergencyContactStudent"])
            expect(requireOwnStudentRecord(OTHER_USER_ID)).toEqual({
                name: "EmergencyContactView",
                params: { pidm: STUDENT_USER_ID },
            })
        })
    })

    describe("edit-access fallback to read-only view", () => {
        it("redirects student to view when app is closed (no edit permission)", () => {
            // App-closed state: student has baseline SVMSecure.Students but no EmergencyContactStudent
            setUserPermissions(["SVMSecure.Students"])
            expect(requireEditAccess(STUDENT_USER_ID)).toEqual({
                name: "EmergencyContactView",
                params: { pidm: STUDENT_USER_ID },
            })
        })

        it("allows student to edit their own record when app is open", () => {
            setUserPermissions(["SVMSecure.Students.EmergencyContactStudent"])
            expect(requireEditAccess(STUDENT_USER_ID)).toBeTruthy()
        })

        it("allows student with individual grant to edit when app is closed", () => {
            // Individual grant surfaces as EmergencyContactStudent regardless of role state
            setUserPermissions(["SVMSecure.Students.EmergencyContactStudent"])
            expect(requireEditAccess(STUDENT_USER_ID)).toBeTruthy()
        })

        it("allows admin to reach edit for any student", () => {
            setUserPermissions(["SVMSecure.Students.EmergencyContactAdmin"])
            expect(requireEditAccess(OTHER_USER_ID)).toBeTruthy()
        })

        it("redirects student to own record when trying to edit another student", () => {
            setUserPermissions(["SVMSecure.Students.EmergencyContactStudent"])
            expect(requireEditAccess(OTHER_USER_ID)).toEqual({
                name: "EmergencyContactView",
                params: { pidm: STUDENT_USER_ID },
            })
        })
    })
})
