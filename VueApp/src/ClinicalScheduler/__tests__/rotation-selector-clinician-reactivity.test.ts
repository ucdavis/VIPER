import { mount, flushPromises } from "@vue/test-utils"
import { setActivePinia, createPinia } from "pinia"
import { Quasar } from "quasar"

import RotationSelector from "../components/RotationSelector.vue"
import { RotationService } from "../services/rotation-service"
import { usePermissionsStore } from "../stores/permissions"

vi.mock("../services/rotation-service", () => ({
    RotationService: {
        getRotations: vi.fn<(...args: unknown[]) => unknown>(),
        getRotationsWithScheduledWeeks: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const emptyOk = { success: true, result: [], errors: [] }

function mountSelector(props: Record<string, unknown> = {}) {
    return mount(RotationSelector, {
        props,
        global: { plugins: [[Quasar, {}]] },
    })
}

describe("RotationSelector - clinician self-scheduling reactivity", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
        // Skip the store's network init; the plain rotations path does not read permissions client-side.
        const store = usePermissionsStore()
        vi.spyOn(store, "initialize").mockResolvedValue()
        vi.mocked(RotationService.getRotations).mockResolvedValue(emptyOk)
        vi.mocked(RotationService.getRotationsWithScheduledWeeks).mockResolvedValue(emptyOk)
    })

    it("forwards clinicianMothraId to the all-rotations endpoint (legacy: every active rotation)", async () => {
        mountSelector({ onlyWithScheduledWeeks: false, clinicianMothraId: "111" })
        await flushPromises()

        expect(RotationService.getRotations).toHaveBeenCalledWith(expect.objectContaining({ clinicianMothraId: "111" }))
        expect(RotationService.getRotationsWithScheduledWeeks).not.toHaveBeenCalled()
    })

    it("forwards clinicianMothraId to the scheduled-weeks endpoint when that mode is on", async () => {
        mountSelector({ onlyWithScheduledWeeks: true, clinicianMothraId: "111", year: 2026 })
        await flushPromises()

        expect(RotationService.getRotationsWithScheduledWeeks).toHaveBeenCalledWith(
            expect.objectContaining({ clinicianMothraId: "111" }),
        )
    })

    it("refetches when clinicianMothraId changes", async () => {
        const wrapper = mountSelector({ onlyWithScheduledWeeks: false, clinicianMothraId: "111" })
        await flushPromises()
        expect(RotationService.getRotations).toHaveBeenCalledOnce()

        await wrapper.setProps({ clinicianMothraId: "222" })
        await flushPromises()

        expect(RotationService.getRotations).toHaveBeenCalledTimes(2)
        expect(RotationService.getRotations).toHaveBeenLastCalledWith(
            expect.objectContaining({ clinicianMothraId: "222" }),
        )
    })

    it("ignores a stale response that resolves after a newer request's response", async () => {
        let resolveFirst!: (value: any) => void
        let resolveSecond!: (value: any) => void

        const firstCall = new Promise<typeof emptyOk>((resolve) => {
            resolveFirst = resolve
        })
        const secondCall = new Promise<typeof emptyOk>((resolve) => {
            resolveSecond = resolve
        })

        vi.mocked(RotationService.getRotations).mockReturnValueOnce(firstCall).mockReturnValueOnce(secondCall)

        const wrapper = mountSelector({ onlyWithScheduledWeeks: false, clinicianMothraId: "111" })
        await flushPromises()

        // Trigger the second (newer) request before the first has resolved.
        await wrapper.setProps({ clinicianMothraId: "222" })
        await flushPromises()

        expect(RotationService.getRotations).toHaveBeenCalledTimes(2)

        const rotationA = {
            rotId: 1,
            name: "Rotation A (stale)",
            abbreviation: "A",
        }
        const rotationB = {
            rotId: 2,
            name: "Rotation B (fresh)",
            abbreviation: "B",
        }

        // Resolve the newer request first, then the stale one after.
        resolveSecond({ success: true, result: [rotationB], errors: [] })
        await flushPromises()

        resolveFirst({ success: true, result: [rotationA], errors: [] })
        await flushPromises()

        const vm = wrapper.vm as unknown as {
            filteredRotations: { rotId: number; name?: string }[]
        }

        expect(vm.filteredRotations).toHaveLength(1)
        expect(vm.filteredRotations[0]?.rotId).toBe(rotationB.rotId)
        expect(vm.filteredRotations.some((r) => r.rotId === rotationA.rotId)).toBeFalsy()
    })
})
