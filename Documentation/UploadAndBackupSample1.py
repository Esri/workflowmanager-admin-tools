# ---------------------------------------------------------------------------
# UploadAndBackupSample1.py
#
# SAMPLE #1:
# This script shows how the map document and Task Assistant workbook
# upload capabilities might be used in a development environment.
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
This script shows how the map document and Task Assistant workbook
upload capabilities might be used in a development environment.

The scenario is that a series of map documents and Task Assistant
workbooks are maintained primarily outside of the Workflow Manager
database (perhaps in a version control system, to facilitate diffs,
change tracking, and so forth).  To ensure that the Workflow Manager
database contains the most recent versions of these documents, this
script uploads the latest files to the database from specified
locations on disk.  After the upload, the database is backed up to
a JXL file (again, presumably so that it can be checked into a
version control system).

Expected Arguments:
  1 - Full path to the JXL file to be created by the DB backup.
  2 - Full path to a folder containing the map documents to be uploaded.
  3 - Full path to a folder containing the Task Assistant workbooks
      to be uploaded.
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
        
        outputJxlFile = arcpy.GetParameterAsText(0)
        mxdDir = arcpy.GetParameterAsText(1)
        tawDir = arcpy.GetParameterAsText(2)
        
        # Get any necessary licenses before importing the toolbox
        checkOutLicenses("", ["JTX"])

        # Import the Workflow Manager toolbox
        wmxToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxToolbox, "WMXAdminUtils")

        # Identify all of the MXD files
        #
        # TODO: Move this code into an "Upload All Map Documents" script?
        fileList = os.listdir(mxdDir)
        mxdFiles = []
        for f in fileList:
            (unused, ext) = os.path.splitext(f)
            if ext.lower() == ".mxd":
                mxdFiles.append(mxdDir + os.sep + f)
        
        # Upload all of the MXDs to the DB
        mxdCategory = "#"
        mxdDescription = "#"
        overwriteMxds = "OVERWRITE"
        for mxd in mxdFiles:
            temp = os.path.basename(mxd)
            (targetName, unused) = os.path.splitext(temp)
            arcpy.UploadMapDocument_WMXAdminUtils(mxd, targetName, mxdCategory, mxdDescription, overwriteMxds)
            logPreviousToolMessages()

        # Upload all of the Task Assistant workbooks to the DB
        arcpy.UploadAllTaskAssistantWorkbooks_WMXAdminUtils(tawDir, "true")
        logPreviousToolMessages()

        # Back up the database to the specified JXL file
        prettyPrint = "PRETTY_PRINT"
        arcpy.BackupWorkflowManagerDatabase_WMXAdminUtils(outputJxlFile, prettyPrint)
        logPreviousToolMessages()

    except Exception, ex:
        printUsage()
        arcpy.AddError("Caught exception: " + str(ex))


# Entry point for the script
if __name__ == "__main__":
    main()
