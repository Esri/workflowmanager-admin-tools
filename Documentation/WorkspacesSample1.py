# ---------------------------------------------------------------------------
# WorkspacesSample1.py
# ---------------------------------------------------------------------------

import arcpy
import os
import sys


# Define a basic class used to call out license errors
class LicenseError(Exception):
    pass


# Define a basic class used to call out core installation errors
class InstallationError(Exception):
    pass


# Function that prints an explanation of how to use this sample
def printUsage():
    print("""
SAMPLE #1:
This script demonstrates how to use the GP utilities relating to
data workspaces.  Given the name of an Excel workbook (which may
or may not already exist), this tool:
 - saves all of the data workspaces in the current Workflow
   Manager database to this workbook
 - deletes all of the data workspaces from the database
 - reloads the data workspaces into the database based on the
 - contents of the Excel workbook

NOTE: If an Excel file of the same name already exists, it will
be completely overwritten by the new file.


Expected arguments:
  1 - Full path of an Excel file (.xls or .xlsx) to be used to
      contain the data workspace information
""")


# Function to retrieve the licenses needed by this utility
def checkOutLicenses(licenseType, extensionList):
    # Check out all necessary licenses
    if licenseType != None and len(licenseType) > 0:
        retVal = arcpy.SetProduct(licenseType)
        if retVal == "CheckedOut" or retVal == "AlreadyInitialized":
            arcpy.AddMessage("Got product successfully.")
        else:
            arcpy.AddError("Could not get license '" + licenseType + "'; return code: " + retVal)
            raise LicenseError

    for extension in extensionList:
        if arcpy.CheckExtension(extension) == "Available":
            arcpy.AddMessage(extension + " extension is available")
            retVal = arcpy.CheckOutExtension(extension)
            if retVal != "CheckedOut":
                arcpy.AddError("Could not get extension: " + extension + "; return code: " + retVal)
                raise LicenseError
            else:
                arcpy.AddMessage("Got extension: " + extension)
        else:
            raise LicenseError


# Function to determine the install location of the workflow manager toolbox
def getWorkflowManagerToolboxLocation():
    # Import the workflow manager toolbox
    wmxToolbox = None
    
    installations = arcpy.ListInstallations()
    for installation in installations:
        installInfo = arcpy.GetInstallInfo(installation)
        if installInfo != None:
            tbx = installInfo["InstallDir"] + os.sep + "ArcToolbox" + os.sep + "Toolboxes" + os.sep + "Workflow Manager Administration Tools.tbx"
            tbx = os.path.normpath(tbx)
            if os.path.exists(tbx):
                wmxToolbox = tbx
                break

    if wmxToolbox == None:
        raise InstallationError("Workflow Manager Administration Tools toolbox not found")

    return wmxToolbox
    

# Function to ensure that messages from a previously-run tool are not lost
def logPreviousToolMessages():
    i = 0
    msgCount = arcpy.GetMessageCount()
    while i < msgCount:
        msg = arcpy.GetMessage(i)
        arcpy.AddReturnMessage(i)
        i += 1


def main():

    try:
        arcpy.env.overwriteOutput = True

        # Get the input parameters to this tool
        if arcpy.GetArgumentCount() != 1:
            raise Exception("Incorrect number of arguments")
        excelWorksheetName = os.path.abspath(arcpy.GetParameterAsText(0))

        # Get any necessary licenses before importing the toolbox
        checkOutLicenses("", ["JTX"])

        # Import the Workflow Manager toolbox
        wmxDevUtilsToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxDevUtilsToolbox, "WMXAdminUtils")

        # Save the existing workspaces to a file
        arcpy.ExportDataWorkspacesToExcel_WMXAdminUtils(excelWorksheetName)
        logPreviousToolMessages()

        # List the current workspaces
        result = arcpy.ListAllDataWorkspaces_WMXAdminUtils()
        logPreviousToolMessages()

        # Delete all of the workspaces from the DB
        allWorkspaces = result.getOutput(0)
        if len(allWorkspaces) > 0:
            allWorkspaces = allWorkspaces.split(";")
            
            for workspace in allWorkspaces:
                workspace = workspace.strip("'")
                workspace = workspace.strip("\"")
                arcpy.DeleteDataWorkspace_WMXAdminUtils(workspace)
                logPreviousToolMessages()

        # List the workspaces again
        result = arcpy.ListAllDataWorkspaces_WMXAdminUtils()
        logPreviousToolMessages()

        allWorkspaces = result.getOutput(0)
        if len(allWorkspaces) > 0:
            arcpy.AddError("Failed to delete all workspaces")

        # Recreate the workspaces from the spreadsheet that was just saved
        arcpy.CreateDataWorkspacesFromExcel_WMXAdminUtils(excelWorksheetName, "IGNORE_ALL_ERRORS")
        logPreviousToolMessages()

    except Exception, ex:
        printUsage()
        arcpy.AddError("Caught exception: " + str(ex))


# Entry point for the script
if __name__ == "__main__":
    main()
