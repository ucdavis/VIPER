# Effort System Database Consolidation & EF Migration Plan

**Project:** VIPER2 Effort System Migration
**Strategy:** Consolidate Efforts Database → VIPER Database using Entity Framework Migrations
**Target:** Single database with proper schema design and relationships
**Approach:** Agile sprint-based migration integrated with feature development

---

## Database Consolidation Overview

This plan migrates all Efforts database tables into the VIPER database using Entity Framework Code First migrations, establishing proper relationships and constraints from the beginning. The migration is integrated with the 16-sprint agile development approach, ensuring database changes align with feature delivery.

## Current Database Architecture

### Source: Efforts Database
- **21 tables** (tblEffort, tblPerson, tblCourses, etc.)
- **93 active stored procedures** requiring migration strategy
- **Missing foreign keys** and performance indexes
- **Separate database** requiring cross-database connections
- **Heavy SP usage** in ColdFusion for CRUD, business logic, and reporting

### Target: VIPER Database
- **Consolidated schema** in single database
- **Entity Framework managed** with Code First migrations
- **Proper relationships** and constraints
- **Performance optimized** with appropriate indexes
- **Service layer approach** replacing stored procedures with C# business logic

## Entity Framework Migration Strategy

### Stored Procedures Migration Strategy

#### Technology Decision Analysis: Stored Procedures vs Entity Framework

**Current State Assessment (Stored Procedures)**

*Advantages:*
- **Raw Performance**: Direct SQL execution with optimized plans and minimal overhead
- **Advanced SQL Features**: Full access to CTEs, window functions, recursive queries, and advanced aggregations
- **Proven Stability**: Battle-tested approach with predictable performance characteristics
- **DBA Optimization**: Database administrators can fine-tune performance without code changes
- **Bulk Operations**: Excellent performance for large-scale data manipulation
- **Security**: Built-in protection against SQL injection when properly parameterized
- **Resource Control**: Fine-grained control over database resources, locking, and transaction isolation

*Disadvantages:*
- **Development Silos**: Creates separation between application and database development teams
- **Limited Testability**: Requires database instance for testing, complicates unit testing strategies
- **Version Control Challenges**: Database schema changes are harder to track and deploy consistently
- **IDE Support**: Limited IntelliSense, refactoring, and debugging capabilities
- **Platform Lock-in**: Ties application to specific database vendor (SQL Server)
- **Deployment Complexity**: Requires coordinated database and application deployments
- **Code Duplication**: Business logic cannot be easily shared between applications
- **Maintenance Overhead**: Logic scattered across application and database layers
- **Developer Skill Requirements**: Requires specialized T-SQL knowledge on development team

**Target State Assessment (Entity Framework + Service Layer)**

*Advantages:*
- **Type Safety**: Compile-time validation prevents runtime data access errors
- **Developer Productivity**: Full IDE support with IntelliSense, refactoring, and debugging
- **Comprehensive Testing**: Easy unit testing with in-memory providers and mocking frameworks
- **Code Maintainability**: All business logic centralized in application code with modern tooling
- **Version Control Integration**: Complete change tracking and branching support for business logic
- **Cross-Platform Flexibility**: Database provider abstraction enables platform changes
- **Modern Architecture Patterns**: Native support for repository, service layer, and dependency injection
- **Team Skill Alignment**: Leverages existing .NET development expertise
- **Rapid Development**: Code-first migrations and scaffolding accelerate development cycles
- **Business Logic Reuse**: Services can be shared across multiple applications and interfaces

*Disadvantages:*
- **ORM Overhead**: Translation layer adds computational overhead and memory usage
- **Query Generation**: Less control over exact SQL queries being executed
- **Complex Query Limitations**: Some advanced SQL patterns may require raw SQL or multiple queries
- **Learning Curve**: Team must learn LINQ, EF Core patterns, and query optimization techniques
- **Performance Tuning Complexity**: Requires understanding of both C# and generated SQL for optimization
- **Migration Investment**: Significant upfront effort to convert 93 stored procedures

#### SP Analysis and Categorization
Based on the technology assessment, the 93 stored procedures fall into three categories requiring different migration approaches:

**Category 1: Simple CRUD Operations (40% - ~37 procedures)**
- Examples: usp_createCourse, usp_createInstructor, usp_updateCourse, usp_delInstructor
- **Migration**: Replace with Entity Framework repository pattern
- **Approach**: Direct LINQ operations with proper validation

