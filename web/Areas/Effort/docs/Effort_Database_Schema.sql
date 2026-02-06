/*
================================================================================
EFFORT SYSTEM DATABASE SCHEMA
================================================================================
Purpose: Complete database schema for the modernized Effort reporting system
Database: VIPER (schema: effort)
Generated from: CreateEffortDatabase.cs
Date: 2025-11-06

Architecture:
- Creates [VIPER].[effort] schema (NOT a separate database)
- Shadow Schema: [VIPER].[EffortShadow] schema with views pointing to [effort] tables
- 16 tables total with proper dependency ordering

Tables:
1. Lookup Tables (no dependencies): Roles, PercentAssignTypes, EffortTypes, Units,
   JobCodes, ReportUnits
2. Term and Course Tables: TermStatus, Courses
3. Person Tables: Persons, AlternateTitles, UserAccess
4. Main Data Tables: Records, Percentages, Sabbaticals
5. Relationship and Audit Tables: CourseRelationships, Audits

NOTE: This file is FOR DOCUMENTATION AND REVIEW ONLY.
      The actual schema is created by running: dotnet script CreateEffortDatabase.cs

To update this file, run: dotnet script CreateEffortDatabase.cs --export-schema

================================================================================
*/

-- ============================================================================
-- SCHEMA CREATION
-- ============================================================================

USE [VIPER]
GO

-- Create the effort schema in VIPER database
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'effort')
BEGIN
    EXEC('CREATE SCHEMA [effort]');
END
GO

-- ============================================================================
-- LOOKUP TABLES (No Dependencies)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: Roles
-- Description: Faculty role types for course instruction
-- Legacy: tblRoles
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[Roles] (
    Id int NOT NULL,
    Description varchar(50) NOT NULL,  -- Matches legacy tblRoles.Role_Desc
    IsActive bit NOT NULL DEFAULT 1,
    SortOrder int NULL,
    CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id)
);
GO

-- ----------------------------------------------------------------------------
-- Table: PercentAssignTypes
-- Description: Types of percent assignments (Clinical, Admin, Other)
-- Legacy: tblEffortType_LU (renamed from EffortTypes to PercentAssignTypes)
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[PercentAssignTypes] (
    Id int IDENTITY(1,1) NOT NULL,
    Class varchar(20) NOT NULL,
    Name varchar(50) NOT NULL,
    ShowOnTemplate bit NOT NULL DEFAULT 1,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_PercentAssignTypes PRIMARY KEY CLUSTERED (Id)
);

SET IDENTITY_INSERT [effort].[PercentAssignTypes] ON;
INSERT INTO [effort].[PercentAssignTypes] (Id, Class, Name, ShowOnTemplate) VALUES
(1, 'Clinical', 'Clinical', 1),
(2, 'Admin', 'Dean', 1),
(3, 'Admin', 'Assoc Dean', 1),
(4, 'Admin', 'Director', 1),
(5, 'Other', 'None', 1),
(6, 'Admin', 'None', 1),
(7, 'Clinical', 'None', 1),
(8, 'Admin', 'Co-Director', 1),
(9, 'Admin', 'Exec Director', 1),
(10, 'Admin', 'Assoc Director', 1),
(11, 'Admin', 'Exec Assoc Dean', 1),
(12, 'Admin', 'Dept Chair', 1),
(13, 'Admin', 'Dept Vice Chair', 1),
(14, 'Admin', 'Chair', 1),
(15, 'Admin', 'Officer', 1),
(16, 'Admin', 'Service Chief', 1),
(17, 'Admin', 'Branch Chief', 1),
(18, 'Admin', 'Section Head', 1),
(19, 'Admin', 'Graduate Group Chair', 1),
(20, 'Admin', 'Graduate Group Advisor', 1),
(21, 'Admin', 'Unknown', 1),
(22, 'Admin', 'Associate Vice Provost', 1),
(23, 'Admin', 'Faculty Assistant', 1),
(24, 'Admin', 'Faculty Chair', 1),
(25, 'Clinical', 'Non VMTH Clinical', 0),
(26, 'Admin', 'Asst Director', 1);
SET IDENTITY_INSERT [effort].[PercentAssignTypes] OFF;
GO

