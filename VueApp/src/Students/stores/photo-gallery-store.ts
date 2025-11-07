import { defineStore } from "pinia"
import { ref, computed } from "vue"
import type { Ref } from "vue"
import type {
    StudentPhoto,
    GroupingInfo,
    PhotoExportRequest,
    GalleryMenu,
    CourseInfo,
    CoursesByTerm,
} from "../services/photo-gallery-service"
import { photoGalleryService } from "../services/photo-gallery-service"

const DATE_FORMAT_LENGTH = 10

function updateGroupTypes(
    menu: GalleryMenu,
    groupTypes: Ref<{ eighths: string[]; twentieths: string[]; teams: string[]; v3specialty: string[] }>,
) {
    const eighthsGroup = menu.groupTypes.find((g) => g.type === "eighths")
    const twentiethsGroup = menu.groupTypes.find((g) => g.type === "twentieths")
    const teamsGroup = menu.groupTypes.find((g) => g.type === "teams")
    const v3specialtyGroup = menu.groupTypes.find((g) => g.type === "v3specialty")

    if (eighthsGroup) {
        groupTypes.value.eighths = eighthsGroup.groups
    }
    if (twentiethsGroup) {
        groupTypes.value.twentieths = twentiethsGroup.groups
    }
    if (teamsGroup) {
        groupTypes.value.teams = teamsGroup.groups
    }
    if (v3specialtyGroup) {
        groupTypes.value.v3specialty = v3specialtyGroup.groups
    }
}

function generateFilename(format: "word" | "pdf"): string {
    const extension = format === "word" ? "docx" : "pdf"
    const date = new Date().toISOString().slice(0, DATE_FORMAT_LENGTH)
    return `StudentPhotos_${date}.${extension}`
}

function calculateGroupCounts(students: StudentPhoto[], groupCounts: Ref<Record<string, Record<string, number>>>) {
    groupCounts.value.eighths = {}
    groupCounts.value.twentieths = {}
    groupCounts.value.teams = {}
    groupCounts.value.v3specialty = {}

    for (const student of students) {
        if (student.eighthsGroup) {
            groupCounts.value.eighths[student.eighthsGroup] = (groupCounts.value.eighths[student.eighthsGroup] || 0) + 1
        }
        if (student.twentiethsGroup) {
            groupCounts.value.twentieths[student.twentiethsGroup] =
                (groupCounts.value.twentieths[student.twentiethsGroup] || 0) + 1
        }
        if (student.teamNumber) {
            groupCounts.value.teams[student.teamNumber] = (groupCounts.value.teams[student.teamNumber] || 0) + 1
        }
        if (student.v3SpecialtyGroup) {
            groupCounts.value.v3specialty[student.v3SpecialtyGroup] =
                (groupCounts.value.v3specialty[student.v3SpecialtyGroup] || 0) + 1
        }
    }
}

interface StoreRefs {
    students: Ref<StudentPhoto[]>
    loading: Ref<boolean>
    error: Ref<string | null>
    selectedClassLevel: Ref<string | null>
    selectedGroup: Ref<GroupingInfo | null>
    selectedCourse: Ref<CourseInfo | null>
    groupCounts: Ref<Record<string, Record<string, number>>>
    galleryMenu: Ref<GalleryMenu | null>
    groupTypes: Ref<{ eighths: string[]; twentieths: string[]; teams: string[]; v3specialty: string[] }>
    availableCourses: Ref<CoursesByTerm[]>
}

async function fetchClassPhotosData(refs: StoreRefs, classLevel: string, includeRoss: boolean) {
    try {
        refs.loading.value = true
        refs.error.value = null
        refs.selectedClassLevel.value = classLevel
        refs.selectedGroup.value = null
        refs.selectedCourse.value = null
        const response = await photoGalleryService.getClassGallery(classLevel, includeRoss)
        refs.students.value = response.students
        calculateGroupCounts(refs.students.value, refs.groupCounts)
    } catch (err: any) {
        refs.error.value = err.message || "Failed to fetch class photos"
        refs.students.value = []
        calculateGroupCounts([], refs.groupCounts)
    } finally {
        refs.loading.value = false
    }
}

interface GroupParams {
    groupType: string
    groupId: string
    classLevel?: string | null
}

