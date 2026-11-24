import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Notify } from "quasar"
import ClinicalImportDialog from "../components/ClinicalImportDialog.vue"
import { clinicalService } from "../services/clinical-service"
import type { ClinicalImportPreviewDto } from "../types"

vi.mock("../services/clinical-service", () => ({
    clinicalService: {
        getPreview: vi.fn<(...args: unknown[]) => unknown>(),
        getStreamUrl: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

function previewWithWarning(warning: string): ClinicalImportPreviewDto {
    return {
        mode: "AddNewOnly",
        addCount: 0,
        updateCount: 0,
        deleteCount: 0,
        skipCount: 0,
        previewGeneratedAt: "2024-09-01T00:00:00",
        assignments: [],
        warnings: [warning],
    }
}

describe("clinicalImportDialog preview loading", () => {
    it("ignores a preview response from before the dialog was closed and reopened", async () => {
        expect.assertions(2)

        // Not Promise.withResolvers: the project's TS lib target predates it
        let resolveStale!: (value: ClinicalImportPreviewDto) => void
        vi.mocked(clinicalService.getPreview)
            .mockReturnValueOnce(
                new Promise((resolve) => {
                    resolveStale = resolve
                }),
            )
            .mockResolvedValueOnce(previewWithWarning("fresh preview"))

        const wrapper = mount(ClinicalImportDialog, {
            props: { modelValue: false, termCode: 202409, termName: "Fall 2024" },
            global: { plugins: [[Quasar, { plugins: { Notify } }]] },
            attachTo: document.body,
        })
        await wrapper.setProps({ modelValue: true })
        await wrapper.setProps({ modelValue: false })
        await wrapper.setProps({ modelValue: true })
        await flushPromises()
        resolveStale(previewWithWarning("stale preview"))
        await flushPromises()

        expect(document.body.innerHTML).toContain("fresh preview")
        expect(document.body.innerHTML).not.toContain("stale preview")

        wrapper.unmount()
    })
})
