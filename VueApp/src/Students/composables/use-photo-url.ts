import type { StudentPhoto } from "../services/photo-gallery-service"

const baseUrl = `${import.meta.env.VITE_API_URL}students/photos`

export function getPhotoUrl(student: StudentPhoto): string {
    if (student.hasPhoto) {
        return `${baseUrl}/student/${student.mailId}`
    }
    return `${baseUrl}/default`
}
