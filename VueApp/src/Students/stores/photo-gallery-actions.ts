/**
 * Photo Gallery Store Actions
 *
 * Factory functions that create actions for managing photo gallery state,
 * including fetching photos, managing selections, and exporting galleries.
 */
import type { Ref } from "vue"
import type {
    StudentPhoto,
    GroupingInfo,
    PhotoExportRequest,
    GalleryMenu,
    CourseInfo,
} from "../services/photo-gallery-service"
import { photoGalleryService } from "../services/photo-gallery-service"
import { calculateGroupCounts } from "./photo-gallery-helpers"

const ISO_DATE_LENGTH = 10 // Length of YYYY-MM-DD in ISO string

interface StoreRefs {
    students: Ref<StudentPhoto[]>
    selectedClassLevel: Ref<string | null>
    selectedGroup: Ref<GroupingInfo | null>
    selectedCourse: Ref<CourseInfo | null>
    availableCourses: Ref<CourseInfo[]>
    includeRossStudents: Ref<boolean>
    exportInProgress: Ref<boolean>
    galleryView: Ref<"grid" | "sheet" | "list">
    galleryMenu: Ref<GalleryMenu | null>
    loading: Ref<boolean>
    error: Ref<string | null>
    groupTypes: Ref<{
        eighths: string[]
        twentieths: string[]
        teams: string[]
        v3specialty: string[]
    }>
    groupCounts: Ref<Record<string, Record<string, number>>>
    exportParams: Ref<PhotoExportRequest>
}

function createFetchClassPhotos(refs: StoreRefs) {
    return async function fetchClassPhotos(classLevel: string) {
        try {
            refs.loading.value = true
            refs.error.value = null
            refs.selectedClassLevel.value = classLevel
            refs.selectedGroup.value = null
            refs.selectedCourse.value = null
            const response = await photoGalleryService.getClassGallery(classLevel, refs.includeRossStudents.value)
            refs.students.value = response.students
            refs.groupCounts.value = calculateGroupCounts(refs.students.value)
        } catch (err: any) {
            refs.error.value = err.message || "Failed to fetch class photos"
            refs.students.value = []
            refs.groupCounts.value = calculateGroupCounts([])
        } finally {
            refs.loading.value = false
        }
    }
}

function createFetchGroupPhotos(refs: StoreRefs) {
    return async function fetchGroupPhotos(groupType: string, groupId: string, classLevel?: string | null) {
        try {
            refs.loading.value = true
            refs.error.value = null
            refs.selectedGroup.value = { groupType, groupId, availableGroups: [] }
            const response = await photoGalleryService.getGroupGallery(groupType, groupId, classLevel || undefined)
            refs.students.value = response.students
            if (response.groupInfo) {
                refs.selectedGroup.value = response.groupInfo
            }
        } catch (err: any) {
            refs.error.value = err.message || "Failed to fetch group photos"
            refs.students.value = []
            refs.selectedGroup.value = null
            refs.groupCounts.value = calculateGroupCounts([])
        } finally {
            refs.loading.value = false
        }
    }
}

function createFetchCoursePhotos(refs: StoreRefs) {
    return async function fetchCoursePhotos(
        termCode: string,
        crn: string,
        options: { groupType?: string | null; groupId?: string | null } = {},
    ) {
        try {
            refs.loading.value = true
            refs.error.value = null
            refs.selectedClassLevel.value = null
            refs.selectedGroup.value = null
            const response = await photoGalleryService.getCourseGallery(termCode, crn, {
                includeRossStudents: refs.includeRossStudents.value,
                ...options,
            })
            refs.students.value = response.students
            refs.selectedCourse.value = response.courseInfo || null
            if (!options.groupType && !options.groupId) {
                refs.groupCounts.value = calculateGroupCounts(refs.students.value)
            }
        } catch (err: any) {
            refs.error.value = err.message || "Failed to fetch course photos"
            refs.students.value = []
            refs.selectedCourse.value = null
            refs.groupCounts.value = calculateGroupCounts([])
        } finally {
            refs.loading.value = false
        }
    }
}

