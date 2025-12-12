import { describe, it, expect, vi, beforeEach } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import { useEffortPermissions, EffortPermissions } from "../composables/use-effort-permissions"
import { useUserStore } from "@/store/UserStore"

describe("useEffortPermissions", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Permission Constants", () => {
        it("exports correct permission strings", () => {
            expect(EffortPermissions.Base).toBe("SVMSecure.Effort")
            expect(EffortPermissions.ViewDept).toBe("SVMSecure.Effort.ViewDept")
            expect(EffortPermissions.ViewAllDepartments).toBe("SVMSecure.Effort.ViewAllDepartments")
            expect(EffortPermissions.EditEffort).toBe("SVMSecure.Effort.EditEffort")
            expect(EffortPermissions.ManageTerms).toBe("SVMSecure.Effort.ManageTerms")
            expect(EffortPermissions.VerifyEffort).toBe("SVMSecure.Effort.VerifyEffort")
        })
    })

    describe("hasPermission function", () => {
        it("returns true when user has the permission", () => {
            const userStore = useUserStore()
            userStore.setPermissions(["SVMSecure.Effort", "SVMSecure.Effort.ViewDept"] as never[])

            const { hasPermission } = useEffortPermissions()

            expect(hasPermission("SVMSecure.Effort")).toBeTruthy()
            expect(hasPermission("SVMSecure.Effort.ViewDept")).toBeTruthy()
        })

        it("returns false when user does not have the permission", () => {
            const userStore = useUserStore()
            userStore.setPermissions(["SVMSecure.Effort"] as never[])

            const { hasPermission } = useEffortPermissions()

            expect(hasPermission("SVMSecure.Effort.ManageTerms")).toBeFalsy()
        })

        it("returns false when user has no permissions", () => {
            const { hasPermission } = useEffortPermissions()

            expect(hasPermission("SVMSecure.Effort")).toBeFalsy()
        })
    })

    describe("computed permission flags", () => {
        describe("hasManageTerms", () => {
            it("returns true when user has ManageTerms permission", () => {
                const userStore = useUserStore()
                userStore.setPermissions([EffortPermissions.ManageTerms] as never[])

                const { hasManageTerms } = useEffortPermissions()

                expect(hasManageTerms.value).toBeTruthy()
            })

            it("returns false when user lacks ManageTerms permission", () => {
                const userStore = useUserStore()
                userStore.setPermissions([EffortPermissions.ViewDept] as never[])

                const { hasManageTerms } = useEffortPermissions()

                expect(hasManageTerms.value).toBeFalsy()
            })
        })

        describe("hasViewAllDepartments", () => {
            it("returns true for admin users with ViewAllDepartments", () => {
                const userStore = useUserStore()
                userStore.setPermissions([EffortPermissions.ViewAllDepartments] as never[])

                const { hasViewAllDepartments, isAdmin } = useEffortPermissions()

                expect(hasViewAllDepartments.value).toBeTruthy()
                expect(isAdmin.value).toBeTruthy()
            })

            it("returns false for regular department users", () => {
                const userStore = useUserStore()
                userStore.setPermissions([EffortPermissions.ViewDept] as never[])

                const { hasViewAllDepartments, isAdmin } = useEffortPermissions()

                expect(hasViewAllDepartments.value).toBeFalsy()
                expect(isAdmin.value).toBeFalsy()
            })
        })

        describe("hasViewDept", () => {
            it("returns true when user has ViewDept permission", () => {
                const userStore = useUserStore()
                userStore.setPermissions([EffortPermissions.ViewDept] as never[])

                const { hasViewDept } = useEffortPermissions()

                expect(hasViewDept.value).toBeTruthy()
            })
        })

        describe("hasEditEffort", () => {
            it("returns true when user has EditEffort permission", () => {
                const userStore = useUserStore()
                userStore.setPermissions([EffortPermissions.EditEffort] as never[])

                const { hasEditEffort } = useEffortPermissions()

                expect(hasEditEffort.value).toBeTruthy()
            })
        })

        describe("hasVerifyEffort", () => {
            it("returns true when user has VerifyEffort permission (self-service)", () => {
                const userStore = useUserStore()
                userStore.setPermissions([EffortPermissions.VerifyEffort] as never[])

                const { hasVerifyEffort } = useEffortPermissions()

                expect(hasVerifyEffort.value).toBeTruthy()
            })
        })
    })

    describe("permission combinations for term management", () => {
        it("term manager can manage terms and view all departments", () => {
            const userStore = useUserStore()
            userStore.setPermissions([EffortPermissions.ViewAllDepartments, EffortPermissions.ManageTerms] as never[])

            const { hasManageTerms, isAdmin } = useEffortPermissions()

            expect(hasManageTerms.value).toBeTruthy()
            expect(isAdmin.value).toBeTruthy()
        })

        it("department user without ManageTerms cannot manage terms", () => {
            const userStore = useUserStore()
            userStore.setPermissions([EffortPermissions.ViewDept, EffortPermissions.EditEffort] as never[])

            const { hasManageTerms, hasEditEffort, hasViewDept } = useEffortPermissions()

            expect(hasManageTerms.value).toBeFalsy()
            expect(hasEditEffort.value).toBeTruthy()
            expect(hasViewDept.value).toBeTruthy()
        })

        it("self-service user can only verify their own effort", () => {
            const userStore = useUserStore()
            userStore.setPermissions([EffortPermissions.VerifyEffort] as never[])

            const { hasVerifyEffort, hasEditEffort, hasManageTerms, isAdmin } = useEffortPermissions()

            expect(hasVerifyEffort.value).toBeTruthy()
            expect(hasEditEffort.value).toBeFalsy()
            expect(hasManageTerms.value).toBeFalsy()
            expect(isAdmin.value).toBeFalsy()
        })
    })

    describe("permissions array exposure", () => {
        it("exposes raw permissions array for advanced checks", () => {
            const userStore = useUserStore()
            const testPermissions = [EffortPermissions.Base, EffortPermissions.ViewDept, EffortPermissions.EditEffort]
            userStore.setPermissions(testPermissions as never[])

            const { permissions } = useEffortPermissions()

            expect(permissions.value).toEqual(testPermissions)
            expect(permissions.value.length).toBe(3)
        })
    })
})
