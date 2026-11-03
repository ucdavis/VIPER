@echo off
REM ================================================================
REM Report Builder - Schema Creation Runner
REM ================================================================
REM Usage: RunCreateSchema.bat [environment] [--apply]
REM
REM Examples:
REM   RunCreateSchema.bat                       (dry run, Development config)
REM   RunCreateSchema.bat --apply               (apply, Development config)
REM   RunCreateSchema.bat Test --apply          (apply, Test config)
REM   RunCreateSchema.bat Production --apply    (apply, Production config)
REM ================================================================

setlocal

REM --apply is a flag; anything else names the environment.
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
echo REPORT BUILDER SCHEMA
echo ================================================================
echo.
echo Environment: %ASPNETCORE_ENVIRONMENT%
if "%SCRIPT_ARGS%"=="" (echo Mode: dry run - changes are rolled back) else (echo Mode: APPLY - changes are permanent)
echo.

REM Run from this folder (the script reads appsettings from three levels up) and name the
REM project explicitly, so it works from any starting directory.
pushd "%~dp0"
dotnet run --project "%~dp0ReportBuilderScripts.csproj" -- create-schema%SCRIPT_ARGS%
set EXIT_CODE=%ERRORLEVEL%
popd

endlocal & exit /b %EXIT_CODE%