**Category 2: Complex Business Logic (35% - ~33 procedures)**
- Examples: usp_getEffortReportMerit, usp_getEffortDeptActivityTotal, usp_VerifyEffort
- **Migration**: Convert to C# service layer methods
- **Approach**: Implement business logic in services using LINQ and EF Core

**Category 3: High-Performance Reports (25% - ~23 procedures)**
- Examples: usp_getEffortReportMeritMultiYear, usp_getEffortDeptActivityTotalWithExcludeTerms
- **Migration**: EF Core with raw SQL for complex aggregations
- **Approach**: Use compiled queries and consider caching for performance

#### Migration Sprint Integration

**Sprints 1-3: Foundation and Simple CRUD**
- Create repository interfaces and implementations
- Replace simple stored procedures with EF operations
- Establish unit testing framework for data access

**Sprints 4-8: Business Logic Migration**
- Convert calculation and validation procedures to services
- Implement term management logic in C# services
- Create comprehensive business rule validation

**Sprints 11-13: Reporting and Performance**
- Migrate complex reporting procedures
- Implement caching strategies for expensive operations
- Performance test and optimize query patterns

**Sprint 14: Validation and Cleanup**
- Parallel testing of old vs new implementations
- Performance benchmarking and optimization
- Remove dependencies on original stored procedures

#### Migration Decision Rationale

**Chosen Approach: Full Entity Framework Migration**

**Strategic Justification:**
1. **Alignment with VIPER2 Architecture**: Maintains consistency with the broader system modernization
2. **Long-term Maintainability**: Reduces technical debt and improves code quality
3. **Development Team Efficiency**: Leverages existing .NET skills and modern tooling
4. **Testing Strategy**: Enables comprehensive unit testing and continuous integration
5. **Future Flexibility**: Provides foundation for future enhancements and platform changes

**Performance Mitigation Strategies:**
1. **Compiled Queries**: Use EF Core compiled queries for frequently executed operations
2. **Query Optimization**: Implement query splitting and projection for complex data retrieval
3. **Raw SQL Integration**: Leverage `FromSqlRaw()` for complex reporting procedures that cannot be efficiently translated to LINQ
4. **Caching Layer**: Implement distributed caching for expensive calculations and reference data
5. **Database Indexing**: Add appropriate indexes based on EF-generated query patterns
6. **Connection Pooling**: Optimize connection management for high-throughput scenarios

**Quality Assurance Approach:**
1. **Parallel Validation**: Run both stored procedures and EF implementations during transition period
2. **Performance Benchmarking**: Establish baseline metrics and monitor performance impact
3. **Comprehensive Testing**: Unit tests for business logic, integration tests for data access
4. **Gradual Rollout**: Sprint-by-sprint migration with rollback capabilities

### 1. Schema Design Approach

#### Table Naming Convention
Prefix all Effort tables with `Effort` to avoid naming conflicts:
```
tblEffort → EffortRecords
tblPerson → EffortPersons
tblCourses → EffortCourses
tblPercent → EffortPercentages
tblRoles → EffortRoles
tblStatus → EffortTerms
```

#### Namespace Organization
```csharp
// Separate DbContext for Effort entities
public class EffortDbContext : DbContext
{
    // Configure connection to VIPER database
    // Use schema separation for organization
}
```

### 2. Entity Model Design

