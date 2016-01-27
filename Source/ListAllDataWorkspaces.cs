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
    class ListAllDataWorkspaces : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_WORKSPACE_LIST = "out_stringlist_dataWorkspaceNames";
        #endregion

        #region MemberVariables
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ListAllDataWorkspaces"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_LIST_ALL_DATA_WORKSPACES; } }
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

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Map document list
                IGPMultiValueType mxdListType = new GPMultiValueTypeClass();
                mxdListType.MemberDataType = new GPStringTypeClass();

                IGPParameterEdit3 mxdList = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_LWSP_DATA_WORKSPACE_LIST,
                    C_PARAM_WORKSPACE_LIST,
                    mxdListType as IGPDataType,
                    null);
                m_parameters.Add(mxdList);

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

            try
            {
                // Ensure that the current user has admin access to the current Workflow Manager DB
                if (!CurrentUserIsWmxAdministrator())
                {
                    throw new WmauException(WmauErrorCodes.C_USER_NOT_ADMIN_ERROR);
                }

                // Retrieve the parameter in which the data workspace names will be stored
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameter3 param = paramMap.GetParam(C_PARAM_WORKSPACE_LIST);
                IGPParameterEdit3 paramEdit = paramMap.GetParamEdit(C_PARAM_WORKSPACE_LIST);

                // Set up the multi-value objects
                IGPMultiValue mvValue = new GPMultiValueClass();
                mvValue.MemberDataType = param.DataType;

                // Workflow Manager intentionally caches the data workspaces in the system.  To ensure
                // that we have the most current list of data workspaces, invalidate this cache
                // before attempting to retrieve the list from the system.
                this.WmxDatabase.InvalidateDataWorkspaceNames();

                // Get the list of names
                SortedList<string, string> sortedValues = new SortedList<string, string>();
                IJTXDataWorkspaceNameSet allValues = this.WmxDatabase.GetDataWorkspaceNames(null);
                for (int i = 0; i < allValues.Count; i++)
                {
                    IJTXDataWorkspaceName ws = allValues.get_Item(i);
                    sortedValues.Add(ws.Name, null);
                }

                // Add the names to the output parameter
                foreach (string name in sortedValues.Keys)
                {
                    IGPString strVal = new GPStringClass();
                    strVal.Value = name;
                    mvValue.AddValue(strVal as IGPValue);
                    msgs.AddMessage("Workspace: " + name);
                }
                paramEdit.Value = (IGPValue)mvValue;

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
