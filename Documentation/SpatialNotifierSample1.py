# ---------------------------------------------------------------------------
# SpatialNotifierSample1.py
#
# SAMPLE #1:
# This file shows how you would use the Spatial Notification GP tools to
# create a basic spatial notification for every feature class in a
# data workspace.
# ---------------------------------------------------------------------------

import arcpy
import os


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
  This file shows how you would use the Spatial Notification GP tools to
  create a basic spatial notification for every feature class in a
  data workspace.  Every change to any feature will be reported.

Expected Arguments:
  1 - Human-readable name of the data workspace to be registered.  (For
      example, "My Data Workspace".)
  2 - A path (.sde connection) to the data workspace.  (For example,
      "C:\\\\Data\\\\my_data_workspace.sde"
  3 - The name of an existing e-mail notification to use as the template for
      this spatial notifier's notification message.  (For example,
      "JobClosed".)
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
        # Get the input parameters to this tool
        if arcpy.GetArgumentCount() != 3:
            raise Exception("Incorrect number of arguments")
        
        dataWorkspaceName = arcpy.GetParameterAsText(0)
        dataWorkspaceSde = arcpy.GetParameterAsText(1)
        emailNotificationName = arcpy.GetParameterAsText(2)
        
        # Get any necessary licenses before importing the toolbox
        checkOutLicenses("", ["JTX"])

        # Import the Workflow Manager toolbox
        wmxToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxToolbox, "WMXAdminUtils")
        arcpy.env.overwriteOutput = True

        # Create the spatial notification
        snName = "Monitor all FCs (Sample 1)"
        snDesc = "This spatial notification will monitor all of the feature classes in the specified workspace for any changes"
        summarize = "SUMMARIZE"
        arcpy.CreateSpatialNotificationWithEmailNotifier_WMXAdminUtils(snName, emailNotificationName, snDesc, summarize)
        logPreviousToolMessages()

        # Get the feature class list from the database
        arcpy.env.workspace = dataWorkspaceSde
        fcList = arcpy.ListFeatureClasses()

        # Add a dataset condition for each feature class
        changeCond = "ALWAYS"
        for fc in fcList:
            arcpy.AddDatasetConditionToSN_WMXAdminUtils(snName, dataWorkspaceName, fc, changeCond, "#", "#", "#", "#")
            logPreviousToolMessages()
        
    except Exception, ex:
        printUsage()
        arcpy.AddError("Caught exception: " + str(ex))


# Entry point for the script
if __name__ == "__main__":
    main()
