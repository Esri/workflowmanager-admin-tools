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
    class AssignJob : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JOB_ID = "in_long_jobId";
        private const string C_PARAM_ASSIGNEE_TYPE = "in_string_assigneeType";
        private const string C_PARAM_ASSIGNEE = "in_string_assignee";
        private const string C_PARAM_OUT_JOB_ID = "out_long_jobId";

        private const string C_OPT_ASSIGN_TO_GROUP = "ASSIGN_TO_GROUP";
        private const string C_OPT_ASSIGN_TO_USER = "ASSIGN_TO_USER";
        private const string C_OPT_UNASSIGNED = "UNASSIGNED";

        private const string C_OPT_VAL_UNASSIGNED = "[Unassigned]";

        private const string C_DEFAULT_ASSIGNEE_TYPE = C_OPT_UNASSIGNED;
        private const string C_DEFAULT_ASSIGNEE = C_OPT_VAL_UNASSIGNED;
        #endregion

        #region MemberVariables
        private int m_jobId = -1;
        private string m_assigneeType = C_DEFAULT_ASSIGNEE_TYPE;
        private string m_assignee = C_DEFAULT_ASSIGNEE;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "AssignJob"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_ASSIGN_JOB; } }
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

            param = paramMap.GetParam(C_PARAM_ASSIGNEE_TYPE);
            m_assigneeType = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_ASSIGNEE);
            m_assignee = param.Value.GetAsText();
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

                // Parameter indicating the job to be assigned
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_AJ_JOB_ID,
                    C_PARAM_JOB_ID,
                    new GPLongTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the type of assignee
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddStringCode(C_OPT_ASSIGN_TO_GROUP, C_OPT_ASSIGN_TO_GROUP);
                cvDomain.AddStringCode(C_OPT_ASSIGN_TO_USER, C_OPT_ASSIGN_TO_USER);
                cvDomain.AddStringCode(C_OPT_UNASSIGNED, C_OPT_UNASSIGNED);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_AJ_ASSIGNEE_TYPE,
                    C_PARAM_ASSIGNEE_TYPE,
                    cvDomain.FindValue(C_DEFAULT_ASSIGNEE_TYPE).DataType,
                    cvDomain.FindValue(C_DEFAULT_ASSIGNEE_TYPE));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter indicating the name of the assignee
                IGPString strParam = new GPStringClass();
                strParam.Value = C_DEFAULT_ASSIGNEE;

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_AJ_ASSIGNEE,
                    C_PARAM_ASSIGNEE,
                    (strParam as IGPValue).DataType,
                    strParam as IGPValue,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the job that was assigned
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_AJ_OUT_JOB_ID,
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

            // Ensure that the current user has permissions to be assigning jobs
            if (!CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_ASSIGN_ANY_JOB) &&
                !CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_INDIVIDUAL_JOB_ASSIGN) &&
                !CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_GROUP_JOB_ASSIGN))
            {
                WmauError error = new WmauError(WmauErrorCodes.C_NO_ASSIGN_JOB_PRIV_ERROR);
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
            IGPParameter3 assigneeType = paramMap.GetParam(C_PARAM_ASSIGNEE_TYPE);
            IGPParameterEdit3 assigneeTypeEdit = paramMap.GetParamEdit(C_PARAM_ASSIGNEE_TYPE);
            IGPParameter3 assignee = paramMap.GetParam(C_PARAM_ASSIGNEE);
            IGPParameterEdit assigneeEdit = paramMap.GetParamEdit(C_PARAM_ASSIGNEE);

            // Set the domains for any parameters that need them

            // Set the job ID domain if it hasn't already been populated
            if (jobId.Domain == null)
            {
                jobIdEdit.Domain = Common.WmauGpDomainBuilder.BuildNonClosedJobIdDomain(this.WmxDatabase);
            }

            // If the assignee type has changed, update the domain for the assignee
            // parameter
            //
            // NOTE: Updating the assignee value can cause things to get confused,
            // particularly when background geoprocessing is enabled.  So rather than
            // do this, let the assignee parameter value conflict with the domain
            // for that parameter, if need be.
            if (!assigneeType.Value.GetAsText().Equals(m_assigneeType) || assignee.Domain == null)
            {
                m_assigneeType = assigneeType.Value.GetAsText();
                //assignee.Value.Empty();

                if (m_assigneeType.Equals(C_OPT_ASSIGN_TO_GROUP))
                {
                    assigneeEdit.Domain = Common.WmauGpDomainBuilder.BuildAssignableGroupsDomain(this.WmxDatabase);
                }
                else if (m_assigneeType.Equals(C_OPT_ASSIGN_TO_USER))
                {
                    assigneeEdit.Domain = Common.WmauGpDomainBuilder.BuildAssignableUsersDomain(this.WmxDatabase);
                }
                else if (m_assigneeType.Equals(C_OPT_UNASSIGNED))
                {
                    assigneeEdit.Domain = null;
                    //assignee.Value.SetAsText(C_OPT_VAL_UNASSIGNED);
                }
            }

            // After the domains have been set, check to see if any parameters should be
            // enabled/disabled based on the contents of these domains.
            assigneeEdit.Enabled = !m_assigneeType.Equals(C_OPT_UNASSIGNED);
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

                jtxAssignmentType assigneeType;
                string descriptionStr;
                string assigneeStr;
                if (m_assigneeType.Equals(C_OPT_ASSIGN_TO_GROUP))
                {
                    assigneeType = jtxAssignmentType.jtxAssignmentTypeGroup;
                    assigneeStr = m_assignee;
                    descriptionStr = "group '" + assigneeStr + "'";
                }
                else if (m_assigneeType.Equals(C_OPT_ASSIGN_TO_USER))
                {
                    assigneeType = jtxAssignmentType.jtxAssignmentTypeUser;
                    assigneeStr = m_assignee;
                    descriptionStr = "user '" + assigneeStr + "'";
                }
                else
                {
                    assigneeType = jtxAssignmentType.jtxAssignmentTypeUnassigned;
                    assigneeStr = string.Empty;
                    descriptionStr = "no one (unassigned)";
                }

                msgs.AddMessage("Assigning job " + m_jobId + " (" + job.Name + ") to " + descriptionStr);
                job.AssignedType = assigneeType;
                job.AssignedTo = assigneeStr;
                job.Store();

                // Do the other things that still need to be handled manually, such as logging
                // the job's reassignment and sending any necessary notifications.
                job.LogJobAction(
                    configMgr.GetActivityType(ESRI.ArcGIS.JTX.Utilities.Constants.ACTTYPE_ASSIGN_JOB),
                    null,
                    string.Empty);
                Common.WmauHelperFunctions.SendNotification(
                    ESRI.ArcGIS.JTX.Utilities.Constants.NOTIF_JOB_ASSIGNED,
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
