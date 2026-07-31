import ImportFiles from "@/CMS/pages/ImportFiles.vue"
import { mountCms, flushPromises } from "./test-utils"

/**
 * ImportFiles preview: pasted lines with a leading "viper/" are rewritten in the textarea
 * (mirroring the server-side strip) and an inline banner reports how many lines changed.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
    }),
}))

const PREVIEW_ROW = {
    filePath: "/cats/docs/manual.pdf",
    canImport: true,
    message: null,
    fileName: "manual.pdf",
    renamedFrom: null,
    friendlyName: "cats-manual.pdf",
    oldUrl: "/cats/docs/manual.pdf",
}

async function mountPage() {
    mockGet.mockReset()
    mockPost.mockReset()
    mockGet.mockResolvedValue({ success: true, result: ["cats"] })
    mockPost.mockResolvedValue({ success: true, result: [PREVIEW_ROW] })
    const wrapper = mountCms(ImportFiles)
    await flushPromises()
    return wrapper
}

async function previewPaths(wrapper: Awaited<ReturnType<typeof mountPage>>, pathsText: string) {
    // First QInput is the paths textarea; first QSelect is the destination folder
    // (the PermissionSelector's QSelect renders after it).
    wrapper.findComponent({ name: "QInput" }).vm.$emit("update:modelValue", pathsText)
    wrapper.findComponent({ name: "QSelect" }).vm.$emit("update:modelValue", "cats")
    await flushPromises()
    await wrapper.find("form").trigger("submit")
    await flushPromises()
    await flushPromises()
}

function lastPreviewRequest(): { filePaths: string[] } {
    return mockPost.mock.calls.at(-1)?.[1] as { filePaths: string[] }
}

describe("importFiles.vue - leading viper/ tolerance", () => {
    it("rewrites viper-prefixed lines, sends the stripped paths, and reports the cleanup", async () => {
        const wrapper = await mountPage()

        const pasted = ["viper/cats/docs/manual.pdf", "/cats/other.pdf", String.raw`\VIPER\cats\more.pdf`]
        await previewPaths(wrapper, pasted.join("\n"))

        expect(lastPreviewRequest().filePaths).toStrictEqual([
            "/cats/docs/manual.pdf",
            "/cats/other.pdf",
            String.raw`/cats\more.pdf`,
        ])
        expect(wrapper.text()).toContain('Removed the leading "viper/" from 2 paths')

        // Back returns to the form; the textarea holds the rewritten lines.
        const back = wrapper.findAll("button").find((b) => b.text() === "Back")!
        await back.trigger("click")
        await flushPromises()
        expect(wrapper.findComponent({ name: "QInput" }).props("modelValue")).toBe(
            ["/cats/docs/manual.pdf", "/cats/other.pdf", String.raw`/cats\more.pdf`].join("\n"),
        )
    })

    it("leaves clean paths untouched and shows no banner", async () => {
        const wrapper = await mountPage()

        await previewPaths(wrapper, "/cats/docs/manual.pdf")

        expect(lastPreviewRequest().filePaths).toStrictEqual(["/cats/docs/manual.pdf"])
        expect(wrapper.text()).not.toContain("Removed the leading")
    })
})
