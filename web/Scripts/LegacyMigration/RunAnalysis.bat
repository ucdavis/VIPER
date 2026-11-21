@echo off
REM RunAnalysis.bat
REM Runs the data quality analysis against a legacy database: the automatic checks, plus any checks
REM declared in Areas\<ConnectionName>\analysis.json, or, for one project of a database holding
REM several, Areas\<ConnectionName>\<Project>\analysis.json. Reads table data and never changes it.
REM The report, saved to Output\<ConnectionName>\analysis\<Environment>\ (plus \<Project>) next to
REM this file, holds only counts, row keys, and dates.
REM
REM Refreshes the schema export for the same environment first (as RunSchemaExport.bat does), and
REM checks every declared name against it.
REM
REM Usage: RunAnalysis.bat <ConnectionName> <Development|Test|Production> [Project]
REM
REM Examples:
REM   RunAnalysis.bat PhoneLists Test
REM   RunAnalysis.bat VIPER Production PhoneLists

setlocal

if "%~1"=="" goto :usage

set "ASPNETCORE_ENVIRONMENT="
if /i "%~2"=="Development" set "ASPNETCORE_ENVIRONMENT=Development"
if /i "%~2"=="Test" set "ASPNETCORE_ENVIRONMENT=Test"
if /i "%~2"=="Production" set "ASPNETCORE_ENVIRONMENT=Production"

if not defined ASPNETCORE_ENVIRONMENT goto :usage

set "PROJECT_ARG="
if not "%~3"=="" set PROJECT_ARG=--project "%~3"

pushd "%~dp0"
dotnet run --project LegacyMigration.csproj --configuration Release -- analysis --connection "%~1" %PROJECT_ARG%
set "EXIT_CODE=%ERRORLEVEL%"
popd

echo.
if %EXIT_CODE% EQU 0 (
    echo Analysis completed successfully.
) else (
    echo ERROR: Analysis failed with error code %EXIT_CODE%. See details above.
)
exit /b %EXIT_CODE%

:usage
echo Usage: RunAnalysis.bat ^<ConnectionName^> ^<Development^|Test^|Production^> [Project]
echo.
echo Examples: RunAnalysis.bat PhoneLists Test
echo           RunAnalysis.bat VIPER Production PhoneLists
exit /b 1
