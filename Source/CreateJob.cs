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

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;
using ESRI.ArcGIS.JTX.Utilities;
using ESRI.ArcGIS.JTXUI;


namespace WorkflowManagerAdministrationUtilities
{
    /// <summary>
    /// GP tool to create a job based on a set of parameters.  Please see
    /// "remarks" for important implementation notes.
    /// </summary>
    /// <remarks>
    /// TODO: Update the job-creation logic.<br/>
    /// <br/>
    /// In subsequent builds of Workflow Manager (post-10.0), there may be
    /// a new API that creates jobs and handles much of the logic included
    /// in this class (ex: checking permissions, sending notifications,
    /// etc.).  If so, this GP tool should be revised to make use of this
    /// simplified interface.<br/>
    /// <br/>
    /// Anyone using this tool as a reference, particularly with regards
    /// to creating Workflow Manager jobs, should keep this in mind.
    /// </remarks>
    class CreateJob : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JOB_TYPE = "in_string_jobType";
        private const string C_PARAM_JOB_OWNER = "in_string_owner";
        private const string C_PARAM_ASSIGNEE_TYPE = "in_string_assigneeType";
        private const string C_PARAM_ASSIGNEE = "in_string_assignee";
        private const string C_PARAM_AOI = "in_layer_aoi";
        private const string C_PARAM_START_DATE = "in_date_startDate";
        private const string C_PARAM_DUE_DATE = "in_date_dueDate";
        private const string C_PARAM_PRIORITY = "in_string_priority";
        private const string C_PARAM_PARENTJOBID = "in_long_parentJobId";
        private const string C_PARAM_DATAWORKSPACE = "in_string_dataWorkspace";
        private const string C_PARAM_PARENTVERSION = "in_string_parentVersion";
        private const string C_PARAM_EXECUTE_NEW_JOB = "in_bool_executeNewJob";
        private const string C_PARAM_NEWJOBID = "out_long_jobId";

        private const string C_OPT_ASSIGN_TO_GROUP = "ASSIGN_TO_GROUP";
        private const string C_OPT_ASSIGN_TO_USER = "ASSIGN_TO_USER";
        private const string C_OPT_UNASSIGNED = "UNASSIGNED";

        private const string C_OPT_VAL_UNASSIGNED = "[Unassigned]";
        private const string C_OPT_VAL_NOT_SET = "[Not set]";

        private const string C_OPT_VAL_EXECUTE = "EXECUTE";
        private const string C_OPT_VAL_NO_EXECUTE = "NO_EXECUTE";

        private const bool C_DEFAULT_EXECUTE_NEW_JOB = false;
        #endregion

