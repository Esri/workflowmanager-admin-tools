using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;


namespace WorkflowManagerAdministrationUtilities.Common
{
    /// <summary>
    /// A utility class that contains functions used to build GP parameter domains
    /// based on the contents of a Workflow Manager database.
    /// </summary>
    class WmauGpDomainBuilder
    {
        #region Private helper functions
        /// <summary>
        /// Helper function to recursively extract the queries stored in a job query
        /// container and add them to a list.
        /// </summary>
        /// <param name="container">The container in which the queries are stored</param>
        /// <param name="prefix">
        /// A "prefix" string to prepend to the query names before they're inserted into
        /// the list
        /// </param>
        /// <param name="queryList">
        /// A sorted list into which the extracted queries will be added.  This function
        /// is intended to be called recursively in the case of job queries contained
        /// within subcontainers, so the list is *not* cleared by the function.
        /// </param>
        private static void AddQueriesFromContainer(
            IJTXJobQueryContainer container,
            string prefix,
            SortedList<string, string> queryList)
        {
            // Add the queries from the container at the current level
            IJTXJobQuerySet querySet = container.Queries;
            for (int i = 0; i < querySet.Count; i++)
            {
                IJTXJobQuery query = querySet.get_Item(i) as IJTXJobQuery;
                queryList.Add(prefix + query.Name, null);
            }

            // Iterate through each of the subcontainers and add them as well
            IJTXJobQueryContainerSet subcontainers = container.SubContainers;
            for (int i = 0; i < subcontainers.Count; i++)
            {
                IJTXJobQueryContainer tempContainer = subcontainers.get_Item(i);
                WmauGpDomainBuilder.AddQueriesFromContainer(
                    tempContainer,
                    prefix + tempContainer.Name + Properties.Resources.CONST_JOB_QUERY_SEP,
                    queryList);
            }
        }
        #endregion

        /// <summary>
        /// Builds a domain consisting of the names of the system groups to which the current
        /// user can assign a job.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildAssignableGroupsDomain(IJTXDatabase3 wmxDb)
        {
            return WmauGpDomainBuilder.BuildAssignableGroupsDomain(
                wmxDb,
                ESRI.ArcGIS.JTXUI.ConfigurationCache.GetCurrentSystemUser(ESRI.ArcGIS.JTXUI.ConfigurationCache.UseUserDomain),
                null);
        }

        /// <summary>
        /// Builds a domain consisting of the names of the system groups to which a
        /// user can assign a job.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="username">The name of the user to be tested</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildAssignableGroupsDomain(IJTXDatabase3 wmxDb, string username)
        {
            return WmauGpDomainBuilder.BuildAssignableGroupsDomain(wmxDb, username, null);
        }

        /// <summary>
        /// Builds a domain consisting of the names of the system groups to which the current
        /// user can assign a job.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="extraValues">An array of string values to be added to the list</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildAssignableGroupsDomain(IJTXDatabase3 wmxDb, string[] extraValues)
        {
            return WmauGpDomainBuilder.BuildAssignableGroupsDomain(
                wmxDb,
                ESRI.ArcGIS.JTXUI.ConfigurationCache.GetCurrentSystemUser(ESRI.ArcGIS.JTXUI.ConfigurationCache.UseUserDomain),
                extraValues);
        }

        /// <summary>
        /// Builds a domain consisting of the names of the system groups to which a
        /// user can assign a job.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="username">The name of the user to be tested</param>
        /// <param name="extraValues">An array of string values to be added to the list</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildAssignableGroupsDomain(IJTXDatabase3 wmxDb, string username, string[] extraValues)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();
            string[] eligibleGroups = null;

            // Only proceed if the user exists in the Workflow Manager database
            IJTXUser3 user = wmxDb.ConfigurationManager.GetUser(username) as IJTXUser3;
            if (user == null)
            {
                return domain as IGPDomain;
            }

