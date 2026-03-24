import { useReportUrlParams } from "../composables/use-report-url-params"

const mockRoute = {
    query: {} as Record<string, string>,
    params: {} as Record<string, string>,
    name: "test-route",
}
const mockReplace = vi.fn()
vi.mock("vue-router", () => ({
    useRoute: () => mockRoute,
    useRouter: () => ({ replace: mockReplace }),
}))

describe("useReportUrlParams composable", () => {
    beforeEach(() => {
        mockRoute.query = {}
        mockRoute.params = {}
        mockReplace.mockClear()
    })

    describe("getInitialFilters", () => {
        it("returns undefined when query is empty", () => {
            const { initialFilters } = useReportUrlParams()

            expect(initialFilters).toBeUndefined()
        })

        it("parses academicYear from query", () => {
            mockRoute.query = { academicYear: "2025-2026" }

            const { initialFilters } = useReportUrlParams()

            expect(initialFilters).toEqual({ academicYear: "2025-2026" })
        })

        it("parses termCode from query as integer", () => {
            mockRoute.query = { termCode: "202510" }

            const { initialFilters } = useReportUrlParams()

            expect(initialFilters).toEqual({ termCode: 202510 })
        })

        it("academicYear takes priority over termCode", () => {
            mockRoute.query = { academicYear: "2025-2026", termCode: "202510" }

            const { initialFilters } = useReportUrlParams()

            expect(initialFilters).toEqual({ academicYear: "2025-2026" })
            expect(initialFilters?.termCode).toBeUndefined()
        })

        it("parses department, personId, role, title", () => {
            mockRoute.query = {
                department: "VMB",
                personId: "12345",
                role: "Instructor",
                title: "Professor",
            }

            const { initialFilters } = useReportUrlParams()

            expect(initialFilters).toEqual({
                department: "VMB",
                personId: 12345,
                role: "Instructor",
                title: "Professor",
            })
        })

        it("parses all filters combined", () => {
            mockRoute.query = {
                academicYear: "2025-2026",
                department: "PHR",
                personId: "99",
                role: "Lecturer",
                title: "Assistant Professor",
            }

            const { initialFilters } = useReportUrlParams()

            expect(initialFilters).toEqual({
                academicYear: "2025-2026",
                department: "PHR",
                personId: 99,
                role: "Lecturer",
                title: "Assistant Professor",
            })
        })
    })

    describe("updateUrlParams", () => {
        it("sets academicYear in query and clears termCode from path params", () => {
            const { updateUrlParams } = useReportUrlParams()

            updateUrlParams({ academicYear: "2025-2026" })

            expect(mockReplace).toHaveBeenCalledWith({
                name: "test-route",
                params: { termCode: undefined },
                query: { academicYear: "2025-2026" },
            })
        })

        it("sets termCode in query and path params", () => {
            const { updateUrlParams } = useReportUrlParams()

            updateUrlParams({ termCode: 202510 })

            expect(mockReplace).toHaveBeenCalledWith({
                name: "test-route",
                params: { termCode: "202510" },
                query: { termCode: "202510" },
            })
        })

        it("passes department, personId, role, title to query", () => {
            const { updateUrlParams } = useReportUrlParams()

            updateUrlParams({
                academicYear: "2025-2026",
                department: "VME",
                personId: 42,
                role: "Instructor",
                title: "Professor",
            })

            expect(mockReplace).toHaveBeenCalledWith({
                name: "test-route",
                params: { termCode: undefined },
                query: {
                    academicYear: "2025-2026",
                    department: "VME",
                    personId: "42",
                    role: "Instructor",
                    title: "Professor",
                },
            })
        })

        it("preserves route.params.termCode when no termCode in params", () => {
            mockRoute.params = { termCode: "202410" }

            const { updateUrlParams } = useReportUrlParams()

            updateUrlParams({ department: "APC" })

            expect(mockReplace).toHaveBeenCalledWith({
                name: "test-route",
                params: { termCode: "202410" },
                query: { department: "APC" },
            })
        })
    })
})
