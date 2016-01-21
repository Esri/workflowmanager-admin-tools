@echo off
echo.

set copycmd=xcopy /q

rem ----------------------------------------------------------------------
rem Register the DLL in its current location
rem ----------------------------------------------------------------------

set dllFile=%~dp0\bin\WorkflowManagerAdministrationUtilities.dll
if not exist "%dllFile%" goto DLLNOTFOUND

rem --- Can't use a nice if-else here because of the parentheses in "Program Files (x86)"
if "%PROCESSOR_ARCHITECTURE%"=="x86" set esriRegAsm=%CommonProgramFiles%\ArcGIS\bin\esriRegAsm.exe & goto :EndSetEsriRegAsm
set esriRegAsm=%CommonProgramFiles(x86)%\ArcGIS\bin\esriRegAsm.exe
:EndSetEsriRegAsm
if not exist "%esriRegAsm%" goto EXENOTFOUND

echo Registering DLL...
"%esriRegAsm%" "%dllFile%" /p:Desktop
if %ERRORLEVEL% neq 0 call :REGFAILED


rem ----------------------------------------------------------------------
rem Find the location of the Workflow Manager Installation; copy any
rem required Workflow Manager DLLs to the current install location
rem ----------------------------------------------------------------------

if "%PROCESSOR_ARCHITECTURE%"=="x86" (
  set wmxInstallDirKeyPath=HKLM\SOFTWARE\ESRI\WMX\10.4
) else (
  set wmxInstallDirKeyPath=HKLM\SOFTWARE\Wow6432Node\ESRI\WMX\10.4
)
for /F "tokens=2* delims=	 " %%A IN ('REG QUERY "%wmxInstallDirKeyPath%" /v InstallDir') do set wmxInstallDir=%%B

rem --- JTX Shared DLL
set dllFile=%wmxInstallDir%bin\ESRI.ArcGIS.JTXUI.JTXShared.dll
if not exist "%dllFile%" goto DLLNOTFOUND

echo Copying shared DLL(s) from Workflow Manager install directory...
%copycmd% "%dllFile%" "%~dp0\bin"


rem ----------------------------------------------------------------------
rem Copy the toolbox to the ArcGIS install location
rem ----------------------------------------------------------------------

if "%PROCESSOR_ARCHITECTURE%"=="x86" (
  set arcgisInstallDirKeyPath=HKLM\SOFTWARE\ESRI\Desktop10.4
) else (
  set arcgisInstallDirKeyPath=HKLM\SOFTWARE\Wow6432Node\ESRI\Desktop10.4
)
for /F "tokens=2* delims=	 " %%A IN ('REG QUERY "%arcgisInstallDirKeyPath%" /v InstallDir') do set arcgisInstallDir=%%B

set sysToolboxDir=%arcgisInstallDir%ArcToolbox\Toolboxes
if not exist "%sysToolboxDir%" goto SYSTBXNOTFOUND

set srcToolbox=%~dp0\ArcToolbox\Toolboxes\Workflow Manager Administration Tools.tbx
if not exist "%srcToolbox%" goto TBXNOTFOUND

echo Copying toolbox to system toolboxes folder...
%copycmd% "%srcToolbox%" "%sysToolboxDir%"
if %ERRORLEVEL% neq 0 goto COPYFAILED


rem ----------------------------------------------------------------------
rem Copy any scripts to the ArcGIS install location
rem ----------------------------------------------------------------------

set sysScriptDir=%arcgisInstallDir%ArcToolbox\Scripts
if not exist "%sysScriptDir%" goto SYSSCRIPTNOTFOUND

echo Copying toolbox scripts to system toolboxes folder...
set srcScript=%~dp0\ArcToolbox\Scripts\CreateJobsBasedOnFC.py
if not exist "%srcScript%" goto SCRIPTNOTFOUND
%copycmd% "%srcScript%" "%sysScriptDir%"
if %ERRORLEVEL% neq 0 goto COPYFAILED

set srcScript=%~dp0\ArcToolbox\Scripts\DeleteJobsMatchingCriteria.py
if not exist "%srcScript%" goto SCRIPTNOTFOUND
%copycmd% "%srcScript%" "%sysScriptDir%"
if %ERRORLEVEL% neq 0 goto COPYFAILED

set srcScript=%~dp0\ArcToolbox\Scripts\SendNotificationForJobsInQuery.py
if not exist "%srcScript%" goto SCRIPTNOTFOUND
%copycmd% "%srcScript%" "%sysScriptDir%"
if %ERRORLEVEL% neq 0 goto COPYFAILED

set srcScript=%~dp0\ArcToolbox\Scripts\UploadAllTaskAssistantWorkbooks.py
if not exist "%srcScript%" goto SCRIPTNOTFOUND
%copycmd% "%srcScript%" "%sysScriptDir%"
if %ERRORLEVEL% neq 0 goto COPYFAILED


rem ----------------------------------------------------------------------
rem Copy any documentation (XML metadata) files to the ArcGIS install
rem location
rem ----------------------------------------------------------------------

echo Copying help files to the system help folder...
set helpFileDir=%arcgisInstallDir%help\gp
if not exist "%helpFileDir%" goto HELPDIRNOTFOUND

set srcHelpFileDir=%~dp0\help\gp
if not exist "%srcHelpFileDir%" goto HELPFILESNOTFOUND
%copycmd% "%srcHelpFileDir%\*_WMXAdminUtils.xml" "%helpFileDir%"
if %ERRORLEVEL% neq 0 goto COPYFAILED


rem ----------------------------------------------------------------------
rem Let the user know that they need to unzip one other GDB manually
rem ----------------------------------------------------------------------

echo.
echo **********************************************************************
echo * NOTE: If you intend to use the example scripts provided in the
echo *   "Documentation" directory, please be sure to unzip the
echo *   "SampleData.gdb" file geodatabase located in that directory.
echo **********************************************************************

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
echo ERROR: Failed to register the DLL; try registering the DLL manually?
echo   Are you an administrator on this machine?
echo   Did you select "Run as administrator" to run this .bat file?
echo   Did you "Unblock" the downloaded .zip file before extracting it?
goto :EOF

:SYSTBXNOTFOUND
echo ERROR: Could not find the system toolbox location: %sysToolboxDir%
echo   Do you have ArcGIS Desktop 10.4 installed on your system?
goto END

:SYSSCRIPTNOTFOUND
echo ERROR: Could not find the system toolbox script location: %sysScriptDir%
echo   Do you have ArcGIS Desktop 10.4 installed on your system?
goto END

:TBXNOTFOUND
echo ERROR: Could not find the toolbox: %srcToolbox%
goto END

:SCRIPTNOTFOUND
echo ERROR: Could not find the toolbox script: %srcScript%
goto END

:COPYFAILED
echo ERROR: Could not copy item to the system toolbox/script location.
echo   Are you an adminstrator on this machine?
echo   Did you select "Run as administrator" to run this .bat file?
goto END

:HELPDIRNOTFOUND
echo ERROR: Could not find the GP tool help directory: %helpFileDir%
echo   Do you have ArcGIS Desktop 10.4 installed on your system?
goto END

:HELPFILESNOTFOUND
echo ERROR: Could not find the help files: %srcHelpFileDir%
goto END


rem ----------------------------------------------------------------------
rem End the program
rem ----------------------------------------------------------------------

:END
echo.
pause
@echo on
@goto :EOF