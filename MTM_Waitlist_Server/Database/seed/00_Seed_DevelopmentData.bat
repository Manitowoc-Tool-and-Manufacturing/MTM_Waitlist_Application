@echo off
setlocal EnableExtensions

set "HOST_NAME=%~1"
if not defined HOST_NAME set "HOST_NAME=localhost"

set "USER_NAME=%~2"
if not defined USER_NAME set "USER_NAME=root"

set "PASSWORD=%~3"

set "MYSQL_EXE=%~4"
if not defined MYSQL_EXE set "MYSQL_EXE=mysql"

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "DATABASE_ROOT=%%~fI"

where "%MYSQL_EXE%" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Could not find mysql executable "%MYSQL_EXE%".
    exit /b 1
)

if "%PASSWORD%"=="" if "%MYSQL_PWD%"=="" (
    echo ERROR: Provide a password as argument 3 or set MYSQL_PWD before running this script.
    echo Usage: 00_Seed_DevelopmentData.bat [host] [user] [password] [mysqlExe]
    exit /b 1
)

set "MYSQL_ARGS=-h %HOST_NAME% -u %USER_NAME%"
if not "%PASSWORD%"=="" set "MYSQL_ARGS=%MYSQL_ARGS% -p%PASSWORD%"

call :RunSqlFile "%DATABASE_ROOT%\seed\01_Seed_WaitlistEntries.sql"
if errorlevel 1 goto :Fail

call :RunSqlFile "%DATABASE_ROOT%\seed\02_Seed_SetupTechDunnageTypeConfig.sql"
if errorlevel 1 goto :Fail

echo Development seed data applied successfully.
exit /b 0

:RunSqlFile
set "FILE_PATH=%~1"
if not exist "%FILE_PATH%" (
    echo ERROR: Required seed file was not found: %FILE_PATH%
    exit /b 1
)
echo Running %~nx1...
"%MYSQL_EXE%" %MYSQL_ARGS% < "%FILE_PATH%"
exit /b %ERRORLEVEL%

:Fail
echo Development seed data failed.
exit /b 1