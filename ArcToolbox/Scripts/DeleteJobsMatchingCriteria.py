# ---------------------------------------------------------------------------
# DeleteJobsMatchingCriteria.py
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
        # Error checking and argument fetching
        if arcpy.GetArgumentCount() < 1:
            raise InvalidArgumentError("Required argument missing")

		# Set up the tool's parameters
        paramIndex = 0
        jobsTable = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        sqlQuery = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1
        wmxDbAlias = arcpy.GetParameterAsText(paramIndex)
        paramIndex += 1

        # Import the Workflow Manager toolbox
        wmxToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxToolbox, "WMXAdminUtils")

        # Get the list of jobs matching the query
        result = arcpy.ListJobs_WMXAdminUtils(jobsTable, sqlQuery, wmxDbAlias)
        logPreviousToolMessages()
        numOutputs = result.outputCount

        if numOutputs <= 0:
            return
        
        # Output is a semicolon-delimited list of job IDs, so split up the
        # list, as required.
        jobListString = result.getOutput(0)
        if jobListString == None or len(jobListString) <= 0:
            arcpy.AddMessage("No jobs matched query")
            return

        jobsToDelete = jobListString.split(";")
        arcpy.AddMessage("Jobs to delete: " + str(jobListString))

        # Set up the progress bar
        arcpy.SetProgressor("step", "Deleting jobs...", 0, len(jobsToDelete), 1)

        # Delete each job
        jobCount = 0
        jobsDeleted = []
        for job in jobsToDelete:
            arcpy.SetProgressorLabel("Deleting job " + str(job))
            arcpy.DeleteJob_WMXAdminUtils(job, wmxDbAlias)
            logPreviousToolMessages()
            jobCount += 1
            jobsDeleted.append(job)
            
            arcpy.SetProgressorPosition(jobCount)

        # Set the return value for this tool (a multivalue containing the list of IDs
        # for the jobs that were deleted)
        jobsDeletedStr = ""
        for jobId in jobsDeleted:
            jobsDeletedStr += jobId + ";"

        jobsDeletedStr = jobsDeletedStr.rstrip(";")
        arcpy.SetParameterAsText(paramIndex, jobsDeletedStr)
        arcpy.AddMessage("Deleted jobs: " + jobsDeletedStr)

    except Exception, ex:
        arcpy.AddError("Caught exception: " + str(ex))


# Entry point for the script
if __name__ == "__main__":
    main()
