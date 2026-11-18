import { flushPromises } from "@vue/test-utils"
import { ref } from "vue"
import { useReportAdminEdit } from "../admin/use-report-admin-edit"
import { reportAdminService } from "../admin/report-admin-service"
import type { ReportAdminDefinition } from "../admin/report-admin-types"
import { rosterReport, rosterView, saved } from "./report-admin-fixtures"
import { deferred } from "./deferred"

const { mockReplace } = vi.hoisted(() => ({ mockReplace: vi.fn<(...args: unknown[]) => Promise<void>>() }))
vi.mock("vue-router", () => ({ useRouter: () => ({ replace: mockReplace }) }))
vi.mock("quasar", () => ({ useQuasar: () => ({ dialog: vi.fn<() => void>() }) }))
vi.mock("../admin/report-admin-service", () => ({
    reportAdminService: {
        getSources: vi.fn<(...args: unknown[]) => unknown>(),
        getSource: vi.fn<(...args: unknown[]) => unknown>(),
        get: vi.fn<(...args: unknown[]) => unknown>(),
        save: vi.fn<(...args: unknown[]) => unknown>(),
        remove: vi.fn<(...args: unknown[]) => unknown>(),
        restore: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const service = vi.mocked(reportAdminService)
const sources = [
    { name: "FacultyRoster", kind: "View" as const, description: null },
    { name: "usp_Leave", kind: "Procedure" as const, description: null },
]

function reset(): void {
    mockReplace.mockResolvedValue()
    service.getSources.mockResolvedValue(sources)
    service.getSource.mockResolvedValue(rosterView)
    service.get.mockResolvedValue(saved)
    service.save.mockResolvedValue({ definition: saved, errors: [], conflict: false })
}

async function edit(id: number | null = 4) {
    reset()
    const state = useReportAdminEdit(id)
    await flushPromises()
    return state
}

describe("report edit state - loading", () => {
    it("loads the report, the sources and its source's shape", async () => {
        expect.hasAssertions()

        const state = await edit()

        expect([state.loading.value, state.form.value.title, state.definition.value?.id]).toStrictEqual([
            false,
            "Faculty roster",
            4,
        ])
        expect([state.views.value, state.targets.value, state.sourceColumns.value]).toStrictEqual([
            ["FacultyRoster"],
            ["Name", "Hired"],
            ["Name", "Hired"],
        ])
        expect([state.isView.value, state.canAddMissing.value, state.isDirty.value]).toStrictEqual([true, true, false])
    })

    it("starts a new report empty", async () => {
        expect.hasAssertions()

        const state = await edit(null)

        expect([state.form.value.title, state.definition.value, state.source.value]).toStrictEqual(["", null, null])
        expect(service.get).not.toHaveBeenCalled()
    })

    it("marks a report that doesn't exist", async () => {
        expect.hasAssertions()
        reset()
        service.get.mockResolvedValue(null)

        const state = useReportAdminEdit(9)
        await flushPromises()

        expect(state.notFound.value).toBeTruthy()
    })

    it("says when the sources couldn't be loaded", async () => {
        expect.hasAssertions()
        reset()
        service.getSources.mockResolvedValue(null)

        const state = useReportAdminEdit(4)
        await flushPromises()

        expect([state.sourcesFailed.value, state.sources.value]).toStrictEqual([true, []])
    })

    it("ignores a load that finishes after a newer one started", async () => {
        expect.hasAssertions()
        reset()
        const slow = deferred<ReportAdminDefinition | null>()
        service.get.mockReturnValueOnce(slow.promise)
        const id = ref<number | null>(4)

        const state = useReportAdminEdit(id)
        id.value = 5
        await flushPromises()
        slow.resolve({ ...saved, report: { ...rosterReport, title: "Stale" } })
        await flushPromises()

        expect(state.form.value.title).toBe("Faculty roster")
    })

    it("ignores a source description that arrives after a newer load", async () => {
        expect.hasAssertions()
        reset()
        const slow = deferred<typeof rosterView | null>()
        service.getSource.mockReturnValueOnce(slow.promise)
        const id = ref<number | null>(4)

        const state = useReportAdminEdit(id)
        await flushPromises()
        id.value = null
        await flushPromises()
        slow.resolve(rosterView)
        await flushPromises()

        expect([state.form.value.title, state.source.value]).toStrictEqual(["", null])
    })
})

describe("report edit state - editing", () => {
    it("fills an empty report with the chosen source's columns", async () => {
        expect.hasAssertions()
        const state = await edit(null)

        await state.changeSource("FacultyRoster")

        expect(state.form.value.columns.map((column) => column.sourceColumn)).toStrictEqual(["Name", "Hired"])
        expect(state.isDirty.value).toBeTruthy()
    })

    it("keeps a report's columns when its source changes", async () => {
        expect.hasAssertions()
        const state = await edit()

        await state.changeSource("FacultyRoster")

        expect(state.form.value.columns).toHaveLength(1)
    })

    it("clears the source's shape when no source is picked", async () => {
        expect.hasAssertions()
        const state = await edit()

        await state.changeSource("")

        expect(state.source.value).toBeNull()
    })

    it("adds the source's other columns", async () => {
        expect.hasAssertions()
        const state = await edit()

        state.addMissingColumns()

        expect(state.form.value.columns.map((column) => column.sourceColumn)).toStrictEqual(["Name", "Hired"])
    })

    it("adds nothing without a source", async () => {
        expect.hasAssertions()
        const state = await edit(null)

        state.addMissingColumns()

        expect(state.form.value.columns).toStrictEqual([])
    })

    it("puts an earlier version in the form, keeping the version being edited", async () => {
        expect.hasAssertions()
        const state = await edit()

        await state.applyVersion({ ...rosterReport, title: "Old title", lastModifiedOn: "2026-01-01T00:00:00" })

        expect([state.form.value.title, state.form.value.lastModifiedOn]).toStrictEqual([
            "Old title",
            rosterReport.lastModifiedOn,
        ])
    })
})

describe("report edit state - saving", () => {
    it("saves over the report and marks the form clean", async () => {
        expect.hasAssertions()
        const state = await edit()
        state.form.value = { ...state.form.value, title: "Renamed" }

        await state.save()

        expect(service.save).toHaveBeenCalledWith(4, expect.objectContaining({ title: "Renamed" }))
        expect([state.isDirty.value, state.saving.value]).toStrictEqual([false, false])
        expect(mockReplace).not.toHaveBeenCalled()
    })

    it("moves a new report to its own address once saved", async () => {
        expect.hasAssertions()
        const state = await edit(null)

        await state.save()

        expect(service.save).toHaveBeenCalledWith(null, expect.anything())
        expect(mockReplace).toHaveBeenCalledWith({ name: "ReportAdminEdit", params: { id: "4" } })
    })

    it("shows the server's messages and whether someone else saved first", async () => {
        expect.hasAssertions()
        const state = await edit()
        service.save.mockResolvedValue({ definition: null, errors: ["Someone else saved it."], conflict: true })

        await state.save()

        expect([state.errors.value, state.conflict.value]).toStrictEqual([["Someone else saved it."], true])
    })

    it("reloads the saved report", async () => {
        expect.hasAssertions()
        const state = await edit()
        state.form.value = { ...state.form.value, title: "Mine" }

        await state.reload()

        expect([state.form.value.title, service.get.mock.calls.length]).toStrictEqual(["Faculty roster", 2])
    })
})

describe("report edit state - deleting", () => {
    const deleted = {
        ...saved,
        deletedOn: "2026-11-16T10:00:00",
        report: { ...rosterReport, lastModifiedOn: "2026-11-16T10:00:00" },
    }

    it("deletes and takes the new version's time, staying clean", async () => {
        expect.hasAssertions()
        const state = await edit()
        service.remove.mockResolvedValue(deleted)

        await state.setDeleted(true)

        expect([state.definition.value?.deletedOn, state.form.value.lastModifiedOn]).toStrictEqual([
            deleted.deletedOn,
            deleted.deletedOn,
        ])
        expect(state.isDirty.value).toBeFalsy()
    })

    it("keeps unsaved edits when restoring", async () => {
        expect.hasAssertions()
        const state = await edit()
        service.restore.mockResolvedValue(saved)
        state.form.value = { ...state.form.value, title: "Mine" }

        await state.setDeleted(false)

        expect([state.form.value.title, state.isDirty.value]).toStrictEqual(["Mine", true])
    })

    it("does nothing when the request fails or the report isn't saved yet", async () => {
        expect.hasAssertions()
        const state = await edit()
        service.remove.mockResolvedValue(null)
        await state.setDeleted(true)
        const fresh = await edit(null)
        await fresh.setDeleted(true)

        expect([state.definition.value?.deletedOn, service.remove.mock.calls.length]).toStrictEqual([null, 1])
    })
})
