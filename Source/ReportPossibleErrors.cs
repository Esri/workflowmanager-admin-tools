using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;
using ESRI.ArcGIS.JTX.Utilities;


namespace WorkflowManagerAdministrationUtilities
{
    class ReportPossibleErrors : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_CHECKLIST = "in_mvString_checklist";
        private const string C_PARAM_OUT_LOG_FILE_PATH = "out_file_logFilePath";
        private const string C_PARAM_OUT_ISSUES_FOUND = "out_long_numIssuesFound";

        private const string C_OPT_DIFF_STEP_NAMES = "DIFFERING_STEP_NAMES";
        private const string C_OPT_GROUPS_WITHOUT_EMAILS = "GROUPS_WITHOUT_EMAILS";
        private const string C_OPT_GROUPS_WITHOUT_PRIVILEGES = "GROUPS_WITHOUT_PRIVILEGES";
        private const string C_OPT_GROUPS_WITHOUT_USERS = "GROUPS_WITHOUT_USERS";
        private const string C_OPT_INVALID_JOB_ASSIGN = "INVALID_JOB_ASSIGNMENTS";
        private const string C_OPT_INVALID_JOB_TYPE_ASSIGN = "INVALID_JOB_TYPE_ASSIGNMENTS";
        private const string C_OPT_INVALID_STEP_ASSIGN = "INVALID_STEP_ASSIGNMENTS";
        private const string C_OPT_IS_SELF_PARENT = "IS_SELF_PARENT";
        private const string C_OPT_JOBS_WITHOUT_TYPES = "JOBS_WITHOUT_TYPES";
        private const string C_OPT_JOB_TYPES_WITHOUT_WORKFLOWS = "JOB_TYPES_WITHOUT_WORKFLOWS";
        private const string C_OPT_MISSING_AOI_MXDS = "MISSING_AOI_MXDS";
        private const string C_OPT_MISSING_BASE_MXDS = "MISSING_BASE_MXDS";
        private const string C_OPT_NON_ACTIVE_JOB_TYPES = "NON_ACTIVE_JOB_TYPES";
        private const string C_OPT_UNASSIGNED_STEPS = "UNASSIGNED_STEPS";
        private const string C_OPT_USERS_WITHOUT_EMAILS = "USERS_WITHOUT_EMAILS";
        private const string C_OPT_USERS_WITHOUT_GROUPS = "USERS_WITHOUT_GROUPS";
        private const string C_OPT_ZERO_PCT_STEPS = "ZERO_PCT_STEPS";

        private const bool C_DEFAULT_DIFFERING_STEP_NAMES = false;
        private const bool C_DEFAULT_GROUPS_WITHOUT_EMAILS = false;
        private const bool C_DEFAULT_GROUPS_WITHOUT_PRIVILEGES = false;
        private const bool C_DEFAULT_GROUPS_WITHOUT_USERS = false;
        private const bool C_DEFAULT_INVALID_JOB_ASSIGN = false;
        private const bool C_DEFAULT_INVALID_JOB_TYPE_ASSIGN = false;
        private const bool C_DEFAULT_INVALID_STEP_ASSIGN = false;
        private const bool C_DEFAULT_IS_SELF_PARENT = false;
        private const bool C_DEFAULT_JOBS_WITHOUT_TYPES = false;
        private const bool C_DEFAULT_JOB_TYPES_WITHOUT_WORKFLOWS = false;
        private const bool C_DEFAULT_MISSING_AOI_MXDS = false;
        private const bool C_DEFAULT_MISSING_BASE_MXDS = false;
        private const bool C_DEFAULT_NON_ACTIVE_JOB_TYPES = false;
        private const bool C_DEFAULT_UNASSIGNED_STEPS = false;
        private const bool C_DEFAULT_USERS_WITHOUT_EMAILS = false;
        private const bool C_DEFAULT_USERS_WITHOUT_GROUPS = false;
        private const bool C_DEFAULT_ZERO_PCT_STEPS = false;
        #endregion

