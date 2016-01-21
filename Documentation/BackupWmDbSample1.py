# ---------------------------------------------------------------------------
# BackupWmDbSample1.py
#
# This script demonstrates how to call the "Backup Workflow Manager Database"
# tool.
#
# It also shows how the "optparse" module might be used as an alternate way
# of passing script information through arguments.  (This approach is
# incompatible with the requirements for a script that is called from an
# ArcGIS toolbox, but may be preferred in some environments or by some users
# who would prefer a more traditional command-line interface.)
# ---------------------------------------------------------------------------

# Import arcpy module
import arcpy
import optparse
import os


# Function that prints an explanation of how to use this sample
def printUsage():
    print("""
SAMPLE #1:
This script shows how to use the "Backup Workflow Manager Database" tool to
back up the default Workflow Manager database to a .jxl file.  Run with
the --help option to see the usage details.
""")


# Define a basic class used to call out license errors
class LicenseError(Exception):
    pass


# Define a basic class used to call out argument value errors
class InvalidArgumentError(Exception):
    pass


# Define a basic class used to call out core installation errors
class InstallationError(Exception):
    pass


# Define a basic class to identify file access exceptions
class FileAccessError(Exception):
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
    

# Parses the argument string taken by this utility
def parseArguments():
    parser = optparse.OptionParser()
    parser.set_defaults(prettyPrint=False)
    parser.add_option("-f", "--outputFile", action="store", dest="outputFile", help="Full path to the file to which the Workflow Manager database should be saved (ex. \"c:\\backups\\WMXDatabase.jxl\")")
    parser.add_option("-p", "--prettyPrint", action="store_true", dest="prettyPrint", help="Make the output file more easily human-readable")
    
    (options, args) = parser.parse_args()

    # Basic argument validation
    outputFile = options.outputFile
    if outputFile == None or len(outputFile) == 0:
        raise InvalidArgumentError("Output file is required")
    else:
        outputFile = os.path.normpath(outputFile)
        outputDir = os.path.dirname(outputFile)
        if not os.path.isdir(outputDir):
            raise InvalidArgumentError("Output directory '" + outputDir + "' does not exist or is not a directory")
        elif not os.access(outputDir, os.W_OK):
            raise FileAccessError("Cannot write to directory: '" + outputDir + "'")
        elif os.path.exists(outputFile) and not os.access(outputFile, os.W_OK):
            raise FileAccessError("Cannot write to file: '" + outputFile + "'")
        else:
            log("Output file: '" + outputFile + "'")

    prettyPrint = options.prettyPrint
    if prettyPrint == True:
        log("Pretty-printing output file")

    return (outputFile, prettyPrint)


def main():

    try:
        # Get arguments from the command line
        (outputFile, prettyPrint) = parseArguments()

        # Get any necessary licenses before importing the toolbox
        checkOutLicenses("", ["JTX"])

        # Import the Workflow Manager toolbox
        wmxToolbox = getWorkflowManagerToolboxLocation()
        arcpy.ImportToolbox(wmxToolbox, "WMXAdminUtils")
        arcpy.env.overwriteOutput = True

        # Run the GP tool
        prettyPrintArg = "PRETTY_PRINT"
        if not prettyPrint:
            prettyPrintArg = "DEFAULT_FORMATTING"
        
        log("Exporting job data")
        arcpy.BackupWorkflowManagerDatabase_WMXAdminUtils(outputFile, prettyPrintArg)
        logPreviousToolMessages()
        log("Backup complete")

    except InvalidArgumentError, argEx:
        printUsage()
        arcpy.AddError("Invalid argument: " + str(argEx))

    except LicenseError, lex:
        printUsage()
        arcpy.AddError("Problem getting license: " + str(lex))

    except Exception, ex:
        printUsage()
        arcpy.AddError("Caught exception: " + str(ex))


# Entry point for the script
if __name__ == "__main__":
    main()
