import { useFetch } from "@/composables/ViperFetch"
import type {
    MyEffortDto,
    VerificationResult,
    CanVerifyResult,
    EmailHistoryDto,
    EmailSendResult,
    BulkEmailResult,
    SendVerificationEmailRequest,
    SendBulkEmailRequest,
} from "../types"

const { get, post } = useFetch()

/**
 * Service for effort verification API calls.
 * Handles self-service verification by instructors and admin email notifications.
 */
class VerificationService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort/verification`

    // ====================
    // Self-service endpoints (instructor verifying own effort)
    // ====================

    /**
     * Get the current user's effort data for self-service verification.
     */
    async getMyEffort(termCode: number): Promise<MyEffortDto | null> {
        const response = await get(`${this.baseUrl}/my-effort?termCode=${termCode}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as MyEffortDto
    }

    /**
     * Verify the current user's effort for a term.
     * Sets the EffortVerified timestamp.
     */
    async verifyEffort(termCode: number): Promise<VerificationResult> {
        const response = await post(`${this.baseUrl}/verify?termCode=${termCode}`)
        if (!response.success) {
            return {
                success: false,
                errorCode: "NETWORK_ERROR",
                errorMessage: "Failed to verify effort. Please try again.",
            }
        }
        return response.result as VerificationResult
    }

    // ====================
    // Admin endpoints (department admin sending emails)
    // ====================

    /**
     * Send a verification email to a specific instructor.
     */
    async sendVerificationEmail(personId: number, termCode: number): Promise<EmailSendResult> {
        const request: SendVerificationEmailRequest = { personId, termCode }
        const response = await post(`${this.baseUrl}/send-email`, request)
        if (!response.success) {
            return {
                success: false,
                error: "Failed to send email. Please try again.",
            }
        }
        return response.result as EmailSendResult
    }

    /**
     * Send verification emails to all unverified instructors in a department.
     */
    async sendBulkVerificationEmails(departmentCode: string, termCode: number): Promise<BulkEmailResult> {
        const request: SendBulkEmailRequest = { departmentCode, termCode }
        const response = await post(`${this.baseUrl}/send-bulk-email`, request)
        if (!response.success) {
            return {
                totalInstructors: 0,
                emailsSent: 0,
                emailsFailed: 1,
                failures: [{ personId: 0, instructorName: "", reason: "Request failed" }],
            }
        }
        return response.result as BulkEmailResult
    }

    /**
     * Get the email history for an instructor (verification emails sent).
     */
    async getEmailHistory(personId: number, termCode: number): Promise<EmailHistoryDto[]> {
        const response = await get(`${this.baseUrl}/${personId}/email-history?termCode=${termCode}`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as EmailHistoryDto[]
    }

    /**
     * Check if an instructor can verify their effort (no zero-effort records).
     */
    async canVerify(personId: number, termCode: number): Promise<CanVerifyResult> {
        const response = await get(`${this.baseUrl}/${personId}/can-verify?termCode=${termCode}`)
        if (!response.success || !response.result) {
            return {
                canVerify: false,
                zeroEffortCount: 0,
                zeroEffortCourses: [],
                zeroEffortRecordIds: [],
            }
        }
        return response.result as CanVerifyResult
    }
}

export const verificationService = new VerificationService()
