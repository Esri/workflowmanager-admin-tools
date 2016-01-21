+-------------------------------------------------------------------+
|             Workflow Manager Administration Utilities             |
|                     v10.4.0.1, Jan. 19, 2016                      |
+-------------------------------------------------------------------+

+----------------+
| CHANGE HISTORY |
+----------------+
V10.4.0.1 (Jan 19, 2016)
 - Rebuild DLL for 10.4
 - Upgrad solution & project files for Visual Studio 2013
 - Fix issue with "revoke" in "Modify Administrator Access" tool
 - Fix issue with "job type has no workflow" in "Delete Orphaned Types"

v10.2.0.2 (Aug 10, 2013)
 - Resolved GP 125999 issue with Report Possible Errors and List Jobs when connecting to Workflow Manager DB as non-data owner

v10.2.0.1 (Aug 3, 2013)
 - Initial build/release for 10.2 Final
 - Resolved issue with Delete Orphaned Types (crashed if an expected system status type was not found)
 - Fixed GP 125999 error when executing tools
 - Updated readme file

v10.1.0.1 (Oct 1, 2012)
 - Initial build/release for 10.1 Final
 - Updated readme file references (10.0 --> 10.1, VS 2008 --> VS 2010, Python 2.6 --> Python 2.7)

v10.0.4.2 (Jul 8, 2012)
  - Modifications to address issues when "Authenticate Users on Domain" setting is enabled

v10.0.4.1 (Apr 27, 2012)
  - Rebuilding DLL for 10.0 SP4
  - Upgraded solution & project files for Visual Studio 2010

v10.0.3.1 (Nov 4, 2011)
  - Rebuilding DLL for 10.0 SP3

v10.0.2.2 (Jul 6, 2011)
  - Adding workaround for problem related to background geoprocessing and extension licenses
  - Fixing parameter text for "Delete Orphaned Types"

v10.0.2.1 (May 18, 2011)
  - Rebuilding DLL for 10.0 SP2

v10.0.1.1 (Apr 12, 2011)
NOTE: This release includes extensive changes to function interfaces.  See below for the full list of changes.  If you used a previous release of the Workflow Manager Adminstration Utilities, particularly in a geoprocessing script, pay particular attention to any argument changes.  Also be aware that your script will now need to check out a Workflow Manager license *BEFORE* importing a toolbox that includes any of the utilities.  Please refer to the sample scripts in the "Documentation" folder.

  - Changing version numbering scheme to align with ArcGIS version numbering (<major release>.<minor release>.<service pack>.<WMX utils release>).  Release number will indicate the ArcGIS version against which the included DLL has been built.
  - Changing "Create Jobs" interface to take assignee type & name rather than user name & group name
  - Updating sample scripts, documentation, etc. to reflect changes to "Create Jobs"
  - Changing "Create Data Workspaces..." tool to take additional argument; adding capability to abort on or ignore DB connection errors; other misc. code revisions
  - Changing *all* references to "Workflow Manager Developer Utilities" to be "Workflow Manager Administration Utilities" (classnames, toolbox names, alias names, DLLs, etc.)
  - Modifying "Report Possible Errors" to take an optional "output log file" parameter
  - Adding "Export Data Workspaces to Excel" GP tool
  - Adding "Is Encrypted" field to worksheets used by "Create Data Workspaces..." and "Export Data Workspaces..." tools; updating sample worksheet
  - Relocating data workspace-related tools into their own tool category
  - Relocating map document-related tools into their own tool category
  - Relocating Task Assistant Workbook-related tools into their own tool category
  - Renaming "Security" and "Notifications" tool categories
  - Adding "WorkspacesSample1.py" sample script to "Documentation" folder
  - Renaming all internal functions to use C# standard "PascalCase" function names (instead of "camelCase")
  - Changing "Delete Orphaned Types" tool: one "types" argument instead of many; also better "lookahead" support for finding unused items and fixing issue related to non-existent users
  - Adding "List Jobs Using Query" tool
  - Adding "Send Job Notification" tool
  - Adding "Send Notification for Jobs in Query" script
  - Modifying "Report Possible Errors" to check for jobs that appear to be their own parent
  - Modifying all tools and scripts; adding optional parameter to allow specification of non-default Workflow Manager databases
  - Updating all sample scripts to check out Workflow Manager license before importing WMX Admin Utils toolbox
  - Added output parameter to "Delete Jobs Matching Criteria" and "Upload All Task Assistant Workbooks"
  - Adding note to FAQ about error caused by Windows blocking downloaded files (registration failed w/HRESULT 0x80131515)

