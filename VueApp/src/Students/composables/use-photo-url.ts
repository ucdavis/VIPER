import type { StudentPhoto } from "../services/photo-gallery-service"

export function getPhotoUrl(student: StudentPhoto): string {
    if (student.hasPhoto) {
        return `/api/students/photos/student/${student.mailId}`
    }
    return "/api/students/photos/default"
}
