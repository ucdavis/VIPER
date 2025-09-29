# Effort System API Design Document

**Project:** VIPER2 Effort System Migration
**API Version:** 1.0
**Base URL:** `/Areas/Effort/api` (VIPER2 Areas-based routing)
**Authentication:** CAS (Central Authentication Service) with RAPS Claims
**Implementation:** Agile sprint-based delivery over 16 two-week sprints

---

## Overview

This document defines the RESTful API design for the Effort System migration to VIPER2. The API follows REST principles, uses standard HTTP methods, and implements consistent response patterns. Endpoints are implemented incrementally across 16 agile sprints, with core functionality available starting Sprint 2.

## Authentication & Authorization

### Authentication
All API endpoints require authentication via the existing VIPER2 CAS authentication system. Users must be authenticated through UC Davis CAS before accessing any endpoints.

```
Authentication Method: CAS (Central Authentication Service)
Session Management: Cookie-based authentication
Claims Integration: RAPS roles injected via ClaimsTransformer
```

### Permission Levels
- **ViewDept**: View department-specific data
- **ViewAllDepartments**: View cross-departmental data
- **EditEffort**: Create/modify effort records
- **ManageTerms**: Control term lifecycle
- **SU**: Super user access

## Base API Structure

### Response Format
All API responses follow a consistent structure:

```json
{
  "data": <response_data>,
  "success": true,
  "message": "Operation completed successfully",
  "errors": [],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalCount": 150,
    "totalPages": 3
  }
}
```

### Error Response Format
```json
{
  "data": null,
  "success": false,
  "message": "Validation failed",
  "errors": [
    {
      "field": "hours",
      "message": "Hours must be greater than 0"
    }
  ]
}
```

## Sprint Implementation Mapping

The API endpoints are implemented across agile sprints as follows:

### Sprint 1: Foundation
- Basic terms GET endpoints (read-only)
- Core authentication setup
- Base response format implementation

### Sprint 2: Term Management
- Term status management (PUT /terms/{termCode}/status)
- Term lifecycle endpoints (open, close, reopen, unopen)

### Sprint 3: Instructor Management
- Instructor CRUD endpoints
- Department filtering and permissions
- Basic instructor import

### Sprint 4: Course Management
- Course CRUD endpoints
- Course import from Banner
- Course-term relationships

### Sprint 5: Basic Effort Entry
- Effort CRUD endpoints
- Session type validation
- Hours/weeks business logic

### Sprint 6: Data Import
- Enhanced import endpoints
- Bulk import validation
- Import status and history

### Sprints 7-16: Advanced Features
- Effort verification (Sprint 7)
- Percentage assignments (Sprint 9)
- Course relationships (Sprint 10)
- Reporting endpoints (Sprints 11-13)
- Email notifications (Sprint 7)
- Audit endpoints (Sprint 8)

---

## API Endpoints

## 1. Terms Management
**Implementation:** Sprint 1-2

### Get Terms
```http
GET /Areas/Effort/api/terms
```

**Query Parameters:**
- `academicYear` (optional): Filter by academic year
- `status` (optional): Filter by term status (Harvested, Opened, Closed)

**Response:**
```json
{
  "data": [
    {
      "termCode": 202501,
      "termName": "Winter Quarter 2025",
      "academicYear": "2024-2025",
      "status": "Opened",
      "statusDate": "2024-12-01T00:00:00Z",
      "harvestedDate": "2024-11-15T10:30:00Z",
      "openedDate": "2024-12-01T09:00:00Z",
      "closedDate": null
    }
  ]
}
```

### Get Current Term
```http
GET /Areas/Effort/api/terms/current
```

### Update Term Status
```http
PUT /Areas/Effort/api/terms/{termCode}/status
```

**Required Permission:** ManageTerms

**Request Body:**
```json
{
  "action": "open", // open, close, reopen, unopen
  "notes": "Opening term for effort entry"
}
```

## 2. Instructors Management
**Implementation:** Sprint 3

### Get Instructors
```http
GET /Areas/Effort/api/instructors
```

**Query Parameters:**
- `termCode` (required): Academic term
- `department` (optional): Department filter
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 50)
- `search` (optional): Search by name

**Response:**
```json
{
  "data": [
    {
      "personId": 12345,
      "termCode": 202501,
      "firstName": "John",
      "lastName": "Smith",
      "middleInitial": "A",
      "effortDepartment": "VMTH",
      "jobTitle": "Professor",
      "percentAdmin": 25.0,
      "percentClinical": 50.0,
      "effortVerified": "2024-12-15T14:30:00Z",
      "totalEffortHours": 120,
      "hasUnverifiedEffort": false
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalCount": 150,
    "totalPages": 3
  }
}
```

