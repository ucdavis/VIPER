-- =================================================================
-- Effort Database Data Migration Script
-- Project: VIPER2 Effort System Migration
-- Purpose: Migrate data from Efforts database to VIPER database
-- Target: VIPER database with effort schema
-- Implementation: Sprint 14 - Data Migration & Validation
-- Date: September 18, 2025
-- =================================================================

-- IMPORTANT: This script migrates data from Efforts database to VIPER database
-- Prerequisites:
-- 1. Entity Framework migrations have been applied to create schema (Sprint 1)
-- 2. All Sprint 1-13 features completed and tested
-- 3. Both databases are accessible from this session
-- 4. Full backup of both databases has been taken

USE [VIPER]
GO

PRINT 'Starting Effort Data Migration from Efforts Database to VIPER Database...'
PRINT 'Migration Start Time: ' + CONVERT(VARCHAR(23), GETDATE(), 121)
PRINT ''

-- =================================================================
-- VALIDATION: Check Prerequisites
-- =================================================================

PRINT '=================================================='
PRINT 'PREREQUISITE VALIDATION'
PRINT '=================================================='

-- Check if source database exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Effort')
BEGIN
    PRINT 'ERROR: Source database [Effort] not found!'
    PRINT 'Please ensure the Efforts database is accessible.'
    RETURN
END
ELSE
    PRINT '✓ Source database [Effort] found'

-- Check if target schema exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = 'effort')
BEGIN
    PRINT 'ERROR: Target schema [effort] not found in VIPER database!'
    PRINT 'Please run Entity Framework migrations first (Sprint 1).'
    RETURN
END
ELSE
    PRINT '✓ Target schema [effort] found in VIPER database'

-- Check if target tables exist
DECLARE @TableCount INT
SELECT @TableCount = COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'effort'
  AND TABLE_NAME IN ('Roles', 'Terms', 'Courses', 'Persons', 'Records')

IF @TableCount < 5
BEGIN
    PRINT 'ERROR: Required target tables not found!'
    PRINT 'Expected: effort.Roles, effort.Terms, effort.Courses, effort.Persons, effort.Records'
    PRINT 'Please ensure Entity Framework migrations have been applied (Sprint 1).'
    RETURN
END
ELSE
    PRINT '✓ Target tables found: ' + CAST(@TableCount AS VARCHAR(10)) + ' core tables'

PRINT 'Prerequisites validated successfully!'
PRINT ''

-- =================================================================
-- SPRINT 14 STEP 1: REFERENCE DATA MIGRATION
-- =================================================================

PRINT '=================================================='
PRINT 'SPRINT 14 STEP 1: REFERENCE DATA MIGRATION'
PRINT '=================================================='

-- 1.1 Migrate Effort Roles
PRINT 'Migrating Effort Roles...'
INSERT INTO [effort].[Roles] (Id, Description)
SELECT Role_ID, Role_Desc
FROM [Effort].[dbo].[tblRoles]
WHERE NOT EXISTS (
    SELECT 1 FROM [effort].[Roles]
    WHERE Id = Role_ID
);

DECLARE @RoleCount INT = @@ROWCOUNT
PRINT '✓ Migrated ' + CAST(@RoleCount AS VARCHAR(10)) + ' effort roles'

-- 1.2 Migrate Session Types (Reference data from legacy effort types)
PRINT 'Migrating Session Types...'
IF EXISTS (SELECT * FROM [Effort].[INFORMATION_SCHEMA].[TABLES] WHERE TABLE_NAME = 'tblEffortType_LU')
BEGIN
    INSERT INTO [effort].[SessionTypes] (Id, Description)
    SELECT effortType_ID, effortType_Desc
    FROM [Effort].[dbo].[tblEffortType_LU]
    WHERE NOT EXISTS (
        SELECT 1 FROM [effort].[SessionTypes]
        WHERE Id = effortType_ID
    );

    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' session types'
END

-- 1.3 Migrate Units Lookup
PRINT 'Migrating Effort Units...'
IF EXISTS (SELECT * FROM [Effort].[INFORMATION_SCHEMA].[TABLES] WHERE TABLE_NAME = 'tblUnits_LU')
BEGIN
    INSERT INTO [Effort].[EffortUnits] (Id, Name)
    SELECT unit_ID, unit_Name
    FROM [Effort].[dbo].[tblUnits_LU]
    WHERE NOT EXISTS (
        SELECT 1 FROM [Effort].[EffortUnits]
        WHERE Id = unit_ID
    );

    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' effort units'