v1.6.1 (Mar 1, 2011)
  - Fixing certain GP tools to only accept non-closed job IDs as arguments (not all job IDs)
  - Fixing issue with parameter update logic in "Assign Job"
  - Fixing sorting problem with "List Jobs" output
  - "Add Attachment to Job", "Assign Job", "Close Job", and "Create Job" now log actions and support sending notifications
  - Changing project to copy JTXShared DLL locally when rebuilding
  - Updates to GP tool help

v1.6.0 (Jan 28, 2011)
  - Adding "Close Job" tool
  - Adding "Assign Job" tool
  - Adding "Add Attachment to Job" tool
  - Adding "Add Comment to Job" tool
  - Adding "Set Default Workspace for Job Type" tool
  - Fixing issue with group assignment parameter for "Create Job" tool (parameter domain was incorrectly populated when user had the "AssignAnyJob" privilege)
  - Fixing issue with "Create Job" not logging all actions like the core tool
  - Fixing issue with "Create Job" not setting job status correctly
  - Created new class for common helper functions (Common.WmduHelperFunctions)
  - Changed project to copy "JTXShared" DLL to build location when rebuilding project (fixed exceptions with certain tools that use the functionality in this DLL)
  - Fixing "List Jobs" to check for an unusual error with the JTX_JOBS table
  - Misc. code revisions
  - Adding missing tools to readme
  - Fixing dates in readme
  - Removing duplicated documentation from readme; referring user to GP tool help

v1.5.2 (Jan 6, 2011)
  - Adding missing documentation for script-based tools

v1.5.1 (Jan 5, 2011)
  - Adding documentation for all tools; modifying installer to support
  - Adding checks to "Delete Orphaned Types"
  - Adding checks to "Report Possible Errors"
  - Revising interface to "Report Possible Errors" to better support new checks in the future
  - Fixing bug with "zero step pct." check in "Report Possible Errors"
  - Fixing launch issue with "Modify Administrative Access"
  - Fixing minor parameter type issues with "Modify Administrative Access" and "Modify Privilege Assignment"
  - Misc. minor code revisions

v1.5.0 (Dec 8, 2010)
  - Fixing bug where Data Workspace parameter domains would not refresh when relaunching a tool
  - Adding sample script showing how to use the TAM & MXD upload utilities, and the JXL backup utility ("UploadAndBackupSample1.py")
  - Adding "bookend" GP tools for more complete functionality:
    - Delete Data Workspace
    - Delete Map Document
    - Delete Task Assistant Workbook
    - List All Data Workspaces
  - Adding "Report Possible Errors" tool
  - Misc. changes to eliminate warnings during build
  - Moving "BackupWorkflowManagerDatabase.py" script into Documentation folder as a sample and renaming; adding to script comments
  - Removing "PythonScripts" directory (now empty)

v1.4.1 (Dec 1, 2010)
  - Fixing issue with boolean arguments not resetting to default on subsequent runs of a tool
  - Consolidated most domain-building functions into a shared helper class

v1.4.0 (Nov 19, 2010)
  - Adding to "troubleshooting" section
  - Added "Create Spatial Notification with E-mail Notifier 2"; does not require an existing e-mail notification as a template; also added sample for this tool
  - Added "Import Active Directory Configuration" tool
  - Added "List Users" tool
  - Added "Modify Administrator Access" tool
  - Added "Modify Privilege Assignment" tool
  - Reorganized tool categories
  - Adding sample to demonstrate recently added tools

v1.3.1 (Nov 16, 2010)
  - Making "Upload All Task Assistant Workbooks" into a toolbox script; other minor changes to script
  - Most tools now check to see if current user is a Workflow Manager DB administrator (exceptions are "Create Job", "Delete Job", and "List Job", which rely on different permissions)
  - Fixing errors reported by TAM & MXD download tools
  - Fixing incorrect references to "notifier" in code, documentation, and tool names (often should have been "notification")
  - Modifying GP tools to have an output parameter, for ease of use in models
  - Modified "Create Spatial Notification with E-mail Notifier" tool to print a warning after *every* run, reminding user that at least one evaluator must be added to it
  - Adding "Troubleshooting/FAQ/Known Issues" section to readme
  - Minor code enhancements (common GP boolean values, adding WmduParameterMap class, removing certain inline string constants, ...)

v1.3.0 (Nov 11, 2010)
  - Added spatial notifier-related tools ("Create Spatial Notification from E-mail Notifier", "Add Area Evaluator to Spatial Notifier", "Add Dataset Condition to Spatial Notifier")
  - Added example scripts & sample data related to the use of the spatial notifier tools
  - Minor fixes to install script (removing extra "set" command)
  - Fix to "Create Job" (data workspace parameter was not working when entered from Python script)
  - Fixing project references (build settings)

v1.2.5 (Oct 29, 2010)
  - Adding notes to code for "Create Job" and "Delete Job" tools; expect that there will be new interfaces available for these tools after 10.0, and anyone developing with/for these tools should be cognizant of this

v1.2.4 (Oct 29, 2010)
  - Forgot to update readme details for "Create Jobs Based on FC" tool

v1.2.3 (Oct 28, 2010)
  - Added "Create Jobs based on FC" script tool
  - Modified errors and exception handling to simplify error messages
  - Relocated many UI descriptions & certain string constants into a resource file to more easily allow for internationalization

v1.2.2 (Oct 22, 2010)
  - Revisions to "Delete Job" tool to better handle non-existent job versions
  - Edits to all Python scripts to report messages from previously called GP tools

v1.2.1 (Oct 21, 2010)
  - Revisions to "Create Job" tool to work with job type defaults
  - Adding messages to "Create Job" when it uses a job type's default setting(s)
  - Adding explicit "[Unassigned]" and "[Not set]" options to for certain job values in "Create Job" where users may want to override the default setting(s)
  - Improving logging in installer script

v1.2.0 (Oct 20, 2010)
  - Added "Create Job" tool
  - Introduced permissions support for "Create Job" and "Delete Job"
  - Added universal checking for default Workflow Manager database

v1.1.1 (Oct 12, 2010)
  - Added "DeleteJobsMatchingCriteria" script
  - Reorganized folders (ArcToolbox\Toolboxes and ArcToolbox\Scripts); matching revisions to installer

v1.1 (Oct 11, 2010)
  - Revised .bat files and C# project to support both 32- and 64-bit versions of Windows
  - Added instructions related to building and installing on UAC-enabled systems (Vista, Windows 7)
  - Fixed output parameters for "List Map Documents" and "List Maps" (params were not populated correctly)
  - Added "List Jobs" and "Delete Job" tools
  - Captured revision history in readme

v1.02 (Sep 23, 2010)
  - Additions to readme file; minor clean-up to code
  
v1.01 (Sep 21, 2010)
  - Revised "Delete Orphaned Step Types" to be the more general "Delete Orphaned Types"
  - Added support for worklows and status types to "Delete Orphaned Types"

v1.0 (Sep 16, 2010)
  - Initial check-in


+-------------------+
| TABLE OF CONTENTS |
+-------------------+

1 – Overview
2 - Prerequisites
3 - Details
4 - Compilation & Customization
5 - Troubleshooting / FAQ / Known Issues


+----------------------+
| SECTION 1 – OVERVIEW |
+----------------------+

1.1 - Package Contents
1.2 - Quick Installation


SECTION 1.1 - PACKAGE CONTENTS
------------------------------

The "Workflow Manager Administration Utilities" are a collection of supplementary geoprocessing tools and Python scripts that may help individuals and organizations who are working with an ArcGIS Workflow Manager database.  They include the following:

Toolboxes:
  - Workflow Manager Administration Tools.tbx

GP Tools (Data Workspaces):
  - Create Data Workspaces from Excel Spreadsheet
  - Delete Data Workspace
  - Export Data Workspaces to Excel Spreadsheet
  - List All Data Workspaces
  - Set Default Workspace for Job Type

GP Tools (Deployment Utilities):
  - Add Attachment to Job
  - Add Comment to Job
  - Assign Job
  - Close Job
  - Create Job
  - Create Jobs Based on Feature Class
  - Delete Data Workspace
  - Delete Job
  - Delete Jobs Matching Criteria
  - List Jobs
  - List Jobs Using Query

GP Tools (Developer Utilities):
  - Backup Workflow Manager Database
  - Delete Orphaned Types
  - Report Possible Errors

