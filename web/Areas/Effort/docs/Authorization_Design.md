# Effort System - Authorization Design

**Status**: Design Document
**Date**: 2024-11-14
**Purpose**: Define authorization architecture for modernized Effort system integration with VIPER2

---

## Overview

The Effort system authorization integrates with VIPER2's existing permission framework while preserving the legacy system's department-level access control. This design follows proven patterns from the ClinicalScheduler area and provides a smooth migration path from the legacy ColdFusion application.

---

## Architecture

### Two-Layer Authorization Model

```
┌─────────────────────────────────────────────────────────────┐
│                    Authorization Layers                      │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  Layer 1: System-Wide Permissions (RAPS)                    │
│  ┌────────────────────────────────────────────────────┐    │
│  │ • SVMSecure.Effort (Base access)                   │    │
│  │ • SVMSecure.Effort.Admin (Full access)             │    │
│  │ • SVMSecure.Effort.Manage (Manage system)          │    │
│  │ • SVMSecure.Effort.ViewDept (View departments)     │    │
│  │ • SVMSecure.Effort.EditDept (Edit departments)     │    │
│  │ • SVMSecure.Effort.VerifyDept (Verify efforts)     │    │
│  │ • SVMSecure.Effort.ViewOwn (View own data)         │    │
│  │ • SVMSecure.Effort.EditOwn (Edit own data)         │    │
│  └────────────────────────────────────────────────────┘    │
│                           ↓                                   │
│  Layer 2: Department-Level Access (UserAccess Table)        │
│  ┌────────────────────────────────────────────────────┐    │
│  │ effort.UserAccess                                   │    │
│  │ ├─ PersonId (FK to users.Person)                   │    │
│  │ ├─ DepartmentCode (e.g., "SURG", "ANAT")           │    │
│  │ ├─ IsActive (soft delete)                          │    │
│  │ └─ Audit fields (ModifiedBy, ModifiedDate)         │    │
│  └────────────────────────────────────────────────────┘    │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

**How it works:**
1. **System-wide permission** (RAPS) determines WHAT the user can do (view, edit, verify)
2. **Department-level access** (UserAccess table) determines WHERE they can do it (which departments)
3. **Permission service** combines both layers to enforce authorization

---

## RAPS Permissions

### Permission Hierarchy

```
Admin > Manage > VerifyDept > EditDept > ViewDept > ViewOwn
```

**Higher permissions bypass lower checks:**
- Admin/Manage users can access ALL departments without UserAccess entries
- Users with ViewDept must ALSO have UserAccess entries for specific departments

### Permission Definitions

| Permission | Purpose | Department Check Required? | Legacy Equivalent |
|------------|---------|---------------------------|-------------------|
| `SVMSecure.Effort` | Base access to Effort system | No | - |
| `SVMSecure.Effort.Admin` | Full administrative access | **No** (bypasses UserAccess) | - |
| `SVMSecure.Effort.Manage` | Manage system configuration | **No** (bypasses UserAccess) | `SVMSecure.Effort.ManageAccess` |
| `SVMSecure.Effort.ViewDept` | View department effort data | **Yes** (requires UserAccess) | `SVMSecure.Effort.ViewDept` |
| `SVMSecure.Effort.EditDept` | Edit department effort data | **Yes** (requires UserAccess) | - |
| `SVMSecure.Effort.VerifyDept` | Verify effort for department | **Yes** (requires UserAccess) | - |
| `SVMSecure.Effort.ViewOwn` | View own effort records only | No (self-service) | - |
| `SVMSecure.Effort.EditOwn` | Edit own effort allocations | No (self-service) | - |
| `SVMSecure.Effort.RunReports` | Run reports | No | - |

---

## Implementation Components

### 1. Permission Constants (`EffortPermissions.cs`)

**Location**: `web/Areas/Effort/Services/EffortPermissions.cs`

```csharp
public static class EffortPermissions
{
    public const string Base = "SVMSecure.Effort";
    public const string Admin = "SVMSecure.Effort.Admin";
    public const string Manage = "SVMSecure.Effort.Manage";
    public const string ViewDept = "SVMSecure.Effort.ViewDept";
    public const string EditDept = "SVMSecure.Effort.EditDept";
    public const string VerifyDept = "SVMSecure.Effort.VerifyDept";
    public const string ViewOwn = "SVMSecure.Effort.ViewOwn";
    public const string EditOwn = "SVMSecure.Effort.EditOwn";
    public const string RunReports = "SVMSecure.Effort.RunReports";
}
```

**Pattern**: Follows ClinicalScheduler naming convention (`ClinicalSchedulePermissions.cs`)

---

### 2. Permission Service Interface (`IEffortPermissionService.cs`)

**Location**: `web/Areas/Effort/Services/IEffortPermissionService.cs`

**Key methods:**
- `CanViewDepartmentAsync(string departmentCode)` - Check view access to department
- `CanEditDepartmentAsync(string departmentCode)` - Check edit access to department
- `CanVerifyDepartmentAsync(string departmentCode)` - Check verify access
- `GetAuthorizedDepartmentsAsync()` - Get all departments user can access
- `GetUserDepartmentPermissionsAsync()` - Get access level per department
- `CanViewPersonEffortAsync(int personId, int termCode)` - Check access to person's data
- `CanEditPersonEffortAsync(int personId, int termCode)` - Check edit access to person's data

**Pattern**: Mirrors `ISchedulePermissionService` from ClinicalScheduler

---

### 3. Permission Service Implementation (`EffortPermissionService.cs`)

**Location**: `web/Areas/Effort/Services/EffortPermissionService.cs`

**Authorization logic:**

```csharp
public async Task<bool> CanViewDepartmentAsync(string departmentCode, CancellationToken ct)
{
    var user = GetCurrentUser();

    // Admin/Manage bypass department checks
    if (HasFullAccessPermission(user)) return true;

    // Check RAPS permission
    if (!HasPermission(user, EffortPermissions.ViewDept)) return false;

    // Check UserAccess table for department
    var personId = GetPersonIdFromUser(user);
    return await UserHasDepartmentAccessAsync(personId, departmentCode);
}
```

**Key features:**
- Caches user lookup (GetCurrentUser via UserHelper)
- Queries RAPS via UserHelper.HasPermission()
- Queries UserAccess table for department filtering
- Logs all authorization decisions
- Returns false on errors (fail-safe)

---

### 4. Controller Integration

**Pattern**: Use `[Permission]` attribute + service layer checks

```csharp
[Route("/api/effort/records")]
[Permission(Allow = EffortPermissions.Base)]  // System-wide check
public class RecordsController : ApiController
{
    private readonly IEffortPermissionService _permissionService;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EffortRecordDto>>> GetRecords(
        int termCode,
        string? departmentCode = null)
    {
        // Get authorized departments for user
        var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync();

        var query = _context.Records.Where(r => r.TermCode == termCode);

        // Apply department filtering (empty list = full access)
        if (authorizedDepts.Any())
        {
            query = query.Where(r => authorizedDepts.Contains(r.Person.EffortDept));
        }

        // If specific department requested, verify access
        if (!string.IsNullOrEmpty(departmentCode))
        {
            if (!await _permissionService.CanViewDepartmentAsync(departmentCode))
            {
                return Forbid();
            }
            query = query.Where(r => r.Person.EffortDept == departmentCode);
        }

        return Ok(await query.ToListAsync());
    }
}
```

**Controller-level authorization:**
- `[Permission(Allow = EffortPermissions.Base)]` - Require system access
- `[Permission(Allow = EffortPermissions.EditDept)]` - Require edit permission for actions

**Action-level filtering:**
- Call permission service to get authorized departments
- Filter queries based on UserAccess table
- Verify access before returning sensitive data

---

## Comparison: Legacy vs. Modern

### Legacy ColdFusion System

**Problems:**
- ❌ Two separate permission systems (RAPS + custom `userAccess` table)
- ❌ No database-level enforcement (application-only checks)
- ❌ Custom admin UI for managing access (DepartmentAccess.cfm)
- ❌ Free-text department codes (no validation)
- ❌ No audit trail for permission changes
- ❌ Manual cleanup when users leave
- ❌ N+1 query performance issues
- ❌ Inconsistent with VIPER2 architecture

**How it worked:**
```cfm
<!-- Check RAPS permission -->
<cfif listFind(Session.User.Permissions, 'SVMSecure.Effort.ViewDept') eq 0>
    <cflocation url="/denied.cfm">
