@echo off
REM ================================================================
REM Effort Data Migration Analysis Script Runner
REM ================================================================
REM Usage: RunDataAnalysis.bat [environment] [custom_config_file]
REM
REM Examples:
REM   RunDataAnalysis.bat                    (uses Development config)
REM   RunDataAnalysis.bat Production         (uses Production config)
REM   RunDataAnalysis.bat Test custom.json  (uses custom config file)
REM
REM NOTE: Make sure to add the Efforts connection string to appsettings.json:
REM   "ConnectionStrings": {
REM     "VIPER": "existing connection...",
REM     "Efforts": "Server=YOUR_SERVER;Database=Efforts;Trusted_Connection=true;"
REM   }
REM ================================================================

echo.
echo ====================================================
echo EFFORT DATA MIGRATION ANALYSIS
echo ====================================================
echo.

REM Set environment
set ASPNETCORE_ENVIRONMENT=Development
if not "%~1"=="" set ASPNETCORE_ENVIRONMENT=%1

echo Environment: %ASPNETCORE_ENVIRONMENT%

REM Check if custom config file provided
if not "%~2"=="" (
    echo Using custom config: %~2
    set CONFIG_ARG=%~2
) else (
    echo Using application configuration from appsettings.json
    set CONFIG_ARG=
)

echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET SDK 8.0 or later
    echo Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Check if project file exists, restore dependencies
if not exist "EffortDataAnalysis.csproj" (
    echo ERROR: EffortDataAnalysis.csproj not found!
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

if defined CONFIG_ARG (
    dotnet run --project . --configuration Release -- "%CONFIG_ARG%"
) else (
    dotnet run --project . --configuration Release
)

echo.
if %errorlevel% equ 0 (
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