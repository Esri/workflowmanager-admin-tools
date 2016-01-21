# ---------------------------------------------------------------------------
# CreateJobsBasedOnFC.py
# ---------------------------------------------------------------------------

# Import arcpy module
import arcpy
import os


# Define a basic class used to call out core installation errors
class InstallationError(Exception):
    pass


# Define a basic class used to call out argument value errors
class InvalidArgumentError(Exception):
    pass


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


# Main function
def main():

    row = None
    rows = None
    specifiedFeatures = "SpecifiedFeatures_layer"

    try:
        # Set up the tool's parameters
        paramIndex = 0
        fc = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        fcExpression = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        jobType = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        owner = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        assigneeType = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        assignee = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        startDate = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        dueDate = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        priority = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        parentJobId = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        dataWorkspace = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        parentVersion = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        wmxDbAlias = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1

        # Import the Workflow Manager toolbox
        wmxToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxToolbox, "WMXAdminUtils")

        # Create a layer based on the specified features
        arcpy.MakeFeatureLayer_management(fc, specifiedFeatures, fcExpression)
        logPreviousToolMessages()

        result = arcpy.GetCount_management(specifiedFeatures)
        numNewJobs = int(result.getOutput(0))

        # Create a new job for each one of these specified features
        newJobs = []
        rows = arcpy.SearchCursor(specifiedFeatures)
        arcpy.SetProgressor("step", "Creating jobs...", 0, numNewJobs, 1)
        for row in rows:
            # Iterate through all of the features in this layer, selecting each one in turn
            objId = row.getValue("OBJECTID")
            selExp = "OBJECTID = " + str(objId)
            arcpy.SelectLayerByAttribute_management(specifiedFeatures, "NEW_SELECTION", selExp)
            logPreviousToolMessages()

            # Create a job based on these parameters
            result = arcpy.CreateJob_WMXAdminUtils(
                jobType, owner, assigneeType, assignee, specifiedFeatures,
                startDate, dueDate, priority, parentJobId, dataWorkspace,
                parentVersion, wmxDbAlias)
            logPreviousToolMessages()
            newJobs.append(result.getOutput(0))

            arcpy.SetProgressorPosition(len(newJobs))

        # Set the return value for this tool (a multivalue containing the list of IDs
        # for the jobs that were created)
        newJobsStr = ""
        for jobId in newJobs:
            newJobsStr += jobId + ";"

        newJobsStr = newJobsStr.rstrip(";")
        arcpy.SetParameterAsText(paramIndex, newJobsStr)
        arcpy.AddMessage("Created jobs: " + newJobsStr)

    except Exception, ex:
        arcpy.AddError("Caught exception: " + str(ex))

    finally:
        # Clean up after the cursor, if necessary
        if row:
            del row
        if rows:
            del rows
            arcpy.Delete_management(specifiedFeatures)


# Entry point for the script
if __name__ == "__main__":
    main()