</cfif>

<!-- Check userAccess table -->
<cfquery name="qUserAccess">
    SELECT departmentAbbreviation
    FROM userAccess
    WHERE mothraID = <cfqueryparam value="#Session.User.MothraID#">
</cfquery>

<!-- Filter data by department -->
<cfquery name="qEffortData">
    SELECT * FROM tblEffort
    WHERE effort_Dept IN (<cfqueryparam value="#valueList(qUserAccess.departmentAbbreviation)#" list="true">)
</cfquery>
```

---

### Modern VIPER2 Integration

**Improvements:**
- ✅ Single integrated permission model (RAPS + UserAccess via service layer)
- ✅ Database roles available for defense-in-depth (optional, DBA-managed)
- ✅ Centralized permission management (VIPER2 admin UI)
- ✅ Department code validation (can enforce against master list)
- ✅ Full audit trail (ModifiedBy, ModifiedDate, IsActive, CreatedDate)
- ✅ Automatic cleanup (IsActive flag + audit)
- ✅ Efficient queries (EF Core + proper indexing)
- ✅ Consistent with VIPER2 patterns (matches ClinicalScheduler)
- ✅ Self-service permissions (ViewOwn, EditOwn)
- ✅ Permission hierarchy (Admin > Manage > Dept permissions)

**How it works:**
```csharp
// Controller-level: RAPS check
[Permission(Allow = EffortPermissions.ViewDept)]

