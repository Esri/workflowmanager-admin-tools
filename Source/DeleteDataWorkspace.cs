//Copyright 2015 Esri
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.​

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;


namespace WorkflowManagerAdministrationUtilities
{
    class DeleteDataWorkspace : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_DATA_WORKSPACE = "in_string_dataWorkspace";
        private const string C_PARAM_OUT_DATA_WORKSPACE = "out_string_workspaceDeleted";
        #endregion

        #region MemberVariables
        private string m_dataWorkspace = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "DeleteDataWorkspace"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_DELETE_DATA_WORKSPACE; } }
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
            param = paramMap.GetParam(C_PARAM_DATA_WORKSPACE);
            m_dataWorkspace = param.Value.GetAsText();
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

                // Parameter indicating the workspace to be deleted
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_DDW_DATA_WORKSPACE,
                    C_PARAM_DATA_WORKSPACE,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the workspace that was deleted
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_DDW_OUT_DATA_WORKSPACE,
                    C_PARAM_OUT_DATA_WORKSPACE,
                    new GPStringTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                return m_parameters;
            }
        }

        /// <summary>
        /// Post validates the given set of values.
        /// This is where you flag parameters with warnings and error messages, among other things.
        /// </summary>
        /// <param name="paramValues"></param>
        /// <param name="pEnvMgr"></param>
        /// <param name="msgs"></param>
        public override void UpdateMessages(IArray paramValues, IGPEnvironmentManager pEnvMgr, IGPMessages msgs)
        {
            // Call the base class function first
            try
            {
                UpdateMessagesCommon(paramValues, pEnvMgr, msgs);
            }
            catch (WmxDefaultDbNotSetException)
            {
                // If the default DB wasn't set, stop executing
                return;
            }

            // Build a hash of which parameter is at which index, for ease of access
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);

            IGPParameter3 dataWorkspace = paramMap.GetParam(C_PARAM_DATA_WORKSPACE);

            // Ensure that there is at least one data workspace defined in the database
            if (dataWorkspace.Domain == null || (dataWorkspace.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                WmauError error = new WmauError(WmauErrorCodes.C_NO_WORKSPACES_DEFINED_ERROR);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_DATA_WORKSPACE), error.ErrorCodeAsInt, error.Message);
            }
        }

        /// <summary>
        /// Pre validates the given set of values.
        /// This is where you populate derived parameters based on input, among other things.
        /// </summary>
        /// <param name="paramValues"></param>
        /// <param name="pEnvMgr"></param>
        public override void UpdateParameters(IArray paramValues, IGPEnvironmentManager pEnvMgr)
        {
            try
            {
                UpdateParametersCommon(paramValues, pEnvMgr);
            }
            catch (WmxDefaultDbNotSetException)
            {
                // If the default DB wasn't set, stop executing
                return;
            }
            catch (NullReferenceException)
            {
                // If one of the parameters was null, stop executing
                return;
            }

            // Build a hash of the parameters for ease of access
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);

            IGPParameter3 dataWorkspace = paramMap.GetParam(C_PARAM_DATA_WORKSPACE);
            IGPParameterEdit3 dataWorkspaceEdit = paramMap.GetParamEdit(C_PARAM_DATA_WORKSPACE);

            // Apply a domain to the data workspace list, if necessary
            if (dataWorkspace.Domain == null || (dataWorkspace.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                dataWorkspaceEdit.Domain = Common.WmauGpDomainBuilder.BuildWorkspaceDomain(this.WmxDatabase);
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

            try
            {
                // Ensure that the current user has admin access to the current Workflow Manager DB
                if (!CurrentUserIsWmxAdministrator())
                {
                    throw new WmauException(WmauErrorCodes.C_USER_NOT_ADMIN_ERROR);
                }

                // Create the object needed to get at the data workspaces
                IJTXDatabaseConnectionManager dbConnectionManager = new JTXDatabaseConnectionManagerClass();
                IJTXDatabaseConnection dbConnection = dbConnectionManager.GetConnection(this.WmxDatabase.Alias);

                // Loop through the data workspaces until we find the one we're looking for, then
                // delete it
                bool removedWorkspace = false;
                for (int i = 0; i < dbConnection.DataWorkspaceCount; i++)
                {
                    IJTXWorkspaceConfiguration workspaceCfg = dbConnection.get_DataWorkspace(i);
                    if (workspaceCfg.Name.Equals(m_dataWorkspace))
                    {
                        dbConnection.RemoveDataWorkspace(i);
                        removedWorkspace = true;
                        break;
                    }
                }

                // Raise an error if we somehow couldn't find it (should never happen with
                // the domain on the input param)
                if (!removedWorkspace)
                {
                    throw new WmauException(WmauErrorCodes.C_WORKSPACE_NOT_FOUND_ERROR);
                }

                // Set the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_OUT_DATA_WORKSPACE);
                IGPString outParamStr = new GPStringClass();
                outParamStr.Value = m_dataWorkspace;
                outParamEdit.Value = outParamStr as IGPValue;

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
                    // Catch anything else that possibly happens
                }
            }
            catch (Exception ex)
            {
                try
                {
                    WmauError error = new WmauError(WmauErrorCodes.C_UNSPECIFIED_ERROR);
                    msgs.AddError(error.ErrorCodeAsInt, error.Message + "; " + ex.Message);
                }
                catch
                {
                    // Catch anything else that possibly happens
                }
            }
            finally
            {
                // Release any COM objects here!
            }
        }
    }
}
