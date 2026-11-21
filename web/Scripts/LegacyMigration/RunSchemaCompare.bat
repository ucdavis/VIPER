@echo off
REM RunSchemaCompare.bat
REM Compares two environments' schema exports of the same connection. Reads only the files
REM RunSchemaExport.bat wrote; it does not connect to any database. The report is printed and
REM saved as Output\<ConnectionName>\compare-<EnvA>-<EnvB>.txt.
REM
REM Usage: RunSchemaCompare.bat <ConnectionName> <EnvA> <EnvB>
REM   Environments: Development, Test, Production
REM
REM Examples:
REM   RunSchemaCompare.bat ClinicalScheduler Test Production
REM   RunSchemaCompare.bat PhoneLists Development Test

setlocal

if "%~3"=="" goto :usage

pushd "%~dp0"
dotnet run --project LegacyMigration.csproj --configuration Release -- schema-compare --connection "%~1" --left "%~2" --right "%~3"
set "EXIT_CODE=%ERRORLEVEL%"
popd

exit /b %EXIT_CODE%

:usage
echo Usage: RunSchemaCompare.bat ^<ConnectionName^> ^<EnvA^> ^<EnvB^>
echo   Environments: Development, Test, Production
echo.
echo Example: RunSchemaCompare.bat ClinicalScheduler Test Production
exit /b 1
