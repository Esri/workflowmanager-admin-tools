using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;
using ESRI.ArcGIS.JTX.Utilities;


namespace WorkflowManagerAdministrationUtilities
{
    class DeleteOrphanedTypes : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_PREVIEW_CHANGES = "in_bool_previewChangesOnly";
        private const string C_PARAM_CHECKLIST = "in_mvString_checklist";
        private const string C_PARAM_NUM_ITEMS_DELETED = "out_long_numItemsDeleted";

        private const string C_OPT_PREVIEW_DELETES = "PREVIEW_DELETES";
        private const string C_OPT_PERFORM_DELETES = "PERFORM_DELETES";
        private const string C_OPT_CLEAN_WORKFLOWS = "CLEAN_WORKFLOWS";
        private const string C_OPT_CLEAN_STEP_TYPES = "CLEAN_STEP_TYPES";
        private const string C_OPT_CLEAN_STATUS_TYPES = "CLEAN_STATUS_TYPES";
        private const string C_OPT_CLEAN_PRIORITIES = "CLEAN_PRIORITIES";
        private const string C_OPT_CLEAN_TA_WORKBOOKS = "CLEAN_TA_WORKBOOKS";
        private const string C_OPT_CLEAN_USERS = "CLEAN_USERS";
        private const string C_OPT_CLEAN_MAP_DOCS = "CLEAN_MAP_DOCS";

        private const bool C_DEFAULT_PREVIEW_CHANGES = true;
        private const bool C_DEFAULT_CLEAN_WORKFLOWS = false;
        private const bool C_DEFAULT_CLEAN_STEP_TYPES = false;
        private const bool C_DEFAULT_CLEAN_STATUS_TYPES = false;
        private const bool C_DEFAULT_CLEAN_PRIORITIES = false;
        private const bool C_DEFAULT_CLEAN_TA_WORKBOOKS = false;
        private const bool C_DEFAULT_CLEAN_USERS = false;
        private const bool C_DEFAULT_CLEAN_MAP_DOCS = false;

        // These values match up with other predetermined values referenced by Workflow
        // Manager.  Do not modify.
        private const string C_STATUS_CLOSED = "Closed";
        private const string C_STATUS_CREATED = "Created";
        private const string C_STATUS_DONE_WORKING = "DoneWorking";
        private const string C_STATUS_READY_TO_WORK = "ReadyToWork";
        private const string C_STATUS_WORKING = "Working";

        private const string C_WORKBOOK_FLAG = "/taworkflow:";
        private const string C_MAP_DOC_FLAG = "/mxd:";
        #endregion

        #region MemberVariables
        private bool m_previewChanges = C_DEFAULT_PREVIEW_CHANGES;
        private bool m_cleanWorkflows = C_DEFAULT_CLEAN_WORKFLOWS;
        private bool m_cleanStepTypes = C_DEFAULT_CLEAN_STEP_TYPES;
        private bool m_cleanStatusTypes = C_DEFAULT_CLEAN_STATUS_TYPES;
        private bool m_cleanPriorities = C_DEFAULT_CLEAN_PRIORITIES;
        private bool m_cleanTaWorkbooks = C_DEFAULT_CLEAN_TA_WORKBOOKS;
        private bool m_cleanUsers = C_DEFAULT_CLEAN_USERS;
        private bool m_cleanMapDocs = C_DEFAULT_CLEAN_MAP_DOCS;

