@echo off
REM ================================================================
REM Career Selection Migration Toolkit - Data Migration Runner
REM ================================================================
REM Usage: RunMigrateData.bat [environment] [--apply]
REM
REM Examples:
REM   RunMigrateData.bat                       (dry run, Development config)
REM   RunMigrateData.bat --apply               (apply, Development config)
REM   RunMigrateData.bat Test                  (dry run, Test config)
REM   RunMigrateData.bat Test --apply          (apply, Test config)
REM   RunMigrateData.bat Production --apply    (apply, Production config)
REM
REM Run RunAnalysis.bat first - this script re-checks its structural
REM assertions as pre-flight guards and aborts rather than writing if
REM the target environment's data violates them.
REM
REM Without --apply everything runs inside a transaction that is rolled
REM back, so a dry run exercises every insert and constraint without
REM keeping the result. The destination DDL must already have been run.
REM ================================================================

setlocal

REM Parse arguments: --apply is a flag, anything else names the environment.
REM Compared directly rather than pattern-matched - pattern matching silently
REM took --apply as the environment name, which left every connection string
REM empty because AWS was then asked for parameters under /--apply.
set ASPNETCORE_ENVIRONMENT=Development
set SCRIPT_ARGS=

:parse
if "%~1"=="" goto parsed
if /i "%~1"=="--apply" (set SCRIPT_ARGS= --apply) else (set ASPNETCORE_ENVIRONMENT=%~1)
shift
goto parse
:parsed

echo.
echo ================================================================
echo CAREER SELECTION DATA MIGRATION
echo ================================================================
echo.
echo Environment: %ASPNETCORE_ENVIRONMENT%
if "%SCRIPT_ARGS%"=="" (
    echo Mode: DRY RUN - everything rolls back
) else (
    echo Mode: APPLY - writes permanently
)
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET SDK 10.0 or later
    echo Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Check if project file exists
if not exist "CareerSelectionMigration.csproj" (
    echo ERROR: CareerSelectionMigration.csproj not found!
    echo Make sure you're running this from the Scripts folder.
    pause
    exit /b 1
)

echo Installing/updating dependencies...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore dependencies
    pause
    exit /b 1
)

echo Compiling migration script...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Failed to compile script
    echo Check for compilation errors above
    pause
    exit /b 1
)

echo.
echo Running migration...
echo.

dotnet run --project CareerSelectionMigration.csproj --configuration Release -- migrate-data%SCRIPT_ARGS%

REM Captured before anything else runs, and returned at the end, so a chained or automated
REM caller sees the failure the banner below reports.
set "RC=%errorlevel%"

echo.
if %RC% equ 0 (
    echo ================================================================
    echo Migration run completed. Check the output above for the summary.
    echo ================================================================
) else if %RC% equ 2 (
    echo ================================================================
    echo Aborted at the confirmation prompt. Nothing was written.
    echo ================================================================
) else if %RC% equ 3 (
    echo ================================================================
    echo The migration COMMITTED, but a step after the commit failed.
    echo Read the output above. Do NOT re-run this migration.
    echo ================================================================
) else (
    echo ================================================================
    echo Migration failed. Check error messages above. Nothing was written.
    echo ================================================================
)
echo.
pause
exit /b %RC%