async function fetchGroupPhotosData(refs: StoreRefs, params: GroupParams) {
    try {
        refs.loading.value = true
        refs.error.value = null
        refs.selectedGroup.value = { groupType: params.groupType, groupId: params.groupId, availableGroups: [] }
        const response = await photoGalleryService.getGroupGallery(
            params.groupType,
            params.groupId,
            params.classLevel || undefined,
        )
        refs.students.value = response.students
        if (response.groupInfo) {
            refs.selectedGroup.value = response.groupInfo
        }
    } catch (err: any) {
        refs.error.value = err.message || "Failed to fetch group photos"
        refs.students.value = []
        refs.selectedGroup.value = null
        calculateGroupCounts([], refs.groupCounts)
    } finally {
        refs.loading.value = false
    }
}

async function fetchGalleryMenuData(refs: StoreRefs) {
    try {
        refs.loading.value = true
        refs.error.value = null
        refs.galleryMenu.value = await photoGalleryService.getGalleryMenu()
        updateGroupTypes(refs.galleryMenu.value, refs.groupTypes)
    } catch (err: any) {
        refs.error.value = err.message || "Failed to fetch gallery menu"
    } finally {
        refs.loading.value = false
    }
}

async function fetchAvailableCoursesData(refs: StoreRefs) {
    try {
        refs.availableCourses.value = await photoGalleryService.getAvailableCourses()
    } catch (err: any) {
        refs.error.value = err.message || "Failed to fetch available courses"
    }
}

interface CourseParams {
    termCode: string
    crn: string
    includeRoss: boolean
}

async function fetchCoursePhotosData(refs: StoreRefs, params: CourseParams) {
    try {
        refs.loading.value = true
        refs.error.value = null
        refs.selectedClassLevel.value = null
        refs.selectedGroup.value = null
        const response = await photoGalleryService.getCourseGallery(params.termCode, params.crn, params.includeRoss)
        refs.students.value = response.students
        refs.selectedCourse.value = response.courseInfo || null
        calculateGroupCounts(refs.students.value, refs.groupCounts)
    } catch (err: any) {
        refs.error.value = err.message || "Failed to fetch course photos"
        refs.students.value = []
        refs.selectedCourse.value = null
        calculateGroupCounts([], refs.groupCounts)
    } finally {
        refs.loading.value = false
    }
}

async function performExport(exportInProgress: Ref<boolean>, params: PhotoExportRequest, format: "word" | "pdf") {
    try {
        exportInProgress.value = true
        const blob =
            format === "word"
                ? await photoGalleryService.exportToWord(params)
                : await photoGalleryService.exportToPDF(params)
        photoGalleryService.downloadFile(blob, generateFilename(format))
    } catch (err: any) {
        throw new Error(err.message || `Failed to export to ${format === "word" ? "Word" : "PDF"}`)
    } finally {
        exportInProgress.value = false
    }
}

interface StoreState {
    students: Ref<StudentPhoto[]>
    loading: Ref<boolean>
    error: Ref<string | null>
    selectedClassLevel: Ref<string | null>
    selectedGroup: Ref<GroupingInfo | null>
    selectedCourse: Ref<CourseInfo | null>
    groupCounts: Ref<Record<string, Record<string, number>>>
    galleryMenu: Ref<GalleryMenu | null>
    groupTypes: Ref<{ eighths: string[]; twentieths: string[]; teams: string[]; v3specialty: string[] }>
    availableCourses: Ref<CoursesByTerm[]>
    includeRossStudents: Ref<boolean>
    exportInProgress: Ref<boolean>
    exportParams: Ref<PhotoExportRequest>
    galleryView: Ref<"grid" | "sheet" | "list">
}

function createStoreRefs(state: StoreState): StoreRefs {
    return {
        students: state.students,
        loading: state.loading,
        error: state.error,
        selectedClassLevel: state.selectedClassLevel,
        selectedGroup: state.selectedGroup,
        selectedCourse: state.selectedCourse,
        groupCounts: state.groupCounts,
        galleryMenu: state.galleryMenu,
        groupTypes: state.groupTypes,
        availableCourses: state.availableCourses,
    }
}

