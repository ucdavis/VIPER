@echo off
setlocal enabledelayedexpansion
REM ================================================================
REM Effort Data Remediation Script Runner
REM ================================================================
REM Usage: RunDataRemediation.bat [flags] [environment]
REM
REM SAFE BY DEFAULT: Runs in DRY-RUN mode (preview only) unless --apply is used
REM
REM Flags:
REM   --apply         Actually apply changes (required to modify database)
REM   --skip-backup   Skip backup creation (only works with --apply)
REM
REM Examples:
REM   RunDataRemediation.bat                         (preview in Development - SAFE)
REM   RunDataRemediation.bat Production              (preview in Production - SAFE)
REM   RunDataRemediation.bat --apply                 (apply changes in Development)
REM   RunDataRemediation.bat --apply Production      (apply changes in Production)
REM   RunDataRemediation.bat --apply --skip-backup   (apply without backup - RISKY)
REM
REM IMPORTANT: Always review the preview output before using --apply!
REM ================================================================

echo.
echo ====================================================
echo EFFORT DATA REMEDIATION SCRIPT
echo ====================================================
echo.

REM Parse command line arguments
set APPLY_CHANGES=
set SKIP_BACKUP=
set ENVIRONMENT=Development

:parse_args
if "%~1"=="" goto done_parsing
if /i "%~1"=="--apply" (
    set APPLY_CHANGES=--apply
    shift
    goto parse_args
)
if /i "%~1"=="-a" (
    set APPLY_CHANGES=--apply
    shift
    goto parse_args
)
if /i "%~1"=="--skip-backup" (
    set SKIP_BACKUP=--skip-backup
    shift
    goto parse_args
)
if /i "%~1"=="-s" (
    set SKIP_BACKUP=--skip-backup
    shift
    goto parse_args
)
REM If not a flag, assume it's the environment
set ENVIRONMENT=%~1
shift
goto parse_args

:done_parsing

REM Set environment variable
set ASPNETCORE_ENVIRONMENT=%ENVIRONMENT%

REM Display configuration
echo Environment: %ASPNETCORE_ENVIRONMENT%

if defined APPLY_CHANGES (
    echo Mode: APPLY ^(will modify database^)
    if defined SKIP_BACKUP (
        echo Backup: DISABLED
        echo.
        echo *** WARNING: Running without backup! ***
        echo *** Make sure you have a database backup! ***
        echo.
    ) else (
        echo Backup: ENABLED ^(creates backup tables before changes^)
        echo.
    )
) else (
    echo Mode: PREVIEW ^(safe - no changes will be applied^)
    echo.
    echo *** PREVIEW MODE: No database changes will be applied ***
    echo *** Use --apply flag to actually perform remediation ***
    echo.
)

echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET SDK 8.0 or later
    echo Download from: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

REM Check if project file exists
if not exist "EffortDataAnalysis.csproj" (
    echo ERROR: EffortDataAnalysis.csproj not found!
    echo Make sure you're running this from the Scripts folder.
    echo Current directory: %CD%
    echo.
    pause
    exit /b 1
)

REM Restore dependencies and build
echo Installing/updating dependencies...
dotnet restore >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore dependencies
    echo.
    pause
    exit /b 1
)

echo Compiling remediation script...
dotnet build -c Release >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Failed to compile script
    echo Run 'dotnet build' to see compilation errors
    echo.
    pause
    exit /b 1
)

REM Confirm before running if --apply is used
if defined APPLY_CHANGES (
    echo.
    echo !!! ATTENTION !!!
    echo This will modify data in the Efforts database.
    echo.
    echo Recommended workflow:
    echo   1. Run without --apply first to preview changes
    echo   2. Review the preview report
    echo   3. Create a database backup
    echo   4. Run with --apply to actually perform fixes
    echo.
    set /p CONFIRM="Are you sure you want to proceed? (yes/no): "

    REM Compare user input (case-insensitive) - using delayed expansion
    if /i "!CONFIRM!" neq "yes" (
        echo.
        echo Operation cancelled by user.
        echo Tip: Run without --apply flag to preview changes safely.
        echo.
        pause
        exit /b 0
    )
)

REM Build the command line arguments
set ARGS=EffortDataRemediation
if defined APPLY_CHANGES set ARGS=%ARGS% %APPLY_CHANGES%
if defined SKIP_BACKUP set ARGS=%ARGS% %SKIP_BACKUP%

REM Run the remediation
echo.
echo Running remediation script...
echo.

dotnet run --project . --configuration Release -- %ARGS%

set EXIT_CODE=%errorlevel%

echo.
if %EXIT_CODE% equ 0 (
    if defined APPLY_CHANGES (
        echo ====================================================
        echo Remediation completed successfully!
        echo ====================================================
        echo.
        echo Check RemediationOutput folder for detailed reports.
        echo.
        echo Next steps:
        echo   1. Run analysis to verify: RunDataAnalysis.bat
        echo   2. Should show 0 critical issues
        echo   3. Proceed with migration if verified
    ) else (
        echo ====================================================
        echo PREVIEW completed successfully!
        echo ====================================================
        echo.
        echo Review the preview report in RemediationOutput folder.
        echo.
        echo Next steps:
        echo   1. Review RemediationReport_[timestamp].txt
        echo   2. Backup your database
        echo   3. Run: RunDataRemediation.bat --apply
    )
) else (
    echo ====================================================
    echo Script failed! Check error messages above.
    echo ====================================================
)
echo.
pause