-- ----------------------------------------------------------------------------
-- Table: EffortTypes
-- Description: Effort types for course records (Lecture, Lab, Clinical, etc.)
-- Legacy: SessionTypes table (renamed from SessionTypes to EffortTypes)
-- Note: CLI (Clinical) is the ONLY effort type that uses weeks instead of hours
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[EffortTypes] (
    Id varchar(3) NOT NULL,
    Description varchar(50) NOT NULL,
    UsesWeeks bit NOT NULL DEFAULT 0,
    IsActive bit NOT NULL DEFAULT 1,
    FacultyCanEnter bit NOT NULL DEFAULT 1,   -- Faculty/instructors can add this type themselves
    AllowedOnDvm bit NOT NULL DEFAULT 1,      -- Allowed on DVM courses
    AllowedOn199299 bit NOT NULL DEFAULT 1,   -- Allowed on 199/299 courses
    AllowedOnRCourses bit NOT NULL DEFAULT 1, -- Allowed on R courses
    CONSTRAINT PK_EffortTypes PRIMARY KEY CLUSTERED (Id)
);

-- Descriptions from CREST tbl_sessiontype (legacy source of truth)
-- CLI is the ONLY effort type that uses weeks instead of hours
INSERT INTO [effort].[EffortTypes] (Id, Description, UsesWeeks) VALUES
('ACT', 'Activity', 0),
('AUT', 'Autotutorial', 0),
('CBL', 'Case Based Learning', 0),
('CLI', 'Clinical Activity', 1),   -- ONLY CLI uses weeks
('CON', 'Conference', 0),
('D/L', 'Discussion/Laboratory', 0),
('DIS', 'Discussion', 0),
('DSL', 'Directed Self Learning', 0),
('EXM', 'Examination', 0),
('FAS', 'Formative Assessment', 0),
('FWK', 'Fieldwork', 0),
('IND', 'Independent Study', 0),
('INT', 'Internship', 0),
('JLC', 'Journal Club', 0),
('L/D', 'Laboratory/Discussion', 0),
('LAB', 'Laboratory', 0),
('LEC', 'Lecture', 0),
('LED', 'Lecture/Discussion', 0),
('LIS', 'Listening', 0),
('LLA', 'Lecture/Laboratory', 0),
('PBL', 'Problem Based Learning', 0),
('PER', 'Performance Instruction', 0),
('PRA', 'Practice', 0),
('PRB', 'Extensive Problem Solving', 0),
('PRJ', 'Project', 0),
('PRS', 'Presentation', 0),
('SEM', 'Seminar', 0),
('STD', 'Studio', 0),
('T-D', 'Term Paper or Discussion', 0),
('TBL', 'Team Based Learning', 0),
('TMP', 'Term Paper', 0),
('TUT', 'Tutorial', 0),
('VAR', 'Variable', 0),
('WED', 'Web Electronic Discussion', 0),
('WRK', 'Workshop', 0),
('WVL', 'Web Virtual Lecture', 0);
GO

-- ----------------------------------------------------------------------------
-- Table: Units
-- Description: Administrative units for effort allocation
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[Units] (
    Id int IDENTITY(1,1) NOT NULL,
    Name varchar(20) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_Units PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Units_Name UNIQUE (Name)
);
GO

-- ----------------------------------------------------------------------------
-- Table: JobCodes
-- Description: Employee job codes and classifications
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[JobCodes] (
    Id int IDENTITY(1,1) NOT NULL,
    Code varchar(10) NOT NULL,
    Description varchar(100) NOT NULL,
    Category varchar(50) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_JobCodes PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_JobCodes_Code UNIQUE (Code)
);
GO

-- ----------------------------------------------------------------------------
-- Table: ReportUnits
-- Description: Hierarchical reporting units for effort reports
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[ReportUnits] (
    Id int IDENTITY(1,1) NOT NULL,
    UnitCode varchar(10) NOT NULL,
    UnitName varchar(100) NOT NULL,
    ParentUnitId int NULL,
    IsActive bit NOT NULL DEFAULT 1,
    SortOrder int NULL,
    CONSTRAINT PK_ReportUnits PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ReportUnits_Parent FOREIGN KEY (ParentUnitId) REFERENCES [effort].[ReportUnits](Id),
    CONSTRAINT UQ_ReportUnits_Code UNIQUE (UnitCode)
);
GO