function createStoreActions(refs: StoreRefs, state: StoreState) {
    async function fetchClassPhotos(classLevel: string) {
        await fetchClassPhotosData(refs, classLevel, state.includeRossStudents.value)
    }

    async function fetchGroupPhotos(groupType: string, groupId: string, classLevel?: string | null) {
        await fetchGroupPhotosData(refs, { groupType, groupId, classLevel })
    }

    async function fetchGalleryMenu() {
        await fetchGalleryMenuData(refs)
    }

    async function fetchAvailableCourses() {
        await fetchAvailableCoursesData(refs)
    }

    async function fetchCoursePhotos(termCode: string, crn: string) {
        await fetchCoursePhotosData(refs, { termCode, crn, includeRoss: state.includeRossStudents.value })
    }

    async function exportToWord() {
        const params = { ...state.exportParams.value, exportFormat: "word" as const }
        await performExport(state.exportInProgress, params, "word")
    }

    async function exportToPDF() {
        const params = { ...state.exportParams.value, exportFormat: "pdf" as const }
        await performExport(state.exportInProgress, params, "pdf")
    }

    const setGalleryView = (view: "grid" | "sheet" | "list") => {
        state.galleryView.value = view
    }

    const toggleRossStudents = () => {
        // If viewing a group, reload group data (groups don't support Ross students, so this just updates the URL)
        if (state.selectedGroup.value) {
            // Groups don't support includeRoss parameter, so no need to reload
            return
        }
        // If viewing a course, reload course data
        if (state.selectedCourse.value) {
            fetchCoursePhotos(state.selectedCourse.value.termCode, state.selectedCourse.value.crn)
            return
        }
        // If viewing a class, reload class data
        if (state.selectedClassLevel.value) {
            fetchClassPhotos(state.selectedClassLevel.value)
        }
    }

    const clearSelection = () => {
        state.students.value = []
        state.selectedClassLevel.value = null
        state.selectedGroup.value = null
        state.selectedCourse.value = null
        state.error.value = null
    }

    return {
        fetchClassPhotos,
        fetchGroupPhotos,
        fetchGalleryMenu,
        fetchAvailableCourses,
        fetchCoursePhotos,
        exportToWord,
        exportToPDF,
        setGalleryView,
        toggleRossStudents,
        clearSelection,
    }
}

export const usePhotoGalleryStore = defineStore("photoGallery", () => {
    const students = ref<StudentPhoto[]>([])
    const selectedClassLevel = ref<string | null>(null)
    const selectedGroup = ref<GroupingInfo | null>(null)
    const selectedCourse = ref<CourseInfo | null>(null)
    const availableCourses = ref<CoursesByTerm[]>([])
    const includeRossStudents = ref(false)
    const exportInProgress = ref(false)
    const galleryView = ref<"grid" | "sheet" | "list">("grid")
    const galleryMenu = ref<GalleryMenu | null>(null)
    const loading = ref(false)
    const error = ref<string | null>(null)
    const groupTypes = ref({
        eighths: ["1A1", "1A2", "1B1", "1B2", "2A1", "2A2", "2B1", "2B2"],
        twentieths: [] as string[],
        teams: [] as string[],
        v3specialty: [] as string[],
    })
    const groupCounts = ref<Record<string, Record<string, number>>>({
        eighths: {},
        twentieths: {},
        teams: {},
        v3specialty: {},
    })
    const hasStudents = computed(() => students.value.length > 0)
    const studentCount = computed(() => students.value.length)
    const exportParams = computed<PhotoExportRequest>(() => ({
        classLevel: selectedClassLevel.value || undefined,
        groupType: selectedGroup.value?.groupType,
        groupId: selectedGroup.value?.groupId,
        termCode: selectedCourse.value?.termCode,
        crn: selectedCourse.value?.crn,
        includeRossStudents: includeRossStudents.value,
    }))

    const state: StoreState = {
        students,
        loading,
        error,
        selectedClassLevel,
        selectedGroup,
        selectedCourse,
        groupCounts,
        galleryMenu,
        groupTypes,
        availableCourses,
        includeRossStudents,
        exportInProgress,
        exportParams,
        galleryView,
    }

    const refs = createStoreRefs(state)
    const actions = createStoreActions(refs, state)

    return {
        students,
        selectedClassLevel,
        selectedGroup,
        selectedCourse,
        availableCourses,
        includeRossStudents,
        exportInProgress,
        galleryView,
        galleryMenu,
        groupTypes,
        groupCounts,
        loading,
        error,
        hasStudents,
        studentCount,
        exportParams,
        ...actions,
    }
})
