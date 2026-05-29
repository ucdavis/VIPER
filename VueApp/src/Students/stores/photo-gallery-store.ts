/**
 * Photo Gallery Store
 *
 * Manages state and actions for the student photo gallery feature including
 * fetching photos by class/group/course, handling Ross student inclusion,
 * and exporting galleries to Word/PDF formats.
 */
import { defineStore } from "pinia"
import { ref, computed } from "vue"
import type {
    StudentPhoto,
    GroupingInfo,
    PhotoExportRequest,
    GalleryMenu,
    CourseInfo,
} from "../services/photo-gallery-service"
import {
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
} from "./photo-gallery-actions"

export const usePhotoGalleryStore = defineStore("photoGallery", () => {
    // State
    const students = ref<StudentPhoto[]>([])
    const selectedClassLevel = ref<string | null>(null)
    const selectedGroup = ref<GroupingInfo | null>(null)
    const selectedCourse = ref<CourseInfo | null>(null)
    const availableCourses = ref<CourseInfo[]>([])
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

    // Computed
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

    // Create refs object for actions
    const refs = {
        students,
        selectedClassLevel,
        selectedGroup,
        selectedCourse,
        availableCourses,
        includeRossStudents,
        exportInProgress,
        galleryView,
        galleryMenu,
        loading,
        error,
        groupTypes,
        groupCounts,
        exportParams,
    }

    // Create actions
    const fetchClassPhotos = createFetchClassPhotos(refs)
    const fetchGroupPhotos = createFetchGroupPhotos(refs)
    const fetchCoursePhotos = createFetchCoursePhotos(refs)
    const fetchGalleryMenu = createFetchGalleryMenu(refs)
    const fetchAvailableCourses = createFetchAvailableCourses(refs)
    const exportToWord = createExportToWord(refs)
    const exportToPDF = createExportToPDF(refs)
    const toggleRossStudents = createToggleRossStudents(refs, fetchClassPhotos, fetchCoursePhotos)
    const clearSelection = createClearSelection(refs)
    const setGalleryView = createSetGalleryView(refs)

    return {
        // State
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
        // Computed
        hasStudents,
        studentCount,
        exportParams,
        // Actions
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
})
