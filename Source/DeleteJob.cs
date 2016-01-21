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
    /// <summary>
    /// GP tool to delete a job.<br/>
    /// <br/>
    /// Please see "remarks" for important implementation notes.
    /// </summary>
    /// <remarks>
    /// TODO: Update the job-deletion logic.<br/>
    /// <br/>
    /// In subsequent builds of Workflow Manager (post-10.0), there may be
    /// a new API that deletes jobs and handles some of the logic included
    /// in this class (ex: checking permissions, deleting versions, etc.).
    /// If so, this GP tool should be revised to make use of this
    /// simplified interface.<br/>
    /// <br/>
    /// Anyone using this tool as a reference, particularly with regards
    /// to deleting Workflow Manager jobs, should keep this in mind.
    /// </remarks>
    class DeleteJob : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JOB_TO_DELETE = "in_long_jobToDelete";
        private const string C_PARAM_JOB_DELETED = "out_long_jobDeleted";
        #endregion

        #region MemberVariables
        private int m_jobToDelete = -1;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "DeleteJob"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_DELETE_JOB; } }
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
            param = paramMap.GetParam(C_PARAM_JOB_TO_DELETE);
            m_jobToDelete = int.Parse(param.Value.GetAsText());
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

                // Parameter indicating the job to be deleted
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_DJ_JOB_TO_DELETE,
                    C_PARAM_JOB_TO_DELETE,
                    new GPLongTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the job that was deleted
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_DJ_JOB_DELETED,
                    C_PARAM_JOB_DELETED,
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

            // Ensure that the current user has permissions to be creating jobs
            if (!CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_DELETE_JOBS))
            {
                WmauError error = new WmauError(WmauErrorCodes.C_NO_DELETE_JOB_PRIV_ERROR);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_JOB_TO_DELETE), error.ErrorCodeAsInt, error.Message);
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

            // Retrieve the parameter for the jobs to delete and add a domain to it
            //
            // NOTE: Add the domain in this function because the Workflow Manager
            // connection/extension/etc. is not necessarily available when ParameterInfo
            // is first accessed.
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);
            IGPParameter3 param = paramMap.GetParam(C_PARAM_JOB_TO_DELETE);
            IGPParameterEdit3 paramEdit = paramMap.GetParamEdit(C_PARAM_JOB_TO_DELETE);

            if (param.Domain == null)
            {
                // If there isn't a domain on this parameter yet, that means that it
                // still needs to be populated.  In that case, do so at this time.
                paramEdit.Domain = Common.WmauGpDomainBuilder.BuildJobIdDomain(this.WmxDatabase);
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

            //////////////////////////////////////////////////////////////////////
            // TODO: Update the job-deletion logic.
            //
            // In subsequent builds of Workflow Manager (post-10.0), there may be
            // a new API that deletes jobs and handles some of the logic included
            // in this class (ex: checking permissions, deleting versions, etc.).
            // If so, this GP tool should be revised to make use of this
            // simplified interface.
            //
            // Anyone using this tool as a reference, particularly with regards
            // to deleting Workflow Manager jobs, should keep this in mind.
            //////////////////////////////////////////////////////////////////////

            // Delete the requested job
            try
            {
                IJTXJobManager jobManager = this.WmxDatabase.JobManager;
                IJTXJob3 job = jobManager.GetJob(m_jobToDelete) as IJTXJob3;

                msgs.AddMessage("Deleting job " + m_jobToDelete + " (" + job.Name + ")");
                job.DeleteMXD();
                if (job.VersionExists())
                {
                    if (CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_DELETE_VERSION))
                    {
                        try
                        {
                            job.DeleteVersion(null);
                        }
                        catch (System.Runtime.InteropServices.COMException comEx)
                        {
                            if (comEx.ErrorCode == (int)fdoError.FDO_E_SE_VERSION_NOEXIST)
                            {
                                // The "VersionExists" method above can apparently be fooled; if it is, an exception with this
                                // error code is thrown.  In this case, add a warning and continue on.
                                msgs.AddWarning("Version '" + job.VersionName + "' is assigned to job " + job.ID.ToString() + " but could not be found");
                            }
                            else
                            {
                                // If there's a different error, then re-throw it for someone else to deal with
                                throw comEx;
                            }
                        }
                    }
                    else
                    {
                        string username = ESRI.ArcGIS.JTXUI.ConfigurationCache.GetCurrentSystemUser(ESRI.ArcGIS.JTXUI.ConfigurationCache.UseUserDomain);
                        msgs.AddWarning("User '" + username + "' does not have permissions to " +
                            "delete job versions; version '" + job.VersionName + "' will not be deleted");
                    }
                }
                jobManager.DeleteJob(m_jobToDelete, true);

                // Set the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_JOB_DELETED);
                IGPLong outValue = new GPLongClass();
                outValue.Value = m_jobToDelete;
                outParamEdit.Value = outValue as IGPValue;

                msgs.AddMessage(Properties.Resources.MSG_DONE);
            }
            catch (Exception ex)
            {
                WmauError error = new WmauError(WmauErrorCodes.C_DELETE_JOB_VERSION_ERROR);
                msgs.AddError(error.ErrorCodeAsInt, error.Message + "; " + ex.Message);
            }
        }
    }
}
