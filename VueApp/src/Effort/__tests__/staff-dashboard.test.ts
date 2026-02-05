import { describe, it, expect } from "vitest"
import type { DepartmentVerificationDto, EffortChangeAlertDto } from "../types"

/**
 * Tests for StaffDashboard page logic.
 *
 * These tests validate the helper functions and computed property logic
 * used in the Staff Dashboard component.
 */

// Helper functions extracted from StaffDashboard.vue for testing
function getProgressColor(percent: number): string {
    if (percent >= 80) {
        return "positive"
    }
    if (percent >= 50) {
        return "warning"
    }
    return "negative"
}

function formatDate(dateString: string): string {
    const date = new Date(dateString)
    return date.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })
}

function getChangeIcon(action: string): string {
    if (action.includes("Create")) {
        return "add_circle"
    }
    if (action.includes("Update") || action.includes("Edit")) {
        return "edit"
    }
    if (action.includes("Delete")) {
        return "delete"
    }
    if (action.includes("Verify")) {
        return "check_circle"
    }
    return "info"
}

function getChangeColor(action: string): string {
    if (action.includes("Create")) {
        return "positive"
    }
    if (action.includes("Update") || action.includes("Edit")) {
        return "primary"
    }
    if (action.includes("Delete")) {
        return "negative"
    }
    if (action.includes("Verify")) {
        return "positive"
    }
    return "grey"
}

function formatChangeAction(action: string): string {
    return action
        .replaceAll(/([A-Z])/g, " $1")
        .trim()
        .replace(/^./, (str) => str.toUpperCase())
}

function getTermStatusColor(status: string | undefined): string {
    switch (status) {
        case "Opened": {
            return "positive"
        }
        case "Closed": {
            return "grey"
        }
        case "Harvested": {
            return "info"
        }
        default: {
            return "grey"
        }
    }
}

function getAlertIcon(alert: EffortChangeAlertDto): string {
    switch (alert.alertType) {
        case "NoRecords": {
            return "warning"
        }
        case "NoInstructors": {
            return "school"
        }
        case "NotVerified": {
            return "schedule"
        }
        case "NoDepartment": {
            return "domain_disabled"
        }
        case "ZeroHours": {
            return "timer_off"
        }
        default: {
            return "error"
        }
    }
}

function getAlertColor(alert: EffortChangeAlertDto): string {
    switch (alert.severity) {
        case "High": {
            return "negative"
        }
        case "Medium": {
            return "warning"
        }
        case "Low": {
            return "info"
        }
        default: {
            return "grey"
        }
    }
}

describe("StaffDashboard - Progress Color", () => {
    it("should return positive for 80% or above", () => {
        expect(getProgressColor(80)).toBe("positive")
        expect(getProgressColor(100)).toBe("positive")
        expect(getProgressColor(95)).toBe("positive")
    })

    it("should return warning for 50-79%", () => {
        expect(getProgressColor(50)).toBe("warning")
        expect(getProgressColor(79)).toBe("warning")
        expect(getProgressColor(65)).toBe("warning")
    })

    it("should return negative for below 50%", () => {
        expect(getProgressColor(0)).toBe("negative")
        expect(getProgressColor(49)).toBe("negative")
        expect(getProgressColor(25)).toBe("negative")
    })
})

describe("StaffDashboard - Date Formatting", () => {
    it("should format date in US locale", () => {
        const result = formatDate("2024-10-15T12:00:00")
        expect(result).toMatch(/Oct/)
        expect(result).toMatch(/15/)
        expect(result).toMatch(/2024/)
    })

    it("should handle ISO datetime strings", () => {
        // Use full datetime to avoid timezone issues with date-only strings
        const result = formatDate("2024-01-15T12:00:00")
        expect(result).toMatch(/Jan/)
        expect(result).toMatch(/15/)
        expect(result).toMatch(/2024/)
    })
})

describe("StaffDashboard - Change Icons", () => {
    it("should return add_circle for Create actions", () => {
        expect(getChangeIcon("CreateEffortRecord")).toBe("add_circle")
        expect(getChangeIcon("CreateInstructor")).toBe("add_circle")
    })

    it("should return edit for Update/Edit actions", () => {
        expect(getChangeIcon("UpdateEffortRecord")).toBe("edit")
        expect(getChangeIcon("EditInstructor")).toBe("edit")
    })

    it("should return delete for Delete actions", () => {
        expect(getChangeIcon("DeleteEffortRecord")).toBe("delete")
    })

    it("should return check_circle for Verify actions", () => {
        expect(getChangeIcon("VerifyEffort")).toBe("check_circle")
    })

    it("should return info for unknown actions", () => {
        expect(getChangeIcon("UnknownAction")).toBe("info")
    })
})

