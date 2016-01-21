# ---------------------------------------------------------------------------
# ActiveDirectorySample1.py
#
# SAMPLE #1:
# This script shows one possible scenario for using the "Import Active
# Directory Configuration" tool.
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
This script shows one possible scenario for using the "Import Active
Directory Configuration" tool.  Specifically, it:
  - Imports the Workflow Manager users & groups from the AD groups that you
    specify
  - Refreshes the privileges for the groups in the system
  - Makes sure that every user in an administrators group has
    administrative access to the Workflow Manager database.

Expected Arguments:
  1 - Name of the AD group containing the full list of Workflow Manager
      USERS; assumes the domain of the user running the script.
  2 - Name of the AD group containing the full list of Workflow Manager
      GROUPS; assumes the domain of the user running the script.
  3 - The name of a Workflow Manager group whose users should have
      administrator access to the database.
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
        
        adUsersGroup = arcpy.GetParameterAsText(0)
        adGroupsGroup = arcpy.GetParameterAsText(1)
        wmxAdminGroup = arcpy.GetParameterAsText(2)
        
        # Get any necessary licenses before importing the toolbox
        checkOutLicenses("", ["JTX"])

        # Import the Workflow Manager toolbox
        wmxToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxToolbox, "WMXAdminUtils")

        # Run the active directory import
        preserve = "NO_PRESERVE"
        arcpy.ImportActiveDirectoryConfiguration_WMXAdminUtils(adUsersGroup, adGroupsGroup, preserve)
        logPreviousToolMessages()
        
        # Ensure that all of the group permissions are in a known state
        #
        # The sequence of events shown here is arbitrary; a real-life example would
        # likely be set up differently

        # Grant everything to everyone
        arcpy.ModifyPrivilegeAssignment_WMXAdminUtils("[All]", "[All]", "GRANT")
        logPreviousToolMessages()

        # Remove some particular permissions from the groups
        arcpy.ModifyPrivilegeAssignment_WMXAdminUtils("DeleteJobs", "[All]", "REVOKE")
        logPreviousToolMessages()
        arcpy.ModifyPrivilegeAssignment_WMXAdminUtils("DeleteVersion", "[All]", "REVOKE")
        logPreviousToolMessages()

        # Add the permissions back to the administrators group
        arcpy.ModifyPrivilegeAssignment_WMXAdminUtils("[All]", wmxAdminGroup, "GRANT")
        logPreviousToolMessages()

        # Now, make sure that all of the users in the admin group have administrator
        # access to the WMX DB
        result = arcpy.ListUsers_WMXAdminUtils(wmxAdminGroup)
        logPreviousToolMessages()

        userListString = result.getOutput(0)
        if userListString != None and len(userListString) > 0:
            users = userListString.split(";")

            arcpy.ModifyAdministratorAccess_WMXAdminUtils("[All]", "REVOKE", "PRESERVE")
            logPreviousToolMessages()

            for user in users:
                arcpy.ModifyAdministratorAccess_WMXAdminUtils(user, "GRANT", "PRESERVE")
                logPreviousToolMessages()

    except Exception, ex:
        printUsage()
        arcpy.AddError("Caught exception: " + str(ex))


# Entry point for the script
if __name__ == "__main__":
    main()