-- ============================================================================
-- TERM AND COURSE TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: TermStatus
-- Description: Effort-specific workflow status for academic terms
-- Note: Term data comes from VIPER.dbo.vwTerms
--       This table tracks Effort-specific workflow status only
--       Status is computed from dates via the EffortTerm.Status property (not stored):
--         - Closed: ClosedDate IS NOT NULL
--         - Opened: OpenedDate IS NOT NULL AND ClosedDate IS NULL
--         - Harvested: HarvestedDate IS NOT NULL AND OpenedDate IS NULL
--         - Created: All dates are NULL
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[TermStatus] (
    TermCode int NOT NULL,
    HarvestedDate datetime2(7) NULL,
    OpenedDate datetime2(7) NULL,
    ClosedDate datetime2(7) NULL,
    CONSTRAINT PK_TermStatus PRIMARY KEY CLUSTERED (TermCode)
);
GO

-- ----------------------------------------------------------------------------
-- Table: Courses
-- Description: Course sections for effort tracking
-- Legacy: tblCourse
-- Note: Title field not included - course titles fetched from [courses].[Catalog]
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[Courses] (
    Id int IDENTITY(1,1) NOT NULL,
    Crn char(5) NOT NULL,
    TermCode int NOT NULL,
    SubjCode char(3) NOT NULL,
    CrseNumb char(5) NOT NULL,
    SeqNumb char(3) NOT NULL,
    Enrollment int NOT NULL DEFAULT 0,
    Units decimal(4,2) NOT NULL,
    CustDept char(6) NOT NULL,
    CONSTRAINT PK_Courses PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Courses_CRN_Term_Units UNIQUE (Crn, TermCode, Units),
    CONSTRAINT FK_Courses_TermStatus FOREIGN KEY (TermCode) REFERENCES [effort].[TermStatus](TermCode),
    CONSTRAINT CK_Courses_Enrollment CHECK (Enrollment >= 0),
    CONSTRAINT CK_Courses_Units CHECK (Units >= 0)
);

CREATE NONCLUSTERED INDEX IX_Courses_TermCode ON [effort].[Courses](TermCode);
CREATE NONCLUSTERED INDEX IX_Courses_CRN ON [effort].[Courses](Crn);
CREATE NONCLUSTERED INDEX IX_Courses_CustDept ON [effort].[Courses](CustDept);
CREATE NONCLUSTERED INDEX IX_Courses_SubjCode_CrseNumb ON [effort].[Courses](SubjCode, CrseNumb);
GO

-- ============================================================================
-- PERSON TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: Persons
-- Description: Faculty/staff participating in effort reporting (per-term snapshot)
-- Legacy: tblPersons
-- Note: Composite primary key (PersonId, TermCode)
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[Persons] (
    PersonId int NOT NULL,
    TermCode int NOT NULL,
    FirstName varchar(50) NOT NULL,
    LastName varchar(50) NOT NULL,
    MiddleInitial varchar(1) NULL,
    EffortTitleCode char(6) NOT NULL,
    EffortDept varchar(6) NOT NULL,
    PercentAdmin decimal(5,2) NOT NULL DEFAULT 0,
    JobGroupId char(3) NULL,
    Title varchar(50) NULL,
    AdminUnit varchar(25) NULL,
    EffortVerified datetime2(7) NULL,
    ReportUnit varchar(50) NULL,
    VolunteerWos tinyint NULL,
    PercentClinical decimal(5,2) NULL,
    LastEmailed datetime2(7) NULL,
    LastEmailedBy int NULL,
    CONSTRAINT PK_Persons PRIMARY KEY CLUSTERED (PersonId, TermCode),
    CONSTRAINT FK_Persons_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
    CONSTRAINT FK_Persons_TermStatus FOREIGN KEY (TermCode) REFERENCES [effort].[TermStatus](TermCode),
    CONSTRAINT FK_Persons_LastEmailedBy FOREIGN KEY (LastEmailedBy) REFERENCES [users].[Person](PersonId),
    CONSTRAINT CK_Persons_PercentAdmin CHECK (PercentAdmin BETWEEN 0 AND 100),
    CONSTRAINT CK_Persons_PercentClinical CHECK (PercentClinical IS NULL OR PercentClinical BETWEEN 0 AND 100)
);