### Get Instructor Detail
```http
GET /Areas/Effort/api/instructors/{personId}/terms/{termCode}
```

**Response:**
```json
{
  "data": {
    "personId": "12345678",
    "termCode": 202501,
    "firstName": "John",
    "lastName": "Smith",
    "effortDepartment": "VMTH",
    "efforts": [
      {
        "effortId": 1001,
        "courseId": 501,
        "course": {
          "id": 501,
          "crn": "12345",
          "subjectCode": "VET",
          "courseNumber": "201",
          "sequenceNumber": "001",
          "title": "Veterinary Anatomy",
          "enrollment": 45,
          "units": 3.0
        },
        "sessionType": "LEC",
        "role": "I",
        "hours": 40,
        "weeks": null,
        "verified": true
      }
    ],
    "percentages": [
      {
        "effortType": "Admin",
        "percentage": 25.0,
        "unit": "Dean's Office"
      }
    ]
  }
}
```

### Create Instructor
```http
POST /Areas/Effort/api/instructors
```

**Required Permission:** EditEffort

**Request Body:**
```json
{
  "personId": "12345678",
  "termCode": 202501,
  "firstName": "John",
  "lastName": "Smith",
  "effortDepartment": "VMTH",
  "jobTitle": "Professor"
}
```

### Update Instructor
```http
PUT /Areas/Effort/api/instructors/{personId}/terms/{termCode}
```

**Required Permission:** EditEffort

## 3. Courses Management

### Get Courses
```http
GET /Areas/Effort/api/courses
```

**Query Parameters:**
- `termCode` (required): Academic term
- `department` (optional): Department filter
- `subjectCode` (optional): Subject code filter
- `search` (optional): Search by course title or CRN

**Response:**
```json
{
  "data": [
    {
      "id": 501,
      "crn": "12345",
      "termCode": 202501,
      "subjectCode": "VET",
      "courseNumber": "201",
      "sequenceNumber": "001",
      "title": "Veterinary Anatomy",
      "enrollment": 45,
      "units": 3.0,
      "custodialDepartment": "VMTH",
      "instructorCount": 2,
      "totalEffortHours": 120
    }
  ]
}
```

### Get Course Detail
```http
GET /Areas/Effort/api/courses/{courseId}
```

**Response:**
```json
{
  "data": {
    "id": 501,
    "crn": "12345",
    "termCode": 202501,
    "subjectCode": "VET",
    "courseNumber": "201",
    "title": "Veterinary Anatomy",
    "enrollment": 45,
    "units": 3.0,
    "efforts": [
      {
        "effortId": 1001,
        "instructor": {
          "personId": 12345,
          "firstName": "John",
          "lastName": "Smith"
        },
        "sessionType": "LEC",
        "role": "I",
        "hours": 40
      }
    ],
    "childCourses": [
      {
        "id": 502,
        "crn": "12346",
        "subjectCode": "VET",
        "courseNumber": "201L"
      }
    ]
  }
}
```

### Create Course
```http
POST /Areas/Effort/api/courses
```

**Request Body:**
```json
{
  "crn": "12345",
  "termCode": 202501,
  "subjectCode": "VET",
  "courseNumber": "201",
  "sequenceNumber": "001",
  "enrollment": 45,
  "units": 3.0,
  "custodialDepartment": "VMTH"
}
```

## 4. Effort Records Management

### Get Effort Records
```http
GET /Areas/Effort/api/efforts
```

**Query Parameters:**
- `personId` (optional): Filter by instructor
- `courseId` (optional): Filter by course
- `termCode` (required): Academic term
- `department` (optional): Department filter

### Create Effort Record
```http
POST /Areas/Effort/api/efforts
```

**Required Permission:** EditEffort

**Request Body:**
```json
{
  "courseId": 501,
  "personId": "12345678",
  "sessionType": "LEC",
  "role": "I",
  "hours": 40,
  "weeks": null
}
```

### Update Effort Record
```http
PUT /Areas/Effort/api/efforts/{effortId}
```

**Request Body:**
```json
{
  "sessionType": "LEC",
  "role": "I",
  "hours": 45,
  "weeks": null
}
```

### Delete Effort Record
```http
DELETE /Areas/Effort/api/efforts/{effortId}
```

### Verify Effort Records
```http
POST /Areas/Effort/api/instructors/{personId}/terms/{termCode}/verify
```

**Request Body:**
```json
{
  "verified": true,
  "notes": "All effort records are accurate"
}
```

## 5. Percentage Assignments

### Get Instructor Percentages
```http
GET /Areas/Effort/api/instructors/{personId}/percentages
```

**Query Parameters:**
- `academicYear` (optional): Filter by academic year