// Action-level: Department filtering
var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync();
var query = _context.Records
    .Where(r => r.TermCode == termCode)
    .Where(r => authorizedDepts.Contains(r.Person.EffortDept));
```

---

## Data Model: UserAccess Table

### Schema

```sql
CREATE TABLE [effort].[UserAccess] (
    Id              int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PersonId        int NOT NULL,  -- FK to [users].[Person]
    DepartmentCode  nvarchar(50) NOT NULL,
    IsActive        bit NOT NULL DEFAULT 1,
    CreatedDate     datetime2(7) NULL,
    ModifiedDate    datetime2(7) NULL,
    ModifiedBy      int NULL,  -- FK to [users].[Person]

    CONSTRAINT FK_UserAccess_Person
        FOREIGN KEY (PersonId) REFERENCES [users].[Person](PersonId),
    CONSTRAINT FK_UserAccess_ModifiedBy
        FOREIGN KEY (ModifiedBy) REFERENCES [users].[Person](PersonId)
);

CREATE INDEX IX_UserAccess_PersonId_IsActive
    ON [effort].[UserAccess](PersonId, IsActive)
    INCLUDE (DepartmentCode);
```

### Migration from Legacy

**Legacy table**: `userAccess`
- `userAccessID` → `Id`
- `mothraID` → `PersonId` (mapped via `users.Person`)
- `departmentAbbreviation` → `DepartmentCode`

**Added fields:**
- `IsActive` - Soft delete (vs. hard DELETE in legacy)
- `CreatedDate` - Track when access granted
- `ModifiedDate` - Track when access changed
- `ModifiedBy` - Audit who made changes

**Migration handled by**: [MigrateEffortData.cs:1220-1304](web/Areas/Effort/Scripts/MigrateEffortData.cs:1220-1304)

---

## Permission Management

### Adding Department Access

**Legacy approach:**
```cfm
<!-- DepartmentAccess.cfm - custom admin UI -->
<form action="DepartmentAccess.cfm" method="post">
    <select name="userIDs">...</select>
    <input type="text" name="deptCode" size="6" maxlength="6" />
    <input type="submit" name="action" value="Add Approver" />
