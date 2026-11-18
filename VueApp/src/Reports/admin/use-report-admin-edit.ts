import { computed, ref, toValue, watch } from "vue"
import type { MaybeRefOrGetter } from "vue"
import { useRouter } from "vue-router"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { emptyForm, missingColumns, parameterTargets, toForm, toRequest } from "./report-admin-form"
import type { ReportAdminForm } from "./report-admin-form"
import { reportAdminService } from "./report-admin-service"
import type {
    ReportAdminDefinition,
    ReportEditRequest,
    ReportSourceDetail,
    ReportSourceSummary,
} from "./report-admin-types"

/**
 * State for the report edit page: loads the report (or starts a new one) with the list of
 * [report] sources, describes the chosen source, and saves, deletes and restores. A new report
 * moves to its own edit address once saved. Only the latest load may update the page.
 */
// oxlint-disable-next-line max-lines-per-function -- one report's edit state and the actions on it belong together
export function useReportAdminEdit(reportId: MaybeRefOrGetter<number | null>) {
    const router = useRouter()

    const form = ref<ReportAdminForm>(emptyForm())
    const definition = ref<ReportAdminDefinition | null>(null)
    const sources = ref<ReportSourceSummary[]>([])
    const source = ref<ReportSourceDetail | null>(null)
    const loading = ref(true)
    const notFound = ref(false)
    const sourcesFailed = ref(false)
    const saving = ref(false)
    const errors = ref<string[]>([])
    const conflict = ref(false)
    const { isDirty, setInitialState } = useUnsavedChanges(form)

    let loadId = 0
    let sourceId = 0

    const isView = computed(() => source.value?.kind !== "Procedure")
    const views = computed(() => sources.value.filter((item) => item.kind === "View").map((item) => item.name))
    const targets = computed(() => parameterTargets(source.value))
    const sourceColumns = computed(
        () => source.value?.columns.filter((column) => column.isSupported).map((column) => column.name) ?? [],
    )
    const canAddMissing = computed(
        () => source.value !== null && missingColumns(source.value, form.value.columns).length > 0,
    )

    /** Describes the source; false when a newer request has taken over and nothing was changed. */
    async function describeSource(name: string): Promise<boolean> {
        sourceId += 1
        const current = sourceId
        const detail = name === "" ? null : await reportAdminService.getSource(name)
        if (current !== sourceId) {
            return false
        }
        source.value = detail
        return true
    }

    async function load(id: number | null): Promise<void> {
        loadId += 1
        const current = loadId
        loading.value = true
        errors.value = []
        conflict.value = false
        const [list, report] = await Promise.all([
            reportAdminService.getSources(),
            id === null ? Promise.resolve(null) : reportAdminService.get(id),
        ])
        if (current !== loadId) {
            return
        }
        sources.value = list ?? []
        sourcesFailed.value = list === null
        notFound.value = id !== null && report === null
        definition.value = report
        form.value = report ? toForm(report.report) : emptyForm()
        await describeSource(form.value.source)
        if (current !== loadId) {
            return
        }
        setInitialState()
        loading.value = false
    }

    /** Picks a source; a report without columns yet gets all the source's columns. */
    async function changeSource(name: string): Promise<void> {
        form.value = { ...form.value, source: name }
        if ((await describeSource(name)) && source.value !== null && form.value.columns.length === 0) {
            form.value = { ...form.value, columns: missingColumns(source.value, []) }
        }
    }

    function addMissingColumns(): void {
        if (source.value !== null) {
            form.value = {
                ...form.value,
                columns: [...form.value.columns, ...missingColumns(source.value, form.value.columns)],
            }
        }
    }

    async function save(): Promise<void> {
        saving.value = true
        errors.value = []
        conflict.value = false
        const wasNew = definition.value === null
        const outcome = await reportAdminService.save(definition.value?.id ?? null, toRequest(form.value))
        saving.value = false
        if (outcome.definition === null) {
            errors.value = outcome.errors
            conflict.value = outcome.conflict
            return
        }
        definition.value = outcome.definition
        form.value = toForm(outcome.definition.report)
        setInitialState()
        if (wasNew) {
            await router.replace({ name: "ReportAdminEdit", params: { id: String(outcome.definition.id) } })
        }
    }

    /**
     * Deletes or restores the report. Either one is a save on the server, so the form takes the
     * new version's time; unsaved edits stay as they are.
     */
    async function setDeleted(deleted: boolean): Promise<void> {
        const current = definition.value
        if (current === null) {
            return
        }
        const updated = deleted
            ? await reportAdminService.remove(current.id)
            : await reportAdminService.restore(current.id)
        if (updated === null) {
            return
        }
        const clean = !isDirty.value
        definition.value = updated
        form.value = { ...form.value, lastModifiedOn: updated.report.lastModifiedOn }
        if (clean) {
            setInitialState()
        }
    }

    /** Puts an earlier version's fields in the form, to save over the current one. */
    async function applyVersion(report: ReportEditRequest): Promise<void> {
        form.value = toForm({ ...report, lastModifiedOn: form.value.lastModifiedOn })
        await describeSource(form.value.source)
    }

    /** Starts again from the saved report, dropping the edits; used after a conflicting save. */
    async function reload(): Promise<void> {
        await load(definition.value?.id ?? null)
    }

    watch(() => toValue(reportId), load, { immediate: true })

    return {
        form,
        definition,
        sources,
        source,
        loading,
        notFound,
        sourcesFailed,
        saving,
        errors,
        conflict,
        isDirty,
        isView,
        views,
        targets,
        sourceColumns,
        canAddMissing,
        changeSource,
        addMissingColumns,
        save,
        setDeleted,
        applyVersion,
        reload,
    }
}
