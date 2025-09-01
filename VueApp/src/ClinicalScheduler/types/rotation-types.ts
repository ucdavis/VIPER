import type { Rotation, Service } from "../types"
import type { ApiResult } from "./api"

interface RotationWithService extends Rotation {
    service?: Service
}

interface RotationSummary {
    totalRotations: number
    serviceCount: number
    services: {
        serviceId: number
        serviceName: string
        shortName: string
        rotationCount: number
        rotations: {
            rotId: number
            name: string
            abbreviation: string
        }[]
    }[]
}


interface InstructorScheduleItem {
    instructorScheduleId: number
    firstName: string
    lastName: string
    fullName: string
    mothraId: string
    evaluator: boolean
    dateStart: string
    dateEnd: string
    week: {
        weekId: number
        dateStart: string
        dateEnd: string
        termCode: number
    }
}

interface WeekItem {
    weekId: number
    dateStart: string
    dateEnd: string
    termCode: number
    weekNumber: number
    requiresPrimaryEvaluator: boolean
}

interface ScheduleItem {
    instructorScheduleId: number
    firstName: string
    lastName: string
    fullName: string
    mothraId: string
    evaluator: boolean
    isPrimaryEvaluator: boolean
}

interface WeekWithSchedules {
    weekId: number
    weekNumber: number
    dateStart: string
    dateEnd: string
    termCode: number
    extendedRotation: boolean
    forcedVacation: boolean
    requiresPrimaryEvaluator: boolean
    instructorSchedules: ScheduleItem[]
}

interface SemesterSchedule {
    semester: string
    weeks: WeekWithSchedules[]
}

interface RecentClinician {
    mothraId: string
    fullName: string
}

interface RotationScheduleData {
    rotation:
        | {
            rotId: number
            name: string
            abbreviation: string
            service: {
                serviceId: number
                serviceName: string
                shortName: string
            }
          }
        | undefined
    gradYear: number
    schedulesBySemester: SemesterSchedule[]
    recentClinicians?: RecentClinician[]
}

export type {
    RotationWithService,
    RotationSummary,
    ApiResult,
    InstructorScheduleItem,
    WeekItem,
    ScheduleItem,
    WeekWithSchedules,
    SemesterSchedule,
    RecentClinician,
    RotationScheduleData,
}
