@echo off
setlocal EnableExtensions

set "HOST_NAME=%~1"
if not defined HOST_NAME set "HOST_NAME=localhost"

set "USER_NAME=%~2"
if not defined USER_NAME set "USER_NAME=root"

set "PASSWORD=%~3"

set "MYSQL_EXE=%~4"
if not defined MYSQL_EXE set "MYSQL_EXE=mysql"

set "DATABASE_NAME=mtm_waitlist"
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "DATABASE_ROOT=%%~fI"

where "%MYSQL_EXE%" >nul 2>&1
if errorlevel 1 (
    echo ERROR: Could not find mysql executable "%MYSQL_EXE%".
    exit /b 1
)

if "%PASSWORD%"=="" if "%MYSQL_PWD%"=="" (
    echo ERROR: Provide a password as argument 3 or set MYSQL_PWD before running this script.
    echo Usage: 00_Database.bat [host] [user] [password] [mysqlExe]
    exit /b 1
)

set "MYSQL_ARGS=-h %HOST_NAME% -u %USER_NAME%"
if not "%PASSWORD%"=="" set "MYSQL_ARGS=%MYSQL_ARGS% -p%PASSWORD%"

call :RunInlineSql "DROP DATABASE IF EXISTS %DATABASE_NAME%;" "drop database"
if errorlevel 1 goto :Fail

call :RunSqlFile "%DATABASE_ROOT%\migrations\V001__Initial_Schema.sql"
if errorlevel 1 goto :Fail

call :RunSqlFile "%DATABASE_ROOT%\migrations\V002__Add_SchemaVersions_Table.sql"
if errorlevel 1 goto :Fail

call :RunSqlFile "%DATABASE_ROOT%\migrations\V003__SetupTech_Schema.sql"
if errorlevel 1 goto :Fail

call :RunSqlFile "%DATABASE_ROOT%\migrations\V004__SetupTech_Default_DunnageTypeConfig.sql"
if errorlevel 1 goto :Fail

call :RunSqlFile "%DATABASE_ROOT%\schema\data\System\SchemaVersions_BaselineHistory.sql"
if errorlevel 1 goto :Fail

call :RunInlineSql "SELECT 'NOTE: Completed destructive database reinstall via schema/00_Database.bat.' AS MigrationNote;" "final reinstall note"
if errorlevel 1 goto :Fail

echo Database reinstall completed successfully for %DATABASE_NAME%.
exit /b 0

:RunInlineSql
set "INLINE_SQL=%~1"
set "STEP_NAME=%~2"
echo Running %STEP_NAME%...
echo %INLINE_SQL% | "%MYSQL_EXE%" %MYSQL_ARGS%
exit /b %ERRORLEVEL%

:RunSqlFile
set "FILE_PATH=%~1"
if not exist "%FILE_PATH%" (
    echo ERROR: Required SQL file was not found: %FILE_PATH%
    exit /b 1
)
echo Running %~nx1...
"%MYSQL_EXE%" %MYSQL_ARGS% < "%FILE_PATH%"
exit /b %ERRORLEVEL%

:Fail
echo Database reinstall failed.
exit /b 1