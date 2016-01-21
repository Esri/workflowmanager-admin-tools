@echo off
echo.

rem ----------------------------------------------------------------------
rem Unregister the DLL in its current location
rem ----------------------------------------------------------------------

set dllFile=%~dp0\bin\WorkflowManagerAdministrationUtilities.dll
if not exist "%dllFile%" goto DLLNOTFOUND

rem --- Can't use a nice if-else here because of the parentheses in "Program Files (x86)"
if "%PROCESSOR_ARCHITECTURE%"=="x86" set esriRegAsm=%CommonProgramFiles%\ArcGIS\bin\esriRegAsm.exe & goto :EndSetEsriRegAsm
set esriRegAsm=%CommonProgramFiles(x86)%\ArcGIS\bin\esriRegAsm.exe
:EndSetEsriRegAsm
if not exist "%esriRegAsm%" goto EXENOTFOUND

echo Unregistering DLL...
"%esriRegAsm%" "%dllFile%" /p:Desktop /u
if %ERRORLEVEL% neq 0 call :REGFAILED


rem ----------------------------------------------------------------------
rem Remove any shared DLLs that we had to copy over
rem ----------------------------------------------------------------------

echo Removing shared DLL(s) copied from Workflow Manager install directory...
set dllFile=%~dp0\bin\ESRI.ArcGIS.JTXUI.JTXShared.dll
if exist "%dllFile%" del "%dllFile%"


rem ----------------------------------------------------------------------
rem Remove the toolbox from the system toolboxes folder
rem ----------------------------------------------------------------------

if "%PROCESSOR_ARCHITECTURE%"=="x86" (
  set arcgisInstallDirKeyPath=HKLM\SOFTWARE\ESRI\Desktop10.4
) else (
  set arcgisInstallDirKeyPath=HKLM\SOFTWARE\Wow6432Node\ESRI\Desktop10.4
)
for /F "tokens=2* delims=	 " %%A IN ('REG QUERY "%arcgisInstallDirKeyPath%" /v InstallDir') do set arcgisInstallDir=%%B
set sysToolboxDir=%arcgisInstallDir%ArcToolbox\

set itemToDelete=%sysToolboxDir%Toolboxes\Workflow Manager Administration Tools.tbx
if not exist "%itemToDelete%" goto ITEMNOTFOUND

echo Removing toolbox from system toolboxes folder...
del "%itemToDelete%"
if %ERRORLEVEL% neq 0 call :DELFAILED


rem ----------------------------------------------------------------------
rem Remove any supporting scripts from the system toolboxes folder
rem ----------------------------------------------------------------------

echo Removing toolbox scripts from system toolboxes folder...

set itemToDelete=%sysToolboxDir%Scripts\CreateJobsBasedOnFC.py
if not exist "%itemToDelete%" goto ITEMNOTFOUND
del "%itemToDelete%"
if %ERRORLEVEL% neq 0 call :DELFAILED

set itemToDelete=%sysToolboxDir%Scripts\DeleteJobsMatchingCriteria.py
if not exist "%itemToDelete%" goto ITEMNOTFOUND
del "%itemToDelete%"
if %ERRORLEVEL% neq 0 call :DELFAILED

set itemToDelete=%sysToolboxDir%Scripts\SendNotificationForJobsInQuery.py
if not exist "%itemToDelete%" goto ITEMNOTFOUND
del "%itemToDelete%"
if %ERRORLEVEL% neq 0 call :DELFAILED

set itemToDelete=%sysToolboxDir%Scripts\UploadAllTaskAssistantWorkbooks.py
if not exist "%itemToDelete%" goto ITEMNOTFOUND
del "%itemToDelete%"
if %ERRORLEVEL% neq 0 call :DELFAILED


rem ----------------------------------------------------------------------
rem Remove any documentation (XML metadata) files from the GP tool help
rem folder
rem ----------------------------------------------------------------------

echo Removing GP tool help from system help folder...
set sysHelpDir=%arcgisInstallDir%help\gp\
set itemToDelete=%sysHelpDir%*_WMXAdminUtils.xml
del "%itemToDelete%"
if %ERRORLEVEL% neq 0 call :DELFAILED

goto END


rem ----------------------------------------------------------------------
rem Misc. error messages & subroutines
rem ----------------------------------------------------------------------

:DLLNOTFOUND
echo ERROR: Could not find the DLL: %dllFile%
goto END

:EXENOTFOUND
echo ERROR: Could not find esriRegAsm where expected: %esriRegAsm%
goto END

:REGFAILED
echo ERROR: Failed to unregister the DLL; are you an administrator on this machine?
echo   Did you select "Run as administrator" to run this .bat file?
goto :EOF

:ITEMNOTFOUND
echo ERROR: Could not find the item: %itemToDelete%
goto END

:DELFAILED
echo ERROR: Could not delete item: %itemToDelete%
echo   Ensure that all ArcGIS applications/processes are closed, and that
echo   you are an administrator on this machine.
echo   Did you select "Run as administrator" to run this .bat file?
goto :EOF


rem ----------------------------------------------------------------------
rem End the program
rem ----------------------------------------------------------------------

:END
echo.
pause
@echo on
@goto :EOF
