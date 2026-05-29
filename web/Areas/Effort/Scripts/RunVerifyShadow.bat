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

REM Parse arguments using shared helper (allowed flags: --verbose; value flags: --test-mothraid)
call "%~dp0ParseArgs.bat" "--verbose" "--test-mothraid" %*
if defined PARSE_ERROR exit /b 1

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
