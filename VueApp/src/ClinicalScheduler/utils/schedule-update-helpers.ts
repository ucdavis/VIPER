import type { RotationScheduleData } from "../services/rotation-service"
import type { ClinicianScheduleData } from "../services/clinician-service"

interface ScheduleAssignmentData {
    rotationId?: number
    rotationName?: string
    rotationAbbreviation?: string
    serviceId?: number
    serviceName?: string
    clinicianMothraId?: string
    clinicianName?: string
    isPrimary?: boolean
    gradYear: number
}

interface AddScheduleParams {
    scheduleData: RotationScheduleData | ClinicianScheduleData
    weekId: number
    assignmentData: ScheduleAssignmentData
}

interface TogglePrimaryParams {
    scheduleData: RotationScheduleData | ClinicianScheduleData
    scheduleId: number
    isPrimary: boolean
}

type ScheduleData = RotationScheduleData | ClinicianScheduleData

interface UpdateRotationParams {
    clinicianData: ClinicianScheduleData
    weekId: number
    assignmentData: ScheduleAssignmentData
    scheduleId: number
}

function isRotationSchedule(data: ScheduleData): data is RotationScheduleData {
    return "rotation" in data && data.rotation !== undefined
}

const MAX_ABBREVIATION_LENGTH = 3

function getRotationDetails(clinicianData: ClinicianScheduleData, assignmentData: ScheduleAssignmentData) {
    // Extract rotation info from assignment data
    const { rotationId, rotationName = "Unknown Rotation" } = assignmentData

    if (!rotationId) {
        throw new Error("Rotation ID required for clinician schedule")
    }

    // Find rotation info from existing data if available in nested structure
    let existingRotation: any = null
    for (const semester of clinicianData.schedulesBySemester) {
        for (const week of semester.weeks) {
            const rotation = week.rotations.find((r: any) => r.rotationId === rotationId)
            if (rotation) {
                existingRotation = rotation
                break
            }
        }
        if (existingRotation) {
            break
        }
    }

    const serviceName = existingRotation?.serviceName || "Unknown Service"
    const abbreviation = existingRotation?.abbreviation || rotationName.slice(0, MAX_ABBREVIATION_LENGTH).toUpperCase()

    return {
        rotationId,
        rotationName,
        serviceName,
        abbreviation,
    }
}

function updateClinicianScheduleWithRotation(params: UpdateRotationParams) {
    const { clinicianData, weekId, assignmentData, scheduleId } = params
    const rotationDetails = getRotationDetails(clinicianData, assignmentData)

    // Find the correct week in the nested structure
    let targetWeek: any = null
    for (const semester of clinicianData.schedulesBySemester) {
        const week = semester.weeks.find((w: any) => w.weekId === weekId)
        if (week) {
            targetWeek = week
            break
        }
    }

    if (!targetWeek) {
        throw new Error(`Week ${weekId} not found in schedule data`)
    }

    // Add the new rotation to the week's rotations
    targetWeek.rotations.push({
        rotationId: rotationDetails.rotationId,
        rotationName: rotationDetails.rotationName,
        name: rotationDetails.rotationName,
        abbreviation: rotationDetails.abbreviation,
        serviceName: rotationDetails.serviceName,
        scheduleId,
        isPrimaryEvaluator: assignmentData.isPrimary,
    })
}

function removeRotationFromClinicianSchedule(clinicianData: ClinicianScheduleData, scheduleId: number) {
    // Find and remove the rotation with the matching scheduleId from the nested structure
    for (const semester of clinicianData.schedulesBySemester) {
        for (const week of semester.weeks) {
            const rotationIndex = week.rotations.findIndex((r: any) => r.scheduleId === scheduleId)
            if (rotationIndex !== -1) {
                week.rotations.splice(rotationIndex, 1)
                return // Found and removed, exit early
            }
        }
    }
}

function clearOtherPrimaries(rotations: any[], excludeId: number) {
    for (const rotation of rotations) {
        if (rotation.scheduleId !== excludeId) {
            rotation.isPrimaryEvaluator = false
        }
    }
}

function updatePrimaryInClinicianSchedule(
    clinicianData: ClinicianScheduleData,
    scheduleId: number,
    isPrimary: boolean,
) {
    // Find the rotation with the matching scheduleId and the week it belongs to
    for (const semester of clinicianData.schedulesBySemester) {
        for (const week of semester.weeks) {
            const rotation = week.rotations.find((r: any) => r.scheduleId === scheduleId)
            if (rotation) {
                // If setting as primary, clear other primaries in the same week first
                if (isPrimary) {
                    clearOtherPrimaries(week.rotations, scheduleId)
                }

                // Update the specific rotation
                rotation.isPrimaryEvaluator = isPrimary
                return // Found and updated, exit early
            }
        }
    }
}

// Export all types and functions
export type { ScheduleAssignmentData, AddScheduleParams, TogglePrimaryParams, ScheduleData, UpdateRotationParams }

export {
    isRotationSchedule,
    getRotationDetails,
    updateClinicianScheduleWithRotation,
    removeRotationFromClinicianSchedule,
    clearOtherPrimaries,
    updatePrimaryInClinicianSchedule,
}
