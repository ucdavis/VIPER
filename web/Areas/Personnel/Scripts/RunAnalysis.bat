@echo off
REM ================================================================
REM PhoneLists Migration Toolkit - Data Analysis Runner
REM ================================================================
REM Usage: RunAnalysis.bat [environment]
REM
REM Examples:
REM   RunAnalysis.bat                    (uses Development config)
REM   RunAnalysis.bat Test               (uses Test config)
REM   RunAnalysis.bat Production         (uses Production config)
REM
REM NOTE: Make sure the PhoneLists connection string is set for the target
REM environment's appsettings (or AWS Parameter Store) before running:
REM   "ConnectionStrings": {
REM     "VIPER": "existing connection...",
REM     "PhoneLists": "Server=YOUR_SERVER;Database=PhoneLists;Trusted_Connection=true;"
REM   }
REM ================================================================

echo.
echo ====================================================
echo PHONELISTS DATA MIGRATION ANALYSIS
echo ====================================================
echo.

REM Set environment
set ASPNETCORE_ENVIRONMENT=Development
if not "%~1"=="" set ASPNETCORE_ENVIRONMENT=%1

echo Environment: %ASPNETCORE_ENVIRONMENT%
echo Using application configuration from appsettings.json
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET SDK 10.0 or later
    echo Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Check if project file exists
if not exist "PhoneListsMigration.csproj" (
    echo ERROR: PhoneListsMigration.csproj not found!
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

dotnet run --project PhoneListsMigration.csproj --configuration Release -- analysis

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