GP Tools (Map Documents)
  - Delete Map Document
  - Download Map Document
  - List All Map Documents
  - Upload Map Document

GP Tools (Notifications)
  - Add Area Evaluator to Spatial Notification
  - Add Dataset Condition to Spatial Notification
  - Create Spatial Notification with E-mail Notifier
  - Create Spatial Notification with E-mail Notifier 2
  - Send Job Notification
  - Send Notification for Jobs in Query

GP Tools (Security)
  - Import Active Directory Configuration
  - List Users
  - Modify Administrator Access
  - Modify Privilege Assignment

GP Tools (Task Assistant Workbooks)
  - Delete Task Assistant Workbook
  - Download Task Assistant Workbook
  - List All Task Assistant Workbooks
  - Upload All Task Assistant Workbooks
  - Upload Task Assistant Workbook

The contents of this package include:
  \ArcToolbox
    \Toolboxes    - Toolbox(es) that expose the tools and scripts included in this DLL
    \Scripts      - Geoprocessing scripts referenced by the toolbox(es)
  \bin            - Includes a pre-built version of the DLL
  \Documentation  - This readme file, example scripts, sample data, etc.
  \Source         - Source code & project/solution files for the DLL

SECTION 1.2 – QUICK INSTALLATION
--------------------------------

You should first verify that all prerequisite software has been installed on the system where you are installing these utilities.  The tools also require a Workflow Manager extension license (a.k.a. "JTX" license).

First, ensure that the .zip file is not blocked.  (Go to the .zip file's properties and click the "Unblock" button if it's available.  Otherwise continue normally.)

Next, unzip the package directly to its intended installation location.  The installer registers certain file(s) directly from whatever directory they are in, so for instance, if you would like to store these tools in "C:\Program Files\Wmdu", you will need to manually unzip/copy the files to this location *before* running the "install.bat" script.

The "install.bat" file included with this package performs the following actions:
  1) Registers the included copy of the "WorkflowManagerAdministrationUtilities.dll" on your system, so that the GP tools described above are available for use.
  2) Locates the "ESRI.ArcGIS.JTXUI.JTXShared.dll" file installed with Workflow Manager and copies it to the "bin" directory for use by the administration utilities.
  3) Copies the "WorkflowManagerAdministrationUtilities" toolbox into your system toolboxes folder, making it available to the Python scripts included with this library.
  4) Copies any supporting geoprocessing scripts into your system toolbox scripts folder, making them available to the toolbox(es).

NOTE: On a system where User Access Control (UAC) is enabled -- commonly Windows Vista or Windows 7 -- you will need to run the "install.bat" script as an Administrator.  If you are an administrator on the target machine, right-click on the .bat file and select "Run as Administrator", then answer "Yes" at the prompt.  The "uninstall.bat" script must also be run as an administrator.

After a successful install, you should see the "Workflow Manager Administration Tools" toolbox available in your System Toolboxes folder in ArcMap and ArcCatalog.


+---------------------------+ 
| SECTION 2 – PREREQUISITES |
+---------------------------+

As of this writing, to use the Administration Utilities, you will need the following software:
  - ArcGIS Desktop 10.1
  - ArcGIS Workflow Manager 10.1
  - Python 2.7 (included with ArcGIS Desktop)
  - Microsoft .NET Framework 3.5
  - Microsoft Excel 2007 or later

Additionally, if you need to build the DLL containing the GP tools, you will need Microsoft Visual Studio 2010 to use the included project/solution files.


+-----------------------------+
| SECTION 3 – DETAILS         |
+-----------------------------+

3.1 - GP Tool Details
3.2 - Sample Script Details


SECTION 3.1 – GP TOOL DETAILS
-----------------------------

NOTE: All tools operate against the default Workflow Manager database (and require that the default workspace be set) unless specifically noted.

Please refer to the Geoprocessing help available for each tool/script for further information.


SECTION 3.2 – SAMPLE SCRIPT DETAILS
-----------------------------------

All example scripts are located in the "Documentation" directory.  There is a header in each example describing how the script works and what it does.  In most cases, running the script from a command prompt without any arguments will cause a usage screen to print.  Please refer to the individual scripts for details.


+-------------------------------------------+
| SECTION 4 – COMPILATION AND CUSTOMIZATION |
+-------------------------------------------+

For anyone who wishes to further customize these tools, the source code and Visual Studio 2010 project/solution files are included with this project.

