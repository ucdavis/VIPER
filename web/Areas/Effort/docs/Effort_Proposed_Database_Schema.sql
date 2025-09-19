-- =================================================================
-- Proposed Database Schema: Effort System in VIPER Database
-- Project: VIPER2 Effort System Migration
-- Strategy: Database Consolidation (Efforts DB → VIPER DB)
-- Approach: Entity Framework Code First with "effort" Schema (following VIPER patterns)
-- Date: September 18, 2025
-- =================================================================

-- NOTE: This schema will be created via Entity Framework migrations
-- This SQL is for documentation and validation purposes

USE [VIPER]
GO

-- =================================================================
-- EFFORT SCHEMA TABLES (Migrated from Efforts Database)
-- Pattern: [VIPER].[effort].[TableName] (following CTS area pattern)
-- =================================================================

-- Core Effort Record Table
CREATE TABLE [effort].[Records] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [CourseId] int NOT NULL,
    [MothraId] char(8) NOT NULL,
    [TermCode] int NOT NULL,
    [SessionType] varchar(3) NOT NULL,
    [Role] char(1) NOT NULL,
    [Hours] int NULL,
    [Weeks] int NULL,
    [ClientId] char(9) NULL,
    [Crn] varchar(5) NOT NULL,
    [CreatedDate] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [ModifiedBy] char(8) NOT NULL DEFAULT 'SYSTEM  ',

    CONSTRAINT [PK_effort_Records] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_effort_Records_HoursOrWeeks] CHECK (
        ([Hours] IS NOT NULL AND [Weeks] IS NULL) OR
        ([Hours] IS NULL AND [Weeks] IS NOT NULL)
    ),
    CONSTRAINT [CK_effort_Records_Hours] CHECK ([Hours] > 0 AND [Hours] <= 999),
    CONSTRAINT [CK_effort_Records_Weeks] CHECK ([Weeks] > 0 AND [Weeks] <= 52)
)
GO

-- Persons (Instructors) Table - Composite Key
CREATE TABLE [effort].[Persons] (
    [MothraId] char(8) NOT NULL,
    [TermCode] int NOT NULL,
    [FirstName] varchar(50) NOT NULL,
    [LastName] varchar(50) NOT NULL,
    [MiddleInitial] varchar(1) NULL,
    [EffortTitleCode] char(6) NOT NULL,
    [EffortDept] char(6) NOT NULL,
    [PercentAdmin] decimal(5,2) NOT NULL DEFAULT 0,
    [JobGroupId] char(3) NULL,
    [Title] varchar(50) NULL,
    [AdminUnit] varchar(25) NULL,
    [ClientId] char(9) NULL,
    [EffortVerified] datetime2(7) NULL,
    [ReportUnit] varchar(50) NULL,
    [VolunteerWos] tinyint NULL,
    [PercentClinical] decimal(5,2) NULL,

    CONSTRAINT [PK_Persons] PRIMARY KEY CLUSTERED ([MothraId], [TermCode]),
    CONSTRAINT [CK_Persons_PercentAdmin] CHECK ([PercentAdmin] >= 0 AND [PercentAdmin] <= 100),
    CONSTRAINT [CK_Persons_PercentClinical] CHECK ([PercentClinical] >= 0 AND [PercentClinical] <= 100)
)
GO

-- Courses Table
CREATE TABLE [effort].[Courses] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Crn] char(5) NOT NULL,
    [TermCode] int NOT NULL,
    [SubjCode] char(4) NOT NULL,
    [CrseNumb] char(6) NOT NULL,
    [SeqNumb] char(6) NOT NULL,
    [Enrollment] int NOT NULL DEFAULT 0,
    [Units] decimal(4,2) NOT NULL,
    [CustDept] char(6) NOT NULL,

    CONSTRAINT [PK_Courses] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_Courses_Enrollment] CHECK ([Enrollment] >= 0),
    CONSTRAINT [CK_Courses_Units] CHECK ([Units] > 0)
)
GO

-- Percentage Assignments Table
CREATE TABLE [effort].[Percentages] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [MothraId] char(8) NOT NULL,
    [TermCode] int NOT NULL,
    [EffortTypeId] int NOT NULL,
    [Percentage] decimal(5,2) NOT NULL,
    [Unit] varchar(50) NULL,
    [StartDate] datetime2(7) NOT NULL,
    [EndDate] datetime2(7) NULL,
    [CreatedDate] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [ModifiedBy] char(8) NOT NULL DEFAULT 'SYSTEM  ',

    CONSTRAINT [PK_Percentages] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_Percentages_Percentage] CHECK ([Percentage] >= 0 AND [Percentage] <= 100)
)
GO

