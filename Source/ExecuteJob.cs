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
    class ExecuteJob : WmauAbstractGpFunction, IJTXWorkflowExecutionCaller
    {
        #region Constants
        private const string C_PARAM_JOB_ID = "in_long_jobId";
        private const string C_PARAM_OUT_JOB_ID = "out_long_jobId";
        #endregion

        #region MemberVariables
        private int m_jobId = -1;

        private List<string> m_queuedMessages = new List<string>();
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ExecuteJob"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_EXECUTE_JOB; } }
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
        }
        #endregion

        #region Required by IJTXWorkflowExecutionCaller
        public int AskNextSteps(IJTXJob pJob, int currStepID, bool bUseDefaultValuesOnly)
        {
            lock (m_queuedMessages)
            {
                m_queuedMessages.Add("AskNextSteps: Returning -1 for next step");
            }
            
            IJTXWorkflowExecution3 jobExec = pJob as IJTXWorkflowExecution3;
            int[] nextSteps = jobExec.GetCurrentSteps();
            if (nextSteps.Length != 1)
            {
                return -1;
            }
            else
            {
                return nextSteps[0];
            }
        }

        public void JobClosed(int jobId)
        {
            lock (m_queuedMessages)
            {
                m_queuedMessages.Add("Closing job: " + jobId.ToString());
            }
        }

        public void JobInvalidated(int jobId)
        {
            lock (m_queuedMessages)
            {
                m_queuedMessages.Add("Job invalidated: " + jobId.ToString());
            }
        }

        public void JobUpdated(int jobId)
        {
            lock (m_queuedMessages)
            {
                m_queuedMessages.Add("Job updated: " + jobId.ToString());
            }
        }

        public void JobWorkflowCompleted(IJTXJob job)
        {
            lock (m_queuedMessages)
            {
                m_queuedMessages.Add("Workflow completed for job: " + job.ID.ToString());
            }
        }

        public void JobWorkflowUpdated(IJTXJob job)
        {
            lock (m_queuedMessages)
            {
                m_queuedMessages.Add("Workflow updated for job: " + job.ID.ToString());
            }
        }

        public void StepExecutionComplete(IJTXJob job, int stepId, IJTXExecuteInfo execInfo)
        {
            lock (m_queuedMessages)
            {
                m_queuedMessages.Add("Finished running step '" + stepId.ToString() + "' for job: " + job.ID.ToString());
            }
        }

        public void StepExecutionStarting(IJTXJob job, int stepId)
        {
            lock (m_queuedMessages)
            {
                m_queuedMessages.Add("Started running step '" + stepId.ToString() + "' for job: " + job.ID.ToString());
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

                // Parameter indicating the job to be executed
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_EJ_JOB_ID,
                    C_PARAM_JOB_ID,
                    new GPLongTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the job that was executed
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_EJ_OUT_JOB_ID,
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
                IJTXWorkflowExecution3 jobExec = jobManager.GetJob(m_jobId) as IJTXWorkflowExecution3;

                // Don't try to deal with the case of multiple active steps
                int[] currentStepIds = jobExec.GetCurrentSteps();
                if (currentStepIds.Length != 1)
                {
                    throw new WmauException(WmauErrorCodes.C_NO_OR_MULTIPLE_STEPS_ERROR);
                }

                jobExec.RunStepChecks(currentStepIds[0], true);
                IJTXExecuteInfo execInfo = jobExec.RunStep(currentStepIds[0], false, true, false, this);
                if (execInfo.ThrewError)
                {
                    throw new WmauException(
                        WmauErrorCodes.C_JOB_EXECUTION_ERROR,
                        new Exception(execInfo.ErrorCode.ToString() + ": " + execInfo.ErrorDescription));
                }

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
