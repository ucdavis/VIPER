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
    // Computed properties from backend for consistent display
    fullName: string
    secondaryTextLines: string[]
}

interface PhotoGalleryViewModel {
    classLevel?: string
    students: StudentPhoto[]
    groupInfo?: GroupingInfo
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

class PhotoGalleryService {
    private baseUrl = "/api/students/photos"

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

    exportToWord = async (request: PhotoExportRequest): Promise<Blob> => {
        return await postForBlob(`${this.baseUrl}/export/word`, request)
    }

    exportToPDF = async (request: PhotoExportRequest): Promise<Blob> => {
        return await postForBlob(`${this.baseUrl}/export/pdf`, request)
    }

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
export type { StudentPhoto, PhotoGalleryViewModel, GroupingInfo, ExportOptions, PhotoExportRequest, GalleryMenu }
