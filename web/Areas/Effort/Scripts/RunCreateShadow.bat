@echo off
REM RunCreateShadow.bat
REM Launcher for Effort Migration Toolkit - Shadow Database Creation
REM This batch file is a convenience wrapper that calls the CreateEffortShadow script
REM
REM Usage:
REM   RunCreateShadow.bat [environment] [options]
REM   RunCreateShadow.bat              (dry-run mode - uses Development config)
REM   RunCreateShadow.bat --apply      (apply mode - uses Development config)
REM   RunCreateShadow.bat Test --apply      (apply mode - uses Test config)
REM   RunCreateShadow.bat Production --apply   (apply mode - uses Production config)

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
echo Effort Shadow Database Creation
echo ================================================================================
echo.
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo This script creates the EffortShadow compatibility database for ColdFusion.
echo Target: EffortShadow database on your configured SQL Server
echo.
echo Available options:
echo   [no args]  DRY-RUN MODE - Tests but rolls back (default, safe)
echo   --apply    APPLY MODE - Actually create shadow database (permanent)
echo.
echo ================================================================================
echo.

REM Run the C# shadow creation tool
echo Running shadow database creation script...
echo.
dotnet run --project EffortMigration.csproj -- create-shadow%SCRIPT_ARGS%

REM Check if the command succeeded
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================================================
    echo Shadow database creation completed successfully!
    echo ================================================================================
    echo.
    echo Next Steps:
    echo   1. DBA will configure database permissions for applications
    echo   2. Test ColdFusion application against EffortShadow
    echo.
) else (
    echo.
    echo ================================================================================
    echo ERROR: Shadow creation failed with error code %ERRORLEVEL%
    echo ================================================================================
    echo.
    echo See error details above for specific issue.
    echo.
)

echo.
pause