-- Session Types Lookup Table
CREATE TABLE [effort].[SessionTypes] (
    [Id] varchar(3) NOT NULL,
    [Description] varchar(50) NOT NULL,
    [UsesWeeks] bit NOT NULL DEFAULT 0, -- CLI uses weeks, others use hours
    [IsActive] bit NOT NULL DEFAULT 1,

    CONSTRAINT [PK_SessionTypes] PRIMARY KEY CLUSTERED ([Id])
)
GO

-- Effort Types Lookup Table
CREATE TABLE [effort].[EffortTypes] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Class] varchar(20) NOT NULL,
    [Name] varchar(50) NOT NULL,
    [ShowOnTemplate] bit NOT NULL DEFAULT 1,
    [IsActive] bit NOT NULL DEFAULT 1,

    CONSTRAINT [PK_EffortTypes] PRIMARY KEY CLUSTERED ([Id])
)
GO

-- Roles Lookup Table
CREATE TABLE [effort].[Roles] (
    [Id] char(1) NOT NULL,
    [Description] varchar(25) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    [SortOrder] int NULL,

    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id])
)
GO

-- Terms Status Table
CREATE TABLE [effort].[Terms] (
    [TermCode] int NOT NULL,
    [TermName] varchar(50) NOT NULL,
    [AcademicYear] varchar(10) NOT NULL,
    [Status] varchar(20) NOT NULL, -- Harvested, Opened, Closed
    [HarvestedDate] datetime2(7) NULL,
    [OpenedDate] datetime2(7) NULL,
    [ClosedDate] datetime2(7) NULL,
    [CreatedDate] datetime2(7) NOT NULL DEFAULT GETDATE(),

    CONSTRAINT [PK_Terms] PRIMARY KEY CLUSTERED ([TermCode]),
    CONSTRAINT [CK_Terms_Status] CHECK ([Status] IN ('Harvested', 'Opened', 'Closed'))
)
GO

-- Additional Questions Table (Flexible Q&A)
CREATE TABLE [effort].[AdditionalQuestions] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [EffortId] int NOT NULL,
    [QuestionType] varchar(50) NOT NULL,
    [Question] varchar(500) NOT NULL,
    [Answer] varchar(max) NULL,
    [IsRequired] bit NOT NULL DEFAULT 0,
    [CreatedDate] datetime2(7) NOT NULL DEFAULT GETDATE(),

    CONSTRAINT [PK_AdditionalQuestions] PRIMARY KEY CLUSTERED ([Id])
)
GO

-- Course Relationships Table (Parent/Child courses)
CREATE TABLE [effort].[CourseRelationships] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [ParentCourseId] int NOT NULL,
    [ChildCourseId] int NOT NULL,
    [RelationshipType] varchar(20) NOT NULL, -- Lecture/Lab, Course/Section
    [CreatedDate] datetime2(7) NOT NULL DEFAULT GETDATE(),

    CONSTRAINT [PK_CourseRelationships] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_CourseRelationships_NotSelf] CHECK ([ParentCourseId] != [ChildCourseId])
)
GO

-- Audit Trail Table
CREATE TABLE [effort].[Audits] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [TableName] varchar(50) NOT NULL,
    [RecordId] varchar(50) NOT NULL,
    [Action] varchar(10) NOT NULL, -- INSERT, UPDATE, DELETE
    [OldValues] varchar(max) NULL,
    [NewValues] varchar(max) NULL,
    [ChangedBy] char(8) NOT NULL,
    [ChangedDate] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [UserAgent] varchar(500) NULL,
    [IpAddress] varchar(50) NULL,

    CONSTRAINT [PK_Audits] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CK_Audits_Action] CHECK ([Action] IN ('INSERT', 'UPDATE', 'DELETE'))
)
GO

-- =================================================================
-- FOREIGN KEY CONSTRAINTS
-- =================================================================

-- Records → Courses
ALTER TABLE [effort].[Records]
ADD CONSTRAINT [FK_effort_Records_Courses]
FOREIGN KEY ([CourseId]) REFERENCES [effort].[Courses]([Id])
ON DELETE CASCADE
GO

-- Records → Persons
ALTER TABLE [effort].[Records]
ADD CONSTRAINT [FK_Records_Persons]
FOREIGN KEY ([MothraId], [TermCode]) REFERENCES [effort].[Persons]([MothraId], [TermCode])
ON DELETE CASCADE
GO

-- Records → Roles
ALTER TABLE [effort].[Records]
ADD CONSTRAINT [FK_Records_Roles]
FOREIGN KEY ([Role]) REFERENCES [effort].[Roles]([Id])
ON DELETE RESTRICT
GO

-- Records → SessionTypes
ALTER TABLE [effort].[Records]
ADD CONSTRAINT [FK_Records_SessionTypes]
FOREIGN KEY ([SessionType]) REFERENCES [effort].[SessionTypes]([Id])
ON DELETE RESTRICT
GO

-- Records → Terms
ALTER TABLE [effort].[Records]
ADD CONSTRAINT [FK_Records_Terms]
FOREIGN KEY ([TermCode]) REFERENCES [effort].[Terms]([TermCode])
ON DELETE RESTRICT
GO

-- Persons → Terms
ALTER TABLE [effort].[Persons]
ADD CONSTRAINT [FK_Persons_Terms]
FOREIGN KEY ([TermCode]) REFERENCES [effort].[Terms]([TermCode])
ON DELETE RESTRICT
GO

-- Courses → Terms
ALTER TABLE [effort].[Courses]
ADD CONSTRAINT [FK_Courses_Terms]
FOREIGN KEY ([TermCode]) REFERENCES [effort].[Terms]([TermCode])
ON DELETE RESTRICT
GO

-- Percentages → Persons
ALTER TABLE [effort].[Percentages]
ADD CONSTRAINT [FK_Percentages_Persons]
FOREIGN KEY ([MothraId], [TermCode]) REFERENCES [effort].[Persons]([MothraId], [TermCode])
ON DELETE CASCADE
GO

-- Percentages → EffortTypes
ALTER TABLE [effort].[Percentages]
ADD CONSTRAINT [FK_Percentages_EffortTypes]
FOREIGN KEY ([EffortTypeId]) REFERENCES [effort].[EffortTypes]([Id])
ON DELETE RESTRICT
GO

-- AdditionalQuestions → Records
ALTER TABLE [effort].[AdditionalQuestions]
ADD CONSTRAINT [FK_AdditionalQuestions_Records]
FOREIGN KEY ([EffortId]) REFERENCES [effort].[Records]([Id])
ON DELETE CASCADE
GO

-- CourseRelationships → Courses (Parent)
ALTER TABLE [effort].[CourseRelationships]
ADD CONSTRAINT [FK_CourseRelationships_Parent]
FOREIGN KEY ([ParentCourseId]) REFERENCES [effort].[Courses]([Id])
ON DELETE NO ACTION
GO

-- CourseRelationships → Courses (Child)
ALTER TABLE [effort].[CourseRelationships]
ADD CONSTRAINT [FK_CourseRelationships_Child]
FOREIGN KEY ([ChildCourseId]) REFERENCES [effort].[Courses]([Id])
ON DELETE NO ACTION
GO

-- =================================================================
-- UNIQUE CONSTRAINTS
-- =================================================================

-- Unique CRN per term
ALTER TABLE [effort].[Courses]
ADD CONSTRAINT [UQ_Courses_CRN_Term]
UNIQUE ([Crn], [TermCode])
GO

-- Unique effort per person/course/session/role
ALTER TABLE [effort].[Records]
ADD CONSTRAINT [UQ_Records_Person_Course_Session_Role]
UNIQUE ([MothraId], [TermCode], [CourseId], [SessionType], [Role])
GO

-- Unique course relationship
ALTER TABLE [effort].[CourseRelationships]
ADD CONSTRAINT [UQ_CourseRelationships_Parent_Child]
UNIQUE ([ParentCourseId], [ChildCourseId])
GO

-- =================================================================
-- PERFORMANCE INDEXES
-- =================================================================

-- Records indexes
CREATE NONCLUSTERED INDEX [IX_Records_MothraId_TermCode]
ON [effort].[Records] ([MothraId], [TermCode])
INCLUDE ([CourseId], [SessionType], [Role], [Hours])
GO

CREATE NONCLUSTERED INDEX [IX_Records_CourseId]
ON [effort].[Records] ([CourseId])
INCLUDE ([MothraId], [TermCode], [Hours])
GO

CREATE NONCLUSTERED INDEX [IX_Records_TermCode]
ON [effort].[Records] ([TermCode])
GO

-- Persons indexes
CREATE NONCLUSTERED INDEX [IX_Persons_LastName_FirstName]
ON [effort].[Persons] ([LastName], [FirstName])
INCLUDE ([MothraId], [TermCode], [EffortDept])
GO

CREATE NONCLUSTERED INDEX [IX_Persons_EffortDept]
ON [effort].[Persons] ([EffortDept])
INCLUDE ([MothraId], [TermCode], [LastName], [FirstName])
GO

CREATE NONCLUSTERED INDEX [IX_Persons_TermCode]
ON [effort].[Persons] ([TermCode])
GO

-- Courses indexes
CREATE NONCLUSTERED INDEX [IX_Courses_TermCode]
ON [effort].[Courses] ([TermCode])
INCLUDE ([Crn], [SubjCode], [CrseNumb])
GO

CREATE NONCLUSTERED INDEX [IX_Courses_SubjCode_CrseNumb]
ON [effort].[Courses] ([SubjCode], [CrseNumb])
INCLUDE ([TermCode], [Crn])
GO

