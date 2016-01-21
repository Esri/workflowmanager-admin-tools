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
    class CreateDataWorkspacesFromExcel : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_EXCEL_FILE_PATH = "in_file_excelFile";
        private const string C_PARAM_ERROR_BEHAVIOR = "in_string_errorBehavior";

        private const string C_PARAM_OUT_WORKSPACES_CREATED = "out_stringList_workspacesCreated";

        private const string C_OPT_FAIL_ON_ERROR = "FAIL_ON_ERROR";
        private const string C_OPT_IGNORE_LOGIN_ERRORS = "IGNORE_LOGIN_ERRORS";
        private const string C_OPT_IGNORE_ALL_ERRORS = "IGNORE_ALL_ERRORS";

        private const string C_DEFAULT_ERROR_BEHAVIOR = C_OPT_FAIL_ON_ERROR;
        #endregion

        #region MemberVariables
        private string m_excelFilePath = string.Empty;
        private string m_errorBehavior = C_DEFAULT_ERROR_BEHAVIOR;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "CreateDataWorkspacesFromExcel"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_CREATE_DATA_WORKSPACES_FROM_SPREADSHEET; } }
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
            param = paramMap.GetParam(C_PARAM_EXCEL_FILE_PATH);
            m_excelFilePath = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_ERROR_BEHAVIOR);
            m_errorBehavior = param.Value.GetAsText();
        }

        /// <summary>
        /// Copies the essential attributes from one JTX workspace configuration object to another
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="target">The target object</param>
        private void CopyDataWorkspace(
            Common.WorkspaceInfo source,
            ref IJTXWorkspaceConfiguration target,
            IGPMessages msgs)
        {
            target.Name = source.Name;
            target.Server = source.Server;
            target.Instance = source.Instance;
            target.Database = source.Database;
            target.Version = source.Version;
            target.OSAuthentication = source.UseOsAuthentication;
            target.IndividualLogins = source.UseIndividualLogins;
            foreach( Common.WorkspaceInfo.LoginInfo srcLogin in source.Logins )
            {
                string srcPassword = srcLogin.DatabasePassword;
                bool isPasswordEncrypted = srcLogin.IsPasswordEncrypted;

                // TODO: Remove this once the "blank password" bug is fixed (WMX TFS #4449)
                if (string.IsNullOrEmpty(srcPassword))
                {
                    srcPassword = "x";
                    isPasswordEncrypted = false;
                }
                // END TODO

                // NOTE: This seems to be where an actual database connection is finally
                // made, and so is the source for many of the exceptions that can be
                // encountered when using this tool.  With this in mind, try to handle
                // exceptions appropriately starting here.
                try
                {
                    target.AddLogin(srcLogin.WmxUsername, srcLogin.DatabaseUsername, srcPassword, isPasswordEncrypted);
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    if (m_errorBehavior.Equals(C_OPT_FAIL_ON_ERROR))
                    {
                        throw comEx;
                    }
                    else if (m_errorBehavior.Equals(C_OPT_IGNORE_LOGIN_ERRORS))
                    {
                        if (comEx.ErrorCode == (int)fdoError.FDO_E_SE_INVALID_USER)
                        {
                            msgs.AddWarning("Invalid user login detected for workspace '" +
                                target.Name + "'; proceeding anyway");
                        }
                        else
                        {
                            throw comEx;
                        }
                    }
                    else
                    {
                        msgs.AddWarning(target.Name + ": " + comEx.Message + "; proceeding anyway");
                    }
                }
            }
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
                IGPCodedValueDomain cvDomain = null;

                // JXL file parameter (path to output JXL file)
                IGPFileDomain excelFileDomain = new GPFileDomainClass();
                excelFileDomain.AddType("xls");
                excelFileDomain.AddType("xlsx");

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_CDW_EXCEL_FILE_PATH,
                    C_PARAM_EXCEL_FILE_PATH,
                    new DEFileTypeClass() as IGPDataType,
                    null);
                paramEdit.Domain = excelFileDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter describing what the tool should do when it encounters
                // an error connecting to a workspace
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddStringCode(C_OPT_FAIL_ON_ERROR, C_OPT_FAIL_ON_ERROR);
                cvDomain.AddStringCode(C_OPT_IGNORE_LOGIN_ERRORS, C_OPT_IGNORE_LOGIN_ERRORS);
                cvDomain.AddStringCode(C_OPT_IGNORE_ALL_ERRORS, C_OPT_IGNORE_ALL_ERRORS);

                IGPString strVal = new GPStringClass();
                strVal.Value = C_DEFAULT_ERROR_BEHAVIOR;

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CDW_ERROR_BEHAVIOR,
                    C_PARAM_ERROR_BEHAVIOR,
                    (strVal as IGPValue).DataType,
                    strVal as IGPValue);
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Output parameter indicating how many workspaces were created
                IGPMultiValueType mvType = new GPMultiValueTypeClass();
                mvType.MemberDataType = new GPStringTypeClass();

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_CDW_WORKSPACES_CREATED,
                    C_PARAM_OUT_WORKSPACES_CREATED,
                    mvType as IGPDataType,
                    null);
                m_parameters.Add(paramEdit);

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

            WorkspaceWorksheetReader reader = null;
            
            try
            {
                // Ensure that the current user has admin access to the current Workflow Manager DB
                if (!CurrentUserIsWmxAdministrator())
                {
                    throw new WmauException(WmauErrorCodes.C_USER_NOT_ADMIN_ERROR);
                }

                reader = new WorkspaceWorksheetReader(this.m_excelFilePath, msgs);

                // Prepare to set/build the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameter3 outParam = paramMap.GetParam(C_PARAM_OUT_WORKSPACES_CREATED);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_OUT_WORKSPACES_CREATED);
                IGPMultiValue outMultiValue = new GPMultiValueClass();
                outMultiValue.MemberDataType = outParam.DataType;

                // Load the workspace info from the spreadsheet
                List<Common.WorkspaceInfo> dataWorkspaces = reader.GetWorkspacesFromSpreadsheet();

                // Loop through each of the workspaces
                IJTXDatabaseConnectionManager dbConnectionManager = new JTXDatabaseConnectionManagerClass();
                IJTXDatabaseConnection dbConnection = dbConnectionManager.GetConnection(WmxDatabase.Alias);
                foreach (Common.WorkspaceInfo wmauWorkspaceInfo in dataWorkspaces)
                {
                    string workspaceName = wmauWorkspaceInfo.Name;
                    if (Common.WmauHelperFunctions.LookupWorkspaceNameObj(this.WmxDatabase, workspaceName) != null)
                    {
                        msgs.AddWarning("Skipping existing workspace '" + workspaceName + "'");
                    }
                    else
                    {
                        IJTXWorkspaceConfiguration workspaceInfo = dbConnection.AddDataWorkspace();
                        this.CopyDataWorkspace(wmauWorkspaceInfo, ref workspaceInfo, msgs);
                        workspaceInfo.Store();
                        msgs.AddMessage("Added new workspace '" + workspaceName + "'");

                        IGPString outElement = new GPStringClass();
                        outElement.Value = workspaceName;
                        outMultiValue.AddValue(outElement as IGPValue);
                    }
                }

                // Set the value of the output parameter
                outParamEdit.Value = outMultiValue as IGPValue;
                
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
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

    }
}
