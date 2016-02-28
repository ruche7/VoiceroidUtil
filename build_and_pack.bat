@setlocal
@echo off

echo -------- BUILD --------
call "%~dp0\_build.bat"
if errorlevel 1 goto ON_ERROR

echo -------- PACK --------
call "%~dp0\_pack.bat"
if errorlevel 1 goto ON_ERROR

exit /b 0

:ON_ERROR
exit /b 1