#### Core Entity Structure
```csharp
// Areas/Effort/Models/Entities/EffortRecord.cs
[Table("Records", Schema = "effort")]
public class EffortRecord
{
    [Key]
    public int Id { get; set; }

    public int CourseId { get; set; }
    public string MothraId { get; set; } = null!;
    public int TermCode { get; set; }
    public string SessionType { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int? Hours { get; set; }
    public int? Weeks { get; set; }
    public string? ClientId { get; set; }
    public string Crn { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = null!;

    // Navigation properties
    public virtual EffortCourse Course { get; set; } = null!;
    public virtual EffortPerson Person { get; set; } = null!;
    public virtual EffortRole RoleNavigation { get; set; } = null!;
    public virtual ICollection<EffortAdditionalQuestion> AdditionalQuestions { get; set; } = new List<EffortAdditionalQuestion>();
}

// Areas/Effort/Models/Entities/EffortPerson.cs
[Table("Persons", Schema = "effort")]
public class EffortPerson
{
    [Key]
    public string MothraId { get; set; } = null!;

    [Key]
    public int TermCode { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? MiddleInitial { get; set; }
    public string EffortTitleCode { get; set; } = null!;
    public string EffortDept { get; set; } = null!;
    public double PercentAdmin { get; set; }
    public string? JobGroupId { get; set; }
    public string? Title { get; set; }
    public string? AdminUnit { get; set; }
    public string? ClientId { get; set; }
    public DateTime? EffortVerified { get; set; }
    public string? ReportUnit { get; set; }
    public byte? VolunteerWos { get; set; }
    public double? PercentClinical { get; set; }

    // Navigation properties
    public virtual ICollection<EffortRecord> EffortRecords { get; set; } = new List<EffortRecord>();
    public virtual ICollection<EffortPercentage> Percentages { get; set; } = new List<EffortPercentage>();
}

// Areas/Effort/Models/Entities/EffortCourse.cs
[Table("Courses", Schema = "effort")]
public class EffortCourse
{
    [Key]
    public int Id { get; set; }

    public string Crn { get; set; } = null!;
    public int TermCode { get; set; }
    public string SubjCode { get; set; } = null!;
    public string CrseNumb { get; set; } = null!;
    public string SeqNumb { get; set; } = null!;
    public int Enrollment { get; set; }
    public double Units { get; set; }
    public string CustDept { get; set; } = null!;

    // Navigation properties
    public virtual ICollection<EffortRecord> EffortRecords { get; set; } = new List<EffortRecord>();
    public virtual ICollection<EffortCourseRelationship> ParentRelationships { get; set; } = new List<EffortCourseRelationship>();
    public virtual ICollection<EffortCourseRelationship> ChildRelationships { get; set; } = new List<EffortCourseRelationship>();
}
```

### 3. DbContext Configuration

```csharp
// Areas/Effort/Data/EffortDbContext.cs
public class EffortDbContext : DbContext
{
    public EffortDbContext(DbContextOptions<EffortDbContext> options) : base(options) { }

    public DbSet<EffortRecord> EffortRecords { get; set; }
    public DbSet<EffortPerson> EffortPersons { get; set; }
    public DbSet<EffortCourse> EffortCourses { get; set; }
    public DbSet<EffortPercentage> EffortPercentages { get; set; }
    public DbSet<EffortRole> EffortRoles { get; set; }
    public DbSet<EffortTerm> EffortTerms { get; set; }
    public DbSet<EffortAdditionalQuestion> EffortAdditionalQuestions { get; set; }
    public DbSet<EffortCourseRelationship> EffortCourseRelationships { get; set; }
    public DbSet<EffortAudit> EffortAudits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite primary key for EffortPerson
        modelBuilder.Entity<EffortPerson>()
            .HasKey(p => new { p.MothraId, p.TermCode });

        // Configure relationships
        modelBuilder.Entity<EffortRecord>()
            .HasOne(e => e.Person)
            .WithMany(p => p.EffortRecords)
            .HasForeignKey(e => new { e.MothraId, e.TermCode })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EffortRecord>()
            .HasOne(e => e.Course)
            .WithMany(c => c.EffortRecords)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EffortRecord>()
            .HasOne(e => e.RoleNavigation)
            .WithMany()
            .HasForeignKey(e => e.Role)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure unique constraints
        modelBuilder.Entity<EffortCourse>()
            .HasIndex(c => new { c.Crn, c.TermCode })
            .IsUnique()
            .HasDatabaseName("IX_EffortCourses_CRN_Term");

        // Configure indexes for performance
        modelBuilder.Entity<EffortRecord>()
            .HasIndex(e => new { e.MothraId, e.TermCode })
            .HasDatabaseName("IX_EffortRecords_MothraId_TermCode");

        modelBuilder.Entity<EffortRecord>()
            .HasIndex(e => e.CourseId)
            .HasDatabaseName("IX_EffortRecords_CourseId");

        modelBuilder.Entity<EffortPerson>()
            .HasIndex(p => new { p.LastName, p.FirstName })
            .HasDatabaseName("IX_EffortPersons_LastName_FirstName");

        modelBuilder.Entity<EffortPerson>()
            .HasIndex(p => p.EffortDept)
            .HasDatabaseName("IX_EffortPersons_EffortDept");
    }
}
```

