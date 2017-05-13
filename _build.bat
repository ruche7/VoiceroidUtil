@setlocal
@echo off
set RET=1

where VsMSBuildCmd.bat >nul 2>&1
if errorlevel 1 (
    echo Please set path to "Common7\Tools" in Visual Studio install directory.
    goto ON_ERROR
)

call VsMSBuildCmd.bat
if errorlevel 1 goto ON_ERROR

pushd "%~dp0"

REM ---- For clean packaging
rmdir /S /Q VoiceroidUtil\bin\Release >nul 2>&1

REM ---- NuGet (if installed)
where nuget >nul 2>&1
if errorlevel 1 (
    echo Nuget is not installed.
) else (
    nuget update -self
    nuget restore VoiceroidUtil.sln
    if errorlevel 1 goto ON_ERROR_POPD
)

REM ---- Overwrite resources
if exist __resources (
    rmdir /S /Q __resources_temp >nul 2>&1
    xcopy /Y /E /I VoiceroidUtil\resources __resources_temp
    if errorlevel 1 goto ON_ERROR_POPD
    xcopy /Y /E /I __resources VoiceroidUtil\resources
    if errorlevel 1 goto ON_ERROR_RESET_RESOURCE
)

REM ---- Build solution
MSBuild VoiceroidUtil.sln /m /t:Rebuild /p:Configuration=Debug
if errorlevel 1 goto ON_ERROR_RESET_RESOURCE
MSBuild VoiceroidUtil.sln /m /t:Rebuild /p:Configuration=Release
if errorlevel 1 goto ON_ERROR_RESET_RESOURCE

set RET=0

REM ---- Reset resources
:ON_ERROR_RESET_RESOURCE
if exist __resources (
    xcopy /Y /E /I __resources_temp VoiceroidUtil\resources
    rmdir /S /Q __resources_temp >nul 2>&1
)

:ON_ERROR_POPD
popd

:ON_ERROR
if not "%RET%"=="0" pause
endlocal && exit /b %RET%
