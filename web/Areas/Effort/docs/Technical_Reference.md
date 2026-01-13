# Effort System - Technical Reference

Reference documentation for the Effort system database schema and field mappings.

---

## Legacy to Modern Schema Mapping

### Core Tables

**tblEffort → [effort].[Records]** (Core effort assignments)
- `effort_ID` → `Id` (int IDENTITY)
- `effort_MothraID` (char 8) → `PersonId` (int FK to VIPER.users.Person)
- `effort_termCode` → `TermCode` (int)
- `effort_CourseID` → `CourseId` (int FK)
- `effort_SessionType` → `EffortTypeId` (varchar 3 FK to EffortTypes)
- `effort_Role` (char 1) → `RoleId` (int FK to Roles)
- `effort_Hours` → `Hours` (int NULL)
- `effort_Weeks` → `Weeks` (int NULL)
- `effort_CRN` → `Crn` (varchar 5)
- Added: `CreatedDate`, `ModifiedDate`, `ModifiedBy`

**tblPerson → [effort].[Persons]** (Instructor information per term)
- `person_MothraID` (char 8) → `PersonId` (int FK, composite PK with TermCode)
- `person_TermCode` → `TermCode` (int, composite PK)
- `person_FirstName` → `FirstName` (varchar 50)
- `person_LastName` → `LastName` (varchar 50)
- `person_MiddleIni` → `MiddleInitial` (varchar 1)
- `person_EffortTitleCode` → `EffortTitleCode` (char 6)
- `person_EffortDept` → `EffortDept` (char 6)
- `person_PercentAdmin` (float) → `PercentAdmin` (decimal 5,2)
- `person_JobGrpID` → `JobGroupId` (char 3)
- `person_Title` → `Title` (varchar 50)
- `person_AdminUnit` → `AdminUnit` (varchar 25)
- `person_EffortVerified` → `EffortVerified` (datetime2)
- `person_ReportUnit` → `ReportUnit` (varchar 50)
- `person_Volunteer_WOS` → `VolunteerWos` (tinyint)
- `person_PercentClinical` (float) → `PercentClinical` (decimal 5,2)

**tblCourses → [effort].[Courses]** (Course information per term)
- `course_id` → `Id` (int IDENTITY)
- `course_CRN` → `Crn` (char 5)
- `course_TermCode` → `TermCode` (int)
- `course_SessionType` → Removed (not applicable for Courses table)
- `course_CustDept` → `CustodialDepartment` (char 6)
- `course_Subject` → `Subject` (char 4)
- `course_CatalogNbr` → `CatalogNumber` (char 4)
- `course_Units` → `Units` (decimal 3,1)
- Added: `CreatedDate`, `ModifiedDate`, `ModifiedBy`

**tblPercent → [effort].[Percentages]** (Percentage allocations)
- Normalized from wide format (Teaching, Research, ClinicalService, etc. columns)
- To narrow format: `PersonId`, `AcademicYear` (char 9, e.g., '2019-2020'), `PercentAssignTypeId` (FK to PercentAssignTypes), `Percentage`
- All records with valid PersonId are migrated (AcademicYear derived from StartDate if missing)
- Added: `Unit`, `StartDate`, `EndDate`, audit fields
- Indexed on: PersonId, AcademicYear, StartDate, EndDate

**tblStatus → [effort].[TermStatus]** (Term lifecycle management)
- `status_id` → `Id` (int IDENTITY)
- `status_TermCode` → `TermCode` (int UNIQUE)
- `status_TermStatus` → `Status` (varchar 50)
- `status_StartDate` → `StartDate` (datetime2)
- `status_EndDate` → `EndDate` (datetime2)
- Added: `IsActive`, `WorkflowStage`, audit fields

### Lookup Tables

**Roles** (Seeded - not migrated)
- '1' = Instructor of Record
- '2' = Instructor
- '3' = Facilitator

**PercentAssignTypes** (26 types - seeded from tblEffortType_LU)
- Teaching, Research, Clinical Service, Outreach, Administration, etc.
- Note: Renamed from EffortTypes to PercentAssignTypes

**EffortTypes** (35 types - seeded)
- LEC, LAB, DIS, SEM, CLI, etc.
- Note: Renamed from SessionTypes to EffortTypes

**Units** (Critical for authorization)
- Migrated from tblUnits_LU
- Maps effort unit codes to descriptions

**JobCodes**, **ReportUnits**, **AlternateTitles**
- Direct migration from legacy equivalents

### Special Tables

**tblSabbatic → [effort].[Sabbaticals]**
- Aggregated by person: `ExcludeClinicalTerms`, `ExcludeDidacticTerms` (comma-separated)

**tblAudit → [effort].[Audits]**
- Single `Changes` column for audit text (legacy plain-text or JSON)
- Legacy preservation columns: `LegacyAction`, `LegacyCRN`, `LegacyMothraID`
- `TermCode` provides term context for audit records
- Added: `MigratedDate`

**userAccess → [effort].[UserAccess]** (Critical for authorization)
- `PersonId` (FK), `DepartmentCode`, `IsActive`
- Added: audit fields

**tblCourseRelationships → [effort].[CourseRelationships]**
- Parent/child course relationships

---

## Key Data Type Changes

**char/varchar → int with FK**:
- MothraID → PersonId (FK to VIPER.users.Person)
- Role → char(1) kept as-is with FK to Roles table

**float → decimal**:
- Percentages (PercentAdmin, PercentClinical)
- Course Units
- Reason: Precise arithmetic, no floating-point errors

**datetime → datetime2(7)**:
- All date/time fields upgraded for precision

**Wide → Narrow normalization**:
- tblPercent multiple columns → Percentages with PercentAssignTypeId

---

## Foreign Key Relationships

**Cross-database FKs to VIPER**:
- `[effort].[Persons].PersonId` → `[VIPER].[users].[Person].PersonId`
- `[effort].[Records].PersonId` → `[VIPER].[users].[Person].PersonId`
- `[effort].[UserAccess].PersonId` → `[VIPER].[users].[Person].PersonId`
- All `ModifiedBy` fields → `[VIPER].[users].[Person].PersonId`

**Within effort schema**:
- `Records.CourseId` → `Courses.Id`
- `Records.RoleId` → `Roles.Id`
- `Records.EffortTypeId` → `EffortTypes.Id`
- `Percentages.PercentAssignTypeId` → `PercentAssignTypes.Id`
- `Sabbaticals.PersonId` → `Persons.PersonId`
- `CourseRelationships.ParentCourseId` → `Courses.Id`
- `CourseRelationships.ChildCourseId` → `Courses.Id`

---

## Composite Keys

**Persons table**: `PRIMARY KEY (PersonId, TermCode)`
- Instructors can have different metadata per term
- Prevents duplicate person records for same term

---

## Migration Notes

**MothraId → PersonId mapping**:
- Script looks up MothraId in VIPER.users.Person
- Unmapped records get PersonId = 0 (requires manual resolution)

**Removed obsolete fields**:
- `effort_ClientID` (no longer used)
- `person_ClientID` (no longer used)

**Audit trail added to all tables**:
- `CreatedDate`, `ModifiedDate`, `ModifiedBy`
- Essential for compliance and troubleshooting

**Check constraints**:
- Percentages: 0-100 range
- Hours: 0-999 range
- Weeks: 0-52 range

---

## Stored Procedures

### Overview

**Total Count**: 89 procedures in legacy Efforts database
- **87 active procedures** to address
- **2 obsolete procedures** (legacy Visual Studio database tools)

### Categories and Migration Strategies

#### 1. Read Operations (46 procedures) - EF Core Replacement

**Strategy**: Replace with EF Core queries, LINQ, or API endpoints

**Basic Getters (14 procedures)**:
- `usp_get_audit` - Get audit records
- `usp_get_effortSessionTypes` - Get effort types lookup (legacy name for session types)
- `usp_get_foreign_course` - Get foreign course details
- `usp_getAcademicYearFromTermCode` - Calculate academic year
- `usp_getCourse` - Get single course
- `usp_getCourseByEffortID` - Get course by effort ID
- `usp_getCourseChildRelationships` - Get child courses
- `usp_getCourseEffort` - Get effort records for course
- `usp_getCourseParentRelationships` - Get parent courses
- `usp_getCourses` - List all courses
- `usp_getEffortTypes` - Get percent assign types lookup (legacy name for EffortTypes, now PercentAssignTypes)
- `usp_getEffortUnits` - Get units lookup
- `usp_getInstructor` - Get single instructor
- `usp_getInstructorEffort` - Get effort records for instructor

