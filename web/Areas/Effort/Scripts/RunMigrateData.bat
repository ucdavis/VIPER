@echo off
REM RunMigrateData.bat
REM Launcher for Effort Migration Toolkit - Data Migration
REM
REM Usage:
REM   RunMigrateData.bat [environment] [options]
REM   RunMigrateData.bat              (dry-run mode - uses Development config)
REM   RunMigrateData.bat --apply      (apply mode - uses Development config)
REM   RunMigrateData.bat Test --apply      (apply mode - uses Test config)
REM   RunMigrateData.bat Production --apply   (apply mode - uses Production config)

setlocal

REM Parse arguments using shared helper (allowed flags: --apply)
call "%~dp0ParseArgs.bat" "--apply" "" %*
if defined PARSE_ERROR exit /b 1

echo ================================================================================
echo Effort Data Migration
echo ================================================================================
echo.
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo This script will migrate all data from the legacy Efforts database to the
echo modernized [VIPER].[effort] schema.
echo.
echo Available options:
echo   [no args]  DRY-RUN MODE - Tests migration but rolls back (default, safe)
echo   --apply    APPLY MODE - Actually migrate data (permanent changes)
echo.
echo ================================================================================
echo.

REM Run the C# data migration tool
echo Running data migration script...
echo.
dotnet run --project EffortMigration.csproj -- migrate-data%SCRIPT_ARGS%

REM Check if the command succeeded
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================================================
    echo Data migration completed successfully!
    echo ================================================================================
    echo.
    echo Next Steps:
    echo   1. Run RunCreateReportingProcedures.bat to create reporting stored procedures
    echo   2. Run RunCreateShadow.bat to create shadow schema for ColdFusion
    echo.
) else (
    echo.
    echo ================================================================================
    echo ERROR: Data migration failed with error code %ERRORLEVEL%
    echo ================================================================================
    echo.
    echo See error details above for specific issue.
    echo.
)

echo.
pause
