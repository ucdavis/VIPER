// Clinical Scheduler types will be defined here

export interface Rotation {
    rotId: number
    serviceId: number
    name: string
    abbreviation: string
    subjectCode?: string
    courseNumber?: string
}

export interface Service {
    serviceId: number
    serviceName: string
    shortName: string
}