END

-- 1.4 Migrate Job Codes
PRINT 'Migrating Job Codes...'
IF EXISTS (SELECT * FROM [Effort].[INFORMATION_SCHEMA].[TABLES] WHERE TABLE_NAME = 'tblJobCode')
BEGIN
    INSERT INTO [Effort].[EffortJobCodes] (Id, Code, Description)
    SELECT jobCode_ID, jobCode_Code, jobCode_Desc
    FROM [Effort].[dbo].[tblJobCode]
    WHERE NOT EXISTS (
        SELECT 1 FROM [Effort].[EffortJobCodes]
        WHERE Id = jobCode_ID
    );

    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' job codes'
END

PRINT 'Sprint 14 Step 1 Complete: Reference Data Migration'
PRINT ''

-- =================================================================
-- SPRINT 14 STEP 2: TERMS MIGRATION
-- =================================================================

PRINT '=================================================='
PRINT 'SPRINT 14 STEP 2: TERMS MIGRATION'
PRINT '=================================================='

PRINT 'Migrating Terms (Status)...'
INSERT INTO [effort].[Terms] (
    TermCode, TermName, AcademicYear, Status,
    HarvestedDate, OpenedDate, ClosedDate, CreatedDate, ModifiedDate
)
SELECT
    status_TermCode,
    status_TermName,
    status_AcademicYear,
    CASE
        WHEN status_Closed IS NOT NULL THEN 'Closed'
        WHEN status_Opened IS NOT NULL THEN 'Opened'
        WHEN status_Harvested IS NOT NULL THEN 'Harvested'
        ELSE 'Created'
    END as Status,
    status_Harvested,
    status_Opened,
    status_Closed,
    COALESCE(status_Harvested, GETDATE()) as CreatedDate,
    GETDATE() as ModifiedDate
FROM [Effort].[dbo].[tblStatus]
WHERE NOT EXISTS (
    SELECT 1 FROM [effort].[Terms]
    WHERE TermCode = status_TermCode
);

DECLARE @TermCount INT = @@ROWCOUNT
PRINT '✓ Migrated ' + CAST(@TermCount AS VARCHAR(10)) + ' terms'

PRINT 'Sprint 14 Step 2 Complete: Terms Migration'
PRINT ''

-- =================================================================
-- SPRINT 14 STEP 3: COURSES MIGRATION
-- =================================================================

PRINT '=================================================='
PRINT 'SPRINT 14 STEP 3: COURSES MIGRATION'
PRINT '=================================================='

PRINT 'Migrating Courses...'
INSERT INTO [effort].[Courses] (
    Id, Crn, TermCode, SubjCode, CrseNumb, SeqNumb,
    Enrollment, Units, CustDept, CreatedDate, ModifiedDate
)
SELECT
    course_id,
    course_CRN,
    course_TermCode,
    course_SubjCode,
    course_CrseNumb,
    course_SeqNumb,
    course_Enrollment,
    course_Units,
    course_CustDept,
    GETDATE() as CreatedDate,
    GETDATE() as ModifiedDate
FROM [Effort].[dbo].[tblCourses]
WHERE NOT EXISTS (
    SELECT 1 FROM [effort].[Courses]
    WHERE Id = course_id
);

DECLARE @CourseCount INT = @@ROWCOUNT
PRINT '✓ Migrated ' + CAST(@CourseCount AS VARCHAR(10)) + ' courses'

-- Migrate Course Relationships
PRINT 'Migrating Course Relationships...'
IF EXISTS (SELECT * FROM [Effort].[INFORMATION_SCHEMA].[TABLES] WHERE TABLE_NAME = 'tblCourseRelationships')
BEGIN
    INSERT INTO [effort].[CourseRelationships] (
        Id, ParentCourseId, ChildCourseId, RelationshipType, CreatedDate
    )
    SELECT
        rel_ID,
        rel_ParentCourseID,
        rel_ChildCourseID,
        'Parent-Child' as RelationshipType,
        GETDATE() as CreatedDate
    FROM [Effort].[dbo].[tblCourseRelationships]
    WHERE NOT EXISTS (
        SELECT 1 FROM [effort].[CourseRelationships]
        WHERE Id = rel_ID
    );

    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' course relationships'
