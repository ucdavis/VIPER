@echo off
REM RunPostDeployment.bat
REM Launcher for Effort Post-Deployment Tasks
REM
REM This script runs post-deployment tasks:
REM   1. Units Migration - Simplify Units table and add UnitId FK to Percentages
REM   2. Add ManageEffortTypes permission to RAPS (cloned from ManageUnits)
REM   3. Rename tables (EffortTypes -> PercentAssignTypes, SessionTypes -> EffortTypes)
REM   4. Add EffortType flag columns (FacultyCanEnter, AllowedOnDvm, AllowedOn199299, AllowedOnRCourses)
REM   5. Rename Records.SessionType column to Records.EffortType
REM   6. Rename Percentages.EffortTypeId column to Percentages.PercentAssignTypeId
REM   7. Duplicate Records Cleanup - Remove duplicates and add unique constraint
REM   8. Add LastEmailed columns to Persons table (performance optimization)
REM   9. Simplify TermStatus table - Remove Status/CreatedDate/ModifiedDate/ModifiedBy columns
REM  10. Fix Percentage Constraint - Update CK_Percentages_Percentage from 0-100 to 0-1
REM  11. Add ViewDeptAudit permission for department chairs to view audit trail
REM  12. Fix EffortType Descriptions - Correct descriptions to match legacy CREST tbl_sessiontype
REM  13. Backfill Harvest Audit Actions - Update audit entries to use new Harvest* action types
REM  14. Add AlertStates Table - Create table for persisting data hygiene alert states
REM  15. Duplicate Percentages Cleanup - Remove duplicate percentage assignments
REM  16. Delete Guest Persons and Records - Purge legacy guest placeholder accounts
REM
REM Usage:
REM   RunPostDeployment.bat [environment] [options]
REM   RunPostDeployment.bat              (dry-run mode - uses Development config)
REM   RunPostDeployment.bat --apply      (apply mode - uses Development config)
REM   RunPostDeployment.bat Test --apply      (apply mode - uses Test config)
REM   RunPostDeployment.bat Production --apply   (apply mode - uses Production config)

setlocal

REM Parse arguments using shared helper (allowed flags: --apply, --force)
call "%~dp0ParseArgs.bat" "--apply,--force" "" %*
if defined PARSE_ERROR exit /b 1

echo ================================================================================
echo Effort Post-Deployment Tasks
echo ================================================================================
echo.
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo.
echo Tasks to run:
echo   1. Units Migration - Simplify Units table and add UnitId FK to Percentages
echo   2. ManageEffortTypes Permission - Add RAPS permission cloned from ManageUnits
echo   3. Rename Tables - EffortTypes to PercentAssignTypes, SessionTypes to EffortTypes
echo   4. EffortType Flag Columns - Add new columns to effort.EffortTypes table
echo   5. Rename Column - Records.SessionType to Records.EffortType
echo   6. Rename Column - Percentages.EffortTypeId to Percentages.PercentAssignTypeId
echo   7. Duplicate Records Cleanup - Remove duplicates and add unique constraint
echo   8. Add LastEmailed Columns - Add LastEmailed/LastEmailedBy to Persons table
echo   9. Simplify TermStatus - Remove redundant columns (Status computed from dates)
echo  10. Fix Percentage Constraint - Update from 0-100 to 0-1 range
echo  11. ViewDeptAudit Permission - Add permission for department chairs to view audit
echo  12. Fix EffortType Descriptions - Correct descriptions to match legacy CREST
echo  13. Backfill Harvest Audit Actions - Update audit entries to use Harvest* actions
echo  14. Add AlertStates Table - Create table for persisting data hygiene alert states
echo  15. Duplicate Percentages Cleanup - Remove duplicate percentage assignments
echo  16. Delete Guest Persons/Records - Purge legacy guest placeholder accounts
echo.
echo Available options:
echo   [no args]  DRY-RUN MODE - Shows what would change (default, safe)
echo   --apply    APPLY MODE - Actually apply changes (permanent changes)
echo   --force    Force recreate permission even if it exists
echo.
echo ================================================================================
echo.

REM Run the C# post-deployment tool
echo Running post-deployment script...
echo.
dotnet run --project EffortMigration.csproj -- post-deployment%SCRIPT_ARGS%

REM Check if the command succeeded
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================================================
    echo Post-deployment completed successfully!
    echo ================================================================================
    echo.
) else (
    echo.
    echo ================================================================================
    echo ERROR: Post-deployment failed with error code %ERRORLEVEL%
    echo ================================================================================
    echo.
    echo See error details above for specific issue.
    echo.
)

echo.
pause
