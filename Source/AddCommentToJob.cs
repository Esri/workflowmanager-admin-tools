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

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;


namespace WorkflowManagerAdministrationUtilities
{
    class AddCommentToJob : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JOB_ID = "in_long_jobId";
        private const string C_PARAM_COMMENT = "in_string_comment";
        private const string C_PARAM_OUT_JOB_ID = "out_long_jobId";
        #endregion

        #region MemberVariables
        private int m_jobId = -1;
        private string m_comment = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "AddCommentToJob"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_ADD_COMMENT_TO_JOB; } }
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

            param = paramMap.GetParam(C_PARAM_COMMENT);
            m_comment = param.Value.GetAsText();
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

                // Parameter indicating the job to which the comment will be added
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_ACTJ_JOB_ID,
                    C_PARAM_JOB_ID,
                    new GPLongTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the comment to be added to the job
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_ACTJ_COMMENT,
                    C_PARAM_COMMENT,
                    new GPStringTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the job that was assigned
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_ACTJ_OUT_JOB_ID,
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
                IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;
                IJTXJob3 job = jobManager.GetJob(m_jobId) as IJTXJob3;

                // As of Jan. 2011, the core Workflow Manager libraries do not
                // seem to check if the user has the privilege to add a comment
                // if a job as a hold on it.  So run the check here.
                IJTXJobHolds jobHolds = job as IJTXJobHolds;
                if (jobHolds.Holds != null &&
                    jobHolds.Holds.Count > 0 &&
                    !CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_CAN_ADD_COMMENTS_FOR_HELD_JOBS))
                {
                    throw new WmauException(WmauErrorCodes.C_NO_ADD_COMMENTS_HELD_JOBS_ERROR);
                }

                // If we get this far, then add the comment to the job.
                IJTXActivityType commentType = configMgr.GetActivityType(ESRI.ArcGIS.JTX.Utilities.Constants.ACTTYPE_COMMENT);
                job.LogJobAction(commentType, null, m_comment);
                job.Store();

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
