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
using System.Xml;

using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;


namespace WorkflowManagerAdministrationUtilities
{
    class BackupWorkflowManagerDatabase : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JXL_FILE_PATH = "out_file_jxlFile";
        private const string C_PARAM_PRETTY_PRINT = "in_bool_prettyPrint";

        private const string C_OPT_PRETTY_PRINT = "PRETTY_PRINT";
        private const string C_OPT_DEFAULT_FORMATTING = "DEFAULT_FORMATTING";

        private const bool C_DEFAULT_PRETTY_PRINT = false;
        #endregion

        #region MemberVariables
        private string m_jxlFilePath = string.Empty;
        private bool m_prettyPrint = C_DEFAULT_PRETTY_PRINT;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "BackupWorkflowManagerDatabase"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_BACKUP_WORKFLOW_MANAGER_DB; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_WMX_DB_UTILS; } }
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

            param = paramMap.GetParam(C_PARAM_JXL_FILE_PATH);
            m_jxlFilePath = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_PRETTY_PRINT);
            m_prettyPrint = (param.Value as IGPBoolean).Value;
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
                IGPFileDomain jxlFileDomain = new GPFileDomainClass();
                jxlFileDomain.AddType("jxl");

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_BW_JXL_FILE_PATH,
                    C_PARAM_JXL_FILE_PATH,
                    new DEFileTypeClass() as IGPDataType,
                    null);
                paramEdit.Domain = jxlFileDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Optional parameter indicating whether the output file should be run through
                // a pretty-printing algorithm to make it more human-readable
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_PRETTY_PRINT);
                cvDomain.AddCode(GpFalse, C_OPT_DEFAULT_FORMATTING);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_BW_PRETTY_PRINT,
                    C_PARAM_PRETTY_PRINT,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_PRETTY_PRINT));
                paramEdit.Domain = cvDomain as IGPDomain;
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
            
            // Back up the database
            IJTXTransfer transfer = WmxDatabase as IJTXTransfer;

            try
            {
                // Ensure that the current user has admin access to the current Workflow Manager DB
                if (!CurrentUserIsWmxAdministrator())
                {
                    throw new WmauException(WmauErrorCodes.C_USER_NOT_ADMIN_ERROR);
                }

                msgs.AddMessage("Retrieving data from Workflow Manager database...");
                string xml = transfer.ExportConfiguration();

                // Pretty-print the JXL file, if the user has selected that option
                if (this.m_prettyPrint)
                {
                    msgs.AddMessage("Making the data more easily human-readable...");
                    XmlDocument xmlDoc = new XmlDocument();
                    XmlTextReader xmlTextReader = new XmlTextReader(xml, XmlNodeType.Document, null);
                    xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
                    xmlDoc.Load(xmlTextReader);
                    xmlTextReader.Close();

                    msgs.AddMessage("Saving data to file...");
                    System.IO.TextWriter textWriter = new System.IO.StreamWriter(this.m_jxlFilePath, false, System.Text.Encoding.UTF8, xml.Length);
                    XmlTextWriter xmlTextWriter = new XmlTextWriter(textWriter);
                    xmlTextWriter.Formatting = Formatting.Indented;
                    xmlDoc.Save(xmlTextWriter);
                    xmlTextWriter.Close();
                    textWriter.Close();
                }
                else
                {
                    msgs.AddMessage("Saving data to file...");
                    System.IO.TextWriter textWriter = new System.IO.StreamWriter(this.m_jxlFilePath, false, System.Text.Encoding.UTF8, xml.Length);
                    textWriter.Write(xml);
                    textWriter.Close();
                }

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
                    WmauError error = new WmauError(WmauErrorCodes.C_JXL_BACKUP_ERROR);
                    msgs.AddError(error.ErrorCodeAsInt, error.Message + "; " + ex.Message);
                }
                catch
                {
                    // Catch anything else that possibly happens }
                }
            }
        }

    }
}
