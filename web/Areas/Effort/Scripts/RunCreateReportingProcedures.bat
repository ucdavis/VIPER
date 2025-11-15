@echo off
REM ================================================================
REM Effort Migration Toolkit - Reporting Procedures Creator
REM ================================================================
REM Usage: RunCreateReportingProcedures.bat [environment] [--apply] [--clean]
REM
REM Examples:
REM   RunCreateReportingProcedures.bat                    (dry-run in Development)
REM   RunCreateReportingProcedures.bat Production         (dry-run in Production)
REM   RunCreateReportingProcedures.bat --apply            (create in Development)
REM   RunCreateReportingProcedures.bat Test --apply       (create in Test)
REM   RunCreateReportingProcedures.bat --clean            (report orphaned procedures)
REM   RunCreateReportingProcedures.bat --apply --clean    (delete orphaned procedures)
REM
REM NOTE: Make sure the [effort] schema exists in VIPER database.
REM       Run RunCreateDatabase.bat and RunMigrateData.bat first.
REM ================================================================

echo.
echo ====================================================
echo EFFORT REPORTING STORED PROCEDURES CREATOR
echo ====================================================
echo.

REM Set environment
set ASPNETCORE_ENVIRONMENT=Development
if not "%~1"=="" (
    if not "%~1"=="--apply" (
        if not "%~1"=="--clean" (
            set ASPNETCORE_ENVIRONMENT=%1
        )
    )
)

echo Environment: %ASPNETCORE_ENVIRONMENT%
echo Using application configuration from appsettings.json
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET SDK 8.0 or later
    echo Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Check if project file exists
if not exist "EffortMigration.csproj" (
    echo ERROR: EffortMigration.csproj not found!
    echo Make sure you're running this from the Scripts folder.
    pause
    exit /b 1
)

REM Restore dependencies and build
echo Installing/updating dependencies...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore dependencies
    pause
    exit /b 1
)

echo Compiling script...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Failed to compile script
    echo Check for compilation errors above
    pause
    exit /b 1
)

REM Determine mode and build command arguments
echo.
set ARGS=create-reporting-procedures

REM Check for --apply flag in any position
echo %* | findstr /i "\-\-apply" >nul
if %errorlevel% equ 0 (
    set ARGS=%ARGS% --apply
    echo [APPLY MODE] Creating procedures permanently...
) else (
    echo [DRY-RUN MODE] Validating SQL only, no changes will be made...
    echo To create procedures permanently, add --apply flag
)

REM Check for --clean flag in any position
echo %* | findstr /i "\-\-clean" >nul
if %errorlevel% equ 0 (
    set ARGS=%ARGS% --clean
    echo [CLEAN MODE] Will check for orphaned procedures...
)

echo.
dotnet run --project EffortMigration.csproj --configuration Release -- %ARGS%
set EXITCODE=%errorlevel%

echo.
if %EXITCODE% equ 0 (
    echo ====================================================
    echo Reporting procedures script completed successfully!
    echo ====================================================
) else (
    echo ====================================================
    echo Script failed. Check error messages above.
    echo ====================================================
)
echo.
pause
exit /b %EXITCODE%
