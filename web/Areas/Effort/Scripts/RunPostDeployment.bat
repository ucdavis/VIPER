@echo off
REM RunPostDeployment.bat
REM Launcher for Effort Post-Deployment Tasks
REM
REM This script runs post-deployment tasks:
REM   1. Add SessionType flag columns (FacultyCanEnter, AllowedOnDvm, AllowedOn199299, AllowedOnRCourses)
REM   2. Add ManageSessionTypes permission to RAPS (cloned from ManageUnits)
REM
REM Usage:
REM   RunPostDeployment.bat [environment] [options]
REM   RunPostDeployment.bat              (dry-run mode - uses Development config)
REM   RunPostDeployment.bat --apply      (apply mode - uses Development config)
REM   RunPostDeployment.bat Test --apply      (apply mode - uses Test config)
REM   RunPostDeployment.bat Production --apply   (apply mode - uses Production config)
REM   RunPostDeployment.bat --task session-type-flags --apply   (run only schema task)
REM   RunPostDeployment.bat --task manage-session-types-permission --apply   (run only permission task)

setlocal

REM Parse arguments using shared helper (allowed flags: --apply, --force; allowed params: --task)
call "%~dp0ParseArgs.bat" "--apply,--force" "--task" %*
if defined PARSE_ERROR exit /b 1

echo ================================================================================
echo Effort Post-Deployment Tasks
echo ================================================================================
echo.
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo.
echo Tasks to run:
echo   1. SessionType Flag Columns - Add new columns to effort.SessionTypes table
echo   2. ManageSessionTypes Permission - Add RAPS permission cloned from ManageUnits
echo.
echo Available options:
echo   [no args]  DRY-RUN MODE - Shows what would change (default, safe)
echo   --apply    APPLY MODE - Actually apply changes (permanent changes)
echo   --force    Force recreate permission even if it exists
echo   --task ^<name^>  Run only specific task (session-type-flags or manage-session-types-permission)
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