**Response:**
```json
{
  "data": [
    {
      "id": 101,
      "personId": 12345,
      "termCode": 202501,
      "effortType": "Admin",
      "percentage": 25.0,
      "unit": "Dean's Office",
      "startDate": "2024-09-01T00:00:00Z",
      "endDate": "2025-06-30T23:59:59Z"
    }
  ]
}
```

### Create Percentage Assignment
```http
POST /Areas/Effort/api/instructors/{personId}/percentages
```

**Request Body:**
```json
{
  "termCode": 202501,
  "effortType": "Admin",
  "percentage": 25.0,
  "unit": "Dean's Office"
}
```

### Update Percentage Assignment
```http
PUT /Areas/Effort/api/percentages/{percentageId}
```

### Delete Percentage Assignment
```http
DELETE /Areas/Effort/api/percentages/{percentageId}
```

## 6. Effort Verification
**Implementation:** Sprint 7

### Verify Instructor Effort
```http
PUT /Areas/Effort/api/instructors/{personId}/terms/{termCode}/verify
```

**Required Permission:** Self or VerifyEffort
**Request Body:**
```json
{
  "verified": true,
  "notes": "All effort assignments reviewed and confirmed"
}
```

### Get Verification Status
```http
GET /Areas/Effort/api/instructors/{personId}/terms/{termCode}/verification
```

## 7. Course Relationships
**Implementation:** Sprint 10

### Get Course Relationships
```http
GET /Areas/Effort/api/courses/{courseId}/relationships
```

### Create Course Relationship
```http
POST /Areas/Effort/api/courses/{courseId}/relationships
```

**Request Body:**
```json
{
  "relatedCourseId": 502,
  "relationshipType": "parent" // parent, child, crosslist
}
```

## 8. Email Notifications
**Implementation:** Sprint 7

### Send Effort Notification
```http
POST /Areas/Effort/api/notifications/effort
```

**Request Body:**
```json
{
  "termCode": 202501,
  "personIds": ["12345678", "87654321"],
  "notificationType": "verification_reminder",
  "customMessage": "Please verify your effort assignments"
}
```

## 9. Audit Trail
**Implementation:** Sprint 8

### Get Audit Log
```http
GET /Areas/Effort/api/audit
```

**Query Parameters:**
- `termCode` (optional): Filter by term
- `personId` (optional): Filter by user
- `action` (optional): Filter by action type
- `startDate` (optional): Date range start
- `endDate` (optional): Date range end

## 10. Import Operations
**Implementation:** Sprint 6

### Get Import Status
```http
GET /Areas/Effort/api/import/status
```

### Start Data Import
```http
POST /Areas/Effort/api/import/start
```

**Request Body:**
```json
{
  "termCode": 202501,
  "importType": "crest", // crest, banner, clinical
  "clearExisting": true
}
```

### Get Import History
```http
GET /Areas/Effort/api/import/history
```

## 11. Reporting
**Implementation:** Sprints 11-13

### Get Available Reports
```http
GET /Areas/Effort/api/reports
```

**Response:**
```json
{
  "data": [
    {
      "reportType": "EffortSummary",
      "name": "Effort Summary Report",
      "description": "Summary of effort by department and instructor",
      "parameters": [
        {
          "name": "termCodes",
          "type": "array",
          "required": true
        },
        {
          "name": "departments",
          "type": "array",
          "required": false
        }
      ]
    }
  ]
}
```

### Generate Report
```http
POST /Areas/Effort/api/reports/{reportType}
```

**Request Body:**
```json
{
  "parameters": {
    "termCodes": [202501, 202502],
    "departments": ["VMTH", "ANAT"],
    "includeZeroEffort": false
  },
  "format": "json" // json, pdf, excel
}
```

**Response:**
```json
{
  "data": {
    "reportId": "rpt_20240917_123456",
    "status": "Processing",
    "downloadUrl": null,
    "estimatedCompletion": "2024-09-17T10:35:00Z"
  }
}
```

### Get Report Status
```http
GET /Areas/Effort/api/reports/status/{reportId}
```

### Download Report
```http
GET /Areas/Effort/api/reports/download/{reportId}
```

## 7. Data Import

### Get Import Status
```http
GET /Areas/Effort/api/import/status/{termCode}
```

**Response:**
```json
{
  "data": {
    "termCode": 202501,
    "lastImportDate": "2024-09-15T08:30:00Z",
    "status": "Completed",
    "coursesImported": 245,
    "instructorsImported": 89,
    "errors": []
  }
}
```

### Start Course Import
```http
POST /Areas/Effort/api/import/courses/{termCode}
```

**Required Permission:** ManageTerms

### Start Instructor Import
```http
POST /Areas/Effort/api/import/instructors/{termCode}
```

### Start Clinical Import
```http
POST /Areas/Effort/api/import/clinical/{termCode}
```

## 8. User Access Management

### Get User Departments
```http
GET /Areas/Effort/api/user/departments
```

**Response:**
```json
{
  "data": [
    {
      "abbreviation": "VMTH",
      "name": "Veterinary Medicine & Epidemiology",
      "canEdit": true,
      "canView": true
    }
  ]
}
```

### Get User Permissions
```http
GET /Areas/Effort/api/user/permissions
```

**Response:**
```json
{
  "data": {
    "canEditEffort": true,
    "canManageTerms": false,
    "canViewAllDepartments": false,
    "departments": ["VMTH"],
    "isSuperUser": false
  }
}
```

## Data Models

### Core Entities

#### Instructor (Person) DTO
```typescript
interface InstructorDto {
  personId: string
  termCode: number
  firstName: string
  lastName: string
  middleInitial?: string
  effortDepartment: string
  jobTitle?: string
  percentAdmin: number
  percentClinical?: number
  effortVerified?: Date
  totalEffortHours: number
  hasUnverifiedEffort: boolean
}
```

#### Course DTO
```typescript
interface CourseDto {
  id: number
  crn: string
  termCode: number
  subjectCode: string
  courseNumber: string
  sequenceNumber: string
  title?: string
  enrollment: number
  units: number
  custodialDepartment: string
  instructorCount: number
  totalEffortHours: number
}
```

#### Effort DTO
```typescript
interface EffortDto {
  effortId: number
  courseId: number
  personId: string
  termCode: number
  sessionType: string
  role: string
  hours?: number
  weeks?: number
  verified: boolean
  createdDate: Date
  modifiedDate: Date
  modifiedBy: string
}
```

#### Term DTO
```typescript
interface TermDto {
  termCode: number
  termName: string
  academicYear: string
  status: 'Harvested' | 'Opened' | 'Closed'
  harvestedDate?: Date
  openedDate?: Date
  closedDate?: Date
}
```

### Request DTOs

#### Create Effort Request
```typescript
interface CreateEffortDto {
  courseId: number
  personId: string
  sessionType: string
  role: string
  hours?: number
  weeks?: number
}
```

#### Update Effort Request
```typescript
interface UpdateEffortDto {
  sessionType?: string
  role?: string
  hours?: number
  weeks?: number
}
```

#### Report Request
```typescript
interface ReportRequestDto {
  parameters: Record<string, any>
  format: 'json' | 'pdf' | 'excel'
}
```

## Validation Rules

### Effort Records
- **Session Type**: Must be valid code (LEC, LAB, CLI, SEM, etc.)
- **Role**: Must be valid role code (I, A, G, etc.)
- **Hours/Weeks**: Mutually exclusive, at least one required
- **Hours**: Must be > 0 and <= 999 (for LEC, LAB, SEM session types)
- **Weeks**: Must be > 0 and <= 52 (for CLI session types only)

### Instructors
- **MothraID**: Must be 8 characters, numeric
- **Names**: Required, max 50 characters
- **Department**: Must be valid department code

### Courses
- **CRN**: Must be 5 characters, numeric, unique per term
- **Term Code**: Must be valid 6-digit term code
- **Enrollment**: Must be >= 0
- **Units**: Must be > 0

## Error Codes

### HTTP Status Codes
- **200**: Success
- **201**: Created
- **400**: Bad Request (validation errors)
- **401**: Unauthorized
- **403**: Forbidden (insufficient permissions)
- **404**: Not Found
- **409**: Conflict (duplicate data)
- **422**: Unprocessable Entity (business rule violations)
- **500**: Internal Server Error

### Custom Error Codes
- **E1001**: Invalid term code
- **E1002**: Term not open for editing
- **E1003**: Instructor not found
- **E1004**: Course not found
- **E1005**: Duplicate effort record
- **E1006**: Invalid department access
- **E1007**: Effort already verified

## Rate Limiting

### Limits
- **General API**: 1000 requests per hour per user
- **Report Generation**: 10 requests per hour per user
- **Import Operations**: 5 requests per hour per user

### Headers
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1632147600
```

## Caching Strategy

### Cache-Control Headers
- **Static Reference Data**: 1 hour
- **Dynamic User Data**: No cache
- **Reports**: 15 minutes

### ETag Support
ETags are provided for cacheable resources to enable conditional requests.

## API Versioning

### URL Versioning
Current version: `/Areas/Effort/api/v1/`
Future versions: `/Areas/Effort/api/v2/`

### Deprecation Policy
- 6 months notice for breaking changes
- Backward compatibility maintained for 1 year
- Clear migration documentation provided

---

**API Status**: Draft v2.0 - Updated for VIPER2 Areas Architecture
**Last Updated**: September 18, 2025
**Review Date**: October 1, 2025