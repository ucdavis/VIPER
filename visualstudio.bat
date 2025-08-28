@echo off
REM Visual Studio wrapper for vite-plugin-vue-inspector
REM 
REM This batch file enables Visual Studio integration with Vue Inspector.
REM When you click a Vue component in the browser (after toggling inspector with Ctrl+Shift),
REM this script will open the component file in Visual Studio Community 2022.
REM
REM Note: This opens files without line navigation to ensure compatibility 
REM with existing VS instances.
REM
REM Usage: Automatically called by vite-plugin-vue-inspector when VITE_EDITOR=visual-studio
REM Parameters: %1 = file path, %2 = line number, %3 = column number (optional)
REM Prerequisites: Visual Studio Community 2022 must be installed at the path below

REM Validate that a file path was provided
if "%~1"=="" (
  echo No file supplied to visualstudio.bat
  exit /b 1
)

REM Use /edit to reuse existing VS instance
"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe" /edit "%~1"
