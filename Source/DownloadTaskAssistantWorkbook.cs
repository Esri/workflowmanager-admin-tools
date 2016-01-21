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
    class DownloadTaskAssistantWorkbook : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_SOURCE_NAME = "in_string_sourceName";
        private const string C_PARAM_XML_FILE_PATH = "out_file_tamWorkbookXml";
        #endregion

        #region MemberVariables
        private string m_sourceName = string.Empty;
        private string m_xmlFilePath = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "DownloadTaskAssistantWorkbook"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_DOWNLOAD_TASK_ASSISTANT_WORKBOOK; } }
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
            string styleFileName = System.IO.Path.GetFileNameWithoutExtension(workbookFileName) + ".TMStyle";
            return System.IO.Path.GetDirectoryName(workbookFileName) + System.IO.Path.DirectorySeparatorChar + styleFileName;
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

            param = paramMap.GetParam(C_PARAM_SOURCE_NAME);
            m_sourceName = param.Value.GetAsText();
        }

        /// <summary>
        /// Helper function to delete a file if it exists
        /// </summary>
        /// <param name="filePath">Full path to the file to be deleted</param>
        private void DeleteFile(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        /// <summary>
        /// Helper function to save non-empty strings to XML files
        /// </summary>
        /// <param name="xml">A text string containing the XML to be written to file</param>
        /// <param name="filePath">Full path to the file to be written</param>
        private void SaveStringToXmlFile(string xml, string filePath)
        {
            if (!string.IsNullOrEmpty(xml))
            {
                System.IO.TextWriter textWriter = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8, xml.Length);
                textWriter.Write(xml);
                textWriter.Close();
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

                // Source name
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_DTAM_SOURCE_NAME,
                    C_PARAM_SOURCE_NAME,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // TAM file parameter (path to target XML file)
                IGPFileDomain xmlFileDomain = new GPFileDomainClass();
                xmlFileDomain.AddType("xml");

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_DTAM_XML_FILE_PATH,
                    C_PARAM_XML_FILE_PATH,
                    new DEFileTypeClass() as IGPDataType,
                    null);
                paramEdit.Domain = xmlFileDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                return m_parameters;
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

            WmauParameterMap paramMap = new WmauParameterMap(paramValues);

            // Retrieve the parameter for source name and add a domain to it
            IGPParameter3 param = paramMap.GetParam(C_PARAM_SOURCE_NAME);
            IGPParameterEdit3 paramEdit = paramMap.GetParamEdit(C_PARAM_SOURCE_NAME);
            if (param.Domain == null)
            {
                // If there isn't a domain on this parameter yet, that means that it has
                // not yet had a list of possible TAM workbooks populated.  In that case, do
                // so at this time.
                paramEdit.Domain = Common.WmauGpDomainBuilder.BuildTamWorkbookDomain(this.WmxDatabase);
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
                // Update the internal parameters used by this GP tool
                string styleFileName = this.DetermineStyleFileName(this.m_xmlFilePath);

                // Retrieve the TA workbook
                IJTXConfiguration3 defaultDbReadonly = WmxDatabase.ConfigurationManager as IJTXConfiguration3;
                IJTXTaskAssistantWorkflowRecord tamRecord = defaultDbReadonly.GetTaskAssistantWorkflowRecord(this.m_sourceName);

                // Delete any existing workflow or style files that we're going to replace
                this.DeleteFile(this.m_xmlFilePath);
                this.DeleteFile(styleFileName);

                // Save the TAM workbook data out to file
                this.SaveStringToXmlFile(tamRecord.WorkflowXML, this.m_xmlFilePath);
                this.SaveStringToXmlFile(tamRecord.StyleXML, styleFileName);

                msgs.AddMessage(Properties.Resources.MSG_DONE);
            }
            catch (System.IO.IOException ioEx)
            {
                try
                {
                    WmauError error = new WmauError(WmauErrorCodes.C_FILE_ACCESS_ERROR);
                    msgs.AddError(error.ErrorCodeAsInt, error.Message + "; " + ioEx.Message);
                }
                catch
                {
                    // Catch anything else that possibly happens
                }
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
                    WmauError error = new WmauError(WmauErrorCodes.C_TAM_DOWNLOAD_ERROR);
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