**Instructor Queries (10 procedures)**:
- `usp_getDistinctCourseInstructors` - List instructors for course
- `usp_getDistinctInstructorCourses` - List courses for instructor
- `usp_getInstructorEvals` - Get instructor evaluations
- `usp_getInstructors` - List all instructors
- `usp_getInstructorsInAcademicYear` - Instructors by year
- `usp_getInstructorsInAcademicYear2` - Instructors by year (v2)
- `usp_getInstructorsRay` - Custom instructor query (Ray)
- `usp_getInstructorsWithClinicalEffort` - Clinical instructors only
- `usp_getPossibleInstructors` - Instructor search/lookup
- `usp_getZeroEffortInstructors` - Instructors with no effort

**Percent/Activity Queries (5 procedures)**:
- `usp_getEffortPercentsForInstructor` - Get percentage breakdown
- `usp_getListOfEffortPercentsForInstructor` - Percentage list
- `usp_getSumEffortPercentsForInstructor` - Sum percentages
- `usp_getNonChildCourses` - Get courses without children
- `usp_getPersonMPLA` - Get person MPLA data

**Job Group/Department Queries (4 procedures)**:
- `usp_getJobGroupName` - Get job group name
- `usp_getJobGroups` - List job groups
- `usp_getDepartmentCountByJobGroup` - Department counts
- `usp_getDepartmentCountByJobGroupWithExcludeTerms` - Dept counts with exclusions

**Term Status (1 procedure)**:
- `usp_getTermStatus` - Get term status

**Department Activity Totals (12 procedures)** - High complexity:
- `usp_getEffortDeptActivityTotal` - Base version
- `usp_getEffortDeptActivityTotal2` - Version 2
- `usp_getEffortDeptActivityTotalBackup` - Backup version
- `usp_getEffortDeptActivityTotalWithExclude` - With exclusions
- `usp_getEffortDeptActivityTotalWithExclude2` - With exclusions v2
- `usp_getEffortDeptActivityTotalWithExcludeTerms` - Term exclusions
- `usp_getEffortDeptActivityTotalWithExcludeTerms_old` - Old version
- `usp_getEffortDeptActivityTotalWithExcludeTerms2` - Term exclusions v2
- `usp_getEffortDeptActivityTotalWorking` - Working version
- `usp_getEffortInstructorActivity` - Instructor activity
- `usp_getEffortInstructorActivity2` - Instructor activity v2
- `usp_getEffortInstructorActivityWithExcludeTerms` - With term exclusions
- `usp_getEffortInstructorActivityWithExcludeTerms2` - With term exclusions v2

**Non-Admin Effort (3 procedures)**:
- `usp_getNonAdminEffort` - Non-admin effort calculation
- `usp_getNonAdminEffort2` - Non-admin effort v2
- `usp_getNonAdminEffortMultiYear` - Multi-year non-admin effort

#### 2. Reporting Procedures (14 procedures) - Migrate & Modernize

**Strategy**: Migrate to new reporting API, consider keeping as stored procedures for performance

**Main Reports (4 procedures)**:
- `usp_getEffortReport` - Main effort report
- `usp_getEffortReportDeptSummary` - Department summary
- `usp_getEffortReportKass` - Custom report (Kass)
- `usp_getEffortReportSchoolSummary` - School-wide summary

**Merit Reports (10 procedures)** - Critical for merit/promotion reviews:
- `usp_getEffortReportMerit` - Base merit report
- `usp_getEffortReportMeritAverage` - Average merit scores
- `usp_getEffortReportMeritMedian` - Median merit scores
- `usp_getEffortReportMeritMultiYear` - Multi-year merit
- `usp_getEffortReportMeritMultiYearWithExcludeTerms` - Multi-year with exclusions
- `usp_getEffortReportMeritMultiYearWithExcludeYear` - Multi-year with year exclusions
- `usp_getEffortReportMeritSummary` - Merit summary
- `usp_getEffortReportMeritSummaryForLairmore` - Custom report (Lairmore)
- `usp_getEffortReportMeritUnit` - Merit by unit
- `usp_getEffortReportMeritWithClinPercent` - Merit with clinical percentages

#### 3. INSERT Operations (6 procedures) - EF Core Replacement

**Strategy**: Replace with EF Core `SaveChanges()` and service layer methods

- `usp_createAudit` - Create audit record
- `usp_createCourse` - Create course
- `usp_createCourseRelationship` - Create course relationship
- `usp_createInstructor` - Create instructor
- `usp_createInstructorEffort` - Create effort record
- `usp_createInstructorPercent` - Create percentage record

#### 4. UPDATE Operations (4 procedures) - EF Core Replacement

**Strategy**: Replace with EF Core `SaveChanges()` and service layer methods

- `usp_updateCourse` - Update course
- `usp_updateInstructor` - Update instructor
- `usp_updateInstructorEffort` - Update effort record
- `usp_updateInstructorPercent` - Update percentage record

#### 5. DELETE Operations (5 procedures) - EF Core Replacement

**Strategy**: Replace with EF Core `Remove()` and service layer methods

- `usp_delCourse` - Delete course
- `usp_delCourseRelationship` - Delete course relationship
- `usp_delInstructor` - Delete instructor
- `usp_delInstructorEffort` - Delete effort record
- `usp_delInstructorPercent` - Delete percentage record

#### 6. Term Management (4 procedures) - Business Logic Methods

**Strategy**: Keep as specialized service methods with business logic

- `usp_closeTerm` - Close term (workflow state change)
- `usp_openTerm` - Open term for effort entry
- `usp_reopenTerm` - Reopen closed term
- `usp_unopenTerm` - Reverse term opening

#### 7. Specialized/Complex Queries (6 procedures) - Requires Analysis

**Strategy**: Analyze individually, may require custom implementation

- `usp_EffortDynamic` - Dynamic query builder (complex)
- `usp_findCourseID` - Course lookup/search
- `usp_VerifyEffort` - Effort verification logic

#### 8. Obsolete (2 procedures) - Delete

**Strategy**: Do not migrate

- `dt_displayoaerror` - Legacy Visual Studio database designer tool
- `dt_displayoaerror_u` - Legacy Visual Studio database designer tool (Unicode)

### Migration Approach Summary

| Category | Count | Strategy | Priority |
|----------|-------|----------|----------|
| Read Operations | 46 | EF Core queries/API endpoints | High |
| Reporting | 14 | New reporting API (consider keeping SPs for performance) | High |
| INSERT | 6 | EF Core SaveChanges | Medium |
| UPDATE | 4 | EF Core SaveChanges | Medium |
| DELETE | 5 | EF Core Remove | Medium |
| Term Management | 4 | Service layer business logic | High |
| Specialized | 6 | Custom analysis required | Medium |
| Obsolete | 2 | Delete | N/A |
| **TOTAL** | **87** | **Active procedures** | - |

### Key Observations

**Versioning Issues**: Multiple procedures have numbered variants (`*2`, `*WithExclude`, `*WithExcludeTerms`, `*_old`, `*Backup`) indicating iterative development without cleanup. The presence of 12 variants of `EffortDeptActivityTotal` suggests this is a critical but unstable calculation that evolved over time.

**Merit Reporting Dominance**: 10 of 14 reporting procedures (71%) are merit-related, making merit/promotion reviews the primary reporting use case.

**External Dependencies**: Some procedures use:
- `OPENQUERY` to Oracle UCDBanner linked server
- LEFT OUTER JOINs to `courses` database (Banner catalog)
- Function calls to `dictionary` database (employee titles)

**Performance Considerations**: Complex aggregation procedures (department activity totals, merit reports) may benefit from remaining as stored procedures rather than converting to LINQ, as they can be 10-30x faster for complex calculations.

---

## Database Architecture

**Modern schema**: `[VIPER].[effort]` (within VIPER database, not separate)
**Shadow schema**: `[VIPER].[EffortShadow]` (schema within VIPER database with views for ColdFusion)

**Connection**:
- VIPER2: Connects to VIPER database, uses [effort] schema
- ColdFusion: Connects to VIPER database, uses [EffortShadow] schema views

---

## External Dependencies

**courses database** (Banner catalog):
- LEFT OUTER JOIN in some views
- Provides course titles and metadata

**dictionary database** (employee titles):
- Function calls for title lookups

**UCDBanner linked server** (Oracle):
- OPENQUERY in one stored procedure
- Banner integration for course imports
