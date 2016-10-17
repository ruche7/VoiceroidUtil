@setlocal
@echo off

if "%VS140COMNTOOLS%"=="" (
  echo VS140COMNTOOLS is not set.
  goto ON_ERROR
)

call "%VS140COMNTOOLS%\VsMSBuildCmd.bat"
if errorlevel 1 goto ON_ERROR

pushd "%~dp0"

REM ---- For clean packaging
del /S /Q VoiceroidUtil/bin/Release >nul 2>&1

REM ---- NuGet (if installed)
where nuget >nul 2>&1
if errorlevel 1 (
    echo Nuget is not installed.
) else (
    nuget update -self
    nuget restore VoiceroidUtil.sln
    if errorlevel 1 goto ON_ERROR_POPD
)

REM ---- Build solution
MSBuild VoiceroidUtil.sln /p:Configuration=Debug
if errorlevel 1 goto ON_ERROR_POPD
MSBuild VoiceroidUtil.sln /p:Configuration=Release
if errorlevel 1 goto ON_ERROR_POPD

popd

exit /b 0

:ON_ERROR_POPD
popd
:ON_ERROR
pause
exit /b 1