### 4. Service Registration

```csharp
// Program.cs or Startup.cs
services.AddDbContext<EffortDbContext>(options =>
    options.UseSqlServer(connectionString, b =>
        b.MigrationsAssembly("Viper")
         .MigrationsHistoryTable("__EffortMigrationsHistory", "Effort")));

// Register repositories and services for SP replacement
services.AddScoped<IEffortRepository, EffortRepository>();
services.AddScoped<IEffortCourseRepository, EffortCourseRepository>();
services.AddScoped<IEffortPersonRepository, EffortPersonRepository>();
services.AddScoped<ITermService, TermService>();
services.AddScoped<IInstructorService, InstructorService>();
services.AddScoped<ICourseService, CourseService>();
services.AddScoped<IEffortService, EffortService>();
services.AddScoped<IReportingService, ReportingService>();
```

## Migration Implementation Strategy

### Sprint 1: Entity Framework Foundation
**Focus:** Core data models and database infrastructure

#### 1.1 Create Entity Models
- Define all entity classes with proper relationships
- Configure DbContext with constraints and indexes
- Set up dependency injection

#### 1.2 Generate Initial Migration
```bash
# Create initial migration
dotnet ef migrations add InitialEffortSchema --context EffortDbContext --output-dir Areas/Effort/Migrations

# Review generated migration
# Modify if needed for proper schema organization
```

#### 1.3 Schema Validation
```csharp
// Validate schema generation
public class EffortSchemaValidationTests
{
    [Test]
    public void Schema_ShouldHaveAllRequiredTables()
    {
        // Test that all tables are created
        // Test that all indexes are created
        // Test that all foreign keys are created
    }
}
```

### Sprint 14: Data Migration Strategy
**Focus:** Historical data migration and validation

#### 2.1 Create Data Migration Scripts
**Note:** This data migration occurs in Sprint 14 after all entity models and services are established
```sql
-- Areas/Effort/Migrations/Data/MigrateFromEffortsDatabase.sql

-- Step 1: Migrate reference data (lookup tables)
INSERT INTO [VIPER].[Effort].[EffortRoles] (Id, Description)
SELECT Role_ID, Role_Desc
FROM [Effort].[dbo].[tblRoles];

-- Step 2: Migrate terms
INSERT INTO [VIPER].[Effort].[EffortTerms] (TermCode, TermName, AcademicYear, Status, HarvestedDate, OpenedDate, ClosedDate)
SELECT status_TermCode, status_TermName, status_AcademicYear,
       CASE
           WHEN status_Closed IS NOT NULL THEN 'Closed'
           WHEN status_Opened IS NOT NULL THEN 'Opened'
           WHEN status_Harvested IS NOT NULL THEN 'Harvested'
           ELSE 'Created'
       END,
       status_Harvested, status_Opened, status_Closed
FROM [Effort].[dbo].[tblStatus];

-- Step 3: Migrate courses
INSERT INTO [VIPER].[Effort].[EffortCourses] (Id, Crn, TermCode, SubjCode, CrseNumb, SeqNumb, Enrollment, Units, CustDept)
SELECT course_id, course_CRN, course_TermCode, course_SubjCode, course_CrseNumb,
       course_SeqNumb, course_Enrollment, course_Units, course_CustDept
FROM [Effort].[dbo].[tblCourses];

-- Step 4: Migrate persons
INSERT INTO [VIPER].[Effort].[EffortPersons] (MothraId, TermCode, FirstName, LastName, MiddleInitial,
                                               EffortTitleCode, EffortDept, PercentAdmin, JobGroupId, Title,
                                               AdminUnit, ClientId, EffortVerified, ReportUnit, VolunteerWos, PercentClinical)
SELECT person_MothraID, person_TermCode, person_FirstName, person_LastName, person_MiddleIni,
       person_EffortTitleCode, person_EffortDept, person_PercentAdmin, person_JobGrpID, person_Title,
       person_AdminUnit, person_ClientID, person_EffortVerified, person_ReportUnit, person_Volunteer_WOS, person_PercentClinical
FROM [Effort].[dbo].[tblPerson];

-- Step 5: Migrate effort records
INSERT INTO [VIPER].[Effort].[EffortRecords] (Id, CourseId, MothraId, TermCode, SessionType, Role, Hours, Weeks, ClientId, Crn, CreatedDate, ModifiedDate, ModifiedBy)
SELECT effort_ID, effort_CourseID, effort_MothraID, effort_termCode, effort_SessionType, effort_Role,
       effort_Hours, effort_Weeks, effort_ClientID, effort_CRN,
       GETDATE(), GETDATE(), 'MIGRATION'
FROM [Effort].[dbo].[tblEffort];
```