        #region MemberVariables
        private bool m_flagDifferingStepNames;
        private bool m_flagGroupsWithoutEmails;
        private bool m_flagGroupsWithoutPrivileges;
        private bool m_flagGroupsWithoutUsers;
        private bool m_flagInvalidJobAssign;
        private bool m_flagInvalidJobTypeAssign;
        private bool m_flagInvalidStepAssign;
        private bool m_flagIsSelfParent;
        private bool m_flagJobsWithoutTypes;
        private bool m_flagJobTypesWithoutWorkflows;
        private bool m_flagMissingAoiMxds;
        private bool m_flagMissingBaseMxds;
        private bool m_flagNonActiveJobTypes;
        private bool m_flagUnassignedSteps;
        private bool m_flagUsersWithoutEmails;
        private bool m_flagUsersWithoutGroups;
        private bool m_flagZeroPctSteps;

        private string m_logFilePath = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ReportPossibleErrors"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_REPORT_POSSIBLE_ERRORS; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_WMX_DB_UTILS; } }
        #endregion

        /// <summary>
        /// Default constructor; keep this around to make sure that the member
        /// variables are set up correctly, since this step is a little different
        /// than many of the others.
        /// </summary>
        public ReportPossibleErrors()
            : base()
        {
            ResetVariables();
        }

        #region Private helper functions
        /// <summary>
        /// Helper function to reinitialize the member variables for this class
        /// </summary>
        private void ResetVariables()
        {
            m_flagDifferingStepNames = C_DEFAULT_DIFFERING_STEP_NAMES;
            m_flagGroupsWithoutEmails = C_DEFAULT_GROUPS_WITHOUT_EMAILS;
            m_flagGroupsWithoutPrivileges = C_DEFAULT_GROUPS_WITHOUT_PRIVILEGES;
            m_flagGroupsWithoutUsers = C_DEFAULT_GROUPS_WITHOUT_USERS;
            m_flagInvalidJobAssign = C_DEFAULT_INVALID_JOB_ASSIGN;
            m_flagInvalidJobTypeAssign = C_DEFAULT_INVALID_JOB_TYPE_ASSIGN;
            m_flagInvalidStepAssign = C_DEFAULT_INVALID_STEP_ASSIGN;
            m_flagIsSelfParent = C_DEFAULT_IS_SELF_PARENT;
            m_flagJobsWithoutTypes = C_DEFAULT_JOBS_WITHOUT_TYPES;
            m_flagJobTypesWithoutWorkflows = C_DEFAULT_JOB_TYPES_WITHOUT_WORKFLOWS;
            m_flagMissingAoiMxds = C_DEFAULT_MISSING_AOI_MXDS;
            m_flagMissingBaseMxds = C_DEFAULT_MISSING_BASE_MXDS;
            m_flagNonActiveJobTypes = C_DEFAULT_NON_ACTIVE_JOB_TYPES;
            m_flagUnassignedSteps = C_DEFAULT_UNASSIGNED_STEPS;
            m_flagUsersWithoutEmails = C_DEFAULT_USERS_WITHOUT_EMAILS;
            m_flagUsersWithoutGroups = C_DEFAULT_USERS_WITHOUT_GROUPS;
            m_flagZeroPctSteps = C_DEFAULT_ZERO_PCT_STEPS;
        }
        
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

            // Ensure that the various parameter values are all restored to their
            // defaults because of how this function works
            ResetVariables();

            // Update the internal values of whatever parameters we're maintaining
            param = paramMap.GetParam(C_PARAM_CHECKLIST);
            IGPValue paramValue = m_gpUtilities.UnpackGPValue(param);
            IGPMultiValue paramMultiValue = paramValue as IGPMultiValue;

            for (int i = 0; i < paramMultiValue.Count; i++)
            {
                IGPValue check = paramMultiValue.get_Value(i);
                string strVal = check.GetAsText();
                if (strVal.Equals(C_OPT_DIFF_STEP_NAMES))
                {
                    m_flagDifferingStepNames = true;
                }
                else if (strVal.Equals(C_OPT_GROUPS_WITHOUT_EMAILS))
                {
                    m_flagGroupsWithoutEmails = true;
                }
                else if (strVal.Equals(C_OPT_GROUPS_WITHOUT_PRIVILEGES))
                {
                    m_flagGroupsWithoutPrivileges = true;
                }
                else if (strVal.Equals(C_OPT_GROUPS_WITHOUT_USERS))
                {
                    m_flagGroupsWithoutUsers = true;
                }
                else if (strVal.Equals(C_OPT_INVALID_JOB_ASSIGN))
                {
                    m_flagInvalidJobAssign = true;
                }
                else if (strVal.Equals(C_OPT_INVALID_JOB_TYPE_ASSIGN))
                {
                    m_flagInvalidJobTypeAssign = true;
                }
                else if (strVal.Equals(C_OPT_INVALID_STEP_ASSIGN))
                {
                    m_flagInvalidStepAssign = true;
                }
                else if (strVal.Equals(C_OPT_IS_SELF_PARENT))
                {
                    m_flagIsSelfParent = true;
                }
                else if (strVal.Equals(C_OPT_JOBS_WITHOUT_TYPES))
                {
                    m_flagJobsWithoutTypes = true;
                }
                else if (strVal.Equals(C_OPT_JOB_TYPES_WITHOUT_WORKFLOWS))
                {
                    m_flagJobTypesWithoutWorkflows = true;
                }
                else if (strVal.Equals(C_OPT_MISSING_AOI_MXDS))
                {
                    m_flagMissingAoiMxds = true;
                }
                else if (strVal.Equals(C_OPT_MISSING_BASE_MXDS))
                {
                    m_flagMissingBaseMxds = true;
                }
                else if (strVal.Equals(C_OPT_NON_ACTIVE_JOB_TYPES))
                {
                    m_flagNonActiveJobTypes = true;
                }
                else if (strVal.Equals(C_OPT_UNASSIGNED_STEPS))
                {
                    m_flagUnassignedSteps = true;
                }
                else if (strVal.Equals(C_OPT_USERS_WITHOUT_EMAILS))
                {
                    m_flagUsersWithoutEmails = true;
                }
                else if (strVal.Equals(C_OPT_USERS_WITHOUT_GROUPS))
                {
                    m_flagUsersWithoutGroups = true;
                }
                else if (strVal.Equals(C_OPT_ZERO_PCT_STEPS))
                {
                    m_flagZeroPctSteps = true;
                }
            }

            param = paramMap.GetParam(C_PARAM_OUT_LOG_FILE_PATH);
            m_logFilePath = param.Value.GetAsText();
        }

        /// <summary>
        /// Helper function that runs all of those checks that operate on each user group;
        /// intended to make the checks slightly more efficient by running them all at once
        /// rather than looping through all of the elements multiple times
        /// </summary>
        /// <param name="msgs">Add any GP messages to this object</param>
        /// <param name="errorCount">Counter used to track the number of problems found</param>
        /// <param name="logFileWriter">Object used to write error descriptions to a text file</param>
        private void ExecuteGroupChecks(IGPMessages msgs, ref int errorCount, StreamWriter logFileWriter)
        {
            // Check for groups w/o privileges
            if (!m_flagGroupsWithoutPrivileges &&
                !m_flagGroupsWithoutEmails &&
                !m_flagGroupsWithoutUsers)
            {
                return;
            }

            IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;
            IJTXConfigurationEdit2 configEdit = configMgr as IJTXConfigurationEdit2;

            // Put the items into a sorted list in order to make the output easier to
            // read/follow
            IJTXUserGroupSet allGroups = configMgr.UserGroups;
            SortedList<string, IJTXUserGroup2> allGroupsSorted = new SortedList<string, IJTXUserGroup2>();
            for (int i = 0; i < allGroups.Count; i++)
            {
                allGroupsSorted[allGroups.get_Item(i).Name] = allGroups.get_Item(i) as IJTXUserGroup2;
            }

            // Iterate over each group, performing the specified checks
            foreach (IJTXUserGroup2 group in allGroupsSorted.Values)
            {
                if (m_flagGroupsWithoutPrivileges)
                {
                    if (group.Privileges.Count < 1)
                    {
                        string message = "Group '" + group.Name + "' has no associated privileges";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }

                if (m_flagGroupsWithoutEmails)
                {
                    if (group.Email == null || group.Email.Equals(string.Empty))
                    {
                        string message = "Group '" + group.Name + "' has no associated e-mail address";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }

                if (m_flagGroupsWithoutUsers)
                {
                    if (group.Users.Count <= 0)
                    {
                        string message = "Group '" + group.Name + "' has no users assigned to it";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Helper function that runs all of those checks that operate on each job; intended
        /// to make the checks slightly more efficient by running through them all at once
        /// rather than looping through all of the elements multiple times
        /// </summary>
        /// <param name="msgs">Add any GP messages to this object</param>
        /// <param name="errorCount">Counter used to track the number of problems found</param>
        /// <param name="logFileWriter">Object used to write error descriptions to a text file</param>
        private void ExecuteJobChecks(IGPMessages msgs, ref int errorCount, StreamWriter logFileWriter)
        {
            // Only continue executing this function if needed
            if (!m_flagInvalidJobAssign &&
                !m_flagIsSelfParent &&
                !m_flagJobsWithoutTypes)
            {
                return;
            }
            
            IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;
            IJTXConfigurationEdit2 configEdit = configMgr as IJTXConfigurationEdit2;
            IJTXJobManager jobMgr = this.WmxDatabase.JobManager;

            // Use a database query as an alternate way of finding certain specific
            // problems with jobs.  Declare some of these ComReleaser objects to help
            // ensure that cursors, etc., are immediately released after they go out
            // of scope.

            // Check for jobs without any job types set (should be a DB error)
            if (m_flagJobsWithoutTypes)
            {
                using (ComReleaser cr1 = new ComReleaser(), cr2 = new ComReleaser())
                {
                    IFeatureWorkspace featureWorkspace = this.WmxDatabase.JTXWorkspace as IFeatureWorkspace;

                    // Get the name of the correct table from the jobs workspace, so
                    // that the table doesn't have to be owned by the connecting user.
                    string tableName = Common.WmauHelperFunctions.GetQualifiedTableName(Constants.JTX_TABLE_JTX_JOBS_TABLE, this.WmxDatabase.JTXWorkspace);

                    ITable jobsTable = featureWorkspace.OpenTable(tableName);
                    cr1.ManageLifetime(jobsTable);

                    IQueryFilter query = new QueryFilterClass();
                    query.WhereClause = Constants.FIELD_JOBTYPEID + " IS NULL";
                    ICursor searchCursor = jobsTable.Search(query, true);
                    cr2.ManageLifetime(searchCursor);

                    // Store the ID and name of each job matching this query
                    int idIndex = jobsTable.FindField(Constants.FIELD_JOBID);
                    int nameIndex = jobsTable.FindField(Constants.FIELD_JOBNAME);
                    IRow row = null;
                    while ((row = searchCursor.NextRow()) != null)
                    {
                        string idStr = row.get_Value(idIndex).ToString();
                        string nameStr = row.get_Value(nameIndex).ToString();
                        string msg = "Job " + idStr + " (" + nameStr + ") has no associated job type";
                        RecordMessage(msg, msgs, logFileWriter);
                        errorCount++;
                    }
                }
            }

            // Check for jobs that are their own parent job
            if (m_flagIsSelfParent)
            {
                using (ComReleaser cr1 = new ComReleaser(), cr2 = new ComReleaser())
                {
                    IFeatureWorkspace featureWorkspace = this.WmxDatabase.JTXWorkspace as IFeatureWorkspace;

                    // Get the name of the correct table from the jobs workspace, so
                    // that the table doesn't have to be owned by the connecting user.
                    string tableName = Common.WmauHelperFunctions.GetQualifiedTableName(Constants.JTX_TABLE_JTX_JOBS_TABLE, this.WmxDatabase.JTXWorkspace);

                    ITable jobsTable = featureWorkspace.OpenTable(tableName);
                    cr1.ManageLifetime(jobsTable);

                    const string C_FIELD_PARENT_JOB = "PARENT_JOB";
                    IQueryFilter query = new QueryFilterClass();
                    query.WhereClause = Constants.FIELD_JOBID + " = " + C_FIELD_PARENT_JOB;
                    ICursor searchCursor = jobsTable.Search(query, true);
                    cr2.ManageLifetime(searchCursor);

                    // Store the ID and name of each job matching this query
                    int idIndex = jobsTable.FindField(Constants.FIELD_JOBID);
                    int nameIndex = jobsTable.FindField(Constants.FIELD_JOBNAME);
                    IRow row = null;
                    while ((row = searchCursor.NextRow()) != null)
                    {
                        string idStr = row.get_Value(idIndex).ToString();
                        string nameStr = row.get_Value(nameIndex).ToString();
                        string msg = "Job " + idStr + " (" + nameStr + ") is its own parent";
                        RecordMessage(msg, msgs, logFileWriter);
                        errorCount++;
                    }
                }
            }

            // See if there are any checks selected for which we should iterate through
            // all of the jobs using the WMX interfaces
            if (m_flagInvalidJobAssign)
            {
                // Put the items into a sorted list in order to make the output easier to
                // read/follow
                IJTXJobSet allJobs = jobMgr.GetAllJobs();
                SortedList<int, IJTXJob3> allJobsSorted = new SortedList<int, IJTXJob3>();
                for (int i = 0; i < allJobs.Count; i++)
                {
                    allJobsSorted[allJobs.get_Item(i).ID] = allJobs.get_Item(i) as IJTXJob3;
                }

                // Iterate over all of the jobs
                foreach (IJTXJob3 job in allJobsSorted.Values)
                {
                    string assignedTo = job.AssignedTo;

                    // Check for any existing jobs with an invalid job assignment.  NOTE: only
                    // want to flag jobs that are not closed
                    if (m_flagInvalidJobAssign && job.Stage != jtxJobStage.jtxJobStageClosed)
                    {
                        if (job.AssignedType == jtxAssignmentType.jtxAssignmentTypeUser && configMgr.GetUser(assignedTo) == null)
                        {
                            string message = "Job '" + job.ID.ToString() +
                                "' assigned to unknown user '" + assignedTo + "'";
                            RecordMessage(message, msgs, logFileWriter);
                            errorCount++;
                        }
                        else if (job.AssignedType == jtxAssignmentType.jtxAssignmentTypeGroup && configMgr.GetUserGroup(assignedTo) == null)
                        {
                            string message = "Job '" + job.ID.ToString() +
                                "' assigned to unknown group '" + assignedTo + "'";
                            RecordMessage(message, msgs, logFileWriter);
                            errorCount++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper function that runs all of those checks that operate on each job type;
        /// intended to make the checks slightly more efficient by running through them all
        /// at once rather than looping through all of the elements multiple times
        /// </summary>
        /// <param name="msgs">Add any GP messages to this object</param>
        /// <param name="errorCount">Counter used to track the number of problems found</param>
        /// <param name="logFileWriter">Object used to write error descriptions to a text file</param>
        private void ExecuteJobTypeChecks(IGPMessages msgs, ref int errorCount, StreamWriter logFileWriter)
        {
            if (!m_flagInvalidJobTypeAssign &&
                !m_flagJobTypesWithoutWorkflows &&
                !m_flagMissingAoiMxds &&
                !m_flagMissingBaseMxds &&
                !m_flagNonActiveJobTypes)
            {
                return;
            }

            IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;
            IJTXConfigurationEdit2 configEdit = configMgr as IJTXConfigurationEdit2;

            // Put the items into a sorted list in order to make the output easier to
            // read/follow
            IJTXJobTypeSet allJobTypes = configMgr.JobTypes;
            SortedList<string, IJTXJobType3> allJobTypesSorted = new SortedList<string, IJTXJobType3>();
            for (int i = 0; i < allJobTypes.Count; i++)
            {
                allJobTypesSorted[allJobTypes.get_Item(i).Name] = allJobTypes.get_Item(i) as IJTXJobType3;
            }

            // Iterate through each item
            foreach (IJTXJobType3 jobType in allJobTypesSorted.Values)
            {
                if (m_flagInvalidJobTypeAssign)
                {
                    string assignedTo = jobType.DefaultAssignedTo;
                    if (jobType.DefaultAssignedType == jtxAssignmentType.jtxAssignmentTypeUser && configMgr.GetUser(assignedTo) == null)
                    {
                        string message = "Job Type '" + jobType.Name +
                            "' assigned to unknown user '" + assignedTo + "'";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                    else if (jobType.DefaultAssignedType == jtxAssignmentType.jtxAssignmentTypeGroup && configMgr.GetUserGroup(assignedTo) == null)
                    {
                        string message = "Job Type '" + jobType.Name +
                            "' assigned to unknown group '" + assignedTo + "'";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }

                if (m_flagJobTypesWithoutWorkflows)
                {
                    if (jobType.Workflow == null)
                    {
                        string message = "Job Type '" + jobType.Name + "' has no workflow defined";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }

                if (m_flagMissingAoiMxds)
                {
                    if (jobType.AOIMap == null)
                    {
                        string message = "Job Type '" + jobType.Name + "' has no AOI map defined";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }

                if (m_flagMissingBaseMxds)
                {
                    if (jobType.JobMap == null)
                    {
                        string message = "Job Type '" + jobType.Name +
                            "' has no job map (a.k.a. basemap) defined";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }

                if (m_flagNonActiveJobTypes)
                {
                    if (jobType.State != jtxJobTypeState.jtxJobTypeStateActive)
                    {
                        string message = "Job Type '" + jobType.Name + "' is not active";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Helper function that runs all of those checks that operate on each user in
        /// the database; intended to make the checks slightly more efficient by running
        /// through them all at once rather than looping through all of the elements
        /// multiple times
        /// </summary>
        /// <param name="msgs">Add any GP messages to this object</param>
        /// <param name="errorCount">Counter used to track the number of problems found</param>
        /// <param name="logFileWriter">Object used to write error descriptions to a text file</param>
        private void ExecuteUserChecks(IGPMessages msgs, ref int errorCount, StreamWriter logFileWriter)
        {
            // Only continue executing this function if needed
            if (!m_flagUsersWithoutEmails &&
                !m_flagUsersWithoutGroups)
            {
                return;
            }

            IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;
            IJTXConfigurationEdit2 configEdit = configMgr as IJTXConfigurationEdit2;

            // Put the items into a sorted list in order to make the output easier to
            // read/follow
            IJTXUserSet allUsers = configMgr.Users;
            SortedList<string, IJTXUser3> allUsersSorted = new SortedList<string, IJTXUser3>();
            for (int i = 0; i < allUsers.Count; i++)
            {
                allUsersSorted[allUsers.get_Item(i).UserName] = allUsers.get_Item(i) as IJTXUser3;
            }

            // Iterate through each item
            foreach (IJTXUser3 user in allUsersSorted.Values)
            {
                if (m_flagUsersWithoutEmails)
                {
                    if (user.Email == null || user.Email.Equals(string.Empty))
                    {
                        string message = "User '" + user.UserName + "' (" + user.FullName +
                            ") does not have an e-mail address configured";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }

                if (m_flagUsersWithoutGroups)
                {
                    if (user.Groups.Count < 1)
                    {
                        string message = "User '" + user.UserName + "' (" + user.FullName +
                            ") does not belong to any groups";
                        RecordMessage(message, msgs, logFileWriter);
                        errorCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Helper function that runs all of those checks that operate on each step in
        /// every workflow; intended to make the checks slightly more efficient by running
        /// through them all at once rather than looping through all of the elements
        /// multiple times
        /// </summary>
        /// <param name="msgs">Add any GP messages to this object</param>
        /// <param name="errorCount">Counter used to track the number of problems found</param>
        /// <param name="logFileWriter">Object used to write error descriptions to a text file</param>
        private void ExecuteWorkflowStepChecks(IGPMessages msgs, ref int errorCount, StreamWriter logFileWriter)
        {
            // Only continue executing this function if needed
            if (!m_flagInvalidStepAssign &&
                !m_flagUnassignedSteps &&
                !m_flagZeroPctSteps &&
                !m_flagDifferingStepNames)
            {
                return;
            }

            IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;
            IJTXConfigurationEdit2 configEdit = configMgr as IJTXConfigurationEdit2;

            // Put the items into a sorted list in order to make the output easier to
            // read/follow
            IJTXWorkflowSet allWorkflows = configMgr.Workflows;
            SortedList<string, IJTXWorkflow> allWorkflowsSorted = new SortedList<string, IJTXWorkflow>();
            for (int i = 0; i < allWorkflows.Count; i++)
            {
                allWorkflowsSorted[allWorkflows.get_Item(i).Name] = allWorkflows.get_Item(i);
            }

            // Iterate through each item
            foreach (IJTXWorkflow workflow in allWorkflowsSorted.Values)
            {
                IJTXWorkflowConfiguration workflowCfg = workflow as IJTXWorkflowConfiguration;
                int[] allStepIds = workflowCfg.GetAllSteps();
                foreach (int j in allStepIds)
                {
                    IJTXStep3 step = workflowCfg.GetStep(j) as IJTXStep3;
                    string assignedTo = step.AssignedTo;

                    // Check for any default step types with an invalid step assignment
                    if (m_flagInvalidStepAssign)
                    {
                        if (step.AssignedType == jtxAssignmentType.jtxAssignmentTypeUser && configMgr.GetUser(assignedTo) == null)
                        {
                            string message = "Workflow '" + workflow.Name + "', step '" +
                                step.StepName + "' assigned to unknown user '" + assignedTo + "'";
                            RecordMessage(message, msgs, logFileWriter);
                            errorCount++;
                        }
                        else if (step.AssignedType == jtxAssignmentType.jtxAssignmentTypeGroup && configMgr.GetUserGroup(assignedTo) == null)
                        {
                            string message = "Workflow '" + workflow.Name + "', step '" +
                                step.StepName + "' assigned to unknown group '" + assignedTo + "'";
                            RecordMessage(message, msgs, logFileWriter);
                            errorCount++;
                        }
                    }

                    // Check for any steps that have not been assigned to a group or user
                    if (m_flagUnassignedSteps)
                    {
                        if (step.AssignedType == jtxAssignmentType.jtxAssignmentTypeUnassigned)
                        {
                            string message = "Workflow '" + workflow.Name + "', step '" +
                                step.StepName + "' is unassigned";
                            RecordMessage(message, msgs, logFileWriter);
                            errorCount++;
                        }
                    }

                    // Check for any steps whose "post-complete" percentage is 0
                    if (m_flagZeroPctSteps)
                    {
                        if (step.DefaultPercComplete < double.Epsilon)
                        {
                            string message = "Workflow '" + workflow.Name + "', step '" +
                                step.StepName + "' sets percent complete to 0";
                            RecordMessage(message, msgs, logFileWriter);
                            errorCount++;
                        }
                    }

                    // Check for any steps whose descriptions in a workflow does not match
                    // the underlying step type name
                    if (m_flagDifferingStepNames)
                    {
                        IJTXStepType2 stepType = configMgr.GetStepTypeByID(step.StepTypeID) as IJTXStepType2;
                        if (!step.StepName.Equals(stepType.Name))
                        {
                            string message = "Workflow '" + workflow.Name + "', step name '" +
                                step.StepName + "' does not match step type name '" + stepType.Name + "'";
                            RecordMessage(message, msgs, logFileWriter);
                            errorCount++;
                        }
                    }
                } // end for each step
            } // end for each workflow
        }

        /// <summary>
        /// Writes a message both to the GP messages object and to a log file (if
        /// one has been specified)
        /// </summary>
        /// <param name="message">The message to be written</param>
        /// <param name="msgs">The IGPMessages object to which the string will be written</param>
        /// <param name="writer">
        /// An optional StreamWriter object (opened log file) to which the messages will
        /// be written
        /// </param>
        private void RecordMessage(string message, IGPMessages msgs, StreamWriter writer)
        {
            if (msgs != null)
            {
                msgs.AddMessage("  " + message);
            }
            if (writer != null)
            {
                writer.WriteLine(message);
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

                // Optional parameter indicating which checks should be run against
                // the database
                IGPCodedValueDomain cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddStringCode(C_OPT_DIFF_STEP_NAMES, C_OPT_DIFF_STEP_NAMES);
                cvDomain.AddStringCode(C_OPT_GROUPS_WITHOUT_EMAILS, C_OPT_GROUPS_WITHOUT_EMAILS);
                cvDomain.AddStringCode(C_OPT_GROUPS_WITHOUT_PRIVILEGES, C_OPT_GROUPS_WITHOUT_PRIVILEGES);
                cvDomain.AddStringCode(C_OPT_GROUPS_WITHOUT_USERS, C_OPT_GROUPS_WITHOUT_USERS);
                cvDomain.AddStringCode(C_OPT_INVALID_JOB_ASSIGN, C_OPT_INVALID_JOB_ASSIGN);
                cvDomain.AddStringCode(C_OPT_INVALID_JOB_TYPE_ASSIGN, C_OPT_INVALID_JOB_TYPE_ASSIGN);
                cvDomain.AddStringCode(C_OPT_INVALID_STEP_ASSIGN, C_OPT_INVALID_STEP_ASSIGN);
                cvDomain.AddStringCode(C_OPT_IS_SELF_PARENT, C_OPT_IS_SELF_PARENT);
                cvDomain.AddStringCode(C_OPT_JOBS_WITHOUT_TYPES, C_OPT_JOBS_WITHOUT_TYPES);
                cvDomain.AddStringCode(C_OPT_JOB_TYPES_WITHOUT_WORKFLOWS, C_OPT_JOB_TYPES_WITHOUT_WORKFLOWS);
                cvDomain.AddStringCode(C_OPT_MISSING_AOI_MXDS, C_OPT_MISSING_AOI_MXDS);
                cvDomain.AddStringCode(C_OPT_MISSING_BASE_MXDS, C_OPT_MISSING_BASE_MXDS);
                cvDomain.AddStringCode(C_OPT_NON_ACTIVE_JOB_TYPES, C_OPT_NON_ACTIVE_JOB_TYPES);
                cvDomain.AddStringCode(C_OPT_UNASSIGNED_STEPS, C_OPT_UNASSIGNED_STEPS);
                cvDomain.AddStringCode(C_OPT_USERS_WITHOUT_EMAILS, C_OPT_USERS_WITHOUT_EMAILS);
                cvDomain.AddStringCode(C_OPT_USERS_WITHOUT_GROUPS, C_OPT_USERS_WITHOUT_GROUPS);
                cvDomain.AddStringCode(C_OPT_ZERO_PCT_STEPS, C_OPT_ZERO_PCT_STEPS);

                IGPMultiValueType checklistType = new GPMultiValueTypeClass();
                checklistType.MemberDataType = new GPStringTypeClass();

                // NOTE: See http://help.arcgis.com/en/sdk/10.0/arcobjects_net/conceptualhelp/index.html#//00010000049w000000
                // ("Building a custom geoprocessing function tool") for an example of the use
                // of the UID class and manually setting the control CLSID of an object.
                // Refer to this page for tips about how to determine the available CLSIDs.
                UID pUID = new UIDClass();
                pUID.Value = "{38C34610-C7F7-11D5-A693-0008C711C8C1}";

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_RPE_CHECKLIST,
                    C_PARAM_CHECKLIST,
                    checklistType as IGPDataType,
                    null);
                paramEdit.Domain = cvDomain as IGPDomain;
                paramEdit.ControlCLSID = pUID;
                m_parameters.Add(paramEdit);
                
                // Optional parameter indicating the file to which a description of any
                // problems should be written
                IGPFileDomain logFileDomain = new GPFileDomainClass();
                logFileDomain.AddType("txt");

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_RPE_OUT_LOG_FILE_PATH,
                    C_PARAM_OUT_LOG_FILE_PATH,
                    new DEFileTypeClass() as IGPDataType,
                    null);
                paramEdit.Domain = logFileDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Output parameter indicating how many total items were flagged as
                // errors
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_RPE_OUT_NUM_ISSUES_FOUND,
                    C_PARAM_OUT_ISSUES_FOUND,
                    new GPLongTypeClass(),
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
            StreamWriter logFileWriter = null;

            try
            {
                int errorCount = 0;

                if (!string.IsNullOrEmpty(m_logFilePath))
                {
                    logFileWriter = new StreamWriter(m_logFilePath);
                }

                IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;
                IJTXConfigurationEdit2 configEdit = configMgr as IJTXConfigurationEdit2;
                IJTXJobManager jobMgr = this.WmxDatabase.JobManager;

                // Workflow Manager intentionally caches the data workspaces in the system.  To ensure
                // that we have the most current list of data workspaces, invalidate this cache
                // before attempting to retrieve the list from the system.
                this.WmxDatabase.InvalidateDataWorkspaceNames();

                // Run checks against users
                ExecuteUserChecks(msgs, ref errorCount, logFileWriter);

                // Run checks against groups
                ExecuteGroupChecks(msgs, ref errorCount, logFileWriter);

                // Run checks against any existing jobs
                ExecuteJobChecks(msgs, ref errorCount, logFileWriter);

                // Check for any template job types with an invalid default assignment
                ExecuteJobTypeChecks(msgs, ref errorCount, logFileWriter);

                // Check the workflow steps for problems
                ExecuteWorkflowStepChecks(msgs, ref errorCount, logFileWriter);

                // Set the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_OUT_ISSUES_FOUND);
                IGPLong outValue = new GPLongClass();
                outValue.Value = errorCount;
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
                    WmauError error = new WmauError(WmauErrorCodes.C_UNSPECIFIED_ERROR);
                    msgs.AddError(error.ErrorCodeAsInt, error.Message + "; " + ex.Message);
                }
                catch
                {
                    // Catch anything else that possibly happens
                }
            }
            finally
            {
                // Release any COM objects here!
                if (logFileWriter != null)
                {
                    logFileWriter.Close();
                }
            }
        }
    }
}