</form>
```

**Modern approach:**
```csharp
// Via VIPER2 admin UI or RAPS management interface
// Insert directly into effort.UserAccess table
// Can be part of centralized user management workflow
```

### Removing Department Access

**Legacy approach:**
```sql
DELETE FROM userAccess WHERE userAccessID = 123;  -- Hard delete
```

**Modern approach:**
```sql
UPDATE effort.UserAccess
SET IsActive = 0,
    ModifiedDate = GETDATE(),
    ModifiedBy = @CurrentUserId
WHERE Id = 123;  -- Soft delete with audit
```

### Bulk Operations

**Example: Grant department access to all users with RAPS permission**

```sql
-- Find users with ViewDept permission who don't have UserAccess entries
INSERT INTO effort.UserAccess (PersonId, DepartmentCode, IsActive, CreatedDate)
SELECT
    p.PersonId,
    @DepartmentCode,
    1,
    GETDATE()
FROM users.Person p
WHERE p.PersonId IN (
    -- Get PersonIds for users with SVMSecure.Effort.ViewDept permission
    SELECT PersonId FROM users.Person
    WHERE MothraId IN (
        SELECT DISTINCT mp.member_ucdPersonUUID
        FROM RAPS.dbo.tblMemberPermission mp
        JOIN RAPS.dbo.tblPermission perm ON mp.permission_id = perm.permission_id
        WHERE perm.permission_name = 'SVMSecure.Effort.ViewDept'
    )
)
AND NOT EXISTS (
    SELECT 1 FROM effort.UserAccess ua
    WHERE ua.PersonId = p.PersonId
    AND ua.DepartmentCode = @DepartmentCode
    AND ua.IsActive = 1
);
```

---

## Security Considerations

### Defense in Depth

**Application Layer** (Primary):
- RAPS permission checks via `[Permission]` attribute
- Department filtering via EffortPermissionService
- Action-level authorization in controllers

**Database Layer** (Optional, DBA-managed):
- VIPER2 app service account has full `[effort]` schema access
- Database enforces FK constraints, CHECK constraints
- Can add Row-Level Security policies if needed (future enhancement)

**Audit Layer**:
- All UserAccess changes tracked (ModifiedBy, ModifiedDate)
- EF Core can track all data modifications
- Can integrate with existing VIPER2 audit logging

### Attack Vectors Mitigated

| Attack | Legacy Vulnerability | Modern Mitigation |
|--------|---------------------|-------------------|
| **Direct database access** | ❌ No protection | ✅ App service account uses trusted connection |
| **SQL injection** | ❌ Some dynamic SQL | ✅ EF Core parameterized queries |
| **Department bypass** | ❌ Application-only checks | ✅ Service layer + database FKs |
| **Permission escalation** | ❌ No hierarchy | ✅ Admin/Manage bypass department checks |
| **Audit trail tampering** | ❌ No audit trail | ✅ Immutable audit fields |
| **Orphaned access** | ❌ Manual cleanup | ✅ IsActive flag + reports |

---

## Migration Path

### Phase 1: RAPS Permission Setup

**Create permissions in RAPS database:**
```sql
INSERT INTO RAPS.dbo.tblPermission (permission_name, permission_description)
VALUES
    ('SVMSecure.Effort', 'Base access to Effort system'),
    ('SVMSecure.Effort.Admin', 'Full administrative access to Effort'),
    ('SVMSecure.Effort.Manage', 'Manage Effort system configuration'),
    ('SVMSecure.Effort.ViewDept', 'View effort data for authorized departments'),
    ('SVMSecure.Effort.EditDept', 'Edit effort data for authorized departments'),
    ('SVMSecure.Effort.VerifyDept', 'Verify effort for authorized departments'),
    ('SVMSecure.Effort.ViewOwn', 'View own effort records'),
    ('SVMSecure.Effort.EditOwn', 'Edit own effort allocations'),
    ('SVMSecure.Effort.RunReports', 'Run effort reports');
