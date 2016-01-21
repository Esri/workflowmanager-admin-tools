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
    class WmauHelperFunctions
    {

        /// <summary>
        /// Finds the first table matching a specified name in a workspace, returning the
        /// fully qualified name for that table
        /// </summary>
        /// <param name="unqualifiedName">Unqualified name of the table</param>
        /// <param name="workspace">The workspace in which the table is located</param>
        /// <returns>
        /// The fully-qualified name of the first matching table found; null if
        /// none was found
        /// </returns>
        public static string GetQualifiedTableName(string unqualifiedName, IWorkspace workspace)
        {
            string tableNameStr = null;
            IEnumDatasetName allTables = workspace.get_DatasetNames(esriDatasetType.esriDTTable);

            // Get the name of the correct table from the workspace so
            // that the table doesn't have to be owned by the connecting user.
            IDatasetName tableName = null;
            while ((tableName = allTables.Next()) != null)
            {
                if (tableName.Name.EndsWith(unqualifiedName, StringComparison.CurrentCultureIgnoreCase))
                {
                    tableNameStr = tableName.Name;
                    break;
                }
            }

            return tableNameStr;
        }

        /// <summary>
        /// Given the human-readable name of a data workspace, this function returns the
        /// unique ID string used by Workflow Manager to identify this workspace connection.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="wsName">
        /// The name of the data workspace whose ID is to be retrieved
        /// </param>
        /// <returns>
        /// The ID string for the specified data workspace; returns the empty string if
        /// no matching workspace can be found.
        /// </returns>
        public static string LookupWorkspaceId(IJTXDatabase3 wmxDb, string wsName)
        {
            string id = string.Empty;

            // Workflow Manager intentionally caches the data workspaces in the system.  To ensure
            // that we have the most current list of data workspaces, invalidate this cache
            // before attempting to retrieve the list from the system.
            wmxDb.InvalidateDataWorkspaceNames();

            // Get the workspace list from the database
            IJTXDataWorkspaceNameSet allValues = wmxDb.GetDataWorkspaceNames(null);
            
            // Search for the workspace ID with the matching name
            for (int i = 0; i < allValues.Count; i++)
            {
                if (allValues.get_Item(i).Name.Equals(wsName))
                {
                    id = allValues.get_Item(i).DatabaseID;
                    break;
                }
            }

            return id;
        }

        /// <summary>
        /// Given the human-readable name of a data workspace, this function returns the
        /// IJTXDataWorkspaceName object associated with this workspace connection.
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="wsName">
        /// The name of the data workspace to be looked up
        /// </param>
        /// <returns>
        /// The workspace name object for the specified data workspace; returns null if
        /// no matching workspace can be found.
        /// </returns>
        public static IJTXDataWorkspaceName LookupWorkspaceNameObj(IJTXDatabase3 wmxDb, string wsName)
        {
            IJTXDataWorkspaceName wsNameObj = null;

            // Workflow Manager intentionally caches the data workspaces in the system.  To ensure
            // that we have the most current list of data workspaces, invalidate this cache
            // before attempting to retrieve the list from the system.
            wmxDb.InvalidateDataWorkspaceNames();

            // Get the workspace list from the database
            IJTXDataWorkspaceNameSet allValues = wmxDb.GetDataWorkspaceNames(null);

            // Search for the workspace ID with the matching name
            for (int i = 0; i < allValues.Count; i++)
            {
                if (allValues.get_Item(i).Name.Equals(wsName))
                {
                    wsNameObj = allValues.get_Item(i);
                    break;
                }
            }

            return wsNameObj;
        }

        /// <summary>
        /// Helper function to send a notification
        /// </summary>
        /// <param name="notificationName">The type (name) of the notification to send</param>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="job">The job for which to send the notification</param>
        public static void SendNotification(string notificationName, IJTXDatabase3 wmxDb, IJTXJob job)
        {
            ESRI.ArcGIS.JTX.Utilities.JTXUtilities.SendNotification(notificationName, wmxDb, job, null);
        }

        /// <summary>
        /// Helper function to update the status of a job
        /// </summary>
        /// <param name="wmxDb">A reference to the active Workflow Manager database</param>
        /// <param name="job">The job whose status is to be updated</param>
        public static void UpdateJobStatus(IJTXDatabase wmxDb, IJTXJob3 job)
        {
            // NOTE: The ConfigurationCache must be initialized before calling this function
            // (this is now handled elsewhere).
            ESRI.ArcGIS.JTXUI.JobUtilities.UpdateStatusOfJob(wmxDb, job, false);
        }
    }
}
