import { useScheduleUpdatesWithRollback } from "../composables/use-optimistic-schedule-updates"
import { InstructorScheduleService } from "../services/instructor-schedule-service"

vi.mock("../services/instructor-schedule-service")
vi.mock("../composables/use-schedule-state-updater", () => ({
    useScheduleStateUpdater: vi.fn(() => ({
        addScheduleToWeek: vi.fn(),
        removeScheduleFromWeek: vi.fn(),
        updateSchedulePrimaryStatus: vi.fn(),
    })),
}))

// Mock structuredClone to handle test data
// eslint-disable-next-line prefer-structured-clone -- Mocking structuredClone for tests
globalThis.structuredClone = vi.fn((obj) => JSON.parse(JSON.stringify(obj)))

describe("useScheduleUpdatesWithRollback - Operation Queue", () => {
    let mockScheduleData: any = null

    beforeEach(() => {
        vi.clearAllMocks()
        // Create a plain object that can be cloned
        mockScheduleData = {
            rotationId: 1,
            rotation: {
                rotId: 1,
                name: "Test Rotation",
                abbreviation: "TR",
                serviceId: 1,
                serviceName: "Test Service",
            },
            schedulesBySemester: [],
        }
    })

    it("should queue concurrent add operations for sequential processing", async () => {
        const { addScheduleWithRollback } = useScheduleUpdatesWithRollback()
        const onSuccess = vi.fn()

        const callOrder: number[] = []
        let callIndex = 0

        // Mock API calls to track order
        vi.mocked(InstructorScheduleService.addInstructor).mockImplementation(() => {
            callIndex += 1
            const currentIndex = callIndex
            callOrder.push(currentIndex)
            return Promise.resolve({
                success: true,
                result: { scheduleIds: [currentIndex] },
                errors: [],
            })
        })

        // Queue three operations rapidly
        addScheduleWithRollback(
            {
                scheduleData: mockScheduleData,
                weekId: 1,
                assignmentData: {
                    clinicianMothraId: "test123",
                    clinicianName: "Test Clinician 1",
                    isPrimary: false,
                    gradYear: 2025,
                },
            },
            { onSuccess },
        )

        addScheduleWithRollback(
            {
                scheduleData: mockScheduleData,
                weekId: 2,
                assignmentData: {
                    clinicianMothraId: "test456",
                    clinicianName: "Test Clinician 2",
                    isPrimary: false,
                    gradYear: 2025,
                },
            },
            { onSuccess },
        )

        addScheduleWithRollback(
            {
                scheduleData: mockScheduleData,
                weekId: 3,
                assignmentData: {
                    clinicianMothraId: "test789",
                    clinicianName: "Test Clinician 3",
                    isPrimary: false,
                    gradYear: 2025,
                },
            },
            { onSuccess },
        )

        // Wait for all operations to complete
        await vi.waitFor(() => {
            expect(InstructorScheduleService.addInstructor).toHaveBeenCalledTimes(3)
        })

        // All operations should have succeeded in order
        expect(callOrder).toEqual([1, 2, 3])
        expect(onSuccess).toHaveBeenCalledTimes(3)
    })

    it("should process mixed operations in queue order", async () => {
        const { addScheduleWithRollback, removeScheduleWithRollback } = useScheduleUpdatesWithRollback()
        const operationLog: string[] = []

        vi.mocked(InstructorScheduleService.addInstructor).mockImplementation(() => {
            operationLog.push("add")
            return Promise.resolve({
                success: true,
                result: { scheduleIds: [1] },
                errors: [],
            })
        })

        vi.mocked(InstructorScheduleService.removeInstructor).mockImplementation(() => {
            operationLog.push("remove")
            return Promise.resolve({
                success: true,
                result: null,
                errors: [],
            })
        })

        // Queue mixed operations
        addScheduleWithRollback({
            scheduleData: mockScheduleData,
            weekId: 1,
            assignmentData: {
                clinicianMothraId: "test123",
                clinicianName: "Test Clinician",
                isPrimary: false,
                gradYear: 2025,
            },
        })

        removeScheduleWithRollback(mockScheduleData, 123)

        addScheduleWithRollback({
            scheduleData: mockScheduleData,
            weekId: 2,
            assignmentData: {
                clinicianMothraId: "test456",
                clinicianName: "Another Clinician",
                isPrimary: false,
                gradYear: 2025,
            },
        })

        // Wait for all operations to complete
        await vi.waitFor(() => {
            expect(operationLog.length).toBe(3)
        })

        // Operations should execute in order
        expect(operationLog).toEqual(["add", "remove", "add"])
    })
})

describe("useScheduleUpdatesWithRollback - Error Handling", () => {
    let mockScheduleData: any = null

    beforeEach(() => {
        vi.clearAllMocks()
        // Create a plain object that can be cloned
        mockScheduleData = {
            rotationId: 1,
            rotation: {
                rotId: 1,
                name: "Test Rotation",
                abbreviation: "TR",
                serviceId: 1,
                serviceName: "Test Service",
            },
            schedulesBySemester: [],
        }
    })

    it("should continue processing queue even when an operation fails", async () => {
        const { addScheduleWithRollback } = useScheduleUpdatesWithRollback()
        const onError = vi.fn()
        const onSuccess = vi.fn()

        // First call will fail, second and third will succeed
        vi.mocked(InstructorScheduleService.addInstructor)
            .mockRejectedValueOnce(new Error("API Error"))
            .mockResolvedValueOnce({
                success: true,
                result: { scheduleIds: [2] },
                errors: [],
            })
            .mockResolvedValueOnce({
                success: true,
                result: { scheduleIds: [3] },
                errors: [],
            })

        // Queue three operations
        addScheduleWithRollback(
            {
                scheduleData: mockScheduleData,
                weekId: 1,
                assignmentData: {
                    clinicianMothraId: "test123",
                    clinicianName: "Test Clinician 1",
                    isPrimary: false,
                    gradYear: 2025,
                },
            },
            { onError },
        )

        addScheduleWithRollback(
            {
                scheduleData: mockScheduleData,
                weekId: 2,
                assignmentData: {
                    clinicianMothraId: "test456",
                    clinicianName: "Test Clinician 2",
                    isPrimary: false,
                    gradYear: 2025,
                },
            },
            { onSuccess },
        )

        addScheduleWithRollback(
            {
                scheduleData: mockScheduleData,
                weekId: 3,
                assignmentData: {
                    clinicianMothraId: "test789",
                    clinicianName: "Test Clinician 3",
                    isPrimary: false,
                    gradYear: 2025,
                },
            },
            { onSuccess },
        )

        // Wait for all operations to complete
        await vi.waitFor(() => {
            expect(InstructorScheduleService.addInstructor).toHaveBeenCalledTimes(3)
        })

        // First operation should have failed, other two should succeed
        expect(onError).toHaveBeenCalledOnce()
        expect(onError).toHaveBeenCalledWith("API Error")
        expect(onSuccess).toHaveBeenCalledTimes(2)
    })
})
