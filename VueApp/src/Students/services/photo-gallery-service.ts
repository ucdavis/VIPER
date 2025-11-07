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
    priorClassYear: number | null
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

interface CoursesByTerm {
    termCode: string
    termDescription: string
    courses: CourseInfo[]
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

interface StudentDetailInfo {
    priorClassYear: number | null
    currentClassYear: number | null
}

class PhotoGalleryService {
    private baseUrl = `${import.meta.env.VITE_API_URL}students/photos`

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

    getAvailableCourses = async (): Promise<CoursesByTerm[]> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/courses`)
        return response.result as CoursesByTerm[]
    }

    getCourseGallery = async (
        termCode: string,
        crn: string,
        includeRossStudents = false,
    ): Promise<PhotoGalleryViewModel> => {
        const { get } = useFetch()
        const response = await get(
            `${this.baseUrl}/gallery/course/${termCode}/${crn}?includeRossStudents=${includeRossStudents}`,
        )
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

    getStudentPhotoUrl = (mailId: string): string => {
        return `${this.baseUrl}/student/${mailId}`
    }

    getDefaultPhotoUrl = (): string => {
        return `${this.baseUrl}/default`
    }

    exportToWord = async (request: PhotoExportRequest): Promise<Blob> =>
        postForBlob(`${this.baseUrl}/export/word`, request)

    exportToPDF = async (request: PhotoExportRequest): Promise<Blob> =>
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

    getStudentDetails = async (mailId: string): Promise<StudentDetailInfo | null> => {
        try {
            // Use raw fetch to avoid triggering the global error handler for 404s
            const response = await fetch(`${this.baseUrl}/student/${mailId}/details`)

            // Return null for 404 or other errors without showing an error notification
            if (!response.ok) {
                return null
            }

            const data = await response.json()
            return (data.result ?? data) as StudentDetailInfo
        } catch {
            // Silently return null on error
            return null
        }
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
    CoursesByTerm,
    ExportOptions,
    PhotoExportRequest,
    GalleryMenu,
    ClassYear,
    StudentDetailInfo,
}