In addition to the prerequisite software listed above, you will need the following to successfully build the toolbox:
  - Microsoft Visual Studio 2010
  - ArcGIS .NET SDK 10.1

The project will automatically re-register the new DLL as part of the build process, so any changes will be available immediately upon a successful build.  Please note new tools will need to be manually added to the toolbox.  Also note that changes to an existing tool's interface (name, input/output parameters, etc.) will require that the tool be deleted from and re-added to any toolbox(es) in which it is present.  Finally, note that on a system where User Access Control (UAC) is enabled -- commonly Windows Vista or Windows 7 -- you will need to launch Visual Studio as an Administrator in order for the new DLL to be registered successfully.


+--------------------------------------------------+
| SECTION 5 – TROUBLESHOOTING / FAQ / KNOWN ISSUES |
+--------------------------------------------------+

5.1 - Installation / Supported Environments
5.2 - Tool Messages / Errors / Crashes
5.3 - Building the Utilities


SECTION 5.1 – INSTALLATION / SUPPORTED ENVIRONMENTS
---------------------------------------------------

Q: The install script doesn't run, or exits with an error.
A: The installer needs to be run with administrative privileges.  This means that the user running it must be an administrator on the target machine.  Additionally, under Windows Vista/7, you must right-click on the script and select the "Run as Administrator" option.

Q: When I run the install.bat file, an error dialog pops up that says, "Registration failed.  Could not load file or assembly 'file:///...' or one of its dependencies.  Operation is not supported. (Exception from HRESULT: 0x80131515)"  Why is this happening and how do I fix it?
A: Depending on your system settings, Windows may "block" downloaded files as a security precaution.  Check the .zip file's properties using Windows Explorer and click the "Unblock" button if it's available, then re-extract the contents of the .zip file, run the uninstall script (to be safe), then re-run the installer.

Q: I don't like how the installer configures things; can I change these settings in any way?
A: The install script is only provided as a convenience; you're welcome to build/register the DLL manually, and/or add its tools to your own toolbox(es).

Q: Is there any other documentation available for the tools?
A: The best sources of documentation for the tools are:
  1) The help embedded with the tools (available by launching the tool through ArcMap or ArcCatalog and clicking the "Show Help" button)
  2) The sample scripts in the "Documentation" folder

Q: Can I run these tools through ArcGIS Server or Workflow Manager Server?
A: No, as of this writing (v10.2.0.1), the tools will not run under ArcGIS Server.

Q: Will these utilities work on older/newer versions of ArcGIS?
A: As of this writing (v10.2.0.1), the tools have been built and tested on ArcGIS 10.2 Final.  The tools should run as provided, or with extremely minimal changes, on any service release for 10.2 (10.2 Final, SP1, etc.).  Using this release of the tools on newer ArcGIS versions may require you to recompile the DLL, and could require minor to moderate code changes.  Backporting the tools to an earlier release will require a significant development effort.


SECTION 5.2 - TOOL MESSAGES / ERRORS / CRASHES
----------------------------------------------

Q1: I created a spatial notification using a GP tool, but now I get an error when I try to manage my notifications.
Q2: Out of nowhere, I'm now seeing crashes with a "HRESULT: 0x80040111" or "CLASS_E_CLASSNOTAVAILABLE" error whenever I launch Workflow Manager.  What's going on??
A: If you ran the "Create Spatial Notification with E-mail Notifier" tool and DID NOT immediately run either the "Add Area Evaluator to Spatial Notification" or "Add Dataset Evaluator to Spatial Notification", it is possible that your database may have become corrupted as spatial notifications with no associated evaluators are not officially supported by Workflow Manager.  You may need to restore your database from a backup.

Q: I'm running a script or a script tool, and keep getting an error message that says one of my arguments is bad.  However, I've double- and triple-checked, and my input values seem fine.  Why is this happening?
A: Some of the tool parameters are case-sensitive.  (For instance, passing in "[Not set]" as a parameter value may run properly, while "[Not Set]" could trigger an error.)  It may be helpful to double-check the case of the string value which you're passing in by running the underlying GP tool by hand.  Many tool parameters populate a drop-down list of parameter values, so checking your input against the values in this list could shed some light on things.