CREATE NONCLUSTERED INDEX IX_Persons_LastName_FirstName ON [effort].[Persons](LastName, FirstName);
CREATE NONCLUSTERED INDEX IX_Persons_EffortDept ON [effort].[Persons](EffortDept);
CREATE NONCLUSTERED INDEX IX_Persons_TermCode ON [effort].[Persons](TermCode);
GO

-- ----------------------------------------------------------------------------
-- Table: AlternateTitles
-- Description: Time-bound alternate job titles for faculty/staff
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[AlternateTitles] (
    Id int IDENTITY(1,1) NOT NULL,
    PersonId int NOT NULL,
    AlternateTitle varchar(100) NOT NULL,
    EffectiveDate date NOT NULL,
    ExpirationDate date NULL,
    ModifiedDate datetime2(7) NULL,
    ModifiedBy int NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_AlternateTitles PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_AlternateTitles_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
    CONSTRAINT FK_AlternateTitles_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId),
    CONSTRAINT CK_AlternateTitles_DateRange CHECK (ExpirationDate IS NULL OR ExpirationDate >= EffectiveDate)
);

CREATE NONCLUSTERED INDEX IX_AlternateTitles_PersonId ON [effort].[AlternateTitles](PersonId) WHERE IsActive = 1;
GO

-- ----------------------------------------------------------------------------
-- Table: UserAccess
-- Description: User access control for department-level effort data
-- CRITICAL: Required for security and data access control
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[UserAccess] (
    Id int IDENTITY(1,1) NOT NULL,
    PersonId int NOT NULL,
    DepartmentCode char(6) NOT NULL,
    ModifiedDate datetime2(7) NULL,
    ModifiedBy int NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CONSTRAINT PK_UserAccess PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_UserAccess_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
    CONSTRAINT FK_UserAccess_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId),
    CONSTRAINT UQ_UserAccess_Person_Dept UNIQUE (PersonId, DepartmentCode)
);

CREATE NONCLUSTERED INDEX IX_UserAccess_PersonId ON [effort].[UserAccess](PersonId) WHERE IsActive = 1;
CREATE NONCLUSTERED INDEX IX_UserAccess_DeptCode ON [effort].[UserAccess](DepartmentCode) WHERE IsActive = 1;
GO

-- ============================================================================
-- MAIN DATA TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: Records
-- Description: Core effort reporting records (teaching hours/weeks)
-- Legacy: tblEffort
-- Note: Either Hours OR Weeks must be populated, but not both
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[Records] (
    Id int IDENTITY(1,1) NOT NULL,
    CourseId int NOT NULL,
    PersonId int NOT NULL,
    TermCode int NOT NULL,
    EffortTypeId varchar(3) NOT NULL,
    RoleId int NOT NULL,
    Hours int NULL,
    Weeks int NULL,
    Crn varchar(5) NOT NULL,
    ModifiedDate datetime2(7) NULL,
    ModifiedBy int NULL,
    CONSTRAINT PK_Records PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Records_Courses FOREIGN KEY (CourseId) REFERENCES [effort].[Courses](Id),
    CONSTRAINT FK_Records_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
    CONSTRAINT FK_Records_Persons FOREIGN KEY (PersonId, TermCode) REFERENCES [effort].[Persons](PersonId, TermCode),
    CONSTRAINT FK_Records_TermStatus FOREIGN KEY (TermCode) REFERENCES [effort].[TermStatus](TermCode),
    CONSTRAINT FK_Records_Roles FOREIGN KEY (RoleId) REFERENCES [effort].[Roles](Id),
    CONSTRAINT FK_Records_EffortTypes FOREIGN KEY (EffortTypeId) REFERENCES [effort].[EffortTypes](Id),
    CONSTRAINT FK_Records_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId),
    CONSTRAINT CK_Records_HoursOrWeeks CHECK ((Hours IS NOT NULL AND Weeks IS NULL) OR (Hours IS NULL AND Weeks IS NOT NULL)),
    CONSTRAINT CK_Records_Hours CHECK (Hours IS NULL OR (Hours >= 0 AND Hours <= 2500)),
    CONSTRAINT CK_Records_Weeks CHECK (Weeks IS NULL OR (Weeks > 0 AND Weeks <= 52))
);

CREATE NONCLUSTERED INDEX IX_Records_PersonId_TermCode ON [effort].[Records](PersonId, TermCode);
CREATE NONCLUSTERED INDEX IX_Records_CourseId ON [effort].[Records](CourseId);
CREATE NONCLUSTERED INDEX IX_Records_TermCode ON [effort].[Records](TermCode);
CREATE NONCLUSTERED INDEX IX_Records_ModifiedDate ON [effort].[Records](ModifiedDate);

-- Prevent duplicate effort records per course/person/effort type
CREATE UNIQUE INDEX [UQ_Records_Course_Person_EffortType]
ON [effort].[Records] ([CourseId], [PersonId], [EffortTypeId]);
GO

-- ----------------------------------------------------------------------------
-- Table: Percentages
-- Description: Administrative and clinical effort percentages by time period
-- Legacy: tblPercentage (normalized structure)
-- Note: UnitId replaces legacy varchar percent_Unit (shadow schema handles conversion)
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[Percentages] (
    Id int IDENTITY(1,1) NOT NULL,
    PersonId int NOT NULL,
    AcademicYear char(9) NOT NULL,  -- Format: 'YYYY-YYYY' (e.g., '2019-2020'), derived from StartDate if missing
    Percentage float NOT NULL,  -- Match legacy float(53)
    PercentAssignTypeId int NOT NULL,
    UnitId int NULL,  -- FK to Units table
    Modifier varchar(50) NULL,
    Comment varchar(100) NULL,
    StartDate datetime2(7) NOT NULL,
    EndDate datetime2(7) NULL,
    ModifiedDate datetime2(7) NULL,
    ModifiedBy int NULL,
    Compensated bit NOT NULL DEFAULT 0,
    CONSTRAINT PK_Percentages PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Percentages_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
    CONSTRAINT FK_Percentages_PercentAssignTypes FOREIGN KEY (PercentAssignTypeId) REFERENCES [effort].[PercentAssignTypes](Id),
    CONSTRAINT FK_Percentages_Units FOREIGN KEY (UnitId) REFERENCES [effort].[Units](Id),
    CONSTRAINT FK_Percentages_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId),
    CONSTRAINT CK_Percentages_Percentage CHECK (Percentage BETWEEN 0 AND 1),
    CONSTRAINT CK_Percentages_DateRange CHECK (EndDate IS NULL OR EndDate >= StartDate)
);

CREATE NONCLUSTERED INDEX IX_Percentages_PersonId ON [effort].[Percentages](PersonId);
CREATE NONCLUSTERED INDEX IX_Percentages_AcademicYear ON [effort].[Percentages](AcademicYear);
CREATE NONCLUSTERED INDEX IX_Percentages_StartDate ON [effort].[Percentages](StartDate);
CREATE NONCLUSTERED INDEX IX_Percentages_EndDate ON [effort].[Percentages](EndDate) WHERE EndDate IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Percentages_UnitId ON [effort].[Percentages](UnitId) WHERE UnitId IS NOT NULL;
GO

-- ----------------------------------------------------------------------------
-- Table: Sabbaticals
-- Description: Sabbatical leave tracking for effort exclusions
-- Legacy: tblSabbaticals
-- Note: Term codes stored as comma-separated lists per exclusion type
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[Sabbaticals] (
    Id int IDENTITY(1,1) NOT NULL,
    PersonId int NOT NULL,
    ExcludeClinicalTerms varchar(2000) NULL,
    ExcludeDidacticTerms varchar(2000) NULL,
    ModifiedDate datetime2(7) NULL,
    ModifiedBy int NULL,
    CONSTRAINT PK_Sabbaticals PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Sabbaticals_Person FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
    CONSTRAINT FK_Sabbaticals_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId)
);

CREATE NONCLUSTERED INDEX IX_Sabbaticals_PersonId ON [effort].[Sabbaticals](PersonId);
GO

-- ============================================================================
-- RELATIONSHIP AND AUDIT TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: CourseRelationships
-- Description: Cross-listed and parent-child course relationships
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[CourseRelationships] (
    Id int IDENTITY(1,1) NOT NULL,
    ParentCourseId int NOT NULL,
    ChildCourseId int NOT NULL,
    RelationshipType varchar(20) NOT NULL,
    CONSTRAINT PK_CourseRelationships PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CourseRelationships_Parent FOREIGN KEY (ParentCourseId) REFERENCES [effort].[Courses](Id),
    CONSTRAINT FK_CourseRelationships_Child FOREIGN KEY (ChildCourseId) REFERENCES [effort].[Courses](Id),
    CONSTRAINT UQ_CourseRelationships UNIQUE (ParentCourseId, ChildCourseId),
    CONSTRAINT CK_CourseRelationships_Type CHECK (RelationshipType IN ('CrossList', 'Section'))
);
GO

-- Unique index to ensure each child course can only have one parent
CREATE UNIQUE NONCLUSTERED INDEX IX_CourseRelationships_ChildCourseId
ON [effort].[CourseRelationships](ChildCourseId);
GO

-- ----------------------------------------------------------------------------
-- Table: Audits
-- Description: Comprehensive audit trail for all effort data changes
-- Legacy: tblAudit
--
-- Legacy preservation columns (LegacyAction, LegacyCRN, LegacyMothraID)
-- enable 1:1 verification against legacy tblAudit during migration.
-- These can be dropped after ColdFusion is decommissioned.
-- TermCode provides term context for audit records.
-- ----------------------------------------------------------------------------
CREATE TABLE [effort].[Audits] (
    Id int IDENTITY(1,1) NOT NULL,
    TableName varchar(50) NOT NULL,
    RecordId int NOT NULL,
    Action varchar(50) NOT NULL,           -- Granular action names (e.g., CreateEffort, UpdateCourse)
    ChangedBy int NOT NULL,
    ChangedDate datetime2(7) NOT NULL DEFAULT GETDATE(),
    Changes nvarchar(MAX) NULL,            -- Audit text (legacy plain-text or JSON)
    MigratedDate datetime2(7) NULL,
    UserAgent varchar(500) NULL,
    IpAddress varchar(50) NULL,
    -- Legacy preservation columns (for 1:1 verification against legacy tblAudit)
    LegacyAction varchar(100) NULL,        -- Original action text (e.g., 'CreateCourse')
    LegacyCRN varchar(20) NULL,            -- Original audit_CRN
    LegacyMothraID varchar(20) NULL,       -- Original audit_MothraID
    -- Term context for audit record
    TermCode int NULL,
    CONSTRAINT PK_Audits PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Audits_ChangedBy FOREIGN KEY (ChangedBy) REFERENCES [users].[Person](PersonId)
);

CREATE NONCLUSTERED INDEX IX_Audits_TableName_RecordId ON [effort].[Audits](TableName, RecordId);
CREATE NONCLUSTERED INDEX IX_Audits_ChangedDate ON [effort].[Audits](ChangedDate DESC);
CREATE NONCLUSTERED INDEX IX_Audits_ChangedBy ON [effort].[Audits](ChangedBy);
CREATE NONCLUSTERED INDEX IX_Audits_TermCode ON [effort].[Audits](TermCode) WHERE TermCode IS NOT NULL;
GO

-- ============================================================================
-- SCHEMA CREATION COMPLETE
-- ============================================================================

PRINT '================================================================================';
PRINT 'EFFORT SCHEMA CREATION COMPLETE';
PRINT '================================================================================';
PRINT '';
PRINT 'Tables Created: 16';
PRINT '';
PRINT 'Lookup Tables (6): Roles, PercentAssignTypes, EffortTypes, Units, JobCodes,';
PRINT '                   ReportUnits';
PRINT '';
PRINT 'Term/Course Tables (2): TermStatus, Courses';
PRINT '';
PRINT 'Person Tables (3): Persons, AlternateTitles, UserAccess';
PRINT '';
PRINT 'Main Data Tables (3): Records, Percentages, Sabbaticals';
PRINT '';
PRINT 'Relationship/Audit Tables (2): CourseRelationships, Audits';
PRINT '';
PRINT 'Seed Data Inserted:';
PRINT '  - Roles: 3 rows';
PRINT '  - PercentAssignTypes: 26 rows';
PRINT '  - EffortTypes: 35 rows';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Run MigrateEffortData.cs to migrate data from legacy database';
PRINT '  2. Run CreateEffortShadow.cs to create shadow schema for ColdFusion';
PRINT '  3. DBA will configure database permissions for applications';
PRINT '';
PRINT '================================================================================';
GO
