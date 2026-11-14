import { shallowMount } from "@vue/test-utils"
import { Quasar } from "quasar"
import SessionTimeout from "@/components/SessionTimeout.vue"

/**
 * The dialog is driven entirely by its poll timers, so these tests run them on a fake clock and
 * pin the timing that decides whether a user gets warned before their session expires.
 */
vi.mock("@/store/UserStore", () => ({
    useUserStore: () => ({ userInfo: { loginId: "tester" } }),
}))

type DialogState = {
    showSessionTimeoutWarning: boolean
    sessionExtendFailed: boolean
    extendSession: () => Promise<void>
    hideSessionTimeoutWarning: () => void
}

const FIRST_POLL_MS = 60_000
const fetchMock = vi.fn<(url: string) => Promise<unknown>>()

function secondsLeft(secondsUntilTimeout: number) {
    return {
        ok: true,
        json: () => Promise.resolve({ secondsUntilTimeout, sessionTimeoutDateTime: "2026-01-01T12:00:00-08:00" }),
    }
}

function mountDialog() {
    return shallowMount(SessionTimeout, { global: { plugins: [[Quasar, {}]] } }).vm as unknown as DialogState
}

function deferred<T>() {
    let resolve!: (value: T) => void
    let reject!: (reason: Error) => void
    const promise = new Promise<T>((res, rej) => {
        resolve = res
        reject = rej
    })
    return { promise, resolve, reject }
}

describe("SessionTimeout", () => {
    beforeEach(() => {
        vi.useFakeTimers()
        fetchMock.mockReset()
        vi.stubGlobal("fetch", fetchMock)
    })

    afterEach(() => {
        vi.useRealTimers()
        vi.unstubAllGlobals()
    })

    it("polls again near the five-minute mark rather than sleeping past the warning", async () => {
        fetchMock.mockResolvedValueOnce(secondsLeft(301)).mockResolvedValue(secondsLeft(296))
        const vm = mountDialog()

        await vi.advanceTimersByTimeAsync(FIRST_POLL_MS)
        expect(vm.showSessionTimeoutWarning).toBe(false)

        await vi.advanceTimersByTimeAsync(5000)
        expect(fetchMock).toHaveBeenCalledTimes(2)
        expect(vm.showSessionTimeoutWarning).toBe(true)
    })

    it("caps the retry after a failed poll by the last known expiry", async () => {
        fetchMock
            .mockResolvedValueOnce(secondsLeft(400))
            .mockRejectedValueOnce(new Error("down"))
            .mockResolvedValue(secondsLeft(285))
        mountDialog()

        await vi.advanceTimersByTimeAsync(FIRST_POLL_MS + 100_000)
        expect(fetchMock).toHaveBeenCalledTimes(2)

        await vi.advanceTimersByTimeAsync(15_000)
        expect(fetchMock).toHaveBeenCalledTimes(3)
    })

    it("ignores a poll that started before a successful renewal", async () => {
        const stalePoll = deferred<unknown>()
        fetchMock
            .mockReturnValueOnce(stalePoll.promise)
            .mockResolvedValueOnce(secondsLeft(1770))
            .mockResolvedValue(secondsLeft(1765))
        const vm = mountDialog()

        await vi.advanceTimersByTimeAsync(FIRST_POLL_MS)
        await vm.extendSession()
        await vi.advanceTimersByTimeAsync(2000)

        stalePoll.resolve(secondsLeft(100))
        await vi.advanceTimersByTimeAsync(0)
        expect(vm.showSessionTimeoutWarning).toBe(false)
    })

    it("does not leave a failure banner for the next warning when the dialog already closed", async () => {
        const extend = deferred<unknown>()
        fetchMock.mockReturnValueOnce(extend.promise)
        const vm = mountDialog()

        await vm.extendSession()
        vm.hideSessionTimeoutWarning()
        extend.reject(new Error("down"))
        await vi.advanceTimersByTimeAsync(0)

        expect(vm.sessionExtendFailed).toBe(false)
    })
})
