/**
 * Verification types for the Effort system.
 * Used for self-service verification by instructors and admin email notifications.
 */

import type { PersonDto } from "./term-types"
import type { InstructorEffortRecordDto, ChildCourseDto } from "./instructor-types"

/**
 * Error codes for verification failures.
 * Allows the frontend to display targeted error messages.
 */
const VerificationErrorCodes = {
    ZERO_EFFORT: "ZERO_EFFORT",
    ALREADY_VERIFIED: "ALREADY_VERIFIED",
    TERM_NOT_FOUND: "TERM_NOT_FOUND",
    PERSON_NOT_FOUND: "PERSON_NOT_FOUND",
    NO_EFFORT_RECORDS: "NO_EFFORT_RECORDS",
} as const

/**
 * DTO for the My Effort self-service verification page.
 * Contains instructor's effort data and verification status.
 */
type MyEffortDto = {
    instructor: PersonDto
    effortRecords: InstructorEffortRecordDto[]
    crossListedCourses: ChildCourseDto[]
    hasZeroEffort: boolean
    zeroEffortRecordIds: number[]
    canVerify: boolean
    canEdit: boolean
    /** Whether the user has the VerifyEffort permission */
    hasVerifyPermission: boolean
    termName: string
    lastModifiedDate: string | null
    clinicalAsWeeksStartTermCode: number
}

/**
 * Result of a verification attempt.
 */
type VerificationResult = {
    success: boolean
    errorCode?: string
    errorMessage?: string
    verifiedDate?: string
    zeroEffortCourses?: string[]
}

/**
 * Result of checking whether an instructor can verify.
 */
type CanVerifyResult = {
    canVerify: boolean
    zeroEffortCount: number
    zeroEffortCourses: string[]
    zeroEffortRecordIds: number[]
}

/**
 * History of verification emails sent to an instructor.
 */
type EmailHistoryDto = {
    sentDate: string
    sentBy: string
    sentByName: string
    recipientEmail: string
    recipientName: string
}

/**
 * Result of sending a single verification email.
 */
type EmailSendResult = {
    success: boolean
    error?: string
}

/**
 * Details about a failed email send.
 */
type EmailFailure = {
    personId: number
    instructorName: string
    reason: string
}

/**
 * Result of sending bulk verification emails.
 */
type BulkEmailResult = {
    totalInstructors: number
    emailsSent: number
    emailsFailed: number
    failures: EmailFailure[]
}

/**
 * Request to send a verification email to a single instructor.
 */
type SendVerificationEmailRequest = {
    personId: number
    termCode: number
}

/**
 * Request to send verification emails to all unverified instructors in a department.
 */
type SendBulkEmailRequest = {
    departmentCode: string
    termCode: number
}

export {
    VerificationErrorCodes,
    type MyEffortDto,
    type VerificationResult,
    type CanVerifyResult,
    type EmailHistoryDto,
    type EmailSendResult,
    type EmailFailure,
    type BulkEmailResult,
    type SendVerificationEmailRequest,
    type SendBulkEmailRequest,
}
