@echo off
REM RunExportSchema.bat
REM Launcher for Effort Migration Toolkit - Schema Export
REM This batch file is a convenience wrapper that calls the consolidated EffortMigration toolkit

setlocal

echo ================================================================================
echo Effort Database Schema Export
echo ================================================================================
echo.
echo This script will export the LEGACY Effort database schema and sample data.
echo Output: %CD%\Effort_Database_Schema_And_Data_LEGACY.txt
echo.
echo ================================================================================
echo.

REM Run the C# export tool via consolidated EffortMigration toolkit
echo Running schema export...
echo.
dotnet run --project EffortMigration.csproj -- schema-export

REM Check if the command succeeded
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================================================
    echo Export completed successfully!
    echo ================================================================================
    echo.
    echo The exported schema file is located at:
    echo   %CD%\Effort_Database_Schema_And_Data_LEGACY.txt
    echo.
) else (
    echo.
    echo ================================================================================
    echo ERROR: Export failed with error code %ERRORLEVEL%
    echo ================================================================================
    echo.
    echo See error details above for specific issue.
    echo.
)

echo.
pause
