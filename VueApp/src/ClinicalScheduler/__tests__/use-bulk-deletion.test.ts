import { useBulkDeletion } from "../composables/use-bulk-deletion"
import type { ScheduleData } from "../utils/schedule-update-helpers"

type RemoveScheduleFn = (
    scheduleData: ScheduleData,
    scheduleId: number,
    options: { onSuccess: (wasPrimary?: boolean, instructorName?: string) => void; onError: () => void },
) => void

// $q.dialog() returns a chainable DialogChainObject, not a Promise. The mock
// mirrors that shape so a regression back to `await $q.dialog(...)` fails here.
type DialogOutcome = "ok" | "cancel" | "dismiss"

const { mockDialog, mockNotify, setUserChoice } = vi.hoisted(() => {
    let choice: DialogOutcome = "ok"
    const chain: Record<string, unknown> = {}
    const handler = (outcome: DialogOutcome) =>
        vi.fn<(callback: () => void) => unknown>((callback) => {
            if (choice === outcome) {
                callback()
            }
            return chain
        })
    chain.onOk = handler("ok")
    chain.onCancel = handler("cancel")
    chain.onDismiss = handler("dismiss")
    return {
        mockDialog: vi.fn<(...args: unknown[]) => unknown>(() => chain),
        mockNotify: vi.fn<(...args: unknown[]) => unknown>(),
        setUserChoice: (next: DialogOutcome) => {
            choice = next
        },
    }
})

vi.mock("quasar", () => ({
    useQuasar: () => ({ dialog: mockDialog, notify: mockNotify }),
}))

describe("useBulkDeletion - confirmation gate", () => {
    const scheduleData = { rotationId: 1 } as unknown as ScheduleData
    const items = [
        { scheduleId: 10, displayName: "Alex Doe", weekNumber: 1 },
        { scheduleId: 11, displayName: "Sam Roe", weekNumber: 2 },
    ]

    const buildOptions = (removeScheduleWithRollback: RemoveScheduleFn) => ({
        confirmationTitle: "Remove assignments",
        confirmationMessage: "Are you sure?",
        successMessage: (count: number) => `${count} removed`,
        errorMessage: "Failed",
        removeScheduleWithRollback,
        clearSelections: vi.fn<() => void>(),
    })

    beforeEach(() => {
        vi.clearAllMocks()
    })

    it("does not delete anything when the user cancels", async () => {
        expect.hasAssertions()
        setUserChoice("cancel")
        const removeScheduleWithRollback = vi.fn<RemoveScheduleFn>()
        const { executeBulkDeletion, isDeleting } = useBulkDeletion()

        await executeBulkDeletion(scheduleData, items, buildOptions(removeScheduleWithRollback))

        expect(mockDialog).toHaveBeenCalledTimes(1)
        expect(removeScheduleWithRollback).not.toHaveBeenCalled()
        expect(isDeleting.value).toBe(false)
    })

    it("settles without deleting when the dialog is dismissed rather than answered", async () => {
        expect.hasAssertions()
        // A programmatic close fires neither onOk nor onCancel; without an onDismiss
        // handler this await never settles and the flow wedges instead of failing.
        setUserChoice("dismiss")
        const removeScheduleWithRollback = vi.fn<RemoveScheduleFn>()
        const { executeBulkDeletion, isDeleting } = useBulkDeletion()

        await executeBulkDeletion(scheduleData, items, buildOptions(removeScheduleWithRollback))

        expect(removeScheduleWithRollback).not.toHaveBeenCalled()
        expect(isDeleting.value).toBe(false)
    })

    it("deletes every selected item when the user confirms", async () => {
        expect.hasAssertions()
        setUserChoice("ok")
        const removeScheduleWithRollback = vi.fn<RemoveScheduleFn>()
        const { executeBulkDeletion } = useBulkDeletion()

        await executeBulkDeletion(scheduleData, items, buildOptions(removeScheduleWithRollback))

        expect(removeScheduleWithRollback).toHaveBeenCalledTimes(items.length)
    })

    it("skips the dialog entirely when skipConfirmation is set", async () => {
        expect.hasAssertions()
        const removeScheduleWithRollback = vi.fn<RemoveScheduleFn>()
        const { executeBulkDeletion } = useBulkDeletion()

        await executeBulkDeletion(scheduleData, items, {
            ...buildOptions(removeScheduleWithRollback),
            skipConfirmation: true,
        })

        expect(mockDialog).not.toHaveBeenCalled()
        expect(removeScheduleWithRollback).toHaveBeenCalledTimes(items.length)
    })
})
