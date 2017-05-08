@setlocal
@echo off

set TARGET_CONFIG=%~1
if "%TARGET_CONFIG%"=="" set TARGET_CONFIG=Release

set SRC_EXE_DIR=%~dp0\VoiceroidUtil\bin\%TARGET_CONFIG%
set SRC_EXE_FILE=%SRC_EXE_DIR%\VoiceroidUtil.exe
set SRC_DOC_FILE=%~dp0\data\readme.txt
set DEST_BASE_DIR=%~dp0\__release\VoiceroidUtil
if not "%TARGET_CONFIG%"=="Release" set DEST_BASE_DIR=%DEST_BASE_DIR%_%TARGET_CONFIG%
set DEST_SYSTEM_DIR=%DEST_BASE_DIR%\system

REM ---- check source

if not exist "%SRC_EXE_FILE%" (
  echo "%SRC_EXE_FILE%" is not found.
  goto ON_ERROR
)
if not exist "%SRC_DOC_FILE%" (
  echo "%SRC_DOC_FILE%" is not found.
  goto ON_ERROR
)

REM ---- remake destination

if exist "%DEST_BASE_DIR%" rmdir /S /Q "%DEST_BASE_DIR%"
mkdir "%DEST_BASE_DIR%"
if errorlevel 1 goto ON_ERROR

if exist "%DEST_SYSTEM_DIR%" rmdir /S /Q "%DEST_SYSTEM_DIR%"
mkdir "%DEST_SYSTEM_DIR%"
if errorlevel 1 goto ON_ERROR

REM ---- copy files

xcopy /Y "%SRC_EXE_FILE%" "%DEST_BASE_DIR%"
if errorlevel 1 goto ON_ERROR
xcopy /Y "%SRC_EXE_FILE%.config" "%DEST_BASE_DIR%"
if errorlevel 1 goto ON_ERROR
xcopy /Y "%SRC_EXE_DIR%"\*.dll "%DEST_SYSTEM_DIR%"
if errorlevel 1 goto ON_ERROR
xcopy /Y /I "%SRC_EXE_DIR%\ja" "%DEST_SYSTEM_DIR%\ja"
if errorlevel 1 goto ON_ERROR
xcopy /Y "%SRC_DOC_FILE%" "%DEST_BASE_DIR%"
if errorlevel 1 goto ON_ERROR

REM ---- remove unused files

del /S /Q "%DEST_SYSTEM_DIR%"\Xceed.Wpf.AvalonDock.* >nul 2>&1
del /S /Q "%DEST_SYSTEM_DIR%"\Xceed.Wpf.DataGrid.dll >nul 2>&1

exit /b 0

:ON_ERROR
pause
exit /b 1