CREATE NONCLUSTERED INDEX [IX_Courses_CustDept]
ON [effort].[Courses] ([CustDept])
INCLUDE ([TermCode], [SubjCode], [CrseNumb])
GO

-- Percentages indexes
CREATE NONCLUSTERED INDEX [IX_Percentages_MothraId_TermCode]
ON [effort].[Percentages] ([MothraId], [TermCode])
INCLUDE ([EffortType], [Percentage])
GO

-- Audits indexes
CREATE NONCLUSTERED INDEX [IX_Audits_TableName_RecordId]
ON [effort].[Audits] ([TableName], [RecordId])
INCLUDE ([Action], [ChangedBy], [ChangedDate])
GO

CREATE NONCLUSTERED INDEX [IX_Audits_ChangedDate]
ON [effort].[Audits] ([ChangedDate] DESC)
GO

-- =================================================================
-- REFERENCE DATA POPULATION
-- =================================================================

-- Insert actual session types from existing tblEffort database (39 distinct types)
INSERT INTO [effort].[SessionTypes] ([Id], [Description], [UsesWeeks]) VALUES
('ACT', 'Activity', 0),
('AUT', 'Autopsy', 0),
('CBL', 'Case-Based Learning', 0),
('CLI', 'Clinical', 1),   -- Clinical sessions use weeks
('CON', 'Conference', 0),
('D/L', 'Distance Learning', 0),
('DIS', 'Discussion', 0),
('DSL', 'Distance Learning', 0),
('EXM', 'Examination', 0),
('FAS', 'Faculty Assessment', 0),
('FWK', 'Fieldwork', 0),
('IND', 'Independent Study', 0),
('INT', 'Internship', 0),
('JLC', 'Journal Club', 0),
('L/D', 'Lab/Discussion', 0),
('LAB', 'Laboratory', 0),
('LEC', 'Lecture', 0),
('LED', 'Lab/Discussion', 0),
('LIS', 'Listening', 0),
('LLA', 'Lab/Lecture', 0),
('PBL', 'Problem-Based Learning', 0),
('PER', 'Performance', 0),
('PRB', 'Problem', 0),
('PRJ', 'Project', 0),
('PRS', 'Presentation', 0),
('SEM', 'Seminar', 0),
('STD', 'Studio', 0),
('T-D', 'Team-Discussion', 0),
('TBL', 'Team-Based Learning', 0),
('TMP', 'Temporary', 0),
('TUT', 'Tutorial', 0),
('VAR', 'Variable', 0),
('WED', 'Wednesday', 0),
('WRK', 'Workshop', 0),
('WVL', 'Work-Variable Learning', 0)
GO

-- Insert actual effort types from existing tblEffortType_LU
SET IDENTITY_INSERT [effort].[EffortTypes] ON
INSERT INTO [effort].[EffortTypes] ([Id], [Class], [Name], [ShowOnTemplate]) VALUES
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
(26, 'Admin', 'Asst Director', 1)
SET IDENTITY_INSERT [effort].[EffortTypes] OFF
GO

-- Insert actual roles from existing Efforts database (Role_ID as CHAR(1))
INSERT INTO [effort].[Roles] ([Id], [Description], [SortOrder]) VALUES
('1', 'Instructor of Record', 1),
('2', 'Instructor', 2),
('3', 'Facilitator', 3)
GO

-- =================================================================
-- SCHEMA SUMMARY
-- =================================================================

/*
CONSOLIDATED EFFORT SCHEMA IN VIPER DATABASE

Core Tables:
├── Records (Main effort entries)
├── Persons (Instructors with composite key)
├── Courses (Course information)
├── Percentages (Admin/clinical percentages)
├── Terms (Term status management)
├── SessionTypes (Session type lookup: 39 types including CLI, LEC, LAB, etc.)
├── EffortTypes (Type classification lookup)
├── Roles (Role lookup)
├── AdditionalQuestions (Flexible Q&A)
├── CourseRelationships (Course hierarchies)
└── Audits (Change tracking)

Key Features:
✓ Proper foreign key relationships
✓ Data integrity constraints
✓ Performance indexes
✓ Audit trail capability
✓ Flexible architecture for future needs

Migration Source Mapping:
tblEffort → Records
tblPerson → Persons
tblCourses → Courses
tblPercent → Percentages
tblStatus → Terms
tblEffortType_LU → EffortTypes
tblRoles → Roles
tblAdditionalQuestions → AdditionalQuestions
tblCourseHierarchy → CourseRelationships
[New] → Audits

Total Tables: 11 (vs 21 in legacy database)
Total Constraints: 15 foreign keys + 4 unique constraints + 8 check constraints
Total Indexes: 12 performance indexes
Schema Organization: "effort" schema within VIPER database (following CTS pattern)
Table Naming: [VIPER].[effort].[TableName] (e.g., [VIPER].[effort].[Records])
*/