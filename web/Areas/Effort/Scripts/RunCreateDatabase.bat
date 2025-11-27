@echo off
REM RunCreateDatabase.bat
REM Launcher for Effort Migration Toolkit - Schema Creation
REM This batch file is a convenience wrapper that calls the CreateEffortDatabase script
REM
REM Usage:
REM   RunCreateDatabase.bat [environment] [options]
REM   RunCreateDatabase.bat              (dry-run mode - uses Development config)
REM   RunCreateDatabase.bat --apply      (apply mode - uses Development config)
REM   RunCreateDatabase.bat Test --apply      (apply mode - uses Test config)
REM   RunCreateDatabase.bat Production --apply   (apply mode - uses Production config)

setlocal

REM Parse arguments to extract environment if provided
set ASPNETCORE_ENVIRONMENT=Development
set SCRIPT_ARGS=

:parse_args
if "%~1"=="" goto :done_parsing
if /i "%~1"=="Development" (
    set ASPNETCORE_ENVIRONMENT=Development
    shift
    goto :parse_args
)
if /i "%~1"=="Test" (
    set ASPNETCORE_ENVIRONMENT=Test
    shift
    goto :parse_args
)
if /i "%~1"=="Production" (
    set ASPNETCORE_ENVIRONMENT=Production
    shift
    goto :parse_args
)
set SCRIPT_ARGS=%SCRIPT_ARGS% %~1
shift
goto :parse_args

:done_parsing

echo ================================================================================
echo Effort Schema Creation
echo ================================================================================
echo.
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo Target: [VIPER].[effort] schema on your configured SQL Server
echo.
echo Available options:
echo   [no args]  DRY-RUN MODE - Tests SQL but rolls back (default, safe)
echo   --apply    APPLY MODE - Actually create tables (permanent changes)
echo   --force    Drop and recreate schema (requires confirmation if data exists)
echo   --drop     Drop schema only without recreating
echo.
echo ================================================================================
echo.

REM Run the C# schema creation tool
echo Running schema creation script...
echo.
dotnet run --project EffortMigration.csproj -- create-database%SCRIPT_ARGS%

REM Check if the command succeeded
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================================================
    echo Schema creation completed successfully!
    echo ================================================================================
    echo.
    echo Next Steps:
    echo   1. Run RunMigrateData.bat to migrate data from legacy database
    echo   2. Run RunCreateReportingProcedures.bat to create reporting stored procedures
    echo   3. Run RunCreateShadow.bat to create shadow schema for ColdFusion
    echo.
) else (
    echo.
    echo ================================================================================
    echo ERROR: Schema creation failed with error code %ERRORLEVEL%
    echo ================================================================================
    echo.
    echo See error details above for specific issue.
    echo.
)

echo.
pause
