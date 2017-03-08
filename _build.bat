@setlocal
@echo off

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
    xcopy /Y /E /I __resources VoiceroidUtil\resources
)

REM ---- Build solution
MSBuild VoiceroidUtil.sln /m /t:Rebuild /p:Configuration=Debug
if errorlevel 1 goto ON_ERROR_POPD
MSBuild VoiceroidUtil.sln /m /t:Rebuild /p:Configuration=Release
if errorlevel 1 goto ON_ERROR_POPD

REM ---- Reset resources
if exist __resources (
    xcopy /Y /E /I __resources_temp VoiceroidUtil\resources
    rmdir /S /Q __resources_temp >nul 2>&1
)

popd

exit /b 0

:ON_ERROR_POPD
popd
:ON_ERROR
pause
exit /b 1