Q: I'm running a (script) tool, and keep getting a message saying that one of the admin utilities "takes no arguments".  But I can see in the documentation for the tool that it takes several arguments.  What's happening?
A: This may be caused by a license issue.  Make sure that:
  a) You're checking out any licenses before trying to use the tool.  Particularly in a Python script, make sure you check out the product and extension licenses *before* importing any toolboxes that use the admin utilities.
  b) You have at least the "Workflow Manager" (a.k.a. "JTX") extension enabled.
  c) If you're using a license server, ensure that it is reachable and functioning correctly.
  d) If you're using single-machine licensing, ensure that a "Workflow Manager" (a.k.a. "JTX") license is available, particularly if you're running on a different machine than previously.

Q: I'm trying to chain several tools together in a GP model, and:
  a) a tool isn't showing as "enabled" even though I've populated all of its arguments, or
  b) certain options/values are missing from some parameters, especially in drop-down lists.
  What's going on?
A: GP model support for GP tools that refresh parameter domains dynamically (ex: drop-down lists containing job IDs) appears to be limited.  However, these tools should work properly when called from a Python script.  Refer to any of the scripts in the "Documentation" section for examples of how to use the Workflow Manager Administration Utilities in Python scripts.

Q: I went into my geoprocessing history to try to run a tool using similar arguments as an earlier run, but the options/values in the drop-down lists aren't reflecting what's in the Workflow Manager database!  What's up?
A: Much like with GP models, the GP results window does not seem to refresh parameter domains when a tool is re-opened; the domain will reflect the state at the time the tool was initially run.  For tools that make use of dynamic elements such as job IDs or workspace names, you may need to re-launch them from a new window to see the current possible values.

Q: When I run the "Create Data Workspaces from Excel Spreadsheet" tool, I see a dialog box popping up.  How do I get rid of it?
A1: If you have a password-protected Excel spreadsheet and are being prompted to enter a password, this is by design.  The only way to avoid this dialog is to remove the password protection on the worksheet.
A2: In some cases, you may be prompted for workspace connection information if the info provided in the worksheet is incomplete (for example, missing passwords) or refers to a server/service that is not accessible at the time the tool is running.  First, double-check your Excel worksheet to ensure that the information it contains is correct.  Second, ensure that you can connect to all of your data workspaces.  Finally, if you've omitted any information from the worksheet (such as login information), you may wish to add it.  The "Export Data Workspaces to Excel Spreadsheet" tool can help you generate a valid Excel worksheet.

Q: When I run the "Create Data Workspaces from Excel Spreadsheet" tool, I get an error that says something like: "Could not load file or assembly 'Microsoft.Office.Interop.Excel, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c' or one of its dependencies".  How do I fix this?
A: You must have installed Excel with the ".NET Programmability Support" enabled.  With a new installation of Excel, be sure that the selected features include this option.  To modify an existing installation of Excel/Office to include this option, use the "Add/Remove Programs" (Windows XP) or "Programs and Features" (Windows Vista and 7) control panel.

Q: I'm getting an error 000816 when I try to run the tools.
A: You most likely have (64-bit?) background geoprocessing enabled.  As of this writing (10.2.0.1), this is not supported.


SECTION 5.3 - BUILDING THE UTILITIES
------------------------------------

Q: When I open the project file, Visual Studio gives a warning about source control and removing bindings.  What did I do wrong?
A: Nothing; this is an artifact of how the project was initially developed.  You can ignore the warning or elect to remove the source control bindings without affecting the functionality of any of the utilities or breaking the build.

Q: When I try to build, I get an error saying that it "cannot register assembly".
A: On a system with User Access Control (UAC) enabled -- typically Windows 7 or Vista -- ensure that you are launching Visual Studio as an administrator.  (When opening Visual Studio, right-click on the VS shortcut and click "Run as Administrator".)

Q: ...but I'm not an administrator on my machine!
A: System administrative privileges are required to install or build the Workflow Manager Administration Utilities.

Q: When I build, I get an error (or two) saying that it's "unable to delete file" and/or "unable to copy file" over the WorkflowManagerAdministrationUtilities.dll.
A: Usually, this happens when another program -- typically ArcMap or ArcCatalog -- has loaded the DLL that you're trying to rebuild.  Try closing these applications and rebuilding.  If you still get the error, check the Windows Task Manager to ensure that there aren't any stale "ArcMap.exe" or "ArcCatalog.exe" processes lingering (perhaps after a crash or an error).  If any exist, close them.  Finally, if even that does not solve the problem, ensure that the file(s) being replaced are not set to be read-only.
