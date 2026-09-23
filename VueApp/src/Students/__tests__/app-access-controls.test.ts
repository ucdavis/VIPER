import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import AppAccessControls from "../components/AppAccessControls.vue"

/**
 * Tests for AppAccessControls, the banner that opens and closes student editing for a
 * self-service app. Its job beyond the toggle itself is to refuse a stale toggle: it re-reads the
 * status first, so one admin cannot undo a change another admin has just made.
 */

const mockNotify = vi.fn<(...args: unknown[]) => unknown>()
/** Captures the dialog's callbacks so a test can confirm or cancel it. */
const dialogCallbacks: { onOk?: () => Promise<void> | void } = {}

vi.mock("quasar", async (importOriginal) => {
    const actual = await importOriginal<Record<string, unknown>>()
    return {
        ...actual,
        useQuasar: () => ({
            notify: (...args: unknown[]) => mockNotify(...args),
            dialog: () => {
                const chain = {
                    onOk(handler: () => Promise<void> | void) {
                        dialogCallbacks.onOk = handler
                        return chain
                    },
                }
                return chain
            },
        }),
    }
})

type Status = { appOpen: boolean; individualGrantCount?: number } | null

function mountControls(statuses: Status[], toggleResult: boolean | null = true) {
    vi.clearAllMocks()
    delete dialogCallbacks.onOk
    const getStatus = vi.fn<() => Promise<Status>>()
    for (const status of statuses) {
        getStatus.mockResolvedValueOnce(status)
    }
    // Later reads repeat the last seeded status.
    getStatus.mockResolvedValue(statuses.at(-1) ?? null)
    const toggle = vi.fn<() => Promise<boolean | null>>().mockResolvedValue(toggleResult)
    return { wrapper: mountWith(getStatus, toggle), getStatus, toggle }
}

function mountWith(getStatus: () => Promise<Status>, toggle: () => Promise<boolean | null>) {
    return mount(AppAccessControls, {
        props: { getStatus, toggle },
        global: {
            // The component's own $q is mocked above; Quasar is installed for the components it renders.
            plugins: [[Quasar, {}]],
            stubs: { StatusBanner: { template: "<div><slot /></div>" } },
            // A rejected callback is still reported; this keeps it from failing the run as unhandled.
            config: { errorHandler: vi.fn<(err: unknown) => void>() },
        },
    })
}

/** Clicks the enable/disable button and runs the confirmation the dialog would have shown. */
async function confirmToggle(wrapper: ReturnType<typeof mountControls>["wrapper"]): Promise<void> {
    await wrapper.find(".app-access-toggle-btn").trigger("click")
    await dialogCallbacks.onOk?.()
    await flushPromises()
}

describe("status banner", () => {
    it("reads the status when it opens", async () => {
        expect.hasAssertions()
        const { wrapper, getStatus } = mountControls([{ appOpen: true }])
        await flushPromises()

        expect(getStatus).toHaveBeenCalledWith()
        expect(wrapper.text()).toContain("Student editing is")
        expect(wrapper.text()).toContain("open")
    })

    it("offers to enable editing while the app is closed", async () => {
        expect.hasAssertions()
        const { wrapper } = mountControls([{ appOpen: false }])
        await flushPromises()

        expect(wrapper.find(".app-access-toggle-btn").text()).toContain("Enable Editing")
    })

    it("shows the individual grant count when the app reports one", async () => {
        expect.hasAssertions()
        const { wrapper } = mountControls([{ appOpen: false, individualGrantCount: 2 }])
        await flushPromises()

        expect(wrapper.text()).toContain("2 students have individually granted access")
    })

    it("uses the singular for one grant", async () => {
        expect.hasAssertions()
        const { wrapper } = mountControls([{ appOpen: false, individualGrantCount: 1 }])
        await flushPromises()

        expect(wrapper.text()).toContain("1 student has individually granted access")
    })

    it("says nothing about grants for an app without them", async () => {
        expect.hasAssertions()
        const { wrapper } = mountControls([{ appOpen: false }])
        await flushPromises()

        expect(wrapper.text()).not.toContain("individually granted access")
    })
})

describe("toggling", () => {
    it("toggles and reports the new state", async () => {
        expect.hasAssertions()
        const { wrapper, toggle } = mountControls([{ appOpen: false }, { appOpen: false }], true)
        await flushPromises()

        await confirmToggle(wrapper)

        expect(toggle).toHaveBeenCalledWith()
        expect(mockNotify).toHaveBeenCalledWith({
            type: "positive",
            message: "Editing enabled for all DVM students.",
        })
        expect(wrapper.emitted("access-status-changed")).toHaveLength(1)
    })

    it("refuses when another admin has already made the change", async () => {
        expect.hasAssertions()
        // Opened against a closed app, but the re-read finds it already open.
        const { wrapper, toggle } = mountControls([{ appOpen: false }, { appOpen: true }])
        await flushPromises()

        await confirmToggle(wrapper)

        expect(toggle).not.toHaveBeenCalled()
        expect(mockNotify).toHaveBeenCalledWith({
            type: "warning",
            message: "Editing status was already changed. Please review the current state.",
        })
    })

    it("refuses when the status cannot be re-read", async () => {
        expect.hasAssertions()
        const { wrapper, toggle } = mountControls([{ appOpen: false }, null])
        await flushPromises()

        await confirmToggle(wrapper)

        expect(toggle).not.toHaveBeenCalled()
        expect(mockNotify).toHaveBeenCalledWith({
            type: "negative",
            message: "Unable to verify current access status. Please try again.",
        })
    })

    it("says nothing and emits nothing when the toggle fails", async () => {
        expect.hasAssertions()
        const { wrapper } = mountControls([{ appOpen: false }, { appOpen: false }], null)
        await flushPromises()

        await confirmToggle(wrapper)

        expect(mockNotify).not.toHaveBeenCalled()
        expect(wrapper.emitted("access-status-changed")).toBeUndefined()
    })

    it("admits it cannot tell, rather than claiming closed, when the first read fails", async () => {
        expect.hasAssertions()
        // Before anything is read appOpen is false, so a banner that speaks unconditionally
        // would report an authoritative "closed" for a status it has never actually seen.
        const { wrapper } = mountControls([null])
        await flushPromises()

        expect(wrapper.text()).toContain("Unable to check")
        expect(wrapper.text()).not.toContain("closed")
    })

    it("keeps the last known state when a later read fails", async () => {
        expect.hasAssertions()
        // Once a real answer has arrived it is the last thing known to be true, so a failed
        // refresh leaves it on screen rather than downgrading to "cannot tell".
        const { wrapper } = mountControls([{ appOpen: true }, null])
        await flushPromises()

        await confirmToggle(wrapper)

        expect(wrapper.text()).toContain("Student editing is")
        expect(wrapper.text()).not.toContain("Unable to check")
        expect(wrapper.text()).not.toContain("closed")
    })
})

describe("rejected callbacks", () => {
    // The callbacks are the host app's, so the component cannot rely on them resolving.
    afterEach(() => {
        vi.useRealTimers()
    })

    it("clears the loading overlay when the status read rejects", async () => {
        expect.hasAssertions()
        // Only the spinner's delay is faked; flushPromises needs the real setImmediate.
        vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] })
        const getStatus = vi.fn<() => Promise<Status>>().mockRejectedValue(new Error("network"))
        const wrapper = mountWith(getStatus, vi.fn<() => Promise<boolean | null>>())
        await flushPromises()

        // A load still in flight would have raised the delayed spinner by now.
        vi.advanceTimersByTime(200)
        await flushPromises()

        expect(wrapper.findComponent({ name: "QInnerLoading" }).props("showing")).toBeFalsy()
    })

    it("stops the toggle button spinning when the toggle rejects", async () => {
        expect.hasAssertions()
        const getStatus = vi.fn<() => Promise<Status>>().mockResolvedValue({ appOpen: false })
        const toggle = vi.fn<() => Promise<boolean | null>>().mockRejectedValue(new Error("network"))
        const wrapper = mountWith(getStatus, toggle)
        await flushPromises()

        await wrapper.find(".app-access-toggle-btn").trigger("click")
        await expect(dialogCallbacks.onOk?.()).rejects.toThrow("network")
        await flushPromises()

        expect(wrapper.findComponent({ name: "QBtn" }).props("loading")).toBeFalsy()
    })
})
