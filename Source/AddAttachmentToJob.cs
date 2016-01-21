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
    class AddAttachmentToJob : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JOB_ID = "in_long_jobId";
        private const string C_PARAM_ATTACHMENT = "in_file_attachment";
        private const string C_PARAM_ATTACHMENT_TYPE = "in_string_attachmentType";
        private const string C_PARAM_OUT_JOB_ID = "out_long_jobId";

        private const string C_OPT_EMBEDDED = "EMBEDDED";
        private const string C_OPT_LINKED = "LINKED";

        private const string C_DEFAULT_ATTACHMENT_TYPE = C_OPT_EMBEDDED;

        // These values match up with other predetermined values referenced by Workflow
        // Manager.  Do not modify.
        private const string C_PROP_VAL_ATTACHMENT = "[ATTACHMENT]";
        #endregion

        #region MemberVariables
        private int m_jobId = -1;
        private string m_attachmentPath = string.Empty;
        private string m_attachmentType = C_DEFAULT_ATTACHMENT_TYPE;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "AddAttachmentToJob"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_ADD_ATTACHMENT_TO_JOB; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_JOB_UTILS; } }
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
            param = paramMap.GetParam(C_PARAM_JOB_ID);
            m_jobId = int.Parse(param.Value.GetAsText());

            param = paramMap.GetParam(C_PARAM_ATTACHMENT);
            m_attachmentPath = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_ATTACHMENT_TYPE);
            m_attachmentType = param.Value.GetAsText();
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

                // Parameter indicating the ID of the job to which the attachment will
                // be added
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_AA_JOB_ID,
                    C_PARAM_JOB_ID,
                    new GPLongTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the path to the file to be attached
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_AA_ATTACHMENT,
                    C_PARAM_ATTACHMENT,
                    new DEFileTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Parameter indicating how to associate the attachment with the job
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddStringCode(C_OPT_EMBEDDED, C_OPT_EMBEDDED);
                cvDomain.AddStringCode(C_OPT_LINKED, C_OPT_LINKED);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_AA_ATTACHMENT_TYPE,
                    C_PARAM_ATTACHMENT_TYPE,
                    cvDomain.FindValue(C_DEFAULT_ATTACHMENT_TYPE).DataType,
                    cvDomain.FindValue(C_DEFAULT_ATTACHMENT_TYPE));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the job that was assigned
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_AA_OUT_JOB_ID,
                    C_PARAM_OUT_JOB_ID,
                    new GPLongTypeClass(),
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

            // Ensure that the current user has permissions to be adding attachments to jobs
            if (!CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_MANAGE_ATTACHMENTS))
            {
                WmauError error = new WmauError(WmauErrorCodes.C_NO_MANAGE_ATTACHMENTS_PRIV_ERROR);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_JOB_ID), error.ErrorCodeAsInt, error.Message);
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

            // Get the parameters as a map for easier access
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);
            IGPParameter3 jobId = paramMap.GetParam(C_PARAM_JOB_ID);
            IGPParameterEdit3 jobIdEdit = paramMap.GetParamEdit(C_PARAM_JOB_ID);

            // Set the domains for any parameters that need them

            // Set the job ID domain if it hasn't already been populated
            if (jobId.Domain == null)
            {
                jobIdEdit.Domain = Common.WmauGpDomainBuilder.BuildNonClosedJobIdDomain(this.WmxDatabase);
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

            // Assign the requested job
            try
            {
                IJTXJobManager jobManager = this.WmxDatabase.JobManager;
                IJTXJob3 job = jobManager.GetJob(m_jobId) as IJTXJob3;
                IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;

                // As of Jan. 2011, the core Workflow Manager libraries do not
                // seem to check if the user has the privilege to add an attachment
                // if a job as a hold on it.  So run the check here.
                IJTXJobHolds jobHolds = job as IJTXJobHolds;
                if (jobHolds.Holds != null &&
                    jobHolds.Holds.Count > 0 &&
                    !CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_CAN_ADD_ATTACHES_FOR_HELD_JOBS))
                {
                    throw new WmauException(WmauErrorCodes.C_NO_ADD_ATTACHMENTS_HELD_JOBS_ERROR);
                }

                // If we get this far, then figure out how to associate the attachment
                // with the job, and add the attachment.
                jtxFileStorageType attachmentType;
                if (m_attachmentType.Equals(C_OPT_EMBEDDED))
                {
                    attachmentType = jtxFileStorageType.jtxStoreInDB;
                }
                else
                {
                    attachmentType = jtxFileStorageType.jtxStoreAsLink;
                }

                msgs.AddMessage("Adding attachment '" + m_attachmentPath + "' to job " + m_jobId + " (" + job.Name + ")");
                job.AddAttachment(m_attachmentPath, attachmentType, m_attachmentType);
                job.Store();

                // Do the other things that still need to be handled manually, such as logging
                // the job's reassignment and sending any necessary notifications.
                IPropertySet propSet = new PropertySetClass();
                propSet.SetProperty(C_PROP_VAL_ATTACHMENT, "'" + m_attachmentPath + "'");
                job.LogJobAction(
                    configMgr.GetActivityType(ESRI.ArcGIS.JTX.Utilities.Constants.ACTTYPE_ADD_ATTACHMENT),
                    propSet,
                    string.Empty);
                Common.WmauHelperFunctions.SendNotification(
                    ESRI.ArcGIS.JTX.Utilities.Constants.NOTIF_ATTACHMENT_ADDED,
                    this.WmxDatabase,
                    job);

                // Set the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_OUT_JOB_ID);
                IGPLong outValue = new GPLongClass();
                outValue.Value = m_jobId;
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