            // The groups to which this user can assign jobs are based on several
            // different permissions.  Check these permissions, in order from least
            // restrictive to most restrictive.
            if (user.HasNamedPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_ASSIGN_ANY_JOB))
            {
                int numGroups = wmxDb.ConfigurationManager.UserGroups.Count;
                eligibleGroups = new string[numGroups];
                for (int i = 0; i < numGroups; i++)
                {
                    eligibleGroups[i] = wmxDb.ConfigurationManager.UserGroups.get_Item(i).Name;
                }
            }
            else if (user.HasNamedPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_GROUP_JOB_ASSIGN))
            {
                eligibleGroups = new string[user.Groups.Count];
                for (int i = 0; i < user.Groups.Count; i++)
                {
                    eligibleGroups[i] = user.Groups.get_Item(i).Name;
                }
            }
            else
            {
                eligibleGroups = new string[0];
            }

            // Sort the types first
            SortedList<string, string> sortedValues = new SortedList<string, string>();
            for (int i = 0; i < eligibleGroups.Length; i++)
            {
                sortedValues.Add(eligibleGroups[i], null);
            }

            // Add the extra values, if any
            if (extraValues != null)
            {
                foreach (string s in extraValues)
                {
                    sortedValues.Add(s, null);
                }
            }

            // Add the sorted types to the domain
            foreach (string value in sortedValues.Keys)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain containing the usernames of the users to whom the current
        /// user can assign jobs.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns></returns>
        public static IGPDomain BuildAssignableUsersDomain(IJTXDatabase3 wmxDb)
        {
            return Common.WmauGpDomainBuilder.BuildAssignableUsersDomain(
                wmxDb,
                ESRI.ArcGIS.JTXUI.ConfigurationCache.GetCurrentSystemUser(ESRI.ArcGIS.JTXUI.ConfigurationCache.UseUserDomain),
                null);
        }

        /// <summary>
        /// Builds a domain containing the usernames of the users to whom a
        /// user can assign jobs.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="username">The name of the user to be tested</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildAssignableUsersDomain(IJTXDatabase3 wmxDb, string username)
        {
            return Common.WmauGpDomainBuilder.BuildAssignableUsersDomain(wmxDb, username, null);
        }

        /// <summary>
        /// Builds a domain containing the usernames of the users to whom the current
        /// user can assign jobs.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="extraValues">An array of string values to be added to the list</param>
        /// <returns>A coded value domain of strings</returns>
        public static IGPDomain BuildAssignableUsersDomain(IJTXDatabase3 wmxDb, string[] extraValues)
        {
            return Common.WmauGpDomainBuilder.BuildAssignableUsersDomain(
                wmxDb,
                ESRI.ArcGIS.JTXUI.ConfigurationCache.GetCurrentSystemUser(ESRI.ArcGIS.JTXUI.ConfigurationCache.UseUserDomain),
                extraValues);
        }

        /// <summary>
        /// Builds a domain containing the usernames of the users to whom a
        /// user can assign jobs.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="username">The name of the user to be tested</param>
        /// <param name="extraValues">An array of string values to be added to the list</param>
        /// <returns>A coded value domain of strings</returns>
        public static IGPDomain BuildAssignableUsersDomain(IJTXDatabase3 wmxDb, string username, string[] extraValues)
        {
            IGPCodedValueDomain domain = null;

            // Only proceed if the user exists in the Workflow Manager database
            IJTXUser3 user = wmxDb.ConfigurationManager.GetUser(username) as IJTXUser3;
            if (user == null)
            {
                return domain as IGPDomain;
            }

            // Case 1: If the user can assign the job to anyone, then
            // just use the "all users" list
            if (user.HasNamedPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_ASSIGN_ANY_JOB))
            {
                domain = Common.WmauGpDomainBuilder.BuildUsersDomain(wmxDb, extraValues) as IGPCodedValueDomain;
            }
            else
            {
                domain = new GPCodedValueDomainClass();
                string[] eligibleUsers = null;

                // Case 2: The user can assign jobs to anyone within any of their groups
                if (user.HasNamedPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_GROUP_JOB_ASSIGN))
                {
                    HashSet<string> usernames = new HashSet<string>();
                    IJTXUserGroupSet groups = user.Groups;

                    for (int i = 0; i < groups.Count; i++)
                    {
                        IJTXUserGroup group = groups.get_Item(i);
                        for (int j = 0; j < group.Users.Count; j++)
                        {
                            usernames.Add(group.Users.get_Item(j).UserName);
                        }
                    }

                    eligibleUsers = usernames.ToArray();
                }
                // Case 3: The user can assign jobs to themselves
                else if (user.HasNamedPrivilege(ESRI.ArcGIS.JTX.Utilities.Constants.PRIV_INDIVIDUAL_JOB_ASSIGN))
                {
                    eligibleUsers = new string[] { username };
                }
                // Case 4: The user can't assign jobs to anyone
                else
                {
                    eligibleUsers = new string[0];
                }

                // Sort the types first
                SortedList<string, string> sortedValues = new SortedList<string, string>();
                for (int i = 0; i < eligibleUsers.Length; i++)
                {
                    sortedValues.Add(eligibleUsers[i], null);
                }

                // Add the extra values, if any
                if (extraValues != null)
                {
                    foreach (string s in extraValues)
                    {
                        sortedValues.Add(s, null);
                    }
                }

                // Add the sorted types to the domain
                foreach (string value in sortedValues.Keys)
                {
                    IGPValue tempGpVal = new GPStringClass();
                    tempGpVal.SetAsText(value);
                    domain.AddCode(tempGpVal, value);
                }
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Set up a coded value domain containing the names of all the spatial notifications
        /// (a.k.a. "change rules") currently in the database.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildChangeRulesDomain(IJTXDatabase3 wmxDb)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();
            SortedList<string, string> sortedValues = new SortedList<string, string>();

            IJTXChangeRuleSet allChangeRules = wmxDb.SpatialNotificationManager.ChangeRules;

            // Sort the types first
            for (int i = 0; i < allChangeRules.Count; i++)
            {
                IJTXChangeRule tempRule = allChangeRules.get_Item(i);
                sortedValues.Add(tempRule.Name, null);
            }

            // Add the sorted types to the domain
            foreach (string value in sortedValues.Keys)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain consisting of the names of the existing notification types
        /// in the system.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildEmailNotificationDomain(IJTXDatabase3 wmxDb)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();
            SortedList<string, string> sortedValues = new SortedList<string, string>();

            IJTXNotificationConfiguration notificationConfig = wmxDb.ConfigurationManager as IJTXNotificationConfiguration;
            IJTXNotificationTypeSet allNotifications = notificationConfig.NotificationTypes;

            // Sort the types first
            for (int i = 0; i < allNotifications.Count; i++)
            {
                sortedValues.Add(allNotifications.get_Item(i).Type, null);
            }

            // Add the sorted types to the domain
            foreach (string value in sortedValues.Keys)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain consisting of the names of the user groups
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildGroupsDomain(IJTXDatabase3 wmxDb)
        {
            return WmauGpDomainBuilder.BuildGroupsDomain(wmxDb, null);
        }
        
        /// <summary>
        /// Builds a domain consisting of the names of the user groups
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="extraValues">An array of string values to be added to the list</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildGroupsDomain(IJTXDatabase3 wmxDb, string[] extraValues)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();

            // Sort the types first
            SortedList<string, string> sortedValues = new SortedList<string, string>();
            IJTXUserGroupSet groups = wmxDb.ConfigurationManager.UserGroups;
            for (int i = 0; i < groups.Count; i++)
            {
                IJTXUserGroup group = groups.get_Item(i);
                sortedValues.Add(group.Name, null);
            }

            // Add the extra values, if any
            if (extraValues != null)
            {
                foreach (string s in extraValues)
                {
                    sortedValues.Add(s, null);
                }
            }

            // Add the sorted types to the domain
            foreach (string value in sortedValues.Keys)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain consisting of the IDs for every job in the database.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildJobIdDomain(IJTXDatabase3 wmxDb)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();
            int[] allJobs = wmxDb.JobManager.GetAllJobIDs();
            System.Array.Sort(allJobs);
            foreach (int job in allJobs)
            {
                IGPValue tempGpVal = new GPLongClass();
                tempGpVal.SetAsText(job.ToString());
                domain.AddCode(tempGpVal, job.ToString());
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain consisting of the available job queries in the system.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildJobQueryDomain(IJTXDatabase3 wmxDb)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();

            IJTXConfiguration3 configMgr = wmxDb.ConfigurationManager as IJTXConfiguration3;
            IJTXJobQueryContainer publicQueriesContainer = configMgr.GetPublicQueryContainer();

            // Sort the queries first
            SortedList<string, string> sortedValues = new SortedList<string, string>();
            WmauGpDomainBuilder.AddQueriesFromContainer(publicQueriesContainer, string.Empty, sortedValues);

            // Add the sorted types to the domain
            foreach (string value in sortedValues.Keys)
            {
                domain.AddStringCode(value, value);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain from the names of the job types in the system
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildJobTypeDomain(IJTXDatabase3 wmxDb)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();

            // Sort the types first
            IJTXJobTypeSet allValues = wmxDb.ConfigurationManager.JobTypes;
            SortedList<string, string> sortedValues = new SortedList<string, string>();
            for (int i = 0; i < allValues.Count; i++)
            {
                sortedValues.Add(allValues.get_Item(i).Name, null);
            }

            // Add the sorted types to the domain
            foreach (string value in sortedValues.Keys)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain containing the names of all the map documents embedded
        /// in the database
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildMapDocumentDomain(IJTXDatabase3 wmxDb)
        {
            IGPCodedValueDomain mxdNames = new GPCodedValueDomainClass();
            SortedList<string, string> mapDocumentNames = new SortedList<string, string>();

            IJTXConfiguration3 configMgr = wmxDb.ConfigurationManager as IJTXConfiguration3;
            IJTXMapSet maps = configMgr.JTXMaps;
            for (int i = 0; i < maps.Count; i++)
            {
                IJTXMap map = maps.get_Item(i);
                mapDocumentNames.Add(map.Name, null);
            }

            foreach (string mapDocName in mapDocumentNames.Keys)
            {
                mxdNames.AddStringCode(mapDocName, mapDocName);
            }

            return mxdNames as IGPDomain;
        }

        /// <summary>
        /// Builds a domain consisting of the IDs of every job in the database that
        /// has not already been closed.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildNonClosedJobIdDomain(IJTXDatabase3 wmxDb)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();
            
            // Set up a query filter to return only those jobs that are not closed
            IQueryFilter queryFilter = new QueryFilterClass();
            queryFilter.WhereClause =
                ESRI.ArcGIS.JTX.Utilities.Constants.FIELD_STAGE + " <> '" +
                ((int)jtxJobStage.jtxJobStageClosed).ToString() + "'";

            IJTXJobSet nonClosedJobs = wmxDb.JobManager.GetJobsByQuery(queryFilter);

            // Iterate through this job list, sorting the IDs
            SortedList<int, string> sortedJobIds = new SortedList<int, string>();
            for (int i = 0; i < nonClosedJobs.Count; i++)
            {
                IJTXJob3 job = nonClosedJobs.get_Item(i) as IJTXJob3;
                sortedJobIds[job.ID] = null;
            }

            // Build a GP domain from the sorted job IDs.
            foreach (int id in sortedJobIds.Keys)
            {
                IGPValue tempGpVal = new GPLongClass();
                tempGpVal.SetAsText(id.ToString());
                domain.AddCode(tempGpVal, id.ToString());
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain of the priority strings in the system
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildPriorityDomain(IJTXDatabase3 wmxDb)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();

            // Sort the types first
            IJTXPrioritySet allValues = wmxDb.ConfigurationManager.Priorities;
            SortedList<int, string> sortedValues = new SortedList<int, string>();
            for (int i = 0; i < allValues.Count; i++)
            {
                IJTXPriority temp = allValues.get_Item(i);
                sortedValues.Add(temp.Value, temp.Name);
            }

            // Since the highest priority elements are those with the largest number,
            // reverse the order of the list so that these priorities show up first
            IEnumerable<string> valueList = sortedValues.Values.Reverse();

            // Add the sorted types to the domain
            foreach (string value in valueList)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain consisting of the Workflow Manager privileges
        /// </summary>
        /// <returns>A coded value domain of strings</returns>
        public static IGPDomain BuildPrivilegesDomain(IJTXDatabase3 wmxDb)
        {
            return WmauGpDomainBuilder.BuildPrivilegesDomain(wmxDb);
        }

        /// <summary>
        /// Builds a domain consisting of the Workflow Manager privileges
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="extraValues">An array of string values to be added to the list</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildPrivilegesDomain(IJTXDatabase3 wmxDb, string[] extraValues)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();

            // Sort the types first
            SortedList<string, string> sortedValues = new SortedList<string, string>();
            IJTXPrivilegeSet privileges = wmxDb.ConfigurationManager.Privileges;
            for (int i = 0; i < privileges.Count; i++)
            {
                IJTXPrivilege2 priv = privileges.get_Item(i) as IJTXPrivilege2;
                sortedValues.Add(priv.Name, null);
            }

            // Add the extra values, if any
            if (extraValues != null)
            {
                foreach (string s in extraValues)
                {
                    sortedValues.Add(s, null);
                }
            }

            // Add the sorted types to the domain
            foreach (string value in sortedValues.Keys)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain containing the names of all the task assistant
        /// workbooks embedded in the database
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildTamWorkbookDomain(IJTXDatabase3 wmxDb)
        {
            IGPCodedValueDomain tamNames = new GPCodedValueDomainClass();
            SortedList<string, string> sortedTamNames = new SortedList<string, string>();

            IJTXConfiguration3 configMgr = wmxDb.ConfigurationManager as IJTXConfiguration3;
            IJTXTaskAssistantWorkflowRecordSet tamWorkbooks = configMgr.TaskAssistantWorkflowRecords;
            for (int i = 0; i < tamWorkbooks.Count; i++)
            {
                IJTXTaskAssistantWorkflowRecord tamWorkbook = tamWorkbooks.get_Item(i);
                sortedTamNames.Add(tamWorkbook.Alias, null);
            }

            foreach (string tamName in sortedTamNames.Keys)
            {
                tamNames.AddStringCode(tamName, tamName);
            }

            return tamNames as IGPDomain;
        }

        /// <summary>
        /// Builds a domain consisting of all of the usernames in the system
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildUsersDomain(IJTXDatabase3 wmxDb)
        {
            return WmauGpDomainBuilder.BuildUsersDomain(wmxDb, null);
        }
        
        /// <summary>
        /// Builds a domain consisting of all the usernames in the system
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="extraValues">An array of string values to be added to the list</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildUsersDomain(IJTXDatabase3 wmxDb, string[] extraValues)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();

            // Sort the types first
            IJTXUserSet allValues = wmxDb.ConfigurationManager.Users;
            SortedList<string, string> sortedValues = new SortedList<string, string>();
            for (int i = 0; i < allValues.Count; i++)
            {
                sortedValues.Add(allValues.get_Item(i).UserName, null);
            }

            // Add the extra values, if any
            if (extraValues != null)
            {
                foreach (string s in extraValues)
                {
                    sortedValues.Add(s, null);
                }
            }

            // Add the sorted types to the domain
            foreach (string value in sortedValues.Keys)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain of the version names for a workspace
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="workspaceName">The human-readable name of the workspace whose versions to look up</param>
        /// <param name="extraValues">An array of string values to be added to the list</param>
        /// <returns>A coded value domain of strings</returns>
        public static IGPDomain BuildVersionsDomain(IJTXDatabase3 wmxDb, string workspaceName, string[] extraValues)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();

            try
            {
                // Get all of the public versions connected to this workspace
                string workspaceId = Common.WmauHelperFunctions.LookupWorkspaceId(wmxDb, workspaceName);
                IWorkspace workspace = wmxDb.GetDataWorkspace(workspaceId, null);
                IVersionedWorkspace3 versionedWorkspace = workspace as IVersionedWorkspace3;
                IEnumVersionInfo allValues = versionedWorkspace.Versions;

                // Sort the types first
                SortedList<string, string> sortedValues = new SortedList<string, string>();
                IVersionInfo version;
                while ((version = allValues.Next()) != null)
                {
                    sortedValues.Add(version.VersionName, null);
                }

                // Add the extra values, if any
                if (extraValues != null)
                {
                    foreach (string s in extraValues)
                    {
                        sortedValues.Add(s, null);
                    }
                }

                // Add the sorted types to the domain
                foreach (string value in sortedValues.Keys)
                {
                    IGPValue tempGpVal = new GPStringClass();
                    tempGpVal.SetAsText(value);
                    domain.AddCode(tempGpVal, value);
                }
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                // If we run into an exception, send word up the chain
                throw new WmauException(WmauErrorCodes.C_VERSION_LOOKUP_ERROR, comEx);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Builds a domain of the data workspace names in the system
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildWorkspaceDomain(IJTXDatabase3 wmxDb)
        {
            return BuildWorkspaceDomain(wmxDb, null);
        }

        /// <summary>
        /// Builds a domain of the data workspace names in the system
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="extraValues">An array of string values to be added to the list</param>
        /// <returns>A coded value domain as an IGPDomain</returns>
        public static IGPDomain BuildWorkspaceDomain(IJTXDatabase3 wmxDb, string[] extraValues)
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();
            SortedList<string, string> sortedValues = new SortedList<string, string>();

            // Workflow Manager intentionally caches the data workspaces in the system.  To ensure
            // that we have the most current list of data workspaces, invalidate this cache
            // before attempting to retrieve the list from the system.
            wmxDb.InvalidateDataWorkspaceNames();

            // Sort the types first
            IJTXDataWorkspaceNameSet allValues = wmxDb.GetDataWorkspaceNames(null);
            for (int i = 0; i < allValues.Count; i++)
            {
                IJTXDataWorkspaceName ws = allValues.get_Item(i);
                sortedValues.Add(ws.Name, null);
            }

            // Add the extra values, if any
            if (extraValues != null)
            {
                foreach (string s in extraValues)
                {
                    sortedValues.Add(s, null);
                }
            }

            // Add the sorted types to the domain
            
            foreach (string value in sortedValues.Keys)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
        }

    }
}
