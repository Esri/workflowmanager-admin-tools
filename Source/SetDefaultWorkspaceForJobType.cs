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
    class SetDefaultWorkspaceForJobType : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JOB_TYPE = "in_string_jobType";
        private const string C_PARAM_DATA_WORKSPACE = "in_string_dataWorkspace";
        private const string C_PARAM_PARENT_VERSION = "in_string_parentVersion";
        private const string C_PARAM_OUT_DATA_WORKSPACE = "out_string_dataWorkspace";

        private const string C_OPT_NONE = "[None]";
        #endregion

        #region MemberVariables
        private string m_jobTypeName = string.Empty;
        private string m_dataWorkspaceName = string.Empty;
        private string m_parentVersion = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "SetDefaultWorkspaceForJobType"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_SET_DEFAULT_WORKSPACE_FOR_JOB_TYPE; } }
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
            param = paramMap.GetParam(C_PARAM_JOB_TYPE);
            m_jobTypeName = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_DATA_WORKSPACE);
            m_dataWorkspaceName = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_PARENT_VERSION);
            m_parentVersion = param.Value.GetAsText();
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

                // Parameter indicating the job type to be used
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_SDW_JOB_TYPE,
                    C_PARAM_JOB_TYPE,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the name of the job type's default data workspace
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_SDW_DATA_WORKSPACE,
                    C_PARAM_DATA_WORKSPACE,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the job type's default parent version
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_SDW_PARENT_VERSION,
                    C_PARAM_PARENT_VERSION,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the name of the job type's default data workspace (output)
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_SDW_OUT_DATA_WORKSPACE,
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
            try
            {
                UpdateMessagesCommon(paramValues, pEnvMgr, msgs);
            }
            catch (WmxDefaultDbNotSetException)
            {
                // If the default DB wasn't set, stop executing
                return;
            }

            // Build a hash of which parameter is at which index for ease of access
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);
            IGPParameter3 dataWorkspaceName = paramMap.GetParam(C_PARAM_DATA_WORKSPACE);
            IGPParameter3 parentVersion = paramMap.GetParam(C_PARAM_PARENT_VERSION);

            // If there's no domain on the parent version parameter, then something went
            // awry
            if (parentVersion.Domain == null)
            {
                WmauError error = new WmauError(WmauErrorCodes.C_VERSION_LOOKUP_ERROR);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_PARENT_VERSION), error.ErrorCodeAsInt, error.Message);
            }

            // Store away the latest value of the data workspace
            m_dataWorkspaceName = dataWorkspaceName.Value.GetAsText();
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

            // Get the parameters as a map for easier access
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);
            IGPParameter3 jobTypeParam = paramMap.GetParam(C_PARAM_JOB_TYPE);
            IGPParameterEdit3 jobTypeParamEdit = paramMap.GetParamEdit(C_PARAM_JOB_TYPE);
            IGPParameter3 dataWorkspace = paramMap.GetParam(C_PARAM_DATA_WORKSPACE);
            IGPParameterEdit3 dataWorkspaceEdit = paramMap.GetParamEdit(C_PARAM_DATA_WORKSPACE);
            IGPParameter3 parentVersion = paramMap.GetParam(C_PARAM_PARENT_VERSION);
            IGPParameterEdit3 parentVersionEdit = paramMap.GetParamEdit(C_PARAM_PARENT_VERSION);

            // Set the domains for any parameters that need them
            if (jobTypeParam.Domain == null)
            {
                jobTypeParamEdit.Domain = Common.WmauGpDomainBuilder.BuildJobTypeDomain(this.WmxDatabase);
            }
            if (dataWorkspace.Domain == null)
            {
                dataWorkspaceEdit.Domain = Common.WmauGpDomainBuilder.BuildWorkspaceDomain(
                    this.WmxDatabase,
                    new string[] { C_OPT_NONE });
            }

            // Only update the domain for the parent version field if it hasn't yet been
            // populated, or if the value of the data workspace parameter has changed
            string newDataWorkspace = dataWorkspace.Value.GetAsText();
            if (parentVersion.Domain == null ||
                !m_dataWorkspaceName.Equals(newDataWorkspace))
            {
                IGPDomain versionDomain = null;
                string newJobType = jobTypeParam.Value.GetAsText();
                IJTXJobType3 jobType = this.WmxDatabase.ConfigurationManager.GetJobType(newJobType) as IJTXJobType3;

                if (newDataWorkspace.Equals(C_OPT_NONE) ||
                    newDataWorkspace.Equals(string.Empty))
                {
                    // Case 1: the only acceptable option for the parent version is "none".
                    IGPCodedValueDomain cvDomain = new GPCodedValueDomainClass();
                    cvDomain.AddStringCode(C_OPT_NONE, C_OPT_NONE);
                    versionDomain = cvDomain as IGPDomain;
                }
                else
                {
                    // Case 2: we need to retrieve the version list for an existing
                    // data workspace
                    try
                    {
                        versionDomain = Common.WmauGpDomainBuilder.BuildVersionsDomain(
                            this.WmxDatabase, newDataWorkspace, new string[] { C_OPT_NONE });
                    }
                    catch (WmauException)
                    {
                        // Use the "null" as an error value; we'll check for this later
                        versionDomain = null;
                    }
                }
                parentVersionEdit.Domain = versionDomain;
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

            // Set the default properties for the specified job type
            try
            {
                IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;
                IJTXConfigurationEdit2 configEdit = this.WmxDatabase.ConfigurationManager as IJTXConfigurationEdit2;
                IJTXJobTypeEdit3 jobType = configEdit.GetJobType(m_jobTypeName) as IJTXJobTypeEdit3;

                // Set the default data workspace for the selected job type
                if (m_dataWorkspaceName.Equals(C_OPT_NONE))
                {
                    jobType.DefaultDataWorkspace = null;
                    msgs.AddMessage("Clearing default data workspace for job type '" + m_jobTypeName + "'");
                }
                else
                {
                    // Translate the workspace name to a DB name object
                    IJTXDataWorkspaceName dwName =
                        Common.WmauHelperFunctions.LookupWorkspaceNameObj(this.WmxDatabase, m_dataWorkspaceName);

                    msgs.AddMessage("Default data workspace for job type '" + m_jobTypeName + "' is now '" + dwName.Name + "'");
                    jobType.DefaultDataWorkspace = dwName;
                }

                // Set the default parent version for the selected job type
                if (m_parentVersion.Equals(C_OPT_NONE))
                {
                    msgs.AddMessage("Clearing default parent version for job type '" + m_jobTypeName + "'");
                    jobType.DefaultParentVersionName_2 = string.Empty;
                }
                else
                {
                    msgs.AddMessage("Default parent version for job type '" + m_jobTypeName + "' is now '" + m_parentVersion + "'");
                    jobType.DefaultParentVersionName_2 = m_parentVersion;
                }

                // Save the changes to the job type
                jobType.Store();

                // Set the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_OUT_DATA_WORKSPACE);
                IGPString outValue = new GPStringClass();
                outValue.Value = m_jobTypeName;
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
                WmauError error = new WmauError(WmauErrorCodes.C_UNSPECIFIED_ERROR);
                msgs.AddError(error.ErrorCodeAsInt, error.Message + "; " + ex.Message);
            }
        }
    }
}
