# ---------------------------------------------------------------------------
# UploadAllTaskAssistantWorkbooks.py
# ---------------------------------------------------------------------------

# Import arcpy module
import arcpy
import optparse
import os


# Define a basic class used to call out license errors
class LicenseError(Exception):
    pass


# Define a basic class used to call out core installation errors
class InstallationError(Exception):
    pass


# Logging helper
def log(msg):
    arcpy.AddMessage(msg)


# Function to ensure that messages from a previously-run tool are not lost
def logPreviousToolMessages():
    i = 0
    msgCount = arcpy.GetMessageCount()
    while i < msgCount:
        msg = arcpy.GetMessage(i)
        arcpy.AddReturnMessage(i)
        i += 1


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
    

def main():

    try:
        # Get arguments from the command line
        paramIndex = 0
        sourceDir = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        tempStr = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        wmxDbAlias = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        
        if tempStr.lower() == "true":
            stripExt = True
        elif tempStr.lower() == "false":
            stripExt = False
        else:
            raise Exception("Problem getting boolean value from argument: '" + str(tempStr) + "'")

        arcpy.AddMessage("stripExt: " + str(stripExt))
        
        # Get any necessary licenses
        checkOutLicenses("", ["JTX"])

        # Import the Workflow Manager toolbox
        wmxToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxToolbox, "WMXAdminUtils")

        # Identify all of the TAM workbook files (or at least the .xml files)
        fileList = os.listdir(sourceDir)
        tamWorkbookFiles = []
        for f in fileList:
            (unused, ext) = os.path.splitext(f)
            if ext.lower() == ".xml":
                tamWorkbookFiles.append(sourceDir + os.sep + f)

        # Upload all of the TA workbooks to the DB
        arcpy.SetProgressor("step", "Uploading Task Assistant Workbooks...", 0, len(tamWorkbookFiles), 1)
        count = 0
        workbooksUploaded = []
        for tamWkbk in tamWorkbookFiles:
            targetName = os.path.basename(tamWkbk)
            if stripExt:
                (targetName, unused) = os.path.splitext(targetName)
            arcpy.SetProgressorLabel("Uploading workbook '" + targetName + "'")
            arcpy.UploadTaskAssistantWorkbook_WMXAdminUtils(tamWkbk, targetName, "OVERWRITE", wmxDbAlias)
            logPreviousToolMessages()
            count += 1
            workbooksUploaded.append(targetName)
            
            arcpy.SetProgressorPosition(count)
            
        # Set the return value for this tool (a multivalue containing the list of
        # Task Assistant workbooks that were uploaded)
        workbooksUploadedStr = ""
        for workbook in workbooksUploaded:
            workbooksUploadedStr += workbook + ";"

        workbooksUploadedStr = workbooksUploadedStr.rstrip(";")
        arcpy.SetParameterAsText(paramIndex, workbooksUploadedStr)
        arcpy.AddMessage("Workbooks uploaded: " + workbooksUploadedStr)
        
    except LicenseError, lex:
        arcpy.AddError("Problem getting license: " + str(lex))

    except Exception, ex:
        arcpy.AddError("Caught exception: " + str(ex))


# Entry point for the script
if __name__ == "__main__":
    main()