END

PRINT 'Sprint 14 Step 3 Complete: Courses Migration'
PRINT ''

-- =================================================================
-- SPRINT 14 STEP 4: PERSONS MIGRATION
-- =================================================================

PRINT '=================================================='
PRINT 'SPRINT 14 STEP 4: PERSONS MIGRATION'
PRINT '=================================================='

PRINT 'Migrating Persons...'
INSERT INTO [effort].[Persons] (
    PersonId, TermCode, FirstName, LastName, MiddleInitial,
    EffortTitleCode, EffortDept, PercentAdmin, JobGroupId, Title,
    AdminUnit, ClientId, EffortVerified, ReportUnit, VolunteerWos,
    PercentClinical, CreatedDate, ModifiedDate
)
SELECT
    p.PersonId,  -- Map MothraID to PersonId from VIPER.users.Person
    person_TermCode,
    person_FirstName,
    person_LastName,
    person_MiddleIni,
    person_EffortTitleCode,
    person_EffortDept,
    person_PercentAdmin,
    person_JobGrpID,
    person_Title,
    person_AdminUnit,
    person_ClientID,
    person_EffortVerified,
    person_ReportUnit,
    person_Volunteer_WOS,
    person_PercentClinical,
    GETDATE() as CreatedDate,
    GETDATE() as ModifiedDate
FROM [Effort].[dbo].[tblPerson] ep
INNER JOIN [VIPER].[users].[Person] p ON ep.person_MothraID = p.MothraId
WHERE NOT EXISTS (
    SELECT 1 FROM [effort].[Persons] per
    WHERE per.PersonId = p.PersonId
      AND per.TermCode = ep.person_TermCode
);

DECLARE @PersonCount INT = @@ROWCOUNT
PRINT '✓ Migrated ' + CAST(@PersonCount AS VARCHAR(10)) + ' persons'

PRINT 'Sprint 14 Step 4 Complete: Persons Migration'
PRINT ''

-- =================================================================
-- SPRINT 14 STEP 5: EFFORT RECORDS MIGRATION
-- =================================================================

PRINT '=================================================='
PRINT 'SPRINT 14 STEP 5: EFFORT RECORDS MIGRATION'
PRINT '=================================================='

PRINT 'Migrating Effort Records...'
INSERT INTO [effort].[Records] (
    Id, CourseId, PersonId, TermCode, SessionType, Role,
    Hours, Weeks, ClientId, Crn, CreatedDate, ModifiedDate, ModifiedBy
)
SELECT
    e.effort_ID,
    e.effort_CourseID,
    p.PersonId,  -- Map MothraID to PersonId from VIPER.users.Person
    effort_termCode,
    effort_SessionType,
    effort_Role,
    effort_Hours,
    effort_Weeks,
    effort_CRN,
    GETDATE() as CreatedDate,
    GETDATE() as ModifiedDate,
    (SELECT PersonId FROM [VIPER].[users].[Person] WHERE MothraId = 'DATAMIGR') as ModifiedBy  -- Use migration user PersonId
FROM [Effort].[dbo].[tblEffort] e
INNER JOIN [VIPER].[users].[Person] p ON e.effort_MothraID = p.MothraId
WHERE NOT EXISTS (
    SELECT 1 FROM [effort].[Records] r
    WHERE r.Id = e.effort_ID
);

DECLARE @EffortCount INT = @@ROWCOUNT
PRINT '✓ Migrated ' + CAST(@EffortCount AS VARCHAR(10)) + ' effort records'

PRINT 'Sprint 14 Step 5 Complete: Effort Records Migration'
PRINT ''

-- =================================================================
-- SPRINT 14 STEP 6: PERCENTAGE ASSIGNMENTS MIGRATION
-- =================================================================

PRINT '=================================================='
PRINT 'SPRINT 14 STEP 6: PERCENTAGE ASSIGNMENTS MIGRATION'
PRINT '=================================================='

