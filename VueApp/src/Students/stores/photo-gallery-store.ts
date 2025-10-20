import { defineStore } from "pinia"
import { ref, computed } from "vue"
import type { Ref } from "vue"
import type { StudentPhoto, GroupingInfo, PhotoExportRequest, GalleryMenu } from "../services/photo-gallery-service"
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
    groupCounts: Ref<Record<string, Record<string, number>>>
    galleryMenu: Ref<GalleryMenu | null>
    groupTypes: Ref<{ eighths: string[]; twentieths: string[]; teams: string[]; v3specialty: string[] }>
}

async function fetchClassPhotosData(refs: StoreRefs, classLevel: string, includeRoss: boolean) {
    try {
        refs.loading.value = true
        refs.error.value = null
        refs.selectedClassLevel.value = classLevel
        refs.selectedGroup.value = null
        const response = await photoGalleryService.getClassGallery(classLevel, includeRoss)
        refs.students.value = response.students
        calculateGroupCounts(refs.students.value, refs.groupCounts)
    } catch (err: any) {
        refs.error.value = err.message || "Failed to fetch class photos"
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

export const usePhotoGalleryStore = defineStore("photoGallery", () => {
    const students = ref<StudentPhoto[]>([])
    const selectedClassLevel = ref<string | null>(null)
    const selectedGroup = ref<GroupingInfo | null>(null)
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
        includeRossStudents: includeRossStudents.value,
    }))

    const refs: StoreRefs = {
        students,
        loading,
        error,
        selectedClassLevel,
        selectedGroup,
        groupCounts,
        galleryMenu,
        groupTypes,
    }

    async function fetchClassPhotos(classLevel: string) {
        await fetchClassPhotosData(refs, classLevel, includeRossStudents.value)
    }

    async function fetchGroupPhotos(groupType: string, groupId: string, classLevel?: string | null) {
        await fetchGroupPhotosData(refs, { groupType, groupId, classLevel })
    }

    async function fetchGalleryMenu() {
        await fetchGalleryMenuData(refs)
    }

    async function exportToWord() {
        const params = { ...exportParams.value, exportFormat: "word" as const }
        await performExport(exportInProgress, params, "word")
    }

    async function exportToPDF() {
        const params = { ...exportParams.value, exportFormat: "pdf" as const }
        await performExport(exportInProgress, params, "pdf")
    }

    const setGalleryView = (view: "grid" | "sheet" | "list") => {
        galleryView.value = view
    }
    const toggleRossStudents = () => {
        if (selectedClassLevel.value) {
            fetchClassPhotos(selectedClassLevel.value)
        }
    }
    const clearSelection = () => {
        students.value = []
        selectedClassLevel.value = null
        selectedGroup.value = null
        error.value = null
    }

    return {
        students,
        selectedClassLevel,
        selectedGroup,
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
        fetchClassPhotos,
        fetchGroupPhotos,
        fetchGalleryMenu,
        exportToWord,
        exportToPDF,
        setGalleryView,
        toggleRossStudents,
        clearSelection,
    }
})
