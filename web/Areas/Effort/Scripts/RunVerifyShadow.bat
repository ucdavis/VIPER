@echo off
REM ==============================================================================
REM Effort Shadow Schema Verification
REM ==============================================================================
REM Compares legacy Efforts DB stored procedures vs [EffortShadow] schema procedures
REM to verify the shadow schema compatibility layer works correctly.
REM
REM Usage:
REM   RunVerifyShadow.bat                                     - Auto-select random employee with effort data
REM   RunVerifyShadow.bat --test-mothraid <mothraid>          - Use specific MothraID (8-digit)
REM   RunVerifyShadow.bat --test-mothraid <id> --verbose      - Detailed output
REM   RunVerifyShadow.bat --test-mothraid <id> Test           - Run in Test environment
REM
REM IMPORTANT: If no MothraID specified, script will auto-select a random employee
REM            with effort data in academic year 2023-2024 (termcodes 202301-202410).
REM ==============================================================================

setlocal

REM Parse arguments to extract environment
set ASPNETCORE_ENVIRONMENT=Development
set SCRIPT_ARGS=

:parse_args
if "%~1"=="" goto :done_parsing

if /i "%~1"=="Development" (
    set ASPNETCORE_ENVIRONMENT=Development
    shift
    goto :parse_args
)
if /i "%~1"=="Test" (
    set ASPNETCORE_ENVIRONMENT=Test
    shift
    goto :parse_args
)
if /i "%~1"=="Production" (
    set ASPNETCORE_ENVIRONMENT=Production
    shift
    goto :parse_args
)
set SCRIPT_ARGS=%SCRIPT_ARGS% %~1
shift
goto :parse_args

:done_parsing

echo ================================================================================
echo Effort Shadow Schema Verification
echo ================================================================================
echo.
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo This script compares legacy Efforts database stored procedures against
echo [EffortShadow] schema procedures to verify the shadow schema compatibility
echo layer works correctly.
echo.
echo ================================================================================
echo.

REM Run the verification script
echo Running verification script...
echo.
dotnet run --project EffortMigration.csproj -- verify-shadow%SCRIPT_ARGS%

REM Check if the command succeeded
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================================================
    echo Verification completed successfully - Shadow schema procedures match legacy!
    echo ================================================================================
    echo.
    echo Next Steps:
    echo   1. Update ColdFusion datasource to VIPER database with [EffortShadow] schema ^(TEST environment only!^)
    echo   2. Test all ColdFusion CRUD operations
    echo   3. Verify authorization queries work correctly
    echo   4. Test all reports generation
    echo   5. Performance validation ^(should be less than 20%% slower than baseline^)
    echo.
) else (
    echo.
    echo ================================================================================
    echo WARNING: Verification found differences
    echo ================================================================================
    echo.
    echo Next Steps:
    echo   1. Review AnalysisOutput\shadow-verification.txt for detailed differences
    echo   2. Investigate shadow schema view/procedure issues
    echo   3. Verify data migration completed successfully
    echo   4. Check shadow schema creation script for errors
    echo.
)

echo.
pause

endlocal
exit /b %ERRORLEVEL%