PRINT 'Migrating Percentage Assignments...'
IF EXISTS (SELECT * FROM [Effort].[INFORMATION_SCHEMA].[TABLES] WHERE TABLE_NAME = 'tblPercent')
BEGIN
    INSERT INTO [effort].[Percentages] (
        Id, PersonId, TermCode, EffortType, Percentage, Unit,
        StartDate, EndDate, CreatedDate, ModifiedDate
    )
    SELECT
        percent_ID,
        p.PersonId,  -- Map MothraID to PersonId from VIPER.users.Person
        percent_TermCode,
        percent_EffortType,
        percent_Percent,
        percent_Unit,
        percent_StartDate,
        percent_EndDate,
        GETDATE() as CreatedDate,
        GETDATE() as ModifiedDate
    FROM [Effort].[dbo].[tblPercent] perc
    INNER JOIN [VIPER].[users].[Person] p ON perc.percent_MothraID = p.MothraId
    WHERE NOT EXISTS (
        SELECT 1 FROM [effort].[Percentages]
        WHERE Id = perc.percent_ID
    );

    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' percentage assignments'
END

-- =================================================================
-- SPRINT 14 STEP 7: SABBATICAL DATA MIGRATION
-- =================================================================

PRINT '=================================================='
PRINT 'SPRINT 14 STEP 7: SABBATICAL DATA MIGRATION'
PRINT '=================================================='

PRINT 'Migrating Sabbatical Records...'
IF EXISTS (SELECT * FROM [Effort].[INFORMATION_SCHEMA].[TABLES] WHERE TABLE_NAME = 'tblSabbatic')
BEGIN
    INSERT INTO [effort].[Sabbaticals] (
        PersonId, ExcludeClinicalTerms, ExcludeDidacticTerms,
        CreatedDate, ModifiedDate, ModifiedBy
    )
    SELECT
        p.PersonId,  -- Map MothraID to PersonId from VIPER.users.Person
        sab.sab_ExcludeClinTerms,
        sab.sab_ExcludeDidacticTerms,
        GETDATE() as CreatedDate,
        GETDATE() as ModifiedDate,
        (SELECT PersonId FROM [VIPER].[users].[Person] WHERE MothraId = 'DATAMIGR') as ModifiedBy
    FROM [Effort].[dbo].[tblSabbatic] sab
    INNER JOIN [VIPER].[users].[Person] p ON sab.sab_MothraID = p.MothraId
    WHERE NOT EXISTS (
        SELECT 1 FROM [effort].[Sabbaticals] s
        WHERE s.PersonId = p.PersonId
    );

    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' sabbatical records'
END
ELSE
BEGIN
    PRINT 'tblSabbatic table not found - skipping sabbatical migration'
END

PRINT 'Sprint 14 Step 7 Complete: Sabbatical Data Migration'
PRINT ''

-- =================================================================
-- SPRINT 14 STEP 8: ADDITIONAL DATA MIGRATION
-- =================================================================

PRINT '=================================================='
PRINT 'SPRINT 14 STEP 8: ADDITIONAL DATA MIGRATION'
PRINT '=================================================='

-- Migrate Additional Questions
PRINT 'Migrating Additional Questions...'
IF EXISTS (SELECT * FROM [Effort].[INFORMATION_SCHEMA].[TABLES] WHERE TABLE_NAME = 'additionalQuestion')
BEGIN
    INSERT INTO [effort].[AdditionalQuestions] (
        Id, MothraId, EffortId, Question, Value, CreatedDate, CreatedBy
    )
    SELECT
        id,
        mothraID,
        effortID,
        question,
        value,
        COALESCE(created, GETDATE()) as CreatedDate,
        COALESCE(createdBy, 'MIGRATION') as CreatedBy
    FROM [Effort].[dbo].[additionalQuestion]
    WHERE NOT EXISTS (
        SELECT 1 FROM [effort].[AdditionalQuestions]
        WHERE Id = id
    );

    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' additional questions'
END

-- Migrate Audit Records
PRINT 'Migrating Audit Records...'
IF EXISTS (SELECT * FROM [Effort].[INFORMATION_SCHEMA].[TABLES] WHERE TABLE_NAME = 'tblAudit')
BEGIN
    INSERT INTO [effort].[Audits] (
        Id, MothraId, Action, Details, CreatedDate, CreatedBy
    )
    SELECT
        audit_ID,
        audit_MothraID,
        audit_Action,
        audit_Details,
        COALESCE(audit_Date, GETDATE()) as CreatedDate,
        COALESCE(audit_User, 'MIGRATION') as CreatedBy
    FROM [Effort].[dbo].[tblAudit]
    WHERE NOT EXISTS (
        SELECT 1 FROM [effort].[Audits]
        WHERE Id = audit_ID
    );

    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' audit records'