        #region MemberVariables
        private string m_jobTypeAsString = string.Empty;
        private string m_jobOwner = string.Empty;
        private string m_assigneeType = string.Empty;
        private string m_assignee = string.Empty;
        private ILayer m_aoiLayer = null;
        private IGPDate m_startDate = null;
        private IGPDate m_dueDate = null;
        private string m_priority = string.Empty;
        private int m_parentJobId = -1;
        private string m_dataWorkspaceId = string.Empty;
        private string m_parentVersion = string.Empty;
        private bool m_executeNewJob = C_DEFAULT_EXECUTE_NEW_JOB;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "CreateJob"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_CREATE_JOB; } }
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
            param = paramMap.GetParam(C_PARAM_JOB_TYPE);
            m_jobTypeAsString = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_JOB_OWNER);
            m_jobOwner = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_ASSIGNEE_TYPE);
            if (param.Value == null)
            {
                m_assigneeType = string.Empty;
            }
            else
            {
                m_assigneeType = param.Value.GetAsText();
            }

            param = paramMap.GetParam(C_PARAM_ASSIGNEE);
            if (param.Value == null)
            {
                m_assignee = string.Empty;
            }
            else
            {
                m_assignee = param.Value.GetAsText();
            }

            param = paramMap.GetParam(C_PARAM_AOI);
            m_aoiLayer = m_gpUtilities.DecodeLayer(param.Value);

            param = paramMap.GetParam(C_PARAM_START_DATE);
            m_startDate = param.Value as IGPDate;

            param = paramMap.GetParam(C_PARAM_DUE_DATE);
            m_dueDate = param.Value as IGPDate;

            param = paramMap.GetParam(C_PARAM_PRIORITY);
            m_priority = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_PARENTJOBID);
            if (param.Value == null || param.Value.GetAsText().Equals(string.Empty))
            {
                m_parentJobId = -1;
            }
            else
            {
                m_parentJobId = int.Parse(param.Value.GetAsText());
            }

            param = paramMap.GetParam(C_PARAM_DATAWORKSPACE);
            if (param.Value == null)
            {
                m_dataWorkspaceId = string.Empty;
            }
            else
            {
                string temp = param.Value.GetAsText();
                m_dataWorkspaceId = Common.WmauHelperFunctions.LookupWorkspaceId(this.WmxDatabase, temp);
            }

            param = paramMap.GetParam(C_PARAM_PARENTVERSION);
            if (param.Value == null)
            {
                m_parentVersion = string.Empty;
            }
            else
            {
                m_parentVersion = param.Value.GetAsText();
            }

            param = paramMap.GetParam(C_PARAM_EXECUTE_NEW_JOB);
            m_executeNewJob = (param.Value as IGPBoolean).Value;
        }

        /// <summary>
        /// Helper function to get a polygon object from the selected feature in the
        /// specified layer.
        /// </summary>
        /// <param name="layerName">The polygon layer that contains exactly one selected feature.</param>
        /// <returns>The polygon object, or null if none could be retrieved.</returns>
        private IPolygon4 GetPolygonFromSpecifiedLayer(ILayer layerObj)
        {
            // Set the AOI of the job, if there is one
            // Ensure that there's nothing wrong with the AOI feature that is selected, if any
            IPolygon4 aoiPolygon = null;
            if (layerObj != null)
            {
                ICursor cursor = null;
                IFeatureLayer featLayer = layerObj as IFeatureLayer;
                IFeatureSelection featSel = layerObj as IFeatureSelection;
                ISelectionSet selSet = featSel.SelectionSet as ISelectionSet;

                if (featLayer.FeatureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                {
                    throw new WmauException(new WmauError(WmauErrorCodes.C_AOI_NOT_POLYGON_ERROR));
                }
                else if (selSet.Count != 1)
                {
                    throw new WmauException(new WmauError(WmauErrorCodes.C_EXPECTED_ONE_SELECTED_FEATURE_ERROR));
                }

                // If we get this far, we know that there's exactly one selected feature, so we
                // don't have to loop through the selection set
                selSet.Search(null, true, out cursor);
                IFeatureCursor featureCursor = cursor as IFeatureCursor;
                IFeature aoiCandidate = featureCursor.NextFeature();

                // We also know that the feature is a polygon, so just make the cast
                aoiPolygon = aoiCandidate.Shape as IPolygon4;
            }

            return aoiPolygon;
        }

        /// <summary>
        /// Helper function used to check if the new data workspace selected by a user differs
        /// from the old data workspace
        /// </summary>
        /// <param name="param">The parameter containing the new data workspace value (DB ID)</param>
        /// <returns>true if the workspace does not match the existing value; false otherwise</returns>
        private bool IsDataWorkspaceParameterChanged(IGPParameter3 param)
        {
            bool retVal = false;

            if (param.Value == null && !m_dataWorkspaceId.Equals(string.Empty))
            {
                retVal = true;
            }
            else if (param.Value != null)
            {
                string temp = param.Value.GetAsText();
                string newDbId = Common.WmauHelperFunctions.LookupWorkspaceId(this.WmxDatabase, temp);
                if (!m_dataWorkspaceId.Equals(newDbId))
                {
                    retVal = true;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Helper function to enable/disable a particular parameter based on the possible values
        /// in its domain.
        /// </summary>
        /// <param name="paramObj">
        /// The parameter to enabled/disable.  The domain for this parameter must be a
        /// IGPCodedValueDomain.
        /// </param>
        private void EnableDisableParamBasedOnDomain(object paramObj)
        {
            IGPParameter3 param = paramObj as IGPParameter3;
            IGPParameterEdit3 paramEdit = paramObj as IGPParameterEdit3;
            IGPCodedValueDomain domain = param.Domain as IGPCodedValueDomain;
            paramEdit.Enabled = (domain != null && domain.CodeCount > 0);
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

                // Parameter indicating the type of job
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_CJ_JOB_TYPE,
                    C_PARAM_JOB_TYPE,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the user designated as the owner of the job
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_CJ_JOB_OWNER,
                    C_PARAM_JOB_OWNER,
                    new GPStringTypeClass(),
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
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_ASSIGNEE_TYPE,
                    C_PARAM_ASSIGNEE_TYPE,
                    new GPStringTypeClass(),
                    null);
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter indicating the name of the assignee
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_ASSIGNEE,
                    C_PARAM_ASSIGNEE,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the AOI for the job
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_AOI,
                    C_PARAM_AOI,
                    new GPFeatureLayerTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Parameter indicating the start date for the job
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_START_DATE,
                    C_PARAM_START_DATE,
                    new GPDateTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Parameter indicating the due date for the job
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_DUE_DATE,
                    C_PARAM_DUE_DATE,
                    new GPDateTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Parameter indicating the priority of the job
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_PRIORITY,
                    C_PARAM_PRIORITY,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the parent job ID (if any)
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_PARENT_JOB_ID,
                    C_PARAM_PARENTJOBID,
                    new GPLongTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the data workspace for the job
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_DATA_WORKSPACE,
                    C_PARAM_DATAWORKSPACE,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the parent database version for this job
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_PARENT_VERSION,
                    C_PARAM_PARENTVERSION,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating whether or not the job should be executed
                // after it has been created
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_VAL_EXECUTE);
                cvDomain.AddCode(GpFalse, C_OPT_VAL_NO_EXECUTE);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_CJ_EXECUTE_NEW_JOB,
                    C_PARAM_EXECUTE_NEW_JOB,
                    this.GpBooleanType,
                    this.ToGpBoolean(C_DEFAULT_EXECUTE_NEW_JOB));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter showing the ID of the job that was created by this step
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_CJ_OUTPUT_JOB_ID,
                    C_PARAM_NEWJOBID,
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
            // Call the base class function first
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
            IGPParameter3 assigneeType = paramMap.GetParam(C_PARAM_ASSIGNEE_TYPE);
            IGPParameter3 assignee = paramMap.GetParam(C_PARAM_ASSIGNEE);
            IGPParameter3 aoi = paramMap.GetParam(C_PARAM_AOI);
            IGPParameter3 startDate = paramMap.GetParam(C_PARAM_START_DATE);
            IGPParameter3 dueDate = paramMap.GetParam(C_PARAM_DUE_DATE);
            IGPParameter3 dataWorkspace = paramMap.GetParam(C_PARAM_DATAWORKSPACE);
            IGPParameter3 parentVersion = paramMap.GetParam(C_PARAM_PARENTVERSION);

            // Ensure that the current user has permissions to be creating jobs
            if (!CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_CREATE_JOB))
            {
                WmauError error = new WmauError(WmauErrorCodes.C_NO_CREATE_JOB_PRIV_ERROR);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_JOB_TYPE), error.ErrorCodeAsInt, error.Message);
            }

            // Check assignments; if a name is specified but a type is not, print a warning
            if (String.IsNullOrEmpty(assigneeType.Value.GetAsText()) &&
                !String.IsNullOrEmpty(assignee.Value.GetAsText()))
            {
                WmauError error = new WmauError(WmauErrorCodes.C_JOB_ASSIGNMENT_IGNORED_ERROR);
                msgs.ReplaceWarning(paramMap.GetIndex(C_PARAM_ASSIGNEE), error.Message);
            }

            // Check the AOI; ensure that there is exactly one feature selected
            if (aoi.Value != null && !aoi.Value.GetAsText().Equals(string.Empty))
            {
                try
                {
                    ILayer aoiLayer = m_gpUtilities.DecodeLayer(aoi.Value);
                    IFeatureLayer featLayer = aoiLayer as IFeatureLayer;
                    IFeatureSelection featSel = aoiLayer as IFeatureSelection;
                    ISelectionSet selSet = featSel.SelectionSet as ISelectionSet;

                    if (featLayer.FeatureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                    {
                        WmauError error = new WmauError(WmauErrorCodes.C_AOI_NOT_POLYGON_ERROR);
                        msgs.ReplaceWarning(paramMap.GetIndex(C_PARAM_AOI), error.Message);
                    }
                    else if (selSet.Count != 1)
                    {
                        WmauError error = new WmauError(WmauErrorCodes.C_EXPECTED_ONE_SELECTED_FEATURE_ERROR);
                        msgs.ReplaceWarning(paramMap.GetIndex(C_PARAM_AOI), error.Message);
                    }
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    WmauError error = new WmauError(WmauErrorCodes.C_AOI_INPUT_ERROR);
                    msgs.ReplaceError(paramMap.GetIndex(C_PARAM_AOI), error.ErrorCodeAsInt, error.Message + "; " + comEx.Message);
                }
            }

            // Check start date and due date; if they're both defined, make sure that start
            // date is <= due date
            if (startDate.Value != null && !startDate.Value.GetAsText().Equals(string.Empty) &&
                dueDate.Value != null && !dueDate.Value.GetAsText().Equals(string.Empty) &&
                DateTime.Parse(dueDate.Value.GetAsText()) < DateTime.Parse(startDate.Value.GetAsText()))
            {
                WmauError error = new WmauError(WmauErrorCodes.C_DUE_DATE_LT_START_DATE_ERROR);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_DUE_DATE), error.ErrorCodeAsInt, error.Message);
            }

            // If a workspace has been chosen but the version field is disabled and the user has
            // permissions to manage versions, something went wrong
            if (dataWorkspace.Value != null &&
                !dataWorkspace.Value.GetAsText().Equals(string.Empty) &&
                !dataWorkspace.Value.GetAsText().Equals(C_OPT_VAL_NOT_SET) &&
                !parentVersion.Enabled &&
                CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_MANAGE_VERSION))
            {
                WmauError error = new WmauError(WmauErrorCodes.C_WORKSPACE_ERROR);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_DATAWORKSPACE), error.ErrorCodeAsInt, error.Message);
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
            IGPParameter3 assigneeType = paramMap.GetParam(C_PARAM_ASSIGNEE_TYPE);
            IGPParameterEdit3 assigneeTypeEdit = paramMap.GetParamEdit(C_PARAM_ASSIGNEE_TYPE);
            IGPParameter3 assignee = paramMap.GetParam(C_PARAM_ASSIGNEE);
            IGPParameterEdit assigneeEdit = paramMap.GetParamEdit(C_PARAM_ASSIGNEE);

            // If the assignee type has changed, update the domain for the assignee
            // parameter
            //
            // NOTE: Updating the assignee value can cause things to get confused,
            // particularly when background geoprocessing is enabled.  So rather than
            // do this, let the assignee parameter value conflict with the domain
            // for that parameter, if need be.
            if (!assigneeType.Value.GetAsText().Equals(m_assigneeType))
            {
                m_assigneeType = assigneeType.Value.GetAsText();

                if (m_assigneeType.Equals(C_OPT_ASSIGN_TO_GROUP))
                {
                    assigneeEdit.Domain = Common.WmauGpDomainBuilder.BuildAssignableGroupsDomain(this.WmxDatabase);
                }
                else if (m_assigneeType.Equals(C_OPT_ASSIGN_TO_USER))
                {
                    assigneeEdit.Domain = Common.WmauGpDomainBuilder.BuildAssignableUsersDomain(this.WmxDatabase);
                }
                else if (m_assigneeType.Equals(C_OPT_UNASSIGNED) ||
                    String.IsNullOrEmpty(m_assigneeType))
                {
                    assigneeEdit.Domain = null;
                }
            }

            // Check to see if the data workspace parameter has changed; if so, store the
            // new workspace value and mark the domain for the workspace version to be reset.
            IGPParameter3 workspaceParam = paramMap.GetParam(C_PARAM_DATAWORKSPACE);
            if (this.IsDataWorkspaceParameterChanged(workspaceParam))
            {
                if (workspaceParam.Value == null)
                {
                    m_dataWorkspaceId = string.Empty;
                }
                else
                {
                    string temp = workspaceParam.Value.GetAsText();
                    m_dataWorkspaceId = Common.WmauHelperFunctions.LookupWorkspaceId(this.WmxDatabase, temp);
                }

                IGPParameterEdit3 versionParam = paramMap.GetParamEdit(C_PARAM_PARENTVERSION);
                
                // Keep the version parameter's value unchanged; changing it "smartly" to help
                // out a user can lead to trouble.  Instead, let any problem value be flagged
                // as something that's not a member of the domain.
                //versionParam.Value = null;
                versionParam.Domain = null;
                m_parentVersion = string.Empty;
            }

            // Set the domains for any parameters that need them
            if (paramMap.GetParam(C_PARAM_JOB_TYPE).Domain == null)
            {
                paramMap.GetParamEdit(C_PARAM_JOB_TYPE).Domain = Common.WmauGpDomainBuilder.BuildJobTypeDomain(this.WmxDatabase);
            }
            if (paramMap.GetParam(C_PARAM_JOB_OWNER).Domain == null)
            {
                paramMap.GetParamEdit(C_PARAM_JOB_OWNER).Domain = Common.WmauGpDomainBuilder.BuildUsersDomain(this.WmxDatabase);
            }
            if (paramMap.GetParam(C_PARAM_PRIORITY).Domain == null)
            {
                paramMap.GetParamEdit(C_PARAM_PRIORITY).Domain = Common.WmauGpDomainBuilder.BuildPriorityDomain(this.WmxDatabase);
            }
            if (paramMap.GetParam(C_PARAM_PARENTJOBID).Domain == null)
            {
                paramMap.GetParamEdit(C_PARAM_PARENTJOBID).Domain = Common.WmauGpDomainBuilder.BuildJobIdDomain(this.WmxDatabase);
            }
            if (paramMap.GetParam(C_PARAM_DATAWORKSPACE).Domain == null)
            {
                if (CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_MANAGE_DATA_WORKSPACE))
                {
                    paramMap.GetParamEdit(C_PARAM_DATAWORKSPACE).Domain =
                        Common.WmauGpDomainBuilder.BuildWorkspaceDomain(this.WmxDatabase, new string[] { C_OPT_VAL_NOT_SET });
                }
                else
                {
                    paramMap.GetParamEdit(C_PARAM_DATAWORKSPACE).Domain = new GPCodedValueDomainClass() as IGPDomain;
                }
            }
            if (paramMap.GetParam(C_PARAM_PARENTVERSION).Domain == null)
            {
                if (!m_dataWorkspaceId.Equals(string.Empty) &&
                    !m_dataWorkspaceId.Equals(C_OPT_VAL_NOT_SET) &&
                    CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_MANAGE_VERSION))
                {
                    try
                    {
                        string dbName = this.WmxDatabase.GetDataWorkspaceName(m_dataWorkspaceId).Name;
                        paramMap.GetParamEdit(C_PARAM_PARENTVERSION).Domain =
                            Common.WmauGpDomainBuilder.BuildVersionsDomain(
                            this.WmxDatabase, dbName, new string[] { C_OPT_VAL_NOT_SET });
                    }
                    catch (WmauException)
                    {
                        // If we run into an exception, set the domain to be empty so that
                        // we can detect it and flag an error during parameter validation
                        paramMap.GetParamEdit(C_PARAM_PARENTVERSION).Domain = new GPCodedValueDomainClass();
                    }
                }
            }

            // After the domains have been set, check to see if any parameters should be
            // enabled/disabled based on the contents of these domains.
            assigneeEdit.Enabled = !m_assigneeType.Equals(C_OPT_UNASSIGNED);
            EnableDisableParamBasedOnDomain(paramMap.GetParam(C_PARAM_DATAWORKSPACE));
            EnableDisableParamBasedOnDomain(paramMap.GetParam(C_PARAM_PARENTVERSION));

            // Also check the AOI; if the user can't manage a job's AOI, then disable the
            // parameter
            paramMap.GetParamEdit(C_PARAM_AOI).Enabled = CurrentUserHasPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_MANAGE_AOI);
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
            IJTXJob4 job = null;
            
            // Do some common error-checking
            base.Execute(paramValues, trackCancel, envMgr, msgs);

            //////////////////////////////////////////////////////////////////////
            // TODO: Update the job-creation logic.
            //
            // In subsequent builds of Workflow Manager (post-10.0), there may be
            // a new API that creates jobs and handles much of the logic included
            // in this function (and this class at large).  If so, this GP tool
            // should be revised to make use of this simplified interface.
            //
            // Anyone using this tool as a reference, particularly with regards
            // to creating Workflow Manager jobs, should keep this in mind.
            //////////////////////////////////////////////////////////////////////

            // Try to create the job, as requested
            try
            {
                IJTXJobManager2 jobManager = this.WmxDatabase.JobManager as IJTXJobManager2;
                IJTXConfiguration configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration;
                IJTXJobType4 jobTypeObj = configMgr.GetJobType(m_jobTypeAsString) as IJTXJobType4;

                // Set up the description object to be used to create this job
                IJTXJobDescription jobDescription = new JTXJobDescriptionClass();
                jobDescription.JobTypeName = m_jobTypeAsString;
                jobDescription.AOI = GetPolygonFromSpecifiedLayer(m_aoiLayer);

                // Set up the ownership & assignment of the job
                jobDescription.OwnedBy = m_jobOwner;
                if (m_assigneeType.Equals(C_OPT_ASSIGN_TO_GROUP))
                {
                    jobDescription.AssignedType = jtxAssignmentType.jtxAssignmentTypeGroup;
                    jobDescription.AssignedTo = m_assignee;
                }
                else if (m_assigneeType.Equals(C_OPT_ASSIGN_TO_USER))
                {
                    jobDescription.AssignedType = jtxAssignmentType.jtxAssignmentTypeUser;
                    jobDescription.AssignedTo = m_assignee;
                }
                else if (m_assigneeType.Equals(C_OPT_UNASSIGNED))
                {
                    jobDescription.AssignedType = jtxAssignmentType.jtxAssignmentTypeUnassigned;
                    jobDescription.AssignedTo = string.Empty;
                }
                else
                {
                    // Do nothing; let the job type defaults take over
                    msgs.AddMessage("Using job type defaults for job assignment");
                    jobDescription.AssignedType = jobTypeObj.DefaultAssignedType;
                    jobDescription.AssignedTo = jobTypeObj.DefaultAssignedTo;
                }

                // Start date
                if (m_startDate != null && m_startDate.Value != null)
                {
                    string tempStr = m_startDate.Value.ToString();

                    // Workflow Manager stores times as UTC times; input times must
                    // therefore be pre-converted
                    DateTime tempDate = DateTime.Parse(tempStr);
                    jobDescription.StartDate = TimeZone.CurrentTimeZone.ToUniversalTime(tempDate);
                }
                else
                {
                    msgs.AddMessage("Using job type defaults for start date");
                    jobDescription.StartDate = jobTypeObj.DefaultStartDate;
                }

                // Due date
                if (m_dueDate != null && m_dueDate.Value != null)
                {
                    string tempStr = m_dueDate.Value.ToString();

                    // Workflow Manager stores times as UTC times; input times must
                    // therefore be pre-converted
                    DateTime tempDate = DateTime.Parse(tempStr);
                    jobDescription.DueDate = TimeZone.CurrentTimeZone.ToUniversalTime(tempDate);
                }
                else
                {
                    msgs.AddMessage("Using job type defaults for due date");
                    jobDescription.DueDate = jobTypeObj.DefaultDueDate;
                }

                // Priority
                if (!m_priority.Equals(string.Empty))
                {
                    IJTXPriority priority = configMgr.GetPriority(m_priority);
                    jobDescription.Priority = priority;
                }
                else
                {
                    msgs.AddMessage("Using job type defaults for priority");
                    jobDescription.Priority = jobTypeObj.DefaultPriority;
                }

                // Parent job
                if (m_parentJobId > 0)
                {
                    jobDescription.ParentJobId = m_parentJobId;
                }

                // Data workspace
                if (m_dataWorkspaceId.Equals(C_OPT_VAL_NOT_SET))
                {
                    jobDescription.DataWorkspaceID = string.Empty;
                }
                else if (!m_dataWorkspaceId.Equals(string.Empty))
                {
                    jobDescription.DataWorkspaceID = m_dataWorkspaceId;
                }
                else
                {
                    msgs.AddMessage("Using job type defaults for data workspace");
                    if (jobTypeObj.DefaultDataWorkspace != null)
                    {
                        jobDescription.DataWorkspaceID = jobTypeObj.DefaultDataWorkspace.DatabaseID;
                    }
                }

                // Parent version
                if (m_parentVersion.Equals(C_OPT_VAL_NOT_SET))
                {
                    jobDescription.ParentVersionName = string.Empty;
                }
                else if (!m_parentVersion.Equals(string.Empty))
                {
                    jobDescription.ParentVersionName = m_parentVersion;
                }
                else
                {
                    msgs.AddMessage("Using job type defaults for parent version");
                    jobDescription.ParentVersionName = jobTypeObj.DefaultParentVersionName;
                }

                // Auto-execution
                jobDescription.AutoExecuteOnCreate = m_executeNewJob;

                // Create the new job
                int expectedNumJobs = 1;
                bool checkAoi = true;
                IJTXJobSet jobSet = null;
                IJTXExecuteInfo execInfo;
                try
                {
                    jobSet = jobManager.CreateJobsFromDescription(jobDescription, expectedNumJobs, checkAoi, out execInfo);
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    throw new WmauException(WmauErrorCodes.C_CREATE_JOB_ERROR, comEx);
                }

                if ((execInfo != null && execInfo.ThrewError) ||
                    jobSet == null ||
                    jobSet.Count != expectedNumJobs)
                {
                    if (execInfo != null && !string.IsNullOrEmpty(execInfo.ErrorDescription))
                    {
                        throw new WmauException(
                            WmauErrorCodes.C_CREATE_JOB_ERROR,
                            new Exception(execInfo.ErrorCode.ToString() + ": " + execInfo.ErrorDescription));
                    }
                    else
                    {
                        throw new WmauException(WmauErrorCodes.C_CREATE_JOB_ERROR);
                    }
                }
                
                // If it gets all the way down here without errors, set the output ID with the
                // ID of the job that was created.
                job = jobSet.Next() as IJTXJob4;
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPValue jobIdGpVal = new GPLongClass();
                jobIdGpVal.SetAsText(job.ID.ToString());
                IGPParameterEdit3 jobIdParam = paramMap.GetParamEdit(C_PARAM_NEWJOBID);
                jobIdParam.Value = jobIdGpVal;
                msgs.AddMessage("Created job: " + job.ID.ToString() + " (" + job.Name + ")");

                msgs.AddMessage(Properties.Resources.MSG_DONE);
            }
            catch (WmauException wmEx)
            {
                try
                {
                    msgs.AddError(wmEx.ErrorCodeAsInt, wmEx.Message);
                    if (job != null)
                    {
                        this.WmxDatabase.JobManager.DeleteJob(job.ID, true);
                    }
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
                    WmauError error = new WmauError(WmauErrorCodes.C_CREATE_JOB_ERROR);
                    msgs.AddError(error.ErrorCodeAsInt, error.Message + "; " + ex.Message);
                    if (job != null)
                    {
                        this.WmxDatabase.JobManager.DeleteJob(job.ID, true);
                    }
                }
                catch
                {
                    // Catch anything else that possibly happens
                }
            }
        }
    }
}
