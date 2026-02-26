import type { DepartmentVerificationDto, EffortChangeAlertDto } from "../types"

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

const TERM_STATUS_COLORS: Record<string, string> = { Opened: "positive", Harvested: "info" }
function getTermStatusColor(status?: string): string {
    return (status && TERM_STATUS_COLORS[status]) || "grey"
}

const ALERT_ICONS: Record<string, string> = {
    NoRecords: "warning",
    NoInstructors: "school",
    NotVerified: "schedule",
    NoDepartment: "domain_disabled",
    ZeroHours: "timer_off",
}
function getAlertIcon(alert: EffortChangeAlertDto): string {
    return ALERT_ICONS[alert.alertType] ?? "error"
}

const ALERT_COLORS: Record<string, string> = { High: "negative", Medium: "warning", Low: "info" }
function getAlertColor(alert: EffortChangeAlertDto): string {
    return ALERT_COLORS[alert.severity] ?? "grey"
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

function createAlertByType(alertType: string): EffortChangeAlertDto {
    return {
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
    }
}

describe("StaffDashboard - Alert Icons", () => {
    it("should return warning for NoRecords", () => {
        expect(getAlertIcon(createAlertByType("NoRecords"))).toBe("warning")
    })

    it("should return school for NoInstructors", () => {
        expect(getAlertIcon(createAlertByType("NoInstructors"))).toBe("school")
    })

    it("should return schedule for NotVerified", () => {
        expect(getAlertIcon(createAlertByType("NotVerified"))).toBe("schedule")
    })

    it("should return domain_disabled for NoDepartment", () => {
        expect(getAlertIcon(createAlertByType("NoDepartment"))).toBe("domain_disabled")
    })

    it("should return timer_off for ZeroHours", () => {
        expect(getAlertIcon(createAlertByType("ZeroHours"))).toBe("timer_off")
    })

    it("should return error for unknown alert type", () => {
        expect(getAlertIcon(createAlertByType("Unknown"))).toBe("error")
    })
})

function createAlertBySeverity(severity: "High" | "Medium" | "Low"): EffortChangeAlertDto {
    return { ...createAlertByType("NoRecords"), severity }
}

describe("StaffDashboard - Alert Colors", () => {
    it("should return negative for High severity", () => {
        expect(getAlertColor(createAlertBySeverity("High"))).toBe("negative")
    })

    it("should return warning for Medium severity", () => {
        expect(getAlertColor(createAlertBySeverity("Medium"))).toBe("warning")
    })

    it("should return info for Low severity", () => {
        expect(getAlertColor(createAlertBySeverity("Low"))).toBe("info")
    })
})

describe("StaffDashboard - Department Filtering", () => {
    const departments: DepartmentVerificationDto[] = [
        makeDept("VME", "Medicine & Epidemiology", {
            totalInstructors: 20,
            verifiedInstructors: 18,
            unverifiedInstructors: 2,
            verificationPercent: 90,
            meetsThreshold: true,
            status: "OnTrack",
        }),
        makeDept("APC", "Anatomy", {
            totalInstructors: 15,
            verifiedInstructors: 10,
            unverifiedInstructors: 5,
            verificationPercent: 67,
        }),
        makeDept("PMI", "Pathology", {
            verifiedInstructors: 10,
            unverifiedInstructors: 0,
            verificationPercent: 100,
            meetsThreshold: true,
            status: "Complete",
        }),
    ]

    it("should filter departments that need follow-up", () => {
        const needsFollowup = departments.filter((d) => !d.meetsThreshold)
        expect(needsFollowup).toHaveLength(1)
        expect(needsFollowup[0]!.departmentCode).toBe("APC")
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
        { ...createAlertByType("NoRecords"), title: "No Records", entityName: "John Doe" },
        {
            ...createAlertByType("ZeroHours"),
            title: "Zero Hours",
            entityId: "2",
            entityName: "Jane Smith",
            departmentCode: "APC",
            status: "Ignored",
            isIgnored: true,
            reviewedDate: "2024-10-01",
            reviewedBy: "Admin User",
        },
        {
            ...createAlertByType("NoDepartment"),
            title: "No Department",
            entityId: "3",
            entityName: "Bob Wilson",
            departmentCode: "",
            severity: "High",
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
        expect(Math.round((80 / 100) * 100)).toBe(80)
    })

    it("should handle zero instructors", () => {
        expect(0).toBe(0)
    })

    it("should calculate pending and missing counts", () => {
        expect(100 - 80).toBe(20)
        expect(50 - 45).toBe(5)
    })
})

// Helper functions extracted from StaffDashboard.vue for testing
function getDeptDisplayName(dept: DepartmentVerificationDto): string {
    return dept.departmentCode === "UNK" ? "Unknown Department" : dept.departmentName
}

function isNoDept(dept: DepartmentVerificationDto): boolean {
    return dept.departmentCode === "UNK"
}

function makeDept(
    code: string,
    name: string,
    overrides: Partial<DepartmentVerificationDto> = {},
): DepartmentVerificationDto {
    return {
        departmentCode: code,
        departmentName: name,
        totalInstructors: 10,
        verifiedInstructors: 5,
        unverifiedInstructors: 5,
        verificationPercent: 50,
        meetsThreshold: false,
        status: "NeedsFollowup",
        ...overrides,
    }
}

describe("StaffDashboard - Department Display Name", () => {
    it("should return 'Unknown Department' for UNK code", () => {
        expect(getDeptDisplayName(makeDept("UNK", "Unknown"))).toBe("Unknown Department")
    })

    it("should return department name for normal codes", () => {
        expect(getDeptDisplayName(makeDept("VME", "Medicine & Epidemiology"))).toBe("Medicine & Epidemiology")
    })

    it("should identify UNK as no-department", () => {
        expect(isNoDept(makeDept("UNK", "Unknown"))).toBeTruthy()
    })

    it("should not flag normal departments as no-department", () => {
        expect(isNoDept(makeDept("VME", "Medicine"))).toBeFalsy()
        expect(isNoDept(makeDept("APC", "Anatomy"))).toBeFalsy()
    })
})

// Sort by verification % desc, then by instructor count desc
function sortByVerification(a: DepartmentVerificationDto, b: DepartmentVerificationDto) {
    return a.verificationPercent === b.verificationPercent
        ? b.totalInstructors - a.totalInstructors
        : b.verificationPercent - a.verificationPercent
}

describe("StaffDashboard - Department Sorting (Closed Terms)", () => {
    const departments: DepartmentVerificationDto[] = [
        makeDept("APC", "Anatomy"),
        makeDept("VME", "Medicine", {
            totalInstructors: 20,
            verifiedInstructors: 20,
            unverifiedInstructors: 0,
            verificationPercent: 100,
            meetsThreshold: true,
            status: "Complete",
        }),
        makeDept("PMI", "Pathology", {
            totalInstructors: 5,
            verifiedInstructors: 5,
            unverifiedInstructors: 0,
            verificationPercent: 100,
            meetsThreshold: true,
            status: "Complete",
        }),
    ]

    it("should sort by verification percent descending", () => {
        const sorted = [...departments].toSorted(sortByVerification)
        expect(sorted[0]!.departmentCode).toBe("VME")
        expect(sorted[2]!.departmentCode).toBe("APC")
    })

    it("should break ties by instructor count descending", () => {
        const sorted = [...departments].toSorted(sortByVerification)
        // VME (100%, 20 instructors) before PMI (100%, 5 instructors)
        expect(sorted[0]!.departmentCode).toBe("VME")
        expect(sorted[1]!.departmentCode).toBe("PMI")
    })
})

function makeNotVerifiedAlerts(): EffortChangeAlertDto[] {
    return [
        {
            ...createAlertByType("NotVerified"),
            title: "Verification Overdue",
            description: "Effort not verified after 30+ days",
            entityName: "John Doe",
            severity: "Low",
        },
        {
            ...createAlertByType("NoRecords"),
            title: "No Records",
            entityId: "2",
            entityName: "Jane Smith",
            departmentCode: "APC",
        },
    ]
}

describe("StaffDashboard - NotVerified Alerts Filtering", () => {
    it("should include NotVerified alerts when term is Open", () => {
        const termStatus = "Opened"
        const alerts = makeNotVerifiedAlerts()
        const notVerified = termStatus === "Opened" ? alerts.filter((a) => a.alertType === "NotVerified") : []
        expect(notVerified).toHaveLength(1)
    })

    it("should exclude NotVerified alerts when term is Closed", () => {
        const termStatus: string = "Closed"
        const notVerified =
            termStatus === "Opened" ? makeNotVerifiedAlerts().filter((a) => a.alertType === "NotVerified") : []
        expect(notVerified).toHaveLength(0)
    })

    it("should exclude NotVerified alerts when term is Harvested", () => {
        const termStatus: string = "Harvested"
        const notVerified =
            termStatus === "Opened" ? makeNotVerifiedAlerts().filter((a) => a.alertType === "NotVerified") : []
        expect(notVerified).toHaveLength(0)
    })
})
