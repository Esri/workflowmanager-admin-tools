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
    class ListAllTaskAssistantWorkbooks : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_TAM_WORKBOOK_LIST = "out_stringlist_workbookNames";
        #endregion

        #region MemberVariables
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ListAllTaskAssistantWorkbooks"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_LIST_ALL_TASK_ASSISTANT_WORKBOOKS; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_TAM_UTILS; } }
        #endregion

        #region Private helper functions
        /// <summary>
        /// Gets a list of all the TAM workbooks that are stored in the current WMX database
        /// </summary>
        /// <returns>A sorted list of the TAM workbooks</returns>
        private SortedList<string, string> ListTamWorkbooksInDatabase()
        {
            SortedList<string, string> tamWorkbookNames = new SortedList<string, string>();

            IJTXConfiguration3 configMgr = WmxDatabase.ConfigurationManager as IJTXConfiguration3;
            IJTXTaskAssistantWorkflowRecordSet tamWorkbooks = configMgr.TaskAssistantWorkflowRecords;
            for (int i = 0; i < tamWorkbooks.Count; i++)
            {
                IJTXTaskAssistantWorkflowRecord tamWorkbook = tamWorkbooks.get_Item(i);
                tamWorkbookNames.Add(tamWorkbook.Alias, null);
            }

            return tamWorkbookNames;
        }

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

                // TAM workbook list
                IGPMultiValueType workbookListType = new GPMultiValueTypeClass();
                workbookListType.MemberDataType = new GPStringTypeClass();

                IGPParameterEdit3 workbookList = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_LTAM_TAM_WORKBOOK_LIST,
                    C_PARAM_TAM_WORKBOOK_LIST,
                    workbookListType as IGPDataType,
                    null);
                m_parameters.Add(workbookList);

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
                // Retrieve the parameter in which the list of workbook names will be stored
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameter3 param = paramMap.GetParam(C_PARAM_TAM_WORKBOOK_LIST);
                IGPParameterEdit3 paramEdit = paramMap.GetParamEdit(C_PARAM_TAM_WORKBOOK_LIST);

                // Set up the multi-value objects
                IGPMultiValue mvValue = new GPMultiValueClass();
                mvValue.MemberDataType = param.DataType;

                // Get the list of TA workbook names and add them all to the multivalue
                SortedList<string, string> tamWorkbooks = this.ListTamWorkbooksInDatabase();
                foreach (string workbookAlias in tamWorkbooks.Keys)
                {
                    IGPString strVal = new GPStringClass();
                    strVal.Value = workbookAlias;
                    mvValue.AddValue(strVal as IGPValue);
                    msgs.AddMessage("Workbook: " + workbookAlias);
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
        }

    }
}