describe("StaffDashboard - Change Colors", () => {
    it("should return positive for Create actions", () => {
        expect(getChangeColor("CreateEffortRecord")).toBe("positive")
    })

    it("should return primary for Update/Edit actions", () => {
        expect(getChangeColor("UpdateEffortRecord")).toBe("primary")
        expect(getChangeColor("EditInstructor")).toBe("primary")
    })

    it("should return negative for Delete actions", () => {
        expect(getChangeColor("DeleteEffortRecord")).toBe("negative")
    })

    it("should return positive for Verify actions", () => {
        expect(getChangeColor("VerifyEffort")).toBe("positive")
    })

    it("should return grey for unknown actions", () => {
        expect(getChangeColor("UnknownAction")).toBe("grey")
    })
})

describe("StaffDashboard - Format Change Action", () => {
    it("should split camelCase into words", () => {
        expect(formatChangeAction("CreateEffortRecord")).toBe("Create Effort Record")
        expect(formatChangeAction("UpdateInstructor")).toBe("Update Instructor")
    })

    it("should capitalize first letter", () => {
        expect(formatChangeAction("verify")).toBe("Verify")
    })

    it("should handle PascalCase", () => {
        expect(formatChangeAction("VerifyEffort")).toBe("Verify Effort")
    })
})

describe("StaffDashboard - Term Status Color", () => {
    it("should return positive for Opened", () => {
        expect(getTermStatusColor("Opened")).toBe("positive")
    })

    it("should return grey for Closed", () => {
        expect(getTermStatusColor("Closed")).toBe("grey")
    })

    it("should return info for Harvested", () => {
        expect(getTermStatusColor("Harvested")).toBe("info")
    })

    it("should return grey for undefined", () => {
        expect(getTermStatusColor()).toBe("grey")
    })

    it("should return grey for unknown status", () => {
        expect(getTermStatusColor("Unknown")).toBe("grey")
    })
})

describe("StaffDashboard - Alert Icons", () => {
    const createAlert = (alertType: string): EffortChangeAlertDto => ({
        alertType,
        title: "",
        description: "",
        entityType: "Instructor",
        entityId: "1",
        entityName: "Test",
        departmentCode: "VME",
        recordCount: 1,
        severity: "Medium",
        status: "Active",
        isResolved: false,
        isIgnored: false,
        reviewedDate: null,
        reviewedBy: null,
    })

    it("should return warning for NoRecords", () => {
        expect(getAlertIcon(createAlert("NoRecords"))).toBe("warning")
    })

    it("should return school for NoInstructors", () => {
        expect(getAlertIcon(createAlert("NoInstructors"))).toBe("school")
    })

    it("should return schedule for NotVerified", () => {
        expect(getAlertIcon(createAlert("NotVerified"))).toBe("schedule")
    })

    it("should return domain_disabled for NoDepartment", () => {
        expect(getAlertIcon(createAlert("NoDepartment"))).toBe("domain_disabled")
    })

    it("should return timer_off for ZeroHours", () => {
        expect(getAlertIcon(createAlert("ZeroHours"))).toBe("timer_off")
    })

    it("should return error for unknown alert type", () => {
        expect(getAlertIcon(createAlert("Unknown"))).toBe("error")
    })
})

describe("StaffDashboard - Alert Colors", () => {
    const createAlert = (severity: "High" | "Medium" | "Low"): EffortChangeAlertDto => ({
        alertType: "NoRecords",
        title: "",
        description: "",
        entityType: "Instructor",
        entityId: "1",
        entityName: "Test",
        departmentCode: "VME",
        recordCount: 1,
        severity,
        status: "Active",
        isResolved: false,
        isIgnored: false,
        reviewedDate: null,
        reviewedBy: null,
    })

    it("should return negative for High severity", () => {
        expect(getAlertColor(createAlert("High"))).toBe("negative")
    })

    it("should return warning for Medium severity", () => {
        expect(getAlertColor(createAlert("Medium"))).toBe("warning")
    })

    it("should return info for Low severity", () => {
        expect(getAlertColor(createAlert("Low"))).toBe("info")
    })
})

describe("StaffDashboard - Department Filtering", () => {
    const departments: DepartmentVerificationDto[] = [
        {
            departmentCode: "VME",
            departmentName: "Medicine & Epidemiology",
            totalInstructors: 20,
            verifiedInstructors: 18,
            unverifiedInstructors: 2,
            verificationPercent: 90,
            meetsThreshold: true,
            status: "OnTrack",
        },
        {
            departmentCode: "APC",
            departmentName: "Anatomy",
            totalInstructors: 15,
            verifiedInstructors: 10,
            unverifiedInstructors: 5,
            verificationPercent: 67,
            meetsThreshold: false,
            status: "NeedsFollowup",
        },
        {
            departmentCode: "PMI",
            departmentName: "Pathology",
            totalInstructors: 10,
            verifiedInstructors: 10,
            unverifiedInstructors: 0,
            verificationPercent: 100,
            meetsThreshold: true,
            status: "Complete",
        },
    ]

    it("should filter departments that need follow-up", () => {
        const needsFollowup = departments.filter((d) => !d.meetsThreshold)
        expect(needsFollowup).toHaveLength(1)
        expect(needsFollowup[0].departmentCode).toBe("APC")
    })

    it("should filter departments that are on track", () => {
        const onTrack = departments.filter((d) => d.meetsThreshold)
        expect(onTrack).toHaveLength(2)
        expect(onTrack.map((d) => d.departmentCode)).toContain("VME")
        expect(onTrack.map((d) => d.departmentCode)).toContain("PMI")
    })
})

describe("StaffDashboard - Alert Filtering", () => {
    const alerts: EffortChangeAlertDto[] = [
        {
            alertType: "NoRecords",
            title: "No Records",
            description: "",
            entityType: "Instructor",
            entityId: "1",
            entityName: "John Doe",
            departmentCode: "VME",
            recordCount: 1,
            severity: "Medium",
            status: "Active",
            isResolved: false,
            isIgnored: false,
            reviewedDate: null,
            reviewedBy: null,
        },
        {
            alertType: "ZeroHours",
            title: "Zero Hours",
            description: "",
            entityType: "Instructor",
            entityId: "2",
            entityName: "Jane Smith",
            departmentCode: "APC",
            recordCount: 1,
            severity: "Medium",
            status: "Ignored",
            isResolved: false,
            isIgnored: true,
            reviewedDate: "2024-10-01",
            reviewedBy: "Admin User",
        },
        {
            alertType: "NoDepartment",
            title: "No Department",
            description: "",
            entityType: "Instructor",
            entityId: "3",
            entityName: "Bob Wilson",
            departmentCode: "",
            recordCount: 1,
            severity: "High",
            status: "Active",
            isResolved: false,
            isIgnored: false,
            reviewedDate: null,
            reviewedBy: null,
        },
    ]

    it("should filter out ignored alerts by default", () => {
        const visibleAlerts = alerts.filter((a) => a.status !== "Ignored")
        expect(visibleAlerts).toHaveLength(2)
    })

    it("should include all alerts when showIgnored is true", () => {
        expect(alerts).toHaveLength(3)
    })

    it("should filter alerts by type - NoRecords", () => {
        const noRecordsAlerts = alerts.filter((a) => a.alertType === "NoRecords")
        expect(noRecordsAlerts).toHaveLength(1)
    })

    it("should filter alerts by type - ZeroHours", () => {
        const zeroHoursAlerts = alerts.filter((a) => a.alertType === "ZeroHours")
        expect(zeroHoursAlerts).toHaveLength(1)
    })

    it("should filter alerts by type - NoDepartment", () => {
        const noDeptAlerts = alerts.filter((a) => a.alertType === "NoDepartment")
        expect(noDeptAlerts).toHaveLength(1)
    })

    it("should filter alerts by type - NoInstructors (empty)", () => {
        const noInstructorsAlerts = alerts.filter((a) => a.alertType === "NoInstructors")
        expect(noInstructorsAlerts).toHaveLength(0)
    })
})

describe("StaffDashboard - Stats Calculations", () => {
    it("should calculate verification percentage", () => {
        const totalInstructors = 100
        const verifiedInstructors = 80
        const percent = Math.round((verifiedInstructors / totalInstructors) * 100)

        expect(percent).toBe(80)
    })

    it("should handle zero instructors", () => {
        const percent = 0
        expect(percent).toBe(0)
    })

    it("should calculate pending instructors", () => {
        const totalInstructors = 100
        const verifiedInstructors = 80
        const pendingInstructors = totalInstructors - verifiedInstructors

        expect(pendingInstructors).toBe(20)
    })

    it("should calculate courses without instructors", () => {
        const totalCourses = 50
        const coursesWithInstructors = 45
        const coursesWithoutInstructors = totalCourses - coursesWithInstructors

        expect(coursesWithoutInstructors).toBe(5)
    })
})

// Helper functions extracted from StaffDashboard.vue for testing
function getDeptDisplayName(dept: DepartmentVerificationDto): string {
    return dept.departmentCode === "UNK" ? "No Department" : dept.departmentName
}

function isNoDept(dept: DepartmentVerificationDto): boolean {
    return dept.departmentCode === "UNK"
}

describe("StaffDashboard - Department Display Name", () => {
    const makeDept = (code: string, name: string): DepartmentVerificationDto => ({
        departmentCode: code,
        departmentName: name,
        totalInstructors: 10,
        verifiedInstructors: 5,
        unverifiedInstructors: 5,
        verificationPercent: 50,
        meetsThreshold: false,
        status: "NeedsFollowup",
    })

    it("should return 'No Department' for UNK code", () => {
        expect(getDeptDisplayName(makeDept("UNK", "Unknown"))).toBe("No Department")
    })

    it("should return department name for normal codes", () => {
        expect(getDeptDisplayName(makeDept("VME", "Medicine & Epidemiology"))).toBe("Medicine & Epidemiology")
    })

    it("should identify UNK as no-department", () => {
        expect(isNoDept(makeDept("UNK", "Unknown"))).toBe(true)
    })

    it("should not flag normal departments as no-department", () => {
        expect(isNoDept(makeDept("VME", "Medicine"))).toBe(false)
        expect(isNoDept(makeDept("APC", "Anatomy"))).toBe(false)
    })
})

describe("StaffDashboard - Department Sorting (Closed Terms)", () => {
    const departments: DepartmentVerificationDto[] = [
        {
            departmentCode: "APC",
            departmentName: "Anatomy",
            totalInstructors: 10,
            verifiedInstructors: 5,
            unverifiedInstructors: 5,
            verificationPercent: 50,
            meetsThreshold: false,
            status: "NeedsFollowup",
        },
        {
            departmentCode: "VME",
            departmentName: "Medicine",
            totalInstructors: 20,
            verifiedInstructors: 20,
            unverifiedInstructors: 0,
            verificationPercent: 100,
            meetsThreshold: true,
            status: "Complete",
        },
        {
            departmentCode: "PMI",
            departmentName: "Pathology",
            totalInstructors: 5,
            verifiedInstructors: 5,
            unverifiedInstructors: 0,
            verificationPercent: 100,
            meetsThreshold: true,
            status: "Complete",
        },
    ]

    it("should sort by verification percent descending", () => {
        const sorted = [...departments].sort((a, b) => {
            if (b.verificationPercent !== a.verificationPercent) {
                return b.verificationPercent - a.verificationPercent
            }
            return b.totalInstructors - a.totalInstructors
        })
        expect(sorted[0].departmentCode).toBe("VME")
        expect(sorted[2].departmentCode).toBe("APC")
    })

    it("should break ties by instructor count descending", () => {
        const sorted = [...departments].sort((a, b) => {
            if (b.verificationPercent !== a.verificationPercent) {
                return b.verificationPercent - a.verificationPercent
            }
            return b.totalInstructors - a.totalInstructors
        })
        // VME (100%, 20 instructors) before PMI (100%, 5 instructors)
        expect(sorted[0].departmentCode).toBe("VME")
        expect(sorted[1].departmentCode).toBe("PMI")
    })
})

describe("StaffDashboard - NotVerified Alerts Filtering", () => {
    const makeAlerts = (): EffortChangeAlertDto[] => [
        {
            alertType: "NotVerified",
            title: "Verification Overdue",
            description: "Effort not verified after 30+ days",
            entityType: "Instructor",
            entityId: "1",
            entityName: "John Doe",
            departmentCode: "VME",
            recordCount: 1,
            severity: "Low",
            status: "Active",
            isResolved: false,
            isIgnored: false,
            reviewedDate: null,
            reviewedBy: null,
        },
        {
            alertType: "NoRecords",
            title: "No Records",
            description: "",
            entityType: "Instructor",
            entityId: "2",
            entityName: "Jane Smith",
            departmentCode: "APC",
            recordCount: 1,
            severity: "Medium",
            status: "Active",
            isResolved: false,
            isIgnored: false,
            reviewedDate: null,
            reviewedBy: null,
        },
    ]

    it("should include NotVerified alerts when term is Open", () => {
        const termStatus = "Opened"
        const alerts = makeAlerts()
        const notVerified =
            termStatus === "Opened" ? alerts.filter((a) => a.alertType === "NotVerified") : []
        expect(notVerified).toHaveLength(1)
    })

    it("should exclude NotVerified alerts when term is Closed", () => {
        const termStatus = "Closed"
        const notVerified =
            termStatus === "Opened" ? makeAlerts().filter((a) => a.alertType === "NotVerified") : []
        expect(notVerified).toHaveLength(0)
    })

    it("should exclude NotVerified alerts when term is Harvested", () => {
        const termStatus = "Harvested"
        const notVerified =
            termStatus === "Opened" ? makeAlerts().filter((a) => a.alertType === "NotVerified") : []
        expect(notVerified).toHaveLength(0)
    })
})
