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
    class ListJobsUsingQuery : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_QUERY_NAME = "in_string_queryName";
        private const string C_PARAM_OUT_JOB_ID_LIST = "out_mvLong_jobIds";
        #endregion

        #region MemberVariables
        private string m_queryName = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ListJobsUsingQuery"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_LIST_JOBS_USING_QUERY; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_JOB_UTILS; } }
        #endregion

        #region Private helper functions
        private void AddQueriesFromContainer(
            IJTXJobQueryContainer container,
            string prefix,
            SortedList<string, IJTXJobQuery> queryList)
        {
            // Add the queries from the container at the current level
            IJTXJobQuerySet querySet = container.Queries;
            for (int i = 0; i < querySet.Count; i++)
            {
                IJTXJobQuery query = querySet.get_Item(i) as IJTXJobQuery;
                queryList.Add(prefix + query.Name, query);
            }

            // Iterate through each of the subcontainers and add them as well
            IJTXJobQueryContainerSet subcontainers = container.SubContainers;
            for (int i = 0; i < subcontainers.Count; i++)
            {
                IJTXJobQueryContainer tempContainer = subcontainers.get_Item(i);
                AddQueriesFromContainer(
                    tempContainer,
                    prefix + tempContainer.Name + Properties.Resources.CONST_JOB_QUERY_SEP,
                    queryList);
            }
        }

        /// <summary>
        /// Helper function to extract a list of job IDs from the XML string
        /// returned by IJTXJobQuery.ExecuteXML()
        /// </summary>
        /// <param name="rawXml">The XML string returned by IJTXJobQuery.ExecuteXML()</param>
        /// <returns>A list of the job IDs contained in the XML query result</returns>
        private List<int> ParseJobIdsFromXml(string rawXml)
        {
            // TODO: Remove this function once IJTXJobQuery.Execute() is fixed

            List<int> jobIDList = new List<int>();
            System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();

            xmlDoc.LoadXml(rawXml);

            foreach (System.Xml.XmlNode node in xmlDoc.SelectNodes("/RS/ROW"))
            {
                int jobID = Int32.Parse(node.ChildNodes[0].InnerText);
                jobIDList.Add(jobID);
            }

            return jobIDList;
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

            // Update the internal values of whatever parameters we're maintaining
            param = paramMap.GetParam(C_PARAM_QUERY_NAME);
            m_queryName = param.Value.GetAsText();
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

                // Parameter indicating the name of the query to be executed
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_LJUQ_QUERY_NAME,
                    C_PARAM_QUERY_NAME,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter indicating the job IDs returned by the specified query
                IGPMultiValueType jobIdType = new GPMultiValueTypeClass();
                jobIdType.MemberDataType = new GPLongTypeClass();

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_LJUQ_OUT_JOB_IDS,
                    C_PARAM_OUT_JOB_ID_LIST,
                    jobIdType as IGPDataType,
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
            IGPParameter3 queryName = paramMap.GetParam(C_PARAM_QUERY_NAME);
            IGPParameterEdit3 queryNameEdit = paramMap.GetParamEdit(C_PARAM_QUERY_NAME);

            // Set the domains for any parameters that need them

            // Set the query domain if it hasn't already been populated
            if (queryName.Domain == null)
            {
                queryNameEdit.Domain = Common.WmauGpDomainBuilder.BuildJobQueryDomain(this.WmxDatabase);
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
                IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;

                // Determine which query the user has selected
                SortedList<string, IJTXJobQuery> queryMap = new SortedList<string, IJTXJobQuery>();
                AddQueriesFromContainer(configMgr.GetPublicQueryContainer(), string.Empty, queryMap);
                if (!queryMap.Keys.Contains(m_queryName))
                {
                    throw new WmauException(WmauErrorCodes.C_UNKNOWN_QUERY_ERROR);
                }

                // Run the selected job query
                IJTXJobQuery tempQuery = queryMap[m_queryName];

                // TODO: Change this to use ".Evaluate()" once it's fixed
                List<int> jobIds = ParseJobIdsFromXml(tempQuery.EvaluateXML());
                jobIds.Sort();
                
                // Store the job IDs from the query into the output GP param
                IGPMultiValue outputValues = new GPMultiValueClass();
                outputValues.MemberDataType = paramMap.GetParam(C_PARAM_OUT_JOB_ID_LIST).DataType;
                for (int i = 0; i < jobIds.Count; i++)
                {
                    IGPLong jobIdVal = new GPLongClass();
                    jobIdVal.Value = jobIds[i];
                    outputValues.AddValue(jobIdVal as IGPValue);
                    msgs.AddMessage("Found job: " + jobIds[i]);
                }

                paramMap.GetParamEdit(C_PARAM_OUT_JOB_ID_LIST).Value = (IGPValue)outputValues;

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