        private Dictionary<int, string> m_unusedWorkflows = new Dictionary<int, string>();
        private Dictionary<int, string> m_unusedStepTypes = new Dictionary<int, string>();
        private Dictionary<int, string> m_unusedStatusTypes = new Dictionary<int, string>();
        private Dictionary<int, string> m_unusedPriorities = new Dictionary<int, string>();
        private Dictionary<string, string> m_unusedTaWorkbooks = new Dictionary<string, string>();
        private Dictionary<string, string> m_unusedUsers = new Dictionary<string, string>();
        private Dictionary<string, int> m_unusedMapDocs = new Dictionary<string, int>();
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "DeleteOrphanedTypes"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_DELETE_ORPHANED_TYPES; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_WMX_DB_UTILS; } }
        #endregion

        #region Private helper functions
        /// <summary>
        /// Helper function to reinitialize the member variables for this class
        /// </summary>
        private void ResetVariables()
        {
            m_cleanWorkflows = C_DEFAULT_CLEAN_WORKFLOWS;
            m_cleanStepTypes = C_DEFAULT_CLEAN_STEP_TYPES;
            m_cleanStatusTypes = C_DEFAULT_CLEAN_STATUS_TYPES;
            m_cleanPriorities = C_DEFAULT_CLEAN_PRIORITIES;
            m_cleanTaWorkbooks = C_DEFAULT_CLEAN_TA_WORKBOOKS;
            m_cleanUsers = C_DEFAULT_CLEAN_USERS;
            m_cleanMapDocs = C_DEFAULT_CLEAN_MAP_DOCS;

            m_unusedWorkflows.Clear();
            m_unusedStepTypes.Clear();
            m_unusedStatusTypes.Clear();
            m_unusedPriorities.Clear();
            m_unusedTaWorkbooks.Clear();
            m_unusedUsers.Clear();
            m_unusedMapDocs.Clear();
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
            param = paramMap.GetParam(C_PARAM_PREVIEW_CHANGES);
            m_previewChanges = (param.Value as IGPBoolean).Value;

            param = paramMap.GetParam(C_PARAM_CHECKLIST);
            IGPValue paramValue = m_gpUtilities.UnpackGPValue(param);
            IGPMultiValue paramMultiValue = paramValue as IGPMultiValue;

            for (int i = 0; i < paramMultiValue.Count; i++)
            {
                IGPValue check = paramMultiValue.get_Value(i);
                string strVal = check.GetAsText();
                if (strVal.Equals(C_OPT_CLEAN_WORKFLOWS))
                {
                    m_cleanWorkflows = true;
                }
                else if (strVal.Equals(C_OPT_CLEAN_STEP_TYPES))
                {
                    m_cleanStepTypes = true;
                }
                else if (strVal.Equals(C_OPT_CLEAN_STATUS_TYPES))
                {
                    m_cleanStatusTypes = true;
                }
                else if (strVal.Equals(C_OPT_CLEAN_PRIORITIES))
                {
                    m_cleanPriorities = true;
                }
                else if (strVal.Equals(C_OPT_CLEAN_TA_WORKBOOKS))
                {
                    m_cleanTaWorkbooks = true;
                }
                else if (strVal.Equals(C_OPT_CLEAN_USERS))
                {
                    m_cleanUsers = true;
                }
                else if (strVal.Equals(C_OPT_CLEAN_MAP_DOCS))
                {
                    m_cleanMapDocs = true;
                }
            }
        }

        /// <summary>
        /// Updates all of the orphan lists in use for this run.  NOTE: "ExtractParameters()"
        /// *must* have run before this function is called.
        /// </summary>
        /// <returns>The total number of orphaned items found in the database</returns>
        private int UpdateAllOrphans()
        {
            int lastOrphanCount = -1;
            int currentOrphanCount = 0;

            // Do this from within a loop so as to (hopefully) avoid any sneaky
            // dependencies between item types.
            while (currentOrphanCount != lastOrphanCount)
            {
                lastOrphanCount = currentOrphanCount;
                currentOrphanCount = 0;

                if (m_cleanWorkflows)
                {
                    UpdateOrphanedWorkflows();
                    currentOrphanCount += m_unusedWorkflows.Count;
                }
                if (m_cleanStepTypes)
                {
                    UpdateOrphanedStepTypes();
                    currentOrphanCount += m_unusedStepTypes.Count;
                }
                if (m_cleanStatusTypes)
                {
                    UpdateOrphanedStatusTypes();
                    currentOrphanCount += m_unusedStatusTypes.Count;
                }
                if (m_cleanPriorities)
                {
                    UpdateOrphanedPriorityTypes();
                    currentOrphanCount += m_unusedPriorities.Count;
                }
                if (m_cleanTaWorkbooks)
                {
                    UpdateOrphanedTaWorkbooks();
                    currentOrphanCount += m_unusedTaWorkbooks.Count;
                }
                if (m_cleanUsers)
                {
                    UpdateOrphanedUsers();
                    currentOrphanCount += m_unusedUsers.Count;
                }
                if (m_cleanMapDocs)
                {
                    UpdateOrphanedMapDocuments();
                    currentOrphanCount += m_unusedMapDocs.Count;
                }
            }

            return currentOrphanCount;
        }

        /// <summary>
        /// Find those workflows in the database that are not being used by any job type.
        /// </summary>
        /// <returns>The total number of orphaned items found</returns>
        private int UpdateOrphanedWorkflows()
        {
            Dictionary<int, int> usedWorkflows = new Dictionary<int, int>();
            IJTXDatabase3 wmxDb = this.WmxDatabase;
            IJTXConfiguration3 configMgr = wmxDb.ConfigurationManager as IJTXConfiguration3;
            IJTXJobTypeSet jobTypes = configMgr.JobTypes;

            // Iterate through each item in the set, building up the list of used types
            for (int i = 0; i < jobTypes.Count; i++)
            {
                // TODO: Do not consider items that we know are not in use

                IJTXJobType3 type = jobTypes.get_Item(i) as IJTXJobType3;
                // job type don't always need to have a workflow
                if (type.Workflow != null)
                    usedWorkflows[type.Workflow.ID] = type.Workflow.ID;
            }

            // Get the complete list of this type of object in the database
            IJTXWorkflowSet workflows = configMgr.Workflows;
            
            // Loop through the complete list of this object type.  If any of the IDs
            // are not in the "used" list, add that object to the "unused" list.
            // If all of the items are used, don't bother trying to add to the
            // unused list.
            if (usedWorkflows.Count != workflows.Count)
            {
                for (int i = 0; i < workflows.Count; i++)
                {
                    IJTXWorkflow workflow = workflows.get_Item(i) as IJTXWorkflow;
                    if (!usedWorkflows.ContainsKey(workflow.ID))
                    {
                        m_unusedWorkflows[workflow.ID] = workflow.Name;
                    }
                }
            }

            return m_unusedWorkflows.Count;
        }

        /// <summary>
        /// Find those step types in the database that are not being used by any workflow.
        /// </summary>
        /// <returns>The total number of orphaned items found</returns>
        private int UpdateOrphanedStepTypes()
        {
            Dictionary<int, int> usedStepTypes = new Dictionary<int, int>();
            IJTXDatabase3 wmxDb = this.WmxDatabase;
            IJTXConfiguration3 configMgr = wmxDb.ConfigurationManager as IJTXConfiguration3;
            IJTXWorkflowSet workflows = configMgr.Workflows;

            // Iterate through each workflow, building up the list of used steps
            for (int i = 0; i < workflows.Count; i++)
            {
                // Skip over those workflows that aren't in use
                IJTXWorkflow workflow = workflows.get_Item(i);
                if (m_unusedWorkflows.Keys.Contains(workflow.ID))
                {
                    continue;
                }

                // Examine the remaining workflows
                IJTXWorkflowConfiguration workflowCfg = workflows.get_Item(i) as IJTXWorkflowConfiguration;
                int[] stepIds = workflowCfg.GetAllSteps();
                foreach (int stepId in stepIds)
                {
                    int stepTypeId = workflowCfg.GetStep(stepId).StepTypeID;
                    usedStepTypes[stepTypeId] = stepTypeId;
                }
            }

            // Get the complete list of step types in the database
            IJTXStepTypeSet allStepTypeObjs = configMgr.StepTypes;

            // Loop over all of the step types.  For anything whose step type ID is not
            // contained in the "used" list, add it to the "unused" list.
            // If all of the items are used, don't bother trying to add to the
            // unused list.
            if (usedStepTypes.Count != allStepTypeObjs.Count)
            {
                for (int i = 0; i < allStepTypeObjs.Count; i++)
                {
                    IJTXStepType2 stepType = allStepTypeObjs.get_Item(i) as IJTXStepType2;
                    int stepTypeId = stepType.ID;
                    if (!usedStepTypes.ContainsKey(stepTypeId))
                    {
                        m_unusedStepTypes[stepTypeId] = stepType.Name;
                    }
                }
            }

            return m_unusedStepTypes.Count;
        }

        /// <summary>
        /// Find those status types in the database that are not being used by any step.
        /// </summary>
        /// <returns>The total number of orphaned items found</returns>
        private int UpdateOrphanedStatusTypes()
        {
            Dictionary<int, int> usedStatusTypes = new Dictionary<int, int>();
            IJTXDatabase3 wmxDb = this.WmxDatabase;
            string[] coreStatusNames =
            {
                C_STATUS_CLOSED,
                C_STATUS_CREATED,
                C_STATUS_DONE_WORKING,
                C_STATUS_READY_TO_WORK,
                C_STATUS_WORKING
            };

            IJTXConfiguration3 configMgr = wmxDb.ConfigurationManager as IJTXConfiguration3;

            // Iterate through each job currently in the system, adding the status types used
            // by these jobs
            IJTXJobSet jobs = wmxDb.JobManager.GetAllJobs();
            for (int i = 0; i < jobs.Count; i++)
            {
                IJTXJob3 job = jobs.get_Item(i) as IJTXJob3;
                if (job.Status != null)
                {
                    usedStatusTypes[job.Status.ID] = job.Status.ID;
                }
            }

            // Iterate through each workflow, building up the list of used status types
            // based on the statuses that are assigned by each step
            IJTXWorkflowSet workflows = configMgr.Workflows;
            for (int i = 0; i < workflows.Count; i++)
            {
                // Skip over those workflows that aren't in use
                IJTXWorkflow workflow = workflows.get_Item(i);
                if (m_unusedWorkflows.Keys.Contains(workflow.ID))
                {
                    continue;
                }

                // Examine the remaining workflows
                IJTXWorkflowConfiguration workflowCfg = workflows.get_Item(i) as IJTXWorkflowConfiguration;
                int[] stepIds = workflowCfg.GetAllSteps();
                foreach (int stepId in stepIds)
                {
                    IJTXStep3 step = workflowCfg.GetStep(stepId) as IJTXStep3;
                    usedStatusTypes[step.StatusID] = step.StatusID;
                }
            }

            // Add the status types used by Workflow Manager itself
            foreach (string s in coreStatusNames)
            {
                IJTXStatus2 status = configMgr.GetStatus(s) as IJTXStatus2;
                
                // Avoid problems if someone deleted one of these mandatory types from the database
                if (status != null)
                {
                    int id = status.ID;
                    usedStatusTypes[id] = id;
                }
            }

            // Get the complete list of status types in the database
            IJTXStatusSet allStatusTypes = configMgr.Statuses;

            // Loop over all of the status types.  For anything whose ID is not contained
            // in the "used" list, add it to the "unused" list. If all of the items are
            // used, don't bother trying to add to the unused list.
            if (usedStatusTypes.Count != allStatusTypes.Count)
            {
                for (int i = 0; i < allStatusTypes.Count; i++)
                {
                    IJTXStatus2 statusType = allStatusTypes.get_Item(i) as IJTXStatus2;
                    if (!usedStatusTypes.ContainsKey(statusType.ID))
                    {
                        m_unusedStatusTypes[statusType.ID] = statusType.Name;
                    }
                }
            }

            return m_unusedStatusTypes.Count;
        }

        /// <summary>
        /// Find those priority levels in the database that are not being used by any
        /// job or job type.
        /// </summary>
        /// <returns>The total number of orphaned items found</returns>
        private int UpdateOrphanedPriorityTypes()
        {
            Dictionary<int, string> usedTypes = new Dictionary<int, string>();
            IJTXDatabase3 wmxDb = this.WmxDatabase;

            // Check all the jobs for priorities currently in use
            IJTXJobSet allJobs = wmxDb.JobManager.GetAllJobs();
            for (int i = 0; i < allJobs.Count; i++)
            {
                IJTXJob3 job = allJobs.get_Item(i) as IJTXJob3;
                if (!usedTypes.ContainsKey(job.Priority.Value))
                {
                    usedTypes[job.Priority.Value] = job.Priority.Name;
                }
            }

            // Check the template job types for default priorities in use
            IJTXJobTypeSet allJobTypes = wmxDb.ConfigurationManager.JobTypes;
            for (int i = 0; i < allJobTypes.Count; i++)
            {
                // TODO: Skip unused job types

                IJTXJobType3 jobType = allJobTypes.get_Item(i) as IJTXJobType3;
                if (!usedTypes.ContainsKey(jobType.DefaultPriority.Value))
                {
                    usedTypes[jobType.DefaultPriority.Value] = jobType.DefaultPriority.Name;
                }
            }

            // Loop over all of the priorities.  For anything whose name is not contained
            // in the "used" list, add it to the "unused" list.  If all of the items are
            // used, don't bother trying to add to the unused list.
            IJTXPrioritySet allTypes = wmxDb.ConfigurationManager.Priorities;
            if (usedTypes.Count != allTypes.Count)
            {
                for (int i = 0; i < allTypes.Count; i++)
                {
                    IJTXPriority priority = allTypes.get_Item(i) as IJTXPriority;
                    if (!usedTypes.ContainsKey(priority.Value))
                    {
                        m_unusedPriorities[priority.Value] = priority.Name;
                    }
                }
            }

            return m_unusedPriorities.Count;
        }

        /// <summary>
        /// Find those Task Assistant workbooks in the database that are not being used by
        /// any step that launches ArcMap
        /// </summary>
        /// <returns>The total number of orphaned items found</returns>
        private int UpdateOrphanedTaWorkbooks()
        {
            SortedList<string, string> unusedItems = new SortedList<string, string>();
            IJTXDatabase3 wmxDb = this.WmxDatabase;
            IJTXConfiguration3 configMgr = wmxDb.ConfigurationManager as IJTXConfiguration3;
            IJTXTaskAssistantWorkflowRecordSet allItems = configMgr.TaskAssistantWorkflowRecords;

            // First check to see if there are even any TA workbooks in the database
            if (allItems.Count > 0)
            {
                Dictionary<string, string> usedTypes = new Dictionary<string, string>();

                // Search all the step types for Task Assistant workbooks currently in use
                IJTXStepTypeSet allStepTypes = wmxDb.ConfigurationManager.StepTypes;
                for (int i = 0; i < allStepTypes.Count; i++)
                {
                    IJTXStepType2 stepType = allStepTypes.get_Item(i) as IJTXStepType2;

                    // Skip over unused step types
                    if (m_unusedStepTypes.Keys.Contains(stepType.ID))
                    {
                        continue;
                    }

                    // Examine the remaining step types
                    for (int j = 0; j < stepType.Arguments.Length; j++)
                    {
                        string stepArg = stepType.Arguments[j].ToString();
                        if (stepArg.StartsWith(C_WORKBOOK_FLAG))
                        {
                            string suffix = stepArg.Substring(C_WORKBOOK_FLAG.Length);
                            suffix = suffix.Trim(new char[] { '"' });
                            usedTypes[suffix] = null;
                        }
                    }
                }

                // Loop over all the Task Assistant workbooks, looking for anything
                // that we didn't identify as "in use"
                for (int i = 0; i < allItems.Count; i++)
                {
                    IJTXTaskAssistantWorkflowRecord item = allItems.get_Item(i);
                    if (!usedTypes.ContainsKey(item.Alias))
                    {
                        m_unusedTaWorkbooks[item.Alias] = null;
                    }
                }
            }

            return m_unusedTaWorkbooks.Count;
        }

        /// <summary>
        /// Find those users in the database who are not being referenced in any way
        /// </summary>
        /// <returns>The total number of orphaned items found</returns>
        private int UpdateOrphanedUsers()
        {
            SortedList<string, string> unusedItems = new SortedList<string, string>();
            IJTXDatabase3 wmxDb = this.WmxDatabase;
            IJTXConfiguration3 configMgr = wmxDb.ConfigurationManager as IJTXConfiguration3;
            IJTXJobManager jobMgr = wmxDb.JobManager;
            IJTXUserSet allUsers = configMgr.Users;

            Dictionary<string, string> usedItems = new Dictionary<string, string>();

            // Get all of the users who are members of a group
            IJTXUserGroupSet allGroups = configMgr.UserGroups;
            for (int i = 0; i < allGroups.Count; i++)
            {
                IJTXUserGroup2 group = allGroups.get_Item(i) as IJTXUserGroup2;
                for (int j = 0; j < group.Users.Count; j++)
                {
                    IJTXUser3 user = group.Users.get_Item(j) as IJTXUser3;
                    usedItems[user.UserName] = user.FullName;
                }
            }

            // If necessary, add in the users who have jobs assigned to them
            if (usedItems.Count < allUsers.Count)
            {
                IJTXJobSet allJobs = jobMgr.GetAllJobs();
                for (int i = 0; i < allJobs.Count; i++)
                {
                    IJTXJob3 job = allJobs.get_Item(i) as IJTXJob3;
                    if (job.AssignedType == jtxAssignmentType.jtxAssignmentTypeUser)
                    {
                        IJTXUser3 user = configMgr.GetUser(job.AssignedTo) as IJTXUser3;
                        
                        // It's possible for a user to have a job assigned, but have
                        // already been removed from the DB.  Throw an exception in
                        // this case, as the DB needs to be cleaned up.
                        if (user == null)
                        {
                            throw new WmauException(WmauErrorCodes.C_USER_NOT_FOUND_ERROR);
                        }
                        usedItems[user.UserName] = user.FullName;
                    }
                }
            }

            // If necessary, add in the users who have a job type's default assignment
            // set to them
            if (usedItems.Count < allUsers.Count)
            {
                IJTXJobTypeSet allJobTypes = configMgr.JobTypes;
                for (int i = 0; i < allJobTypes.Count; i++)
                {
                    // TODO: Exclude orphaned job types

                    IJTXJobType3 jobType = allJobTypes.get_Item(i) as IJTXJobType3;
                    if (jobType.DefaultAssignedType == jtxAssignmentType.jtxAssignmentTypeUser)
                    {
                        IJTXUser3 user = configMgr.GetUser(jobType.DefaultAssignedTo) as IJTXUser3;

                        // It's possible for a user to have a job assigned, but have
                        // already been removed from the DB.  Throw an exception in
                        // this case, as the DB needs to be cleaned up.
                        if (user == null)
                        {
                            throw new WmauException(WmauErrorCodes.C_USER_NOT_FOUND_ERROR);
                        }
                        usedItems[user.UserName] = user.FullName;
                    }
                }
            }

            // If necessary, add in the users who have steps assigned to them
            // by default
            if (usedItems.Count < allUsers.Count)
            {
                IJTXWorkflowSet allWorkflows = configMgr.Workflows;
                for (int i = 0; i < allWorkflows.Count; i++)
                {
                    // Skip over unused workflows
                    IJTXWorkflow workflow = allWorkflows.get_Item(i);
                    if (m_unusedWorkflows.Keys.Contains(workflow.ID))
                    {
                        continue;
                    }

                    // Examine the other items
                    IJTXWorkflowConfiguration workflowCfg = allWorkflows.get_Item(i) as IJTXWorkflowConfiguration;
                    int[] workflowStepIds = workflowCfg.GetAllSteps();
                    foreach (int j in workflowStepIds)
                    {
                        IJTXStep3 step = workflowCfg.GetStep(j) as IJTXStep3;
                        if (step.AssignedType == jtxAssignmentType.jtxAssignmentTypeUser)
                        {
                            IJTXUser3 user = configMgr.GetUser(step.AssignedTo) as IJTXUser3;

                            // It's possible for a user to have a job assigned, but have
                            // already been removed from the DB.  Throw an exception in
                            // this case, as the DB needs to be cleaned up.
                            if (user == null)
                            {
                                throw new WmauException(WmauErrorCodes.C_USER_NOT_FOUND_ERROR);
                            }
                            usedItems[user.UserName] = user.FullName;
                        }
                    }
                }
            }

            // Loop over all the users in the DB, looking for anything
            // that we didn't identify as "in use"
            for (int i = 0; i < allUsers.Count; i++)
            {
                IJTXUser3 item = allUsers.get_Item(i) as IJTXUser3;
                if (!usedItems.ContainsKey(item.UserName))
                {
                    m_unusedUsers[item.UserName] = item.FullName;
                }
            }

            return m_unusedUsers.Count;
        }

        /// <summary>
        /// Find those map documents embedded in the database that are not being
        /// referenced in any way
        /// </summary>
        /// <returns>The total number of orphaned items found</returns>
        private int UpdateOrphanedMapDocuments()
        {
            SortedList<string, int> unusedItems = new SortedList<string, int>();
            IJTXDatabase3 wmxDb = this.WmxDatabase;
            IJTXConfiguration3 configMgr = wmxDb.ConfigurationManager as IJTXConfiguration3;
            IJTXConfigurationEdit2 configEdit = wmxDb.ConfigurationManager as IJTXConfigurationEdit2;

            IJTXMapSet allMaps = configMgr.JTXMaps;
            Dictionary<string, int> allMapNames = new Dictionary<string, int>();
            for (int i = 0; i < allMaps.Count; i++)
            {
                IJTXMap map = allMaps.get_Item(i);
                allMapNames[map.Name] = map.ID;
            }

            Dictionary<string, int> usedItems = new Dictionary<string, int>();

            // Find the map types that are associated with job types
            IJTXJobTypeSet allJobTypes = configMgr.JobTypes;
            for (int i = 0; i < allJobTypes.Count; i++)
            {
                // TODO: Skip orphaned job types

                IJTXJobType3 jobType = allJobTypes.get_Item(i) as IJTXJobType3;
                if (jobType.AOIMap != null)
                {
                    usedItems[jobType.AOIMap.Name] = jobType.AOIMap.ID;
                }
                if (jobType.JobMap != null)
                {
                    usedItems[jobType.JobMap.Name] = jobType.JobMap.ID;
                }
            }

            // If necessary, find the map types launched by custom steps.  Look for
            // the "/mxd:" argument as an identifier.
            IJTXStepTypeSet allStepTypes = wmxDb.ConfigurationManager.StepTypes;
            for (int i = 0; i < allStepTypes.Count; i++)
            {
                IJTXStepType2 stepType = allStepTypes.get_Item(i) as IJTXStepType2;

                // Skip orphaned step types
                if (m_unusedStepTypes.Keys.Contains(stepType.ID))
                {
                    continue;
                }

                for (int j = 0; j < stepType.Arguments.Length; j++)
                {
                    string stepArg = stepType.Arguments[j].ToString();
                    if (stepArg.StartsWith(C_MAP_DOC_FLAG))
                    {
                        string suffix = stepArg.Substring(C_MAP_DOC_FLAG.Length);
                        suffix = suffix.Trim(new char[] { '"' });
                        if (allMapNames.Keys.Contains(suffix))
                        {
                            usedItems[suffix] = allMapNames[suffix];
                        }
                    }
                }
            }

            // Add in the map document that's used as the template map document
            // (if one exists)
            IJTXConfigurationProperties configProps = this.WmxDatabase.ConfigurationManager as IJTXConfigurationProperties;
            string mapIdStr = configProps.GetProperty(Constants.JTX_PROPERTY_MAPVIEW_MAP_GUID);
            if (mapIdStr != null && !mapIdStr.Equals(string.Empty))
            {
                for (int i = 0; i < allMaps.Count; i++)
                {
                    IJTXMap tempMap = allMaps.get_Item(i);
                    IJTXIdentifier tempMapId = tempMap as IJTXIdentifier;
                    if (tempMapId.GUID.Equals(mapIdStr))
                    {
                        usedItems[tempMap.Name] = tempMap.ID;
                        break;
                    }
                }
            }

            // Loop over all the map documents in the DB, looking for anything
            // that we didn't identify as "in use"
            foreach (string name in allMapNames.Keys)
            {
                if (!usedItems.ContainsKey(name))
                {
                    m_unusedMapDocs[name] = allMapNames[name];
                }
            }

            return m_unusedMapDocs.Count;
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

                // Optional parameter indicating whether the orphaned items should truly
                // be deleted from the Workflow Manager database, or whether these changes
                // should merely be listed.
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_PREVIEW_DELETES);
                cvDomain.AddCode(GpFalse, C_OPT_PERFORM_DELETES);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_DOT_PREVIEW_CHANGES,
                    C_PARAM_PREVIEW_CHANGES,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_PREVIEW_CHANGES));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Optional parameter indicating what kinds of items should
                // be removed from the database
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddStringCode(C_OPT_CLEAN_MAP_DOCS, C_OPT_CLEAN_MAP_DOCS);
                cvDomain.AddStringCode(C_OPT_CLEAN_PRIORITIES, C_OPT_CLEAN_PRIORITIES);
                cvDomain.AddStringCode(C_OPT_CLEAN_STATUS_TYPES, C_OPT_CLEAN_STATUS_TYPES);
                cvDomain.AddStringCode(C_OPT_CLEAN_STEP_TYPES, C_OPT_CLEAN_STEP_TYPES);
                cvDomain.AddStringCode(C_OPT_CLEAN_TA_WORKBOOKS, C_OPT_CLEAN_TA_WORKBOOKS);
                cvDomain.AddStringCode(C_OPT_CLEAN_USERS, C_OPT_CLEAN_USERS);
                cvDomain.AddStringCode(C_OPT_CLEAN_WORKFLOWS, C_OPT_CLEAN_WORKFLOWS);

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

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Output parameter indicating how many total items were deleted
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_DOT_NUM_ITEMS_DELETED,
                    C_PARAM_NUM_ITEMS_DELETED,
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

            try
            {
                // Ensure that the current user has admin access to the current Workflow Manager DB
                if (!CurrentUserIsWmxAdministrator())
                {
                    throw new WmauException(WmauErrorCodes.C_USER_NOT_ADMIN_ERROR);
                }

                // Indicate if changes are not actually being made
                string actionStr = "  Deleting";
                if (m_previewChanges)
                {
                    msgs.AddMessage("PREVIEWING CHANGES ONLY; no changes will be made");
                    actionStr = "  Found";
                }

                IJTXConfiguration3 configMgr = WmxDatabase.ConfigurationManager as IJTXConfiguration3;
                IJTXConfigurationEdit2 configEdit = WmxDatabase.ConfigurationManager as IJTXConfigurationEdit2;

                // Find all of the orphans in the database
                msgs.AddMessage("Searching for orphaned items...");
                int orphanCount = UpdateAllOrphans();
                msgs.AddMessage("Found " + orphanCount.ToString() + " total orphaned items");

                // If requested, delete any workflows first
                if (m_cleanWorkflows)
                {
                    List<int> unusedWorkflowIds = m_unusedWorkflows.Keys.ToList();
                    unusedWorkflowIds.Sort();
                    foreach (int id in unusedWorkflowIds)
                    {
                        msgs.AddMessage(actionStr + " workflow " + id.ToString() + " (" + m_unusedWorkflows[id] + ")");
                        if (!this.m_previewChanges)
                        {
                            configMgr.DeleteWorkflow(id);
                        }
                    }
                }

                // If requested, delete any step types
                if (m_cleanStepTypes)
                {
                    List<int> unusedStepTypeIds = m_unusedStepTypes.Keys.ToList();
                    unusedStepTypeIds.Sort();
                    foreach (int stepTypeId in unusedStepTypeIds)
                    {
                        msgs.AddMessage(actionStr + " step type " + stepTypeId.ToString() + " (" + m_unusedStepTypes[stepTypeId] + ")");
                        if (!this.m_previewChanges)
                        {
                            configEdit.DeleteStepType(stepTypeId);
                        }
                    }
                }

                // If requested, delete any status types
                if (m_cleanStatusTypes)
                {
                    List<int> unusedStatusTypeIds = m_unusedStatusTypes.Keys.ToList();
                    unusedStatusTypeIds.Sort();
                    foreach (int statusTypeId in unusedStatusTypeIds)
                    {
                        msgs.AddMessage(actionStr + " status type " + statusTypeId.ToString() + " (" + m_unusedStatusTypes[statusTypeId] + ")");
                        if (!this.m_previewChanges)
                        {
                            configEdit.DeleteStatus(statusTypeId);
                        }
                    }
                }

                // If requested, delete any priority types
                if (m_cleanPriorities)
                {
                    List<int> unusedPriorityTypeIds = m_unusedPriorities.Keys.ToList();
                    unusedPriorityTypeIds.Sort();
                    foreach (int priority in unusedPriorityTypeIds)
                    {
                        msgs.AddMessage(actionStr + " priority " + priority.ToString() + " (" + m_unusedPriorities[priority] + ")");
                        if (!m_previewChanges)
                        {
                            configEdit.DeletePriority(priority);
                        }
                    }
                }

                // If requested, delete any unused Task Assistant workbooks
                if (m_cleanTaWorkbooks)
                {
                    List<string> unusedTaWorkbookNames = m_unusedTaWorkbooks.Keys.ToList();
                    unusedTaWorkbookNames.Sort();
                    foreach (string workbookName in unusedTaWorkbookNames)
                    {
                        msgs.AddMessage(actionStr + " workbook " + workbookName);
                        if (!m_previewChanges)
                        {
                            configMgr.RemoveTaskAssistantWorkflowRecord(workbookName);
                        }
                    }
                }

                // If requested, delete any unused users
                if (m_cleanUsers)
                {
                    List<string> unusedUserNames = m_unusedUsers.Keys.ToList();
                    unusedUserNames.Sort();
                    foreach (string user in unusedUserNames)
                    {
                        msgs.AddMessage(actionStr + " user " + user + " (" + m_unusedUsers[user] + ")");
                        if (!m_previewChanges)
                        {
                            configEdit.DeleteUser(user);
                        }
                    }
                }

                // If requested, delete any unused map documents
                if (m_cleanMapDocs)
                {
                    List<string> unusedMapDocs = m_unusedMapDocs.Keys.ToList();
                    unusedMapDocs.Sort();
                    foreach (string mapName in unusedMapDocs)
                    {
                        msgs.AddMessage(actionStr + " map document " + mapName + " (" + m_unusedMapDocs[mapName].ToString() + ")");
                        if (!m_previewChanges)
                        {
                            configMgr.DeleteJTXMap(m_unusedMapDocs[mapName]);
                        }
                    }
                }

                // Set the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_NUM_ITEMS_DELETED);
                IGPLong outValue = new GPLongClass();
                outValue.Value = orphanCount;
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
        }

    }
}
