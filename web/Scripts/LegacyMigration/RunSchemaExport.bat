@echo off
REM RunSchemaExport.bat
REM Exports a legacy database's schema and object definitions (no table data) to
REM Output\<ConnectionName>\<Environment>\ next to this file. That folder is cleared on each run.
REM
REM Usage: RunSchemaExport.bat <ConnectionName> <Development|Test|Production>
REM
REM The connection string resolves from AWS Parameter Store as
REM /<Environment>/ConnectionStrings/<ConnectionName>.
REM
REM Examples:
REM   RunSchemaExport.bat ClinicalScheduler Test
REM   RunSchemaExport.bat ClinicalScheduler Production

setlocal

if "%~1"=="" goto :usage

set "ASPNETCORE_ENVIRONMENT="
if /i "%~2"=="Development" set "ASPNETCORE_ENVIRONMENT=Development"
if /i "%~2"=="Test" set "ASPNETCORE_ENVIRONMENT=Test"
if /i "%~2"=="Production" set "ASPNETCORE_ENVIRONMENT=Production"

if not defined ASPNETCORE_ENVIRONMENT goto :usage

pushd "%~dp0"
dotnet run --project LegacyMigration.csproj --configuration Release -- schema-export --connection "%~1"
set "EXIT_CODE=%ERRORLEVEL%"
popd

echo.
if %EXIT_CODE% EQU 0 (
    echo Export completed successfully.
) else (
    echo ERROR: Export failed with error code %EXIT_CODE%. See details above.
)
exit /b %EXIT_CODE%

:usage
echo Usage: RunSchemaExport.bat ^<ConnectionName^> ^<Development^|Test^|Production^>
echo.
echo Example: RunSchemaExport.bat ClinicalScheduler Test
exit /b 1