```

**Migrate existing users:**
- Users with legacy `SVMSecure.Effort.ViewDept` → Keep same permission
- Users with legacy `SVMSecure.Effort.ManageAccess` → Grant `SVMSecure.Effort.Manage`

### Phase 2: Implement Services

**Create files:**
1. `web/Areas/Effort/Services/EffortPermissions.cs`
2. `web/Areas/Effort/Services/IEffortPermissionService.cs`
3. `web/Areas/Effort/Services/EffortPermissionService.cs`

**Register in DI container** (`Program.cs`):
```csharp
builder.Services.AddScoped<IEffortPermissionService, EffortPermissionService>();
```

### Phase 3: Build Controllers

**Create API controllers:**
- RecordsController - Effort record CRUD
- PersonsController - Person/instructor management
- PercentagesController - Percentage allocation management
- ReportsController - Merit reports, summaries

**Apply authorization:**
```csharp
[Route("/api/effort/{controller}")]
[Permission(Allow = EffortPermissions.Base)]
public class {Controller} : ApiController
{
    private readonly IEffortPermissionService _permissionService;
    // Use _permissionService for all data access filtering
}
```

### Phase 4: Testing

**Unit tests:**
- Mock RAPSContext, VIPERContext
- Test all EffortPermissionService methods
- Verify Admin/Manage bypass logic
- Test department filtering

**Integration tests:**
- Test controllers with different user roles
- Verify department access restrictions
- Test self-service ViewOwn/EditOwn permissions

**Manual testing:**
- Admin user can access all departments
- Department user can only access assigned departments
- Faculty can view/edit their own data with ViewOwn/EditOwn

---

## Future Enhancements

### Row-Level Security (Optional)

If additional database-level enforcement is desired:

```sql
-- Create security function
CREATE FUNCTION dbo.fn_EffortSecurityPredicate(@DepartmentCode nvarchar(50))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN (
    SELECT 1 AS result
    WHERE
        -- Admin/Manage users can see all
        IS_MEMBER('EffortAdmin') = 1
        OR IS_MEMBER('EffortManage') = 1
        -- Or user has UserAccess entry for department
        OR EXISTS (
            SELECT 1 FROM effort.UserAccess ua
            JOIN users.Person p ON ua.PersonId = p.PersonId
            WHERE ua.DepartmentCode = @DepartmentCode
            AND ua.IsActive = 1
            AND p.LoginId = SUSER_SNAME()
        )
);

-- Apply to tables
CREATE SECURITY POLICY EffortDeptFilter
ADD FILTER PREDICATE dbo.fn_EffortSecurityPredicate(EffortDept)
ON effort.Persons,
ADD FILTER PREDICATE dbo.fn_EffortSecurityPredicate(EffortDept)
ON effort.Records
WITH (STATE = ON);
```

**Note**: This is **optional** and should only be implemented if defense-in-depth at database level is required. The application-level authorization via EffortPermissionService is the primary security control.

---

## References

- **VIPER2 Permission Patterns**: [PermissionAttribute.cs](../../../Classes/PermissionAttribute%20.cs)
- **ClinicalScheduler Example**: [SchedulePermissionService.cs](../../ClinicalScheduler/Services/SchedulePermissionService.cs)
- **UserHelper**: [UserHelper.cs](../../../Classes/UserHelper.cs)
- **UserAccess Migration**: [MigrateEffortData.cs](../Scripts/MigrateEffortData.cs)
- **Master Plan**: [EFFORT_MASTER_PLAN.md](EFFORT_MASTER_PLAN.md)
