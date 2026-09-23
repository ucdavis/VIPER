@echo off
REM ================================================================
REM Career Selection Migration Toolkit - Data Analysis Runner
REM ================================================================
REM Usage: RunAnalysis.bat [environment]
REM
REM Examples:
REM   RunAnalysis.bat                    (uses Development config)
REM   RunAnalysis.bat Test               (uses Test config)
REM   RunAnalysis.bat Production         (uses Production config)
REM
REM Reads only. The SIS connection is forced to ApplicationIntent=ReadOnly
REM by the script, so this cannot write to the legacy student system.
REM
REM Needs three connection strings for the target environment, all of which
REM the web app already uses: SIS (legacy source), VIPER (destination and
REM users.Person) and AAUD (vw_CurrentAffiliates, for the mentor check).
REM ================================================================

echo.
echo ====================================================
echo CAREER SELECTION DATA MIGRATION ANALYSIS
echo ====================================================
echo.

REM setlocal so ASPNETCORE_ENVIRONMENT does not outlive this script. Without it,
REM "RunAnalysis.bat Production" leaves Production set in the caller's shell, where
REM a later dotnet run would pick it up without anyone noticing.
setlocal

REM Set environment
set ASPNETCORE_ENVIRONMENT=Development
if not "%~1"=="" set ASPNETCORE_ENVIRONMENT=%1

echo Environment: %ASPNETCORE_ENVIRONMENT%
echo Using application configuration from appsettings.json
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

REM Restore dependencies and build
echo Installing/updating dependencies...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore dependencies
    pause
    exit /b 1
)

echo Compiling analysis script...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Failed to compile script
    echo Check for compilation errors above
    pause
    exit /b 1
)

REM Run the analysis
echo.
echo Running analysis...
echo.

dotnet run --project CareerSelectionMigration.csproj --configuration Release -- analysis

REM Captured before anything else runs, and returned at the end, so a chained or automated
REM caller sees the failure the banner below reports.
set "RC=%errorlevel%"

echo.
if %RC% equ 0 (
    echo ====================================================
    echo Analysis completed successfully!
    echo Check the AnalysisOutput folder for detailed reports.
    echo ====================================================
) else (
    echo ====================================================
    echo Analysis failed. Check error messages above.
    echo ====================================================
)
echo.
pause
exit /b %RC%
