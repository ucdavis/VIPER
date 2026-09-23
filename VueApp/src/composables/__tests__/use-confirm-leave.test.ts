import { ref } from "vue"
import type { Ref } from "vue"
import type { NavigationGuard } from "vue-router"
import { useConfirmLeave } from "../use-confirm-leave"

/**
 * Tests for useConfirmLeave: the guard that stops a form page from navigating away while it
 * holds unsaved changes.
 */

const mockConfirmAction = vi.fn<(...args: unknown[]) => Promise<boolean>>()
const registered: { guard?: NavigationGuard } = {}

vi.mock("vue-router", () => ({
    onBeforeRouteLeave: (guard: NavigationGuard) => {
        registered.guard = guard
    },
}))

vi.mock("@/composables/use-confirm-dialog", () => ({
    useConfirmDialog: () => ({ confirmAction: (...args: unknown[]) => mockConfirmAction(...args) }),
}))

/** Arms the guard for a form in the given state, as mounting the page would. */
function armGuard(isDirty: Ref<boolean>): void {
    vi.clearAllMocks()
    delete registered.guard
    useConfirmLeave(isDirty)
}

/** Runs the guard the composable registered, as the router would on a navigation. */
async function leave(): Promise<unknown> {
    // The guard ignores its arguments, so the router's to/from/next are not worth faking.
    return await (registered.guard as unknown as () => Promise<unknown>)()
}

describe("use confirm leave", () => {
    it("registers a leave guard", () => {
        expect.hasAssertions()
        armGuard(ref(false))

        expect(registered.guard).toBeDefined()
    })

    it("leaves without asking when the form is clean", async () => {
        expect.hasAssertions()
        armGuard(ref(false))

        await expect(leave()).resolves.toBeTruthy()
        expect(mockConfirmAction).not.toHaveBeenCalled()
    })

    it("asks before discarding unsaved changes", async () => {
        expect.hasAssertions()
        mockConfirmAction.mockResolvedValue(true)
        armGuard(ref(true))

        await expect(leave()).resolves.toBeTruthy()
        expect(mockConfirmAction).toHaveBeenCalledWith(
            expect.objectContaining({ title: "Unsaved Changes", okLabel: "Discard Changes", okColor: "negative" }),
        )
    })

    it("stays on the page when the student keeps editing", async () => {
        expect.hasAssertions()
        mockConfirmAction.mockResolvedValue(false)
        armGuard(ref(true))

        await expect(leave()).resolves.toBeFalsy()
    })

    it("follows the form's current state, not the state at registration", async () => {
        expect.hasAssertions()
        const isDirty = ref(false)
        armGuard(isDirty)

        isDirty.value = true
        mockConfirmAction.mockResolvedValue(false)

        await expect(leave()).resolves.toBeFalsy()
        expect(mockConfirmAction).toHaveBeenCalledWith(expect.anything())
    })
})
