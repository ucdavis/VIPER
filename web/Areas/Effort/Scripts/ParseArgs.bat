@echo off
REM ParseArgs.bat - Shared argument parsing for Effort Migration scripts
REM
REM Usage: call ParseArgs.bat %* "flag1,flag2,flag3" "valueflag1,valueflag2"
REM   %* = All arguments to parse
REM   Arg 2 = Comma-separated list of allowed boolean flags (e.g., "--apply,--force,--drop")
REM   Arg 3 = Comma-separated list of flags that take values (e.g., "--test-mothraid")
REM
REM Output variables:
REM   ASPNETCORE_ENVIRONMENT = Development|Test|Production
REM   SCRIPT_ARGS = Validated arguments to pass to dotnet run
REM   PARSE_ERROR = 1 if validation failed, empty otherwise
REM
REM Security: Only whitelisted flags are accepted, all others are rejected

setlocal EnableDelayedExpansion

REM Initialize outputs
set "ENV_OUT=Development"
set "ARGS_OUT="
set "ERROR_OUT="

REM Get the allowed flags lists (passed as last two arguments)
set "ALLOWED_FLAGS=%~1"
set "VALUE_FLAGS=%~2"
shift
shift

:parse_loop
if "%~1"=="" goto :done_parsing

REM Check for environment names
if /i "%~1"=="Development" (
    set "ENV_OUT=Development"
    shift
    goto :parse_loop
)
if /i "%~1"=="Test" (
    set "ENV_OUT=Test"
    shift
    goto :parse_loop
)
if /i "%~1"=="Production" (
    set "ENV_OUT=Production"
    shift
    goto :parse_loop
)

REM Check if it's an allowed boolean flag
set "FLAG_FOUND="
for %%F in (%ALLOWED_FLAGS:,= %) do (
    if /i "%~1"=="%%F" (
        set "ARGS_OUT=!ARGS_OUT! %%F"
        set "FLAG_FOUND=1"
    )
)
if defined FLAG_FOUND (
    shift
    goto :parse_loop
)

REM Check if it's a value flag (takes next argument as value)
for %%F in (%VALUE_FLAGS:,= %) do (
    if /i "%~1"=="%%F" (
        set "ARGS_OUT=!ARGS_OUT! %%F"
        shift
        if not "%~1"=="" (
            REM Validate MothraID format (8 digits) for --test-mothraid
            if /i "%%F"=="--test-mothraid" (
                echo %~1| findstr /r "^[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]$" >nul
                if errorlevel 1 (
                    echo ERROR: Invalid MothraID format. Must be 8 digits.
                    set "ERROR_OUT=1"
                    goto :done_parsing
                )
            )
            set "ARGS_OUT=!ARGS_OUT! %~1"
            shift
        ) else (
            echo ERROR: Flag %%F requires a value.
            set "ERROR_OUT=1"
            goto :done_parsing
        )
        goto :parse_loop
    )
)

REM Unknown argument - reject it
echo WARNING: Unknown argument '%~1' ignored
shift
goto :parse_loop

:done_parsing

REM Export results to parent scope
endlocal & set "ASPNETCORE_ENVIRONMENT=%ENV_OUT%" & set "SCRIPT_ARGS=%ARGS_OUT%" & set "PARSE_ERROR=%ERROR_OUT%"
exit /b 0
