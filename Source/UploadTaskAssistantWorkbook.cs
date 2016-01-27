//Copyright 2015 Esri
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.?

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;


namespace WorkflowManagerAdministrationUtilities
{
    public class UploadTaskAssistantWorkbook : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_XML_FILE_PATH = "in_file_tamWorkbookXml";
        private const string C_PARAM_TARGET_NAME = "in_string_targetName";
        private const string C_PARAM_OVERWRITE_EXISTING = "in_boolean_overwriteExisting";
        private const string C_PARAM_OUT_TARGET_NAME = "out_string_targetName";

        private const string C_OPT_OVERWRITE = "OVERWRITE";
        private const string C_OPT_NO_OVERWRITE = "NO_OVERWRITE";

        private const bool C_DEFAULT_OVERWRITE_EXISTING = true;
        #endregion

        #region MemberVariables
        private string m_xmlFilePath = string.Empty;
        private string m_targetName = string.Empty;
        private bool m_overwriteExisting = C_DEFAULT_OVERWRITE_EXISTING;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "UploadTaskAssistantWorkbook"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_UPLOAD_TASK_ASSISTANT_WORKBOOK; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_TAM_UTILS; } }
        #endregion

        #region Private helper functions
        /// <summary>
        /// Helper function to build a ".TMStyle" file name given a ".xml" workflow file name
        /// </summary>
        /// <param name="workbookFileName"></param>
        /// <returns></returns>
        private string DetermineStyleFileName(string workbookFileName)
        {
            string styleFileName = System.IO.Path.GetDirectoryName(workbookFileName) +
                System.IO.Path.DirectorySeparatorChar + 
                System.IO.Path.GetFileNameWithoutExtension(workbookFileName) +
                ".TMStyle";

            return styleFileName;
        }

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
            param = paramMap.GetParam(C_PARAM_XML_FILE_PATH);
            m_xmlFilePath = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_TARGET_NAME);
            m_targetName = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_OVERWRITE_EXISTING);
            m_overwriteExisting = bool.Parse(param.Value.GetAsText());
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

                // TAM file parameter (path to source XML file)
                IGPFileDomain xmlFileDomain = new GPFileDomainClass();
                xmlFileDomain.AddType("xml");

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_UTAM_XML_FILE_PATH,
                    C_PARAM_XML_FILE_PATH,
                    new DEFileTypeClass() as IGPDataType,
                    null);
                paramEdit.Domain = xmlFileDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Target name
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_UTAM_TARGET_NAME,
                    C_PARAM_TARGET_NAME,
                    new GPStringTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Optional parameter indicating whether existing TAM workbooks of the same name should
                // be overwritten
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_OVERWRITE);
                cvDomain.AddCode(GpFalse, C_OPT_NO_OVERWRITE);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_UTAM_OVERWRITE_EXISTING,
                    C_PARAM_OVERWRITE_EXISTING,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_OVERWRITE_EXISTING));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Output parameter (echoing the uploaded workbook's name in the database)
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_UTAM_OUT_TARGET_NAME,
                    C_PARAM_OUT_TARGET_NAME,
                    new GPStringTypeClass(),
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

            try
            {
                // Ensure that the current user has admin access to the current Workflow Manager DB
                if (!CurrentUserIsWmxAdministrator())
                {
                    throw new WmauException(WmauErrorCodes.C_USER_NOT_ADMIN_ERROR);
                }

                // Retrieve the TA workbook
                IJTXConfiguration3 defaultDbReadonly = WmxDatabase.ConfigurationManager as IJTXConfiguration3;
                IJTXTaskAssistantWorkflowRecord tamRecord = defaultDbReadonly.GetTaskAssistantWorkflowRecord(this.m_targetName);
                string styleFileName = this.DetermineStyleFileName(this.m_xmlFilePath);

                // If we're not allowed to overwrite an existing TA record, then do some error checking
                if (!this.m_overwriteExisting && tamRecord != null)
                {
                    msgs.AddWarning("Did not overwrite Task Assistant workbook: " + this.m_targetName);
                    return;
                }
                else if (tamRecord != null)
                {
                    msgs.AddMessage("Replacing Task Assistant workbook '" + m_targetName + "' in database...");
                    defaultDbReadonly.ReplaceTaskAssistantWorkflowRecord(this.m_targetName, this.m_targetName, this.m_xmlFilePath, styleFileName);
                }
                else // tamRecord == null
                {
                    msgs.AddMessage("Adding Task Assistant workbook '" + m_targetName + "' to database...");
                    defaultDbReadonly.AddTaskAssistantWorkflowRecord(this.m_targetName, this.m_xmlFilePath, styleFileName);
                }

                // Update the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_OUT_TARGET_NAME);
                IGPString outValue = new GPStringClass();
                outValue.Value = m_targetName;
                outParamEdit.Value = outValue as IGPValue;

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
                    WmauError error = new WmauError(WmauErrorCodes.C_TAM_UPLOAD_ERROR);
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
