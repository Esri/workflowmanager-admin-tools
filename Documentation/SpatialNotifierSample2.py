# ---------------------------------------------------------------------------
# SpatialNotifierSample2.py
#
# SAMPLE #2:
# This file shows how you would use the Spatial Notification GP tools to
# create a different spatial notification rule for every polygon in a
# feature class.  No dataset conditions are applied.
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
SAMPLE #2:
  This file shows how you would use the Spatial Notification GP tools to
  create a different spatial notification rule for every polygon in a
  feature class.  No dataset conditions are applied, so every feature class
  in any Workflow Manager data workspace will be monitored for changes.

  In this example, the "Quadrants" feature class in the included "SampleData"
  file geodatabase will be used as the source feature class.  Please ensure
  that the file GDB has been unzipped before running this script.

Expected arguments:
  1 - The name of an existing e-mail notification to use as the template for
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
        if arcpy.GetArgumentCount() != 1:
            raise Exception("Incorrect number of arguments")
        
        polygonFC = ".\\SampleData.gdb\\Quadrants"
        emailNotificationName = arcpy.GetParameterAsText(0)
        
        # Get any necessary licenses before importing the toolbox
        checkOutLicenses("", ["JTX"])

        # Import the Workflow Manager toolbox
        wmxToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxToolbox, "WMXAdminUtils")
        arcpy.env.overwriteOutput = True

        # Make a feature layer from the polygon FC passed in;
        # features will be selected from this layer
        polygonLayer = "PolygonLayer"
        arcpy.MakeFeatureLayer_management(polygonFC, polygonLayer)

        # Iterate through all of the features in this feature class
        rows = arcpy.SearchCursor(polygonFC)
        counter = 1
        for row in rows:
            # The OBJECTID field will be unique for all features,
            # so retrieve its value for each feature and create a new
            # selection based on this value.
            objectID = row.OBJECTID
            arcpy.SelectLayerByAttribute_management(polygonLayer, "NEW_SELECTION", "OBJECTID = " + str(objectID))

            # Create a new spatial notifier
            snName = "Monitor Region " + str(counter) + " (Sample 2)"
            snDesc = "This spatial notification will monitor all of the features in the region bounded by the polygon with OBJECTID " + str(objectID)
            summarize = "SUMMARIZE"
            arcpy.CreateSpatialNotificationWithEmailNotifier_WMXAdminUtils(snName, emailNotificationName, snDesc, summarize)
            logPreviousToolMessages()

            # Add an area condition for this spatial notifier
            #
            # NOTE: To see the full list of options available for each argument,
            # launch the GP tool from ArcMap or ArcCatalog.  The arguments
            # correspond to options provided by Workflow Manager.
            geomOp = "INTERSECTS"
            doNotUseInverse = "USE_OPERATION"
            useFeature = "USE_SELECTED_FEATURE"
            arcpy.AddAreaEvaluatorToSN_WMXAdminUtils(snName, geomOp, doNotUseInverse, useFeature, polygonLayer)
            logPreviousToolMessages()
            
            # Increment the counter and move on to the next feature
            counter += 1            

        # Free up any cursor objects and layers
        if row != None:
            del row
        if rows != None:
            del rows

        arcpy.Delete_management(polygonLayer)

    except Exception, ex:
        printUsage()
        arcpy.AddError("Caught exception: " + str(ex))


# Entry point for the script
if __name__ == "__main__":
    main()
