# ---------------------------------------------------------------------------
# SendNotificationForJobsInQuery.py
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

    try:
        # Set up the tool's parameters
        paramIndex = 0
        queryName = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        notificationName = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        wmxDbAlias = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1

        # Import the Workflow Manager toolbox
        wmxToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxToolbox, "WMXAdminUtils")

        # Run the specified query to get the list of jobs for which
        # a notification should be sent
        result = arcpy.ListJobsUsingQuery_WMXAdminUtils(queryName, wmxDbAlias)
        logPreviousToolMessages()

        jobIdList = result.getOutput(0).split(";")

        # Send a notification for each one of these jobs
        arcpy.SetProgressor("step", "Sending notifications...", 0, len(jobIdList), 1)
        jobCount = 0
        for jobId in jobIdList:
            arcpy.SendJobNotification_WMXAdminUtils(jobId, notificationName, wmxDbAlias)
            logPreviousToolMessages()

            jobCount += 1
            arcpy.SetProgressorPosition(jobCount)

        # Set the return value for this tool (a multivalue containing
        # the same list of job IDs that was passed in)
        arcpy.SetParameterAsText(paramIndex, result.getOutput(0))

    except Exception, ex:
        arcpy.AddError("Caught exception: " + str(ex))


# Entry point for the script
if __name__ == "__main__":
    main()