END

-- Migrate User Access
PRINT 'Migrating User Access...'
IF EXISTS (SELECT * FROM [Effort].[INFORMATION_SCHEMA].[TABLES] WHERE TABLE_NAME = 'userAccess')
BEGIN
    INSERT INTO [Effort].[EffortUserAccess] (
        Id, MothraId, DepartmentAbbreviation, CreatedDate
    )
    SELECT
        userAccessID,
        mothraID,
        departmentAbbreviation,
        GETDATE() as CreatedDate
    FROM [Effort].[dbo].[userAccess]
    WHERE NOT EXISTS (
        SELECT 1 FROM [Effort].[EffortUserAccess]
        WHERE Id = userAccessID
    );

    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' user access records'
END

PRINT 'Sprint 14 Step 7 Complete: Additional Data Migration'
PRINT ''

-- =================================================================
-- SPRINT 14 STEP 8: DATA VALIDATION
-- =================================================================

PRINT '=================================================='
PRINT 'SPRINT 14 STEP 8: DATA VALIDATION'
PRINT '=================================================='

PRINT 'Validating migrated data...'

-- Create validation summary
CREATE TABLE #MigrationValidation (
    TableName NVARCHAR(50),
    SourceCount INT,
    TargetCount INT,
    Status NVARCHAR(20)
)

-- Validate core tables
INSERT INTO #MigrationValidation (TableName, SourceCount, TargetCount, Status)
SELECT 'EffortRoles',
       (SELECT COUNT(*) FROM [Effort].[dbo].[tblRoles]),
       (SELECT COUNT(*) FROM [effort].[Roles]),
       CASE WHEN (SELECT COUNT(*) FROM [Effort].[dbo].[tblRoles]) = (SELECT COUNT(*) FROM [Effort].[EffortRoles])
            THEN 'MATCH' ELSE 'MISMATCH' END

INSERT INTO #MigrationValidation (TableName, SourceCount, TargetCount, Status)
SELECT 'EffortTerms',
       (SELECT COUNT(*) FROM [Effort].[dbo].[tblStatus]),
       (SELECT COUNT(*) FROM [effort].[Terms]),
       CASE WHEN (SELECT COUNT(*) FROM [Effort].[dbo].[tblStatus]) = (SELECT COUNT(*) FROM [Effort].[EffortTerms])
            THEN 'MATCH' ELSE 'MISMATCH' END

INSERT INTO #MigrationValidation (TableName, SourceCount, TargetCount, Status)
SELECT 'EffortCourses',
       (SELECT COUNT(*) FROM [Effort].[dbo].[tblCourses]),
       (SELECT COUNT(*) FROM [effort].[Courses]),
       CASE WHEN (SELECT COUNT(*) FROM [Effort].[dbo].[tblCourses]) = (SELECT COUNT(*) FROM [Effort].[EffortCourses])
            THEN 'MATCH' ELSE 'MISMATCH' END

INSERT INTO #MigrationValidation (TableName, SourceCount, TargetCount, Status)
SELECT 'EffortPersons',
       (SELECT COUNT(*) FROM [Effort].[dbo].[tblPerson]),
       (SELECT COUNT(*) FROM [effort].[Persons]),
       CASE WHEN (SELECT COUNT(*) FROM [Effort].[dbo].[tblPerson]) = (SELECT COUNT(*) FROM [Effort].[EffortPersons])
            THEN 'MATCH' ELSE 'MISMATCH' END

INSERT INTO #MigrationValidation (TableName, SourceCount, TargetCount, Status)
SELECT 'EffortRecords',
       (SELECT COUNT(*) FROM [Effort].[dbo].[tblEffort]),
       (SELECT COUNT(*) FROM [effort].[Records]),
       CASE WHEN (SELECT COUNT(*) FROM [Effort].[dbo].[tblEffort]) = (SELECT COUNT(*) FROM [Effort].[EffortRecords])
            THEN 'MATCH' ELSE 'MISMATCH' END

-- Display validation results
PRINT 'Migration Validation Results:'
PRINT '=============================='
SELECT TableName, SourceCount, TargetCount, Status
FROM #MigrationValidation
ORDER BY TableName

-- Check for any mismatches
DECLARE @MismatchCount INT
SELECT @MismatchCount = COUNT(*) FROM #MigrationValidation WHERE Status = 'MISMATCH'

