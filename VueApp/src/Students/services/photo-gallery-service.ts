import { useFetch, postForBlob } from "@/composables/ViperFetch"

interface StudentPhoto {
    mailId: string
    firstName: string
    lastName: string
    displayName: string
    photoUrl: string
    groupAssignment: string | null
    eighthsGroup: string | null
    twentiethsGroup: string | null
    teamNumber: string | null
    v3SpecialtyGroup: string | null
    hasPhoto: boolean
    isRossStudent: boolean
    classLevel: string | null
    // Computed properties from backend for consistent display
    fullName: string
    secondaryTextLines: string[]
}

interface CourseInfo {
    termCode: string
    crn: string
    subjectCode: string
    courseNumber: string
    title: string
    termDescription: string
}

interface PhotoGalleryViewModel {
    classLevel?: string
    students: StudentPhoto[]
    groupInfo?: GroupingInfo
    courseInfo?: CourseInfo
    exportOptions?: ExportOptions
}

interface GroupingInfo {
    groupType: string
    groupId: string
    availableGroups: string[]
}

interface ExportOptions {
    format?: string
    includeGroups?: boolean
    includeRossStudents?: boolean
    title?: string
    subtitle?: string
}

interface PhotoExportRequest {
    classLevel?: string
    groupType?: string
    groupId?: string
    termCode?: string
    crn?: string
    includeRossStudents?: boolean
    exportFormat?: string
}

interface GalleryMenu {
    classLevels: string[]
    groupTypes: {
        type: string
        label: string
        groups: string[]
    }[]
}

interface ClassYear {
    year: number
    classLevel: string
}

/**
 * Service for interacting with the Photo Gallery API
 * Provides methods for fetching student photos, exporting galleries, and managing photo data
 */
class PhotoGalleryService {
    private baseUrl = `${import.meta.env.VITE_API_URL}students/photos`

    /**
     * Fetches photo gallery for a specific class level (V1-V4)
     * @param classLevel - The class level to fetch (V1, V2, V3, or V4)
     * @param includeRossStudents - Whether to include Ross transfer students
     * @returns Photo gallery view model with students and metadata
     */
    getClassGallery = async (classLevel: string, includeRossStudents = false): Promise<PhotoGalleryViewModel> => {
        const { get } = useFetch()
        const response = await get(
            `${this.baseUrl}/gallery/class/${classLevel}?includeRossStudents=${includeRossStudents}`,
        )
        return response.result as PhotoGalleryViewModel
    }

    getGroupGallery = async (
        groupType: string,
        groupId: string,
        classLevel?: string,
    ): Promise<PhotoGalleryViewModel> => {
        const { get } = useFetch()
        const url = classLevel
            ? `${this.baseUrl}/gallery/group/${groupType}/${groupId}?classLevel=${classLevel}`
            : `${this.baseUrl}/gallery/group/${groupType}/${groupId}`
        const response = await get(url)
        return response.result as PhotoGalleryViewModel
    }

    getAvailableCourses = async (): Promise<CourseInfo[]> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/courses`)
        return response.result as CourseInfo[]
    }

    getCourseGallery = async (
        termCode: string,
        crn: string,
        options: { includeRossStudents?: boolean; groupType?: string | null; groupId?: string | null } = {},
    ): Promise<PhotoGalleryViewModel> => {
        const { includeRossStudents = false, groupType, groupId } = options
        const { get } = useFetch()
        const params = new URLSearchParams()
        params.append("includeRossStudents", includeRossStudents.toString())
        if (groupType) {
            params.append("groupType", groupType)
        }
        if (groupId) {
            params.append("groupId", groupId)
        }
        const response = await get(`${this.baseUrl}/gallery/course/${termCode}/${crn}?${params.toString()}`)
        if (!response.success || !response.result) {
            throw new Error(response.errors?.[0] || "Failed to fetch course gallery")
        }
        return response.result as PhotoGalleryViewModel
    }

    getGalleryMenu = async (): Promise<GalleryMenu> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/gallery/menu`)
        return response.result as GalleryMenu
    }

    getStudentPhotoUrl = (mailId: string): string => `${this.baseUrl}/student/${mailId}`

    getDefaultPhotoUrl = (): string => `${this.baseUrl}/default`

    exportToWord = (request: PhotoExportRequest): Promise<{ blob: Blob; filename: string | null }> =>
        postForBlob(`${this.baseUrl}/export/word`, request)

    exportToPDF = (request: PhotoExportRequest): Promise<{ blob: Blob; filename: string | null }> =>
        postForBlob(`${this.baseUrl}/export/pdf`, request)

    getAvailableGroups = async (): Promise<any> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/metadata/groups`)
        return response.result
    }

    getStudentsInClass = async (classLevel: string): Promise<StudentPhoto[]> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/metadata/students/${classLevel}`)
        return response.result as StudentPhoto[]
    }

    getClassYears = async (): Promise<ClassYear[]> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/metadata/classyears`)
        return response.result as ClassYear[]
    }

    downloadFile = (blob: Blob, filename: string): void => {
        const url = globalThis.URL.createObjectURL(blob)
        const link = document.createElement("a")
        link.href = url
        link.download = filename
        document.body.append(link)
        link.click()
        link.remove()
        globalThis.URL.revokeObjectURL(url)
    }
}

const photoGalleryService = new PhotoGalleryService()

export { photoGalleryService }
export type {
    StudentPhoto,
    PhotoGalleryViewModel,
    GroupingInfo,
    CourseInfo,
    ExportOptions,
    PhotoExportRequest,
    GalleryMenu,
    ClassYear,
}
