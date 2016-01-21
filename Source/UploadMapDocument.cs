using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;


namespace WorkflowManagerAdministrationUtilities
{
    class UploadMapDocument : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_MXD_FILE_PATH = "in_file_mapDocument";
        private const string C_PARAM_TARGET_NAME = "in_string_targetName";
        private const string C_PARAM_TARGET_CATEGORY = "in_string_targetCategory";
        private const string C_PARAM_DESCRIPTION = "in_string_description";
        private const string C_PARAM_OVERWRITE_EXISTING = "in_boolean_overwriteExisting";
        private const string C_PARAM_OUT_TARGET_NAME = "out_string_targetName";

        private const string C_OPT_OVERWRITE = "OVERWRITE";
        private const string C_OPT_NO_OVERWRITE = "NO_OVERWRITE";

        private const bool C_DEFAULT_OVERWRITE_EXISTING = true;
        #endregion

        #region MemberVariables
        private string m_mxdFilePath = string.Empty;
        private string m_targetName = string.Empty;
        private string m_targetCategory = string.Empty;
        private string m_description = string.Empty;
        private bool m_overwriteExisting = C_DEFAULT_OVERWRITE_EXISTING;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "UploadMapDocument"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_UPLOAD_MAP_DOCUMENT; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_MXD_UTILS; } }
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
            param = paramMap.GetParam(C_PARAM_MXD_FILE_PATH);
            m_mxdFilePath = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_TARGET_NAME);
            m_targetName = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_TARGET_CATEGORY);
            m_targetCategory = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_DESCRIPTION);
            m_description = param.Value.GetAsText();

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

                // MXD file parameter (path to source MXD file)
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_UMXD_MXD_FILE_PATH,
                    C_PARAM_MXD_FILE_PATH,
                    new DEMapDocumentTypeClass() as IGPDataType,
                    null);
                m_parameters.Add(paramEdit);

                // Target name
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_UMXD_TARGET_NAME,
                    C_PARAM_TARGET_NAME,
                    new GPStringTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Optional parameter indicating the category of the map document
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_UMXD_TARGET_CATEGORY,
                    C_PARAM_TARGET_CATEGORY,
                    new GPStringTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Optional parameter indicating a description for the map document
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_UMXD_DESCRIPTION,
                    C_PARAM_DESCRIPTION,
                    new GPStringTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Optional parameter indicating whether existing items of the same name should
                // be overwritten
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_OVERWRITE);
                cvDomain.AddCode(GpFalse, C_OPT_NO_OVERWRITE);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_UMXD_OVERWRITE_EXISTING,
                    C_PARAM_OVERWRITE_EXISTING,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_OVERWRITE_EXISTING));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Output parameter (echoing the uploaded document's final name)
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_UMXD_OUT_TARGET_NAME,
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

                IJTXConfiguration3 configMgr = WmxDatabase.ConfigurationManager as IJTXConfiguration3;
                IJTXMapEdit wmxMapDoc = configMgr.GetJTXMap(this.m_targetName) as IJTXMapEdit;

                // If we're not allowed to overwrite an existing map document, then do some error checking
                if (!this.m_overwriteExisting && wmxMapDoc != null)
                {
                    msgs.AddWarning("Did not overwrite Map Document: " + this.m_targetName);
                    return;
                }
                else if (wmxMapDoc != null)
                {
                    msgs.AddMessage("Replacing Map Document '" + this.m_targetName + "' in database...");
                }
                else // wmxMapDoc == null
                {
                    msgs.AddMessage("Adding Map Document '" + this.m_targetName + "' to database...");
                    wmxMapDoc = configMgr.CreateJTXMap() as IJTXMapEdit;
                }

                IMapDocument mapDoc = new MapDocumentClass() as IMapDocument;
                mapDoc.Open(this.m_mxdFilePath, string.Empty);
                wmxMapDoc.Name = this.m_targetName;
                if (!string.IsNullOrEmpty(this.m_targetCategory))
                {
                    wmxMapDoc.Category = this.m_targetCategory;
                }
                if (!string.IsNullOrEmpty(this.m_description))
                {
                    wmxMapDoc.Description = this.m_description;
                }
                wmxMapDoc.Directory = string.Empty;
                wmxMapDoc.FileName = string.Empty;
                wmxMapDoc.MapDocument = mapDoc;
                wmxMapDoc.Store();
                mapDoc.Close();

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
                    WmauError error = new WmauError(WmauErrorCodes.C_MXD_UPLOAD_ERROR);
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