IF @MismatchCount > 0
BEGIN
    PRINT ''
    PRINT 'WARNING: ' + CAST(@MismatchCount AS VARCHAR(10)) + ' table(s) have count mismatches!'
    PRINT 'Please investigate before proceeding.'
END
ELSE
BEGIN
    PRINT ''
    PRINT '✓ All table counts match - validation successful!'
END

-- =================================================================
-- REFERENTIAL INTEGRITY VALIDATION
-- =================================================================

PRINT ''
PRINT 'Checking Referential Integrity...'
PRINT '================================='

-- Check for orphaned effort records (missing courses)
DECLARE @OrphanedCourses INT
SELECT @OrphanedCourses = COUNT(*)
FROM [effort].[Records] e
LEFT JOIN [effort].[Courses] c ON e.CourseId = c.Id
WHERE c.Id IS NULL

IF @OrphanedCourses > 0
    PRINT 'WARNING: ' + CAST(@OrphanedCourses AS VARCHAR(10)) + ' effort records have missing courses'
ELSE
    PRINT '✓ All effort records have valid course references'

-- Check for orphaned effort records (missing persons)
DECLARE @OrphanedPersons INT
SELECT @OrphanedPersons = COUNT(*)
FROM [effort].[Records] e
LEFT JOIN [effort].[Persons] p ON e.PersonId = p.PersonId AND e.TermCode = p.TermCode
WHERE p.PersonId IS NULL

IF @OrphanedPersons > 0
    PRINT 'WARNING: ' + CAST(@OrphanedPersons AS VARCHAR(10)) + ' effort records have missing persons'
ELSE
    PRINT '✓ All effort records have valid person references'

-- Check for invalid role references
DECLARE @InvalidRoles INT
SELECT @InvalidRoles = COUNT(*)
FROM [effort].[Records] e
LEFT JOIN [effort].[Roles] r ON e.Role = r.Id
WHERE r.Id IS NULL

IF @InvalidRoles > 0
    PRINT 'WARNING: ' + CAST(@InvalidRoles AS VARCHAR(10)) + ' effort records have invalid role references'
ELSE
    PRINT '✓ All effort records have valid role references'

-- =================================================================
-- MIGRATION COMPLETION
-- =================================================================

PRINT ''
PRINT '=================================================='
PRINT 'MIGRATION COMPLETION SUMMARY'
PRINT '=================================================='
PRINT 'Migration End Time: ' + CONVERT(VARCHAR(23), GETDATE(), 121)

-- Final summary
DECLARE @TotalSourceRecords INT = 0
DECLARE @TotalTargetRecords INT = 0

SELECT @TotalSourceRecords = SUM(SourceCount), @TotalTargetRecords = SUM(TargetCount)
FROM #MigrationValidation

PRINT ''
PRINT 'Migration Statistics:'
PRINT '===================='
PRINT 'Total Source Records: ' + CAST(@TotalSourceRecords AS VARCHAR(10))
PRINT 'Total Target Records: ' + CAST(@TotalTargetRecords AS VARCHAR(10))
PRINT 'Migration Success Rate: ' + CAST(ROUND(CAST(@TotalTargetRecords AS FLOAT) / CAST(@TotalSourceRecords AS FLOAT) * 100, 2) AS VARCHAR(10)) + '%'

-- Create migration log entry
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MigrationHistory' AND TABLE_SCHEMA = 'effort')
BEGIN
    INSERT INTO [effort].[MigrationHistory] (MigrationId, ProductVersion, AppliedDate, Details)
    VALUES ('DataMigration_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss'), '1.0.0', GETDATE(),
            'Sprint 14 data migration from Efforts database completed. ' +
            CAST(@TotalTargetRecords AS VARCHAR(10)) + ' records migrated.')
END

PRINT ''
PRINT 'Next Steps:'
PRINT '==========='
PRINT '1. Review any warnings above'
PRINT '2. Test application functionality with migrated data'
PRINT '3. Update application connection strings to remove Efforts database'
PRINT '4. Run Sprint 15 integration tests and performance validation'
PRINT '5. Plan decommissioning of Efforts database (after Sprint 16 go-live)'
PRINT ''
PRINT 'Sprint 14 data migration completed successfully!'

-- Cleanup
DROP TABLE #MigrationValidation

GO