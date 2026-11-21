import type { ReportAdminDefinition, ReportEditRequest, ReportSourceDetail } from "../admin/report-admin-types"

/** Shared admin test data: a saved roster report over a view. */

const rosterView: ReportSourceDetail = {
    name: "FacultyRoster",
    kind: "View",
    description: "Current faculty",
    columns: [
        { name: "Name", sqlType: "varchar", kind: "Text", isSupported: true },
        { name: "Hired", sqlType: "date", kind: "Date", isSupported: true },
        { name: "Photo", sqlType: "varbinary", kind: "Binary", isSupported: false },
    ],
    parameters: [],
    columnsError: null,
    pagesInSql: true,
}

const rosterReport: ReportEditRequest = {
    title: "Faculty roster",
    description: null,
    area: "Personnel",
    source: "FacultyRoster",
    paginate: true,
    pageSize: null,
    allowExcel: true,
    allowPdf: false,
    pdfLandscape: false,
    sensitiveNotice: null,
    orderBy: "Name",
    orderByDescending: false,
    permissions: ["SVMSecure.Personnel"],
    columns: [{ sourceColumn: "Name", label: null, format: "Text", sortable: true, visible: true }],
    parameters: [
        {
            name: "Dept",
            label: "Department",
            type: "Choice",
            operator: "Equals",
            required: false,
            defaultValue: null,
            choicesSource: null,
            choicesList: [{ value: "VME", label: "Medicine" }],
            binding: "UserInput",
        },
    ],
    lastModifiedOn: "2026-11-16T09:30:00",
}

const saved: ReportAdminDefinition = {
    id: 4,
    slug: "8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e",
    createdOn: "2026-11-01T08:00:00",
    createdBy: "jdoe",
    modifiedOn: "2026-11-16T09:30:00",
    modifiedBy: "jdoe",
    deletedOn: null,
    deletedBy: null,
    report: rosterReport,
    missingPermissions: [],
}

export { rosterReport, rosterView, saved }
