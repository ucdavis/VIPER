@echo off
REM RunSuggestions.bat
REM Suggests declarations that Areas\<ConnectionName>\analysis.json (or, for one project of a
REM database holding several, Areas\<ConnectionName>\<Project>\analysis.json) does not have yet: guesses, from
REM table and column names and keys, of tables to exclude, the date column of each table, likely
REM relationships, and person ID columns to check; for a file with no include and no checks, also the
REM tables to choose an include from. Reads no table data. Saved to
REM Output\<ConnectionName>\analysis\<Environment>\ (plus \<Project>) as analysis.suggestions.json
REM next to this file; the committed analysis.json is read, never changed.
REM
REM Refreshes the schema export for the same environment first (as RunSchemaExport.bat does), and
REM checks that the suggestions bind against it.
REM
REM Usage: RunSuggestions.bat <ConnectionName> <Development|Test|Production> [Project]
REM
REM Examples:
REM   RunSuggestions.bat PhoneLists Test
REM   RunSuggestions.bat VIPER Production PhoneLists

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
dotnet run --project LegacyMigration.csproj --configuration Release -- suggestions --connection "%~1" %PROJECT_ARG%
set "EXIT_CODE=%ERRORLEVEL%"
popd

echo.
if %EXIT_CODE% EQU 0 (
    echo Suggestions written successfully.
) else (
    echo ERROR: Suggestions failed with error code %EXIT_CODE%. See details above.
)
exit /b %EXIT_CODE%

:usage
echo Usage: RunSuggestions.bat ^<ConnectionName^> ^<Development^|Test^|Production^> [Project]
echo.
echo Examples: RunSuggestions.bat PhoneLists Test
echo           RunSuggestions.bat VIPER Production PhoneLists
exit /b 1
