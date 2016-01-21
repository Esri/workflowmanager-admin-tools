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
    class SendJobNotification : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JOB_ID = "in_long_jobId";
        private const string C_PARAM_NOTIFICATION_NAME = "in_string_notificationName";
        private const string C_PARAM_OUT_JOB_ID = "out_long_jobId";
        #endregion

        #region MemberVariables
        private int m_jobId = -1;
        private string m_notificationName = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "SendJobNotification"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_SEND_JOB_NOTIFICATION; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_NOTIFICATION_UTILS; } }
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

            param = paramMap.GetParam(C_PARAM_NOTIFICATION_NAME);
            m_notificationName = param.Value.GetAsText();
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

                // Parameter indicating the ID of the job for which the notification
                // should be sent
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_SJN_JOB_ID,
                    C_PARAM_JOB_ID,
                    new GPLongTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the name of the notification to be sent
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_SJN_NOTIFICATION_NAME,
                    C_PARAM_NOTIFICATION_NAME,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the job for which the notification was sent
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_SJN_OUT_JOB_ID,
                    C_PARAM_OUT_JOB_ID,
                    new GPLongTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

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

            // Get the parameters as a map for easier access
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);
            IGPParameter3 jobId = paramMap.GetParam(C_PARAM_JOB_ID);
            IGPParameterEdit3 jobIdEdit = paramMap.GetParamEdit(C_PARAM_JOB_ID);
            IGPParameter3 notifName = paramMap.GetParam(C_PARAM_NOTIFICATION_NAME);
            IGPParameterEdit3 notifNameEdit = paramMap.GetParamEdit(C_PARAM_NOTIFICATION_NAME);

            // Set the domains for any parameters that need them

            // Set the job ID domain if it hasn't already been populated
            if (jobId.Domain == null)
            {
                jobIdEdit.Domain = Common.WmauGpDomainBuilder.BuildJobIdDomain(this.WmxDatabase);
            }
            // Add a domain to the notification parameter
            if (notifName.Domain == null || (notifName.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                notifNameEdit.Domain = Common.WmauGpDomainBuilder.BuildEmailNotificationDomain(this.WmxDatabase);
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
                // Send the notification
                IJTXJobManager jobManager = this.WmxDatabase.JobManager;
                Common.WmauHelperFunctions.SendNotification(
                    m_notificationName, this.WmxDatabase, jobManager.GetJob(m_jobId));

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
