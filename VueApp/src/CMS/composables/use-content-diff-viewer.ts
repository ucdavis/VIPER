import { ref } from "vue"
import { useQuasar } from "quasar"
import { useDateFunctions } from "@/composables/DateFunctions"
import type { CmsContentHistoryDiff } from "@/CMS/types/"

// Shared state and helpers for every ContentDiffDialog consumer (history page, block editor,
// recent-activity rail): one open/reset shape, one way to apply a diff response, one error
// path, and the shared "date by author" stamp wording for subtitles.
export function useContentDiffViewer() {
    const $q = useQuasar()
    const { formatDateTime } = useDateFunctions()

    const viewer = ref({
        open: false,
        loading: false,
        title: "",
        subtitle: "",
        content: "",
        hasComparison: true,
        hasChanges: true,
    })

    function openViewer(title: string) {
        viewer.value = {
            open: true,
            loading: true,
            title,
            subtitle: "",
            content: "",
            hasComparison: true,
            hasChanges: true,
        }
    }

    function applyDiff(diff: CmsContentHistoryDiff, subtitle: string) {
        viewer.value.content = diff.content
        viewer.value.hasComparison = diff.hasComparison
        viewer.value.hasChanges = diff.hasChanges
        viewer.value.subtitle = subtitle
        viewer.value.loading = false
    }

    function closeViewer() {
        viewer.value.open = false
        viewer.value.loading = false
    }

    function failViewer(message: string) {
        $q.notify({ type: "negative", message })
        closeViewer()
    }

    function diffStamp(on: string | null, by: string | null): string {
        return (
            (on ? formatDateTime(on, { dateStyle: "short", timeStyle: "short" }) : "unknown date") +
            (by ? ` by ${by}` : "")
        )
    }

    // Subtitle for a saved-version diff (GET .../diff): direction reads old version -> new version.
    function savedDiffSubtitle(diff: CmsContentHistoryDiff): string {
        return diff.hasComparison
            ? `Changes from ${diffStamp(diff.oldModifiedOn, diff.oldModifiedBy)} to ${diffStamp(diff.newModifiedOn, diff.newModifiedBy)}`
            : `Original version, ${diffStamp(diff.newModifiedOn, diff.newModifiedBy)}`
    }

    return { viewer, openViewer, applyDiff, closeViewer, failViewer, diffStamp, savedDiffSubtitle }
}