function createFetchGalleryMenu(refs: StoreRefs) {
    return async function fetchGalleryMenu() {
        try {
            refs.loading.value = true
            refs.error.value = null
            refs.galleryMenu.value = await photoGalleryService.getGalleryMenu()

            // Update group types from menu
            const eighthsGroup = refs.galleryMenu.value.groupTypes.find((g) => g.type === "eighths")
            const twentiethsGroup = refs.galleryMenu.value.groupTypes.find((g) => g.type === "twentieths")
            const teamsGroup = refs.galleryMenu.value.groupTypes.find((g) => g.type === "teams")
            const v3specialtyGroup = refs.galleryMenu.value.groupTypes.find((g) => g.type === "v3specialty")

            if (eighthsGroup) {
                refs.groupTypes.value.eighths = eighthsGroup.groups
            }
            if (twentiethsGroup) {
                refs.groupTypes.value.twentieths = twentiethsGroup.groups
            }
            if (teamsGroup) {
                refs.groupTypes.value.teams = teamsGroup.groups
            }
            if (v3specialtyGroup) {
                refs.groupTypes.value.v3specialty = v3specialtyGroup.groups
            }
        } catch (err: any) {
            refs.error.value = err.message || "Failed to fetch gallery menu"
        } finally {
            refs.loading.value = false
        }
    }
}

function createFetchAvailableCourses(refs: StoreRefs) {
    return async function fetchAvailableCourses() {
        try {
            refs.availableCourses.value = await photoGalleryService.getAvailableCourses()
        } catch (err: any) {
            refs.error.value = err.message || "Failed to fetch available courses"
        }
    }
}

function createExportToWord(refs: StoreRefs) {
    return async function exportToWord(overrides?: Partial<PhotoExportRequest>) {
        try {
            refs.exportInProgress.value = true
            const blob = await photoGalleryService.exportToWord({
                ...refs.exportParams.value,
                ...overrides,
                exportFormat: "word" as const,
            })
            const filename = `StudentPhotos_${new Date().toISOString().slice(0, ISO_DATE_LENGTH)}.docx`
            photoGalleryService.downloadFile(blob, filename)
        } catch (err: any) {
            throw new Error(err.message || "Failed to export to Word")
        } finally {
            refs.exportInProgress.value = false
        }
    }
}

function createExportToPDF(refs: StoreRefs) {
    return async function exportToPDF(overrides?: Partial<PhotoExportRequest>) {
        try {
            refs.exportInProgress.value = true
            const blob = await photoGalleryService.exportToPDF({
                ...refs.exportParams.value,
                ...overrides,
                exportFormat: "pdf" as const,
            })
            const filename = `StudentPhotos_${new Date().toISOString().slice(0, ISO_DATE_LENGTH)}.pdf`
            photoGalleryService.downloadFile(blob, filename)
        } catch (err: any) {
            throw new Error(err.message || "Failed to export to PDF")
        } finally {
            refs.exportInProgress.value = false
        }
    }
}

function createToggleRossStudents(refs: StoreRefs, fetchClassPhotos: any, fetchCoursePhotos: any) {
    return function toggleRossStudents() {
        if (refs.selectedGroup.value) {
            return
        }
        if (refs.selectedCourse.value) {
            fetchCoursePhotos(refs.selectedCourse.value.termCode, refs.selectedCourse.value.crn, {})
            return
        }
        if (refs.selectedClassLevel.value) {
            fetchClassPhotos(refs.selectedClassLevel.value)
        }
    }
}

function createClearSelection(refs: StoreRefs) {
    return function clearSelection() {
        refs.students.value = []
        refs.selectedClassLevel.value = null
        refs.selectedGroup.value = null
        refs.selectedCourse.value = null
        refs.error.value = null
    }
}

function createSetGalleryView(refs: StoreRefs) {
    return function setGalleryView(view: "grid" | "sheet" | "list") {
        refs.galleryView.value = view
    }
}

export {
    createFetchClassPhotos,
    createFetchGroupPhotos,
    createFetchCoursePhotos,
    createFetchGalleryMenu,
    createFetchAvailableCourses,
    createExportToWord,
    createExportToPDF,
    createToggleRossStudents,
    createClearSelection,
    createSetGalleryView,
}