#### 2.2 Data Validation Scripts
```sql
-- Validate data migration
SELECT 'EffortRecords' as TableName, COUNT(*) as ViperCount,
       (SELECT COUNT(*) FROM [Effort].[dbo].[tblEffort]) as OriginalCount
FROM [VIPER].[Effort].[EffortRecords]
UNION ALL
SELECT 'EffortPersons', COUNT(*), (SELECT COUNT(*) FROM [Effort].[dbo].[tblPerson])
FROM [VIPER].[Effort].[EffortPersons]
UNION ALL
SELECT 'EffortCourses', COUNT(*), (SELECT COUNT(*) FROM [Effort].[dbo].[tblCourses])
FROM [VIPER].[Effort].[EffortCourses];

-- Validate relationships
SELECT 'Orphaned Efforts' as Issue, COUNT(*) as Count
FROM [VIPER].[Effort].[EffortRecords] e
LEFT JOIN [VIPER].[Effort].[EffortCourses] c ON e.CourseId = c.Id
WHERE c.Id IS NULL;
```

### Sprint 14: Migration Execution
**Focus:** Actual data migration execution and validation

#### 3.1 Pre-Migration Checklist (Sprint 14)
- [ ] Backup existing VIPER database
- [ ] Backup Efforts database
- [ ] Verify Entity Framework migrations from Sprint 1
- [ ] Test data migration scripts in staging
- [ ] Validate foreign key constraints
- [ ] Ensure all Sprint 1-13 features are complete and tested

#### 3.2 Migration Execution Steps
```bash
# 1. Apply EF migrations to create schema
dotnet ef database update --context EffortDbContext

# 2. Execute data migration scripts
sqlcmd -S server -d VIPER -i "MigrateFromEffortsDatabase.sql"

# 3. Validate data integrity
sqlcmd -S server -d VIPER -i "ValidateDataMigration.sql"

# 4. Update application connection strings
# Remove Efforts database connection
# Update to use single VIPER database
```

#### 3.3 Post-Migration Validation
```csharp
// Integration tests to validate migration
[Test]
public async Task Migration_ShouldPreserveAllData()
{
    // Compare record counts
    var effortCount = await _context.EffortRecords.CountAsync();
    // Assert expected counts

    // Validate relationships
    var orphanedEfforts = await _context.EffortRecords
        .Where(e => e.Course == null)
        .CountAsync();
    Assert.That(orphanedEfforts, Is.EqualTo(0));
}
```

### Sprints 2-13: Stored Procedures Migration Implementation
**Focus:** Progressive replacement of stored procedures with service layer

#### 4.1 Repository Pattern Implementation
**Implementation:** Sprints 3-5 (Basic CRUD operations)
```csharp
// Areas/Effort/Repositories/IEffortRepository.cs
public interface IEffortRepository
{
    Task<EffortRecord> CreateEffortAsync(EffortRecord effort);
    Task<EffortRecord> UpdateEffortAsync(EffortRecord effort);
    Task<bool> DeleteEffortAsync(int effortId);
    Task<EffortRecord> GetEffortByIdAsync(int effortId);
    Task<IEnumerable<EffortRecord>> GetEffortsByInstructorAsync(string mothraId, int termCode);
    Task<IEnumerable<EffortRecord>> GetEffortsByCourseAsync(int courseId);
}

// Areas/Effort/Repositories/EffortRepository.cs
public class EffortRepository : IEffortRepository
{
    private readonly EffortDbContext _context;

    public EffortRepository(EffortDbContext context)
    {
        _context = context;
    }

    // Replaces usp_createInstructorEffort
    public async Task<EffortRecord> CreateEffortAsync(EffortRecord effort)
    {
        effort.CreatedDate = DateTime.UtcNow;
        effort.ModifiedDate = DateTime.UtcNow;

        _context.EffortRecords.Add(effort);
        await _context.SaveChangesAsync();
        return effort;
    }

    // Replaces usp_updateInstructorEffort
    public async Task<EffortRecord> UpdateEffortAsync(EffortRecord effort)
    {
        effort.ModifiedDate = DateTime.UtcNow;

        _context.EffortRecords.Update(effort);
        await _context.SaveChangesAsync();
        return effort;
    }

    // Replaces usp_delInstructorEffort
    public async Task<bool> DeleteEffortAsync(int effortId)
    {
        var effort = await _context.EffortRecords.FindAsync(effortId);
        if (effort == null) return false;

        _context.EffortRecords.Remove(effort);
        await _context.SaveChangesAsync();
        return true;
    }
}
```

#### 4.2 Service Layer Implementation
**Implementation:** Sprints 2, 6-8 (Business logic and validation)
```csharp
// Areas/Effort/Services/ITermService.cs
public interface ITermService
{
    Task<bool> OpenTermAsync(int termCode, string modifiedBy);
    Task<bool> CloseTermAsync(int termCode, string modifiedBy);
    Task<bool> ReopenTermAsync(int termCode, string modifiedBy);
    Task<bool> UnopenTermAsync(int termCode, string modifiedBy);
    Task<TermStatus> GetTermStatusAsync(int termCode);
}

// Areas/Effort/Services/TermService.cs
public class TermService : ITermService
{
    private readonly EffortDbContext _context;
    private readonly ILogger<TermService> _logger;

    public TermService(EffortDbContext context, ILogger<TermService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Replaces usp_openTerm
    public async Task<bool> OpenTermAsync(int termCode, string modifiedBy)
    {
        var term = await _context.EffortTerms.FindAsync(termCode);
        if (term == null) return false;

        term.Status = "Opened";
        term.OpenedDate = DateTime.UtcNow;
        term.ModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Term {TermCode} opened by {ModifiedBy}", termCode, modifiedBy);

        return true;
    }

    // Replaces usp_closeTerm
    public async Task<bool> CloseTermAsync(int termCode, string modifiedBy)
    {
        var term = await _context.EffortTerms.FindAsync(termCode);
        if (term == null) return false;

        // Business rule: Cannot close term with unverified efforts
        var unverifiedEfforts = await _context.EffortPersons
            .Where(p => p.TermCode == termCode && p.EffortVerified == null)
            .CountAsync();

        if (unverifiedEfforts > 0)
        {
            _logger.LogWarning("Cannot close term {TermCode}: {Count} unverified efforts",
                termCode, unverifiedEfforts);
            return false;
        }

        term.Status = "Closed";
        term.ClosedDate = DateTime.UtcNow;
        term.ModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Term {TermCode} closed by {ModifiedBy}", termCode, modifiedBy);

        return true;
    }
}
```

#### 4.3 Reporting Service Implementation
**Implementation:** Sprints 11-13 (Complex reporting and analytics)
```csharp
// Areas/Effort/Services/IReportingService.cs
public interface IReportingService
{
    Task<MeritReportResult> GetEffortReportMeritAsync(int startYear, int endYear,
        string department = null, bool useAcademicYear = false);
    Task<DeptActivityResult> GetEffortDeptActivityTotalAsync(int startYear, int endYear,
        string excludeTerms = "");
    Task<IEnumerable<InstructorEffortSummary>> GetInstructorsWithZeroEffortAsync(int termCode,
        string department = null);
}

// Areas/Effort/Services/ReportingService.cs
public class ReportingService : IReportingService
{
    private readonly EffortDbContext _context;
    private readonly IMemoryCache _cache;

    public ReportingService(EffortDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // Replaces usp_getEffortReportMerit
    public async Task<MeritReportResult> GetEffortReportMeritAsync(int startYear, int endYear,
        string department = null, bool useAcademicYear = false)
    {
        var cacheKey = $"merit_report_{startYear}_{endYear}_{department}_{useAcademicYear}";

        if (_cache.TryGetValue(cacheKey, out MeritReportResult cachedResult))
        {
            return cachedResult;
        }

        var query = _context.EffortRecords
            .Include(e => e.Person)
            .Include(e => e.Course)
            .Where(e => e.TermCode >= startYear && e.TermCode <= endYear);

        if (!string.IsNullOrEmpty(department))
        {
            query = query.Where(e => e.Person.EffortDept == department);
        }

        var result = await query
            .GroupBy(e => new { e.Person.MothraId, e.Person.LastName, e.Person.FirstName })
            .Select(g => new InstructorMeritSummary
            {
                MothraId = g.Key.MothraId,
                LastName = g.Key.LastName,
                FirstName = g.Key.FirstName,
                TotalHours = g.Sum(e => e.Hours ?? 0),
                TotalWeeks = g.Sum(e => e.Weeks ?? 0),
                CourseCount = g.Select(e => e.CourseId).Distinct().Count()
            })
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync();

        var reportResult = new MeritReportResult { Instructors = result };

        // Cache for 30 minutes
        _cache.Set(cacheKey, reportResult, TimeSpan.FromMinutes(30));

        return reportResult;
    }
}
```

