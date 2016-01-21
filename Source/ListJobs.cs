using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;
using ESRI.ArcGIS.JTX.Utilities;


namespace WorkflowManagerAdministrationUtilities
{
    class ListJobs : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_JOBS_TABLE = "in_table_jobsTable";
        private const string C_PARAM_SQL_QUERY_FILTER = "in_string_sqlQueryFilter";
        private const string C_PARAM_JOB_ID_LIST = "out_intlist_jobIds";
        #endregion

        #region MemberVariables
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ListJobs"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_LIST_JOBS; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_JOB_UTILS; } }
        #endregion

        #region Private helper functions
        /// <summary>
        /// Gets a list of the jobs stored in the current WMX database that match the specified
        /// query filter.
        /// </summary>
        /// <returns>A sorted list of the job IDs</returns>
        private SortedList<int, string> ListJobsInDatabase(string filter)
        {
            SortedList<int, string> jobIds = new SortedList<int, string>();

            // Build up a query that will return the job IDs filtered by the provided query string
            IQueryFilter query = new QueryFilterClass();
            IQueryFilterDefinition queryDef = query as IQueryFilterDefinition;
            query.WhereClause = filter;
            queryDef.PostfixClause = "ORDER BY " + Constants.FIELD_JOBID;

            IFeatureWorkspace featureWorkspace = this.WmxDatabase.JTXWorkspace as IFeatureWorkspace;

            // Declare some of these ComReleaser objects to help ensure that cursors, etc., are
            // immediately released after they go out of scope.
            using (ComReleaser cr1 = new ComReleaser(), cr2 = new ComReleaser())
            {
                // Get the name of the correct table from the jobs workspace, so
                // that the table doesn't have to be owned by the connecting user.
                string tableName = Common.WmauHelperFunctions.GetQualifiedTableName(Constants.JTX_TABLE_JTX_JOBS_TABLE, this.WmxDatabase.JTXWorkspace);

                ITable jobsTable = featureWorkspace.OpenTable(tableName);
                cr1.ManageLifetime(jobsTable);
                ICursor searchCursor = jobsTable.Search(query, true);
                cr2.ManageLifetime(searchCursor);

                // Store the ID and name of each job matching this query
                int idIndex = jobsTable.FindField(Constants.FIELD_JOBID);
                int nameIndex = jobsTable.FindField(Constants.FIELD_JOBNAME);
                IRow row = null;
                while ((row = searchCursor.NextRow()) != null)
                {
                    string targetIdxStr = row.get_Value(idIndex).ToString();
                    int targetIdx = -1;
                    try
                    {
                        // NOTE: Have seen cases where the JTX_JOBS table contained a row with
                        // nothing but null attributes; since this function accesses the table
                        // directly, try to protect against this error.
                        targetIdx = int.Parse(targetIdxStr);
                    }
                    catch (Exception ex)
                    {
                        throw new WmauException(WmauErrorCodes.C_JOB_ID_PARSE_ERROR, ex);
                    }
                    string targetValStr = row.get_Value(nameIndex).ToString();
                    jobIds[targetIdx] = targetValStr;
                }
            }

            return jobIds;
        }

        /// <summary>
        /// Updates the internal values used by this tool based on the parameters from an input array
        /// </summary>
        /// <param name="paramValues"></param>
        protected override void ExtractParameters(IArray paramValues)
        {
            // Get the values for any parameters common to all GP tools
            ExtractParametersCommon(paramValues);
        }
        #endregion

        /// <summary>
        /// Required by IGPFunction2 interface.
        /// </summary>
        public override IArray ParameterInfo
        {
            get
            {
                // Use a temporary array so that m_parameters is not set in the event
                // that an exception is thrown before the array is completely initialized
                IArray tempArray = new ArrayClass();

                // Dummy "table" parameter, used to populate the SQL query dialog
                GPTableViewClass tableView = new GPTableViewClass();
                IGPParameterEdit3 jobsTableLocation = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    String.Format(Properties.Resources.DESC_LJ_JOBS_TABLE_1, Constants.JTX_TABLE_JTX_JOBS_TABLE),
                    C_PARAM_JOBS_TABLE,
                    tableView.DataType,
                    tableView as IGPValue);
                tempArray.Add(jobsTableLocation);

                // SQL query filter
                GPSQLExpressionClass expression = new GPSQLExpressionClass();
                IGPParameterEdit3 sqlQuery = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    String.Format(Properties.Resources.DESC_LJ_SQL_QUERY_FILTER_1, Constants.JTX_TABLE_JTX_JOBS_TABLE),
                    C_PARAM_SQL_QUERY_FILTER,
                    expression.DataType,
                    expression as IGPValue);
                tempArray.Add(sqlQuery);

                // Add a dependency between the jobs table parameter and the SQL query
                sqlQuery.AddDependency((jobsTableLocation as IGPParameter3).Name);

                // Parameter for specifying the WMX database
                tempArray.Add(BuildWmxDbParameter());

                // Job ID workbook list
                IGPMultiValueType jobIdType = new GPMultiValueTypeClass();
                jobIdType.MemberDataType = new GPLongTypeClass();

                IGPParameterEdit3 jobIdList = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_LJ_JOB_ID_LIST,
                    C_PARAM_JOB_ID_LIST,
                    jobIdType as IGPDataType,
                    null);
                tempArray.Add(jobIdList);

                m_parameters = tempArray;

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
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);

                // Retrieve the parameter in which the list of workbook names will be stored
                IGPParameter3 param = paramMap.GetParam(C_PARAM_JOB_ID_LIST);
                IGPParameterEdit3 paramEdit = paramMap.GetParamEdit(C_PARAM_JOB_ID_LIST);
                IGPParameter3 filterParam = paramMap.GetParam(C_PARAM_SQL_QUERY_FILTER);

                // Get the multivalue object into which the output will be stored
                IGPMultiValue outputValues = new GPMultiValueClass();
                outputValues.MemberDataType = param.DataType;
                for (int i = 0; i < outputValues.Count; i++)
                {
                    outputValues.Remove(i);
                }

                // Get the list of job IDs and add them all to the multivalue
                SortedList<int, string> jobs = this.ListJobsInDatabase(filterParam.Value.GetAsText());
                msgs.AddMessage("Jobs matching query:");
                foreach (KeyValuePair<int, string> item in jobs)
                {
                    IGPLong value = new GPLongClass();
                    value.Value = item.Key;
                    outputValues.AddValue(value as IGPValue);
                    msgs.AddMessage("  " + value.Value.ToString() + " (" + item.Value + ")");
                }

                paramEdit.Value = (IGPValue)outputValues;

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
