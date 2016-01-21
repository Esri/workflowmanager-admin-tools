using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;


namespace WorkflowManagerAdministrationUtilities
{
    class ExportDataWorkspacesToExcel : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_OUT_EXCEL_FILE_PATH = "out_file_excelFile";
        #endregion

        #region MemberVariables
        private string m_excelFilePath = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ExportDataWorkspacesToExcel"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_EXPORT_DATA_WORKSPACES_TO_SPREADSHEET; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_DATA_WORKSPACE_UTILS; } }
        #endregion

        #region Private helper functions
        /// <summary>
        /// Updates the internal values used by this tool based on the parameters from an input array
        /// </summary>
        /// <param name="paramValues"></param>
        protected override void ExtractParameters(IArray paramValues)
        {
            // Get the values for any parameters common to all GP tools
            ExtractParametersCommon(paramValues);

            WmauParameterMap paramMap = new WmauParameterMap(paramValues);
            IGPParameter3 param = null;

            // Update the internal values of whatever parameters we're maintaining
            param = paramMap.GetParam(C_PARAM_OUT_EXCEL_FILE_PATH);
            m_excelFilePath = param.Value.GetAsText();
        }
        #endregion

        /// <summary>
        /// Required by IGPFunction2 interface.
        /// </summary>
        public override IArray ParameterInfo
        {
            get
            {
                m_parameters = new ArrayClass();
                IGPParameterEdit3 paramEdit = null;

                // JXL file parameter (path to output JXL file)
                IGPFileDomain excelFileDomain = new GPFileDomainClass();
                excelFileDomain.AddType("xlsx");
                excelFileDomain.AddType("xls");

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_EDW_EXCEL_FILE_PATH,
                    C_PARAM_OUT_EXCEL_FILE_PATH,
                    new DEFileTypeClass() as IGPDataType,
                    null);
                paramEdit.Domain = excelFileDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                return m_parameters;
            }
        }

        /// <summary>
        /// Required by IGPFunction2 interface; this function is called when the GP tool is ready to be executed.
        /// </summary>
        /// <param name="paramValues"></param>
        /// <param name="trackCancel"></param>
        /// <param name="envMgr"></param>
        /// <param name="msgs"></param>
        public override void Execute(IArray paramValues, ITrackCancel trackCancel, IGPEnvironmentManager envMgr, IGPMessages msgs)
        {
            // Do some common error-checking
            base.Execute(paramValues, trackCancel, envMgr, msgs);

            WorkspaceWorksheetWriter writer = null;

            try
            {
                // Ensure that the current user has admin access to the current Workflow Manager DB
                if (!CurrentUserIsWmxAdministrator())
                {
                    throw new WmauException(WmauErrorCodes.C_USER_NOT_ADMIN_ERROR);
                }

                // Load the workspace information from the database
                IJTXDatabaseConnectionManager dbConnectionManager = new JTXDatabaseConnectionManagerClass();
                IJTXDatabaseConnection dbConnection = dbConnectionManager.GetConnection(WmxDatabase.Alias);
                int numWorkspaces = dbConnection.DataWorkspaceCount;
                
                // Copy the info into an intermediate list
                List<Common.WorkspaceInfo> workspaceInfo = new List<Common.WorkspaceInfo>();
                Dictionary<string, string> namedWorkspaces = new Dictionary<string, string>();
                for (int i = 0; i < numWorkspaces; i++)
                {
                    IJTXWorkspaceConfiguration workspace = dbConnection.get_DataWorkspace(i);
                    Common.WorkspaceInfo tempInfo = new Common.WorkspaceInfo();
                    
                    tempInfo.Name = workspace.Name;
                    tempInfo.Server = workspace.Server;
                    tempInfo.Instance = workspace.Instance;
                    tempInfo.Database = workspace.Database;
                    tempInfo.Version = workspace.Version;
                    tempInfo.UseOsAuthentication = workspace.OSAuthentication;
                    tempInfo.UseIndividualLogins = workspace.IndividualLogins;
                    
                    int numLogins = workspace.LoginCount;
                    for (int j = 0; j < numLogins; j++)
                    {
                        IJTXWorkspaceLogin tempLogin = workspace.get_Login(j);
                        tempInfo.AddLogin(tempLogin.JTXUserName, tempLogin.UserName, tempLogin.Password, true);
                    }

                    if (namedWorkspaces.Keys.Contains(tempInfo.Name))
                    {
                        msgs.AddWarning("Multiple workspaces detected with name '" + tempInfo.Name +
                            "'; only the first workspace found will be exported.");
                    }
                    else
                    {
                        workspaceInfo.Add(tempInfo);
                        namedWorkspaces.Add(tempInfo.Name, null);
                    }
                }

                // Save the list to an Excel worksheet
                writer = new WorkspaceWorksheetWriter(this.m_excelFilePath, msgs);
                writer.SaveWorkspacesToSpreadsheet(workspaceInfo);

                msgs.AddMessage(Properties.Resources.MSG_DONE);
            }
            catch (WmauException wmEx)
            {
                try
                {
                    msgs.AddError(wmEx.ErrorCodeAsInt, wmEx.Message);
                }
                catch
                {
                    // Catch anything else that possibly happens }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    WmauError error = new WmauError(WmauErrorCodes.C_WORKSPACE_LOAD_ERROR);
                    msgs.AddError(error.ErrorCodeAsInt, error.Message + "; " + ex.Message);
                }
                catch
                {
                    // Catch anything else that possibly happens }
                }
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
            }
        }
    }
}