## Updated Application Configuration

### Connection String Changes
```json
// appsettings.json - BEFORE
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=VIPER;...",
    "EffortConnection": "Server=...;Database=Effort;..."
  }
}

// appsettings.json - AFTER
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=VIPER;..."
  }
}
```

### Service Registration Updates
```csharp
// Remove separate Effort database context
// services.AddDbContext<EffortDbContext>(options => options.UseSqlServer(effortConnection));

// Use single context pointing to VIPER database
services.AddDbContext<EffortDbContext>(options =>
    options.UseSqlServer(viperConnection));
```

## Migration Benefits

### Immediate Benefits
- **Single Database**: Eliminates cross-database complexity
- **Proper Relationships**: Foreign keys and constraints enforced
- **Performance**: Optimized indexes from the start
- **Maintainability**: Entity Framework manages schema changes

### Long-term Benefits
- **Code First Development**: Schema managed in code
- **Easy Updates**: EF migrations for future changes
- **Better Testing**: Easier to create test databases
- **Simplified Deployment**: Single database to manage

## Rollback Strategy

### Emergency Rollback Plan
1. **Keep Original Database**: Maintain Efforts database until migration validated
2. **Application Rollback**: Switch connection strings back to separate databases
3. **Data Sync**: If needed, sync any changes made post-migration

### Rollback Implementation
```csharp
// Feature flag for database selection
if (configuration.GetValue<bool>("UseConsolidatedDatabase"))
{
    services.AddDbContext<EffortDbContext>(options =>
        options.UseSqlServer(viperConnection));
}
else
{
    services.AddDbContext<EffortDbContext>(options =>
        options.UseSqlServer(effortConnection));
}
```

## Testing Strategy

### Unit Tests
- Entity relationship validation
- DbContext configuration tests
- Migration script validation

### Integration Tests
- End-to-end data flow testing
- Performance testing with consolidated database
- Multi-user concurrent access testing

### Migration Tests
- Data integrity validation
- Performance comparison (before/after)
- Rollback procedure testing

## Implementation Sprints and Milestones

| Sprint Range | Focus Area | Deliverables |
|-------|------------|--------------|
| **Sprint 1** | Entity Setup | Entity models, DbContext, initial migration |
| **Sprints 2-8** | Service Layer & Business Logic | Repository pattern, term management, business rules |
| **Sprints 11-13** | Reporting & Performance | Complex reports, caching, query optimization |
| **Sprint 14** | Data Migration & Validation | Historical data migration, parallel testing, cleanup |

## Success Metrics

### Technical Metrics
- **Zero Data Loss**: 100% data preservation during migration
- **Performance**: Query performance equal or better than before
- **Integrity**: All relationships properly enforced

### Operational Metrics
- **Downtime**: < 2 hours for migration execution
- **Rollback Time**: < 30 minutes if needed
- **Validation**: All tests pass post-migration

---

**Migration Approach**: Database consolidation with Entity Framework Code First
**Risk Level**: Medium (mitigated by comprehensive testing and rollback plan)
**Implementation**: Agile sprint-based approach with incremental delivery
**Dependencies**: VIPER2 environment setup, database access permissions
**Integration**: Aligns with 16-sprint VIPER2 migration plan
**Last Updated**: September 18, 2025 - Updated for agile sprint approach