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
    class CloseJob : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JOB_TO_CLOSE = "in_long_jobToClose";
        private const string C_PARAM_JOB_CLOSED = "out_long_jobClosed";

        // These values match up with other predetermined values referenced by Workflow
        // Manager.  Do not modify.
        private const string C_PROP_VAL_CLOSED_STATUS = "Closed";
        #endregion

        #region MemberVariables
        private int m_jobToClose = -1;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "CloseJob"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_CLOSE_JOB; } }
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
            param = paramMap.GetParam(C_PARAM_JOB_TO_CLOSE);
            m_jobToClose = int.Parse(param.Value.GetAsText());
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

                // Parameter indicating the job to be closed
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_CLJ_JOB_TO_CLOSE,
                    C_PARAM_JOB_TO_CLOSE,
                    new GPLongTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the job that was closed
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_CLJ_JOB_CLOSED,
                    C_PARAM_JOB_CLOSED,
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

            // Ensure that the current user has permissions to be closing jobs
            if (!CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_CLOSE_JOB))
            {
                WmauError error = new WmauError(WmauErrorCodes.C_NO_CLOSE_JOB_PRIV_ERROR);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_JOB_TO_CLOSE), error.ErrorCodeAsInt, error.Message);
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

            // Retrieve the parameter for the jobs to close and add a domain to it
            //
            // NOTE: Add the domain in this function because the Workflow Manager
            // connection/extension/etc. is not necessarily available when ParameterInfo
            // is first accessed.
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);
            IGPParameter3 param = paramMap.GetParam(C_PARAM_JOB_TO_CLOSE);
            IGPParameterEdit3 paramEdit = paramMap.GetParamEdit(C_PARAM_JOB_TO_CLOSE);

            if (param.Domain == null)
            {
                // If there isn't a domain on this parameter yet, that means that it
                // still needs to be populated.  In that case, do so at this time.
                paramEdit.Domain = Common.WmauGpDomainBuilder.BuildNonClosedJobIdDomain(this.WmxDatabase);
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

            // Close the requested job
            try
            {
                IJTXJobManager jobManager = this.WmxDatabase.JobManager;
                IJTXJob3 job = jobManager.GetJob(m_jobToClose) as IJTXJob3;
                IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;

                if (job.Stage != jtxJobStage.jtxJobStageClosed && !job.CanClose())
                {
                    throw new WmauException(WmauErrorCodes.C_CANNOT_CLOSE_JOB_ERROR);
                }

                msgs.AddMessage("Closing job " + m_jobToClose + " (" + job.Name + ")");
                job.Close();

                // Once the job is closed, do the other things that still need to be handled
                // separately (status updates, notifications, ...)
                Common.WmauHelperFunctions.UpdateJobStatus(this.WmxDatabase, job);
                job.Store();

                job.LogJobAction(
                    configMgr.GetActivityType(ESRI.ArcGIS.JTX.Utilities.Constants.ACTTYPE_CLOSE_JOB),
                    null,
                    string.Empty);
                Common.WmauHelperFunctions.SendNotification(
                    ESRI.ArcGIS.JTX.Utilities.Constants.NOTIF_JOB_CLOSED,
                    this.WmxDatabase,
                    job);

                // Set the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_JOB_CLOSED);
                IGPLong outValue = new GPLongClass();
                outValue.Value = m_jobToClose;
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
