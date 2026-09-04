@echo off
REM ================================================================
REM PhoneLists Migration Toolkit - Data Migration Runner
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
REM ================================================================

setlocal

REM Parse arguments: --apply is a flag, anything else names the environment.
REM Compared directly rather than pattern-matched - an earlier findstr version
REM silently took --apply as the environment name, which left every connection
REM string empty because AWS was then asked for parameters under /--apply.
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
echo PHONELISTS DATA MIGRATION
echo ================================================================
echo.
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo.
echo Available options:
echo   [no args]  DRY-RUN MODE - previews the migration, then rolls back (safe)
echo   --apply    APPLY MODE - writes permanently, requires typing DELETE
echo.
echo ================================================================
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
if not exist "PhoneListsMigration.csproj" (
    echo ERROR: PhoneListsMigration.csproj not found!
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

dotnet run --project PhoneListsMigration.csproj --configuration Release -- migrate-data%SCRIPT_ARGS%

echo.
if %errorlevel% equ 0 (
    echo ================================================================
    echo Migration run completed.
    echo Review the output above before proceeding.
    echo ================================================================
) else (
    echo ================================================================
    echo Migration failed. Check error messages above.
    echo ================================================================
)
echo.
pause
