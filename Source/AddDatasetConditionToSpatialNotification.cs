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
    class AddDatasetConditionToSpatialNotification : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_SPATIAL_NOTIFICATION = "in_string_spatialNotification";
        private const string C_PARAM_DATA_WORKSPACE = "in_string_dataWorkspace";
        private const string C_PARAM_FEATURE_CLASS = "in_fc_featureClass";
        private const string C_PARAM_CHANGE_CONDITION = "in_string_changeCondition";
        private const string C_PARAM_WHERE_CLAUSES = "in_valueTable_whereClauses";
        private const string C_PARAM_MONITOR_ALL_COLUMNS = "in_bool_monitorAllFields";
        private const string C_PARAM_COLUMNS = "in_field_columns";
        private const string C_PARAM_TRY_EXISTING_EVALUATOR = "in_bool_tryExistingEvaluator";
        private const string C_PARAM_OUT_SPATIAL_NOTIFICATION = "out_string_spatialNotification";

        private const string C_OPT_ALWAYS = "ALWAYS";
        private const string C_OPT_CHANGED_FROM = "CHANGED_FROM";
        private const string C_OPT_CHANGED_TO = "CHANGED_TO";

        private const string C_OPT_TRY_EXISTING = "TRY_EXISTING_EVAULATOR";
        private const string C_OPT_MAKE_NEW = "MAKE_NEW_EVALUATOR";

        private const string C_OPT_MONITOR_ALL_COLUMNS = "MONITOR_ALL_COLUMNS";
        private const string C_OPT_MONITOR_SELECTED_COLUMNS = "MONITOR_SELECTED_COLUMNS";

        private const bool C_DEFAULT_MONITOR_ALL_COLS = true;
        private const bool C_DEFAULT_TRY_EXISTING_EVAL = true;

        private const int C_ID_VT_ATTRIBUTE = 0;
        private const int C_ID_VT_OPERATOR = 1;
        private const int C_ID_VT_VALUE = 2;

        // These values match up with other predetermined values referenced by Workflow
        // Manager.  Do not modify.
        private const string C_TYPE_DATASET_EVALUATOR = "Dataset Evaluator";

        private const string C_OPERATOR_CONTAINS = "Contains";
        private const string C_OPERATOR_EQ = "=";
        private const string C_OPERATOR_NEQ = "<>";
        private const string C_OPERATOR_GT = ">";
        private const string C_OPERATOR_GTEQ = ">=";
        private const string C_OPERATOR_LT = "<";
        private const string C_OPERATOR_LTEQ = "<=";
        #endregion

        #region MemberVariables
        private string m_spatialNotification = string.Empty;
        private string m_dataWorkspace = string.Empty;
        private string m_featureClass = string.Empty;
        private jtxChangeCondition m_whenToMonitor = jtxChangeCondition.All;
        private IJTXAttributeConditionSet m_whereClauses = null;
        private bool m_monitorAllColumns = C_DEFAULT_MONITOR_ALL_COLS;
        private string m_columns = string.Empty;
        private bool m_tryExistingEvaluator = C_DEFAULT_TRY_EXISTING_EVAL;

        private Dictionary<string, jtxChangeCondition> m_optChangeConditions = new Dictionary<string, jtxChangeCondition>();
        private Dictionary<string, jtxWhereClauseOperator> m_optWhereClauseOps = new Dictionary<string, jtxWhereClauseOperator>();
        private Dictionary<int, int> m_whereClauseIndices = new Dictionary<int, int>();
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "AddDatasetConditionToSN"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_ADD_DATASET_CONDITION_TO_SN; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_NOTIFICATION_UTILS; } }
        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        public AddDatasetConditionToSpatialNotification()
            : base()
        {
            // Set up the change condition mapping
            m_optChangeConditions.Add(C_OPT_ALWAYS, jtxChangeCondition.All);
            m_optChangeConditions.Add(C_OPT_CHANGED_FROM, jtxChangeCondition.Before);
            m_optChangeConditions.Add(C_OPT_CHANGED_TO, jtxChangeCondition.After);

            // Set up the where clause operator mapping
            m_optWhereClauseOps.Add(C_OPERATOR_CONTAINS, jtxWhereClauseOperator.Contains);
            m_optWhereClauseOps.Add(C_OPERATOR_EQ, jtxWhereClauseOperator.Equal);
            m_optWhereClauseOps.Add(C_OPERATOR_GT, jtxWhereClauseOperator.GreaterThan);
            m_optWhereClauseOps.Add(C_OPERATOR_GTEQ, jtxWhereClauseOperator.GreaterThanOrEqual);
            m_optWhereClauseOps.Add(C_OPERATOR_LT, jtxWhereClauseOperator.LessThan);
            m_optWhereClauseOps.Add(C_OPERATOR_LTEQ, jtxWhereClauseOperator.LessThanOrEqual);
            m_optWhereClauseOps.Add(C_OPERATOR_NEQ, jtxWhereClauseOperator.NotEqual);
        }

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
            string tempStr = null;

            // Update the internal values of whatever parameters we're maintaining
            param = paramMap.GetParam(C_PARAM_SPATIAL_NOTIFICATION);
            m_spatialNotification = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_DATA_WORKSPACE);
            tempStr = param.Value.GetAsText();
            m_dataWorkspace = Common.WmauHelperFunctions.LookupWorkspaceId(this.WmxDatabase, tempStr);

            param = paramMap.GetParam(C_PARAM_FEATURE_CLASS);
            tempStr = param.Value.GetAsText();
            if (tempStr.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                m_featureClass = tempStr.Substring(tempStr.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                m_featureClass = tempStr;
            }

            param = paramMap.GetParam(C_PARAM_CHANGE_CONDITION);
            m_whenToMonitor = m_optChangeConditions[param.Value.GetAsText()];

            param = paramMap.GetParam(C_PARAM_WHERE_CLAUSES);
            m_whereClauses = ExtractAttributeConditionsFromWhereClause(param.Value as IGPValueTable);

            param = paramMap.GetParam(C_PARAM_MONITOR_ALL_COLUMNS);
            m_monitorAllColumns = bool.Parse(param.Value.GetAsText());

            param = paramMap.GetParam(C_PARAM_COLUMNS);
            m_columns = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_TRY_EXISTING_EVALUATOR);
            m_tryExistingEvaluator = bool.Parse(param.Value.GetAsText());
        }

        /// <summary>
        /// Set up a coded value domain with the supported geometric operations for area
        /// evaluators
        /// </summary>
        /// <returns></returns>
        private IGPDomain BuildChangeConditionsDomain()
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();
            foreach (string s in m_optChangeConditions.Keys)
            {
                domain.AddStringCode(s, s);
            }

            return domain as IGPDomain;
        }

        /// <summary>
        /// Constructs the value table used for the 
        /// </summary>
        /// <returns></returns>
        private IGPValueTableType BuildWhereClauseValueTable()
        {
            IGPValueTableType vt = new GPValueTableTypeClass();
            IGPDataType type = null;
            int columnIndex = 0;

            m_whereClauseIndices.Clear();

            // First column: attribute names (based on the selected feature class)
            type = new FieldTypeClass();
            vt.AddDataType(type, Properties.Resources.DESC_ADC_VT_ATTRIBUTE, 150, null);
            m_whereClauseIndices.Add(C_ID_VT_ATTRIBUTE, columnIndex++);

            // Second column: comparison operators (fixed, based on what Workflow Manager supports)
            type = new GPStringTypeClass();
            vt.AddDataType(type, Properties.Resources.DESC_ADC_VT_OPERATOR, 50, null);
            m_whereClauseIndices.Add(C_ID_VT_OPERATOR, columnIndex++);

            // Third column: values (based on the selected attribute)
            type = new GPStringTypeClass();
            vt.AddDataType(type, Properties.Resources.DESC_ADC_VT_VALUE, 150, null);
            m_whereClauseIndices.Add(C_ID_VT_VALUE, columnIndex++);

            return vt;
        }

        /// <summary>
        /// Helper function to create a new instance of the specified type of condition evaluator
        /// </summary>
        /// <param name="evalType"></param>
        /// <returns></returns>
        private IJTXConditionEvaluator CreateEvaluator(string evalType)
        {
            IJTXConditionEvaluator retVal = null;

            IJTXSpatialNotificationManager snManager = this.WmxDatabase.SpatialNotificationManager;
            IJTXConditionEvaluatorNameSet allEvaluators = snManager.ConditionEvaluators;

            // Find the type of evaluator specified
            for (int i = 0; i < allEvaluators.Count; i++)
            {
                IJTXConditionEvaluatorName tempEval = allEvaluators.get_Item(i);
                if (tempEval.Name.Equals(evalType))
                {
                    // Once found, create a new instance of this evaluator
                    retVal = snManager.CreateConditionEvaluator(tempEval);
                    break;
                }
            }

            return retVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vt"></param>
        /// <returns></returns>
        private IJTXAttributeConditionSet ExtractAttributeConditionsFromWhereClause(IGPValueTable vt)
        {
            IJTXAttributeConditionSet attributeConditions = new JTXAttributeConditionSetClass();

            // Preliminary error-checking
            for (int i = 0; i < vt.RecordCount; i++)
            {
                IArray tempRecord = vt.GetRecord(i);
                IGPValue tempVal = tempRecord.get_Element(m_whereClauseIndices[C_ID_VT_OPERATOR]) as IGPValue;
                if (!m_optWhereClauseOps.ContainsKey(tempVal.GetAsText()))
                {
                    throw new WmauException(WmauErrorCodes.C_OPERATOR_NOT_FOUND_ERROR);
                }
            }

            // Build out the attribute conditions
            for (int i = 0; i < vt.RecordCount; i++)
            {
                IArray tempRecord = vt.GetRecord(i);
                IGPValue attribute = tempRecord.get_Element(m_whereClauseIndices[C_ID_VT_ATTRIBUTE]) as IGPValue;
                IGPValue compOperator = tempRecord.get_Element(m_whereClauseIndices[C_ID_VT_OPERATOR]) as IGPValue;
                IGPValue value = tempRecord.get_Element(m_whereClauseIndices[C_ID_VT_VALUE]) as IGPValue;
                
                IJTXAttributeCondition attributeCondition = new JTXAttributeConditionClass();
                attributeCondition.FieldName = attribute.GetAsText();
                attributeCondition.Operator = m_optWhereClauseOps[compOperator.GetAsText()];
                attributeCondition.CompareValue = value.GetAsText();

                attributeConditions.Add(attributeCondition);
            }

            return attributeConditions;
        }

        /// <summary>
        /// Looks up a change rule (spatial notification) by its name
        /// </summary>
        /// <param name="name">The name of the change rule</param>
        /// <returns>The change rule object, or null if no match was found</returns>
        private IJTXChangeRule GetChangeRuleByName(string name)
        {
            IJTXChangeRule retVal = null;

            IJTXSpatialNotificationManager snManager = this.WmxDatabase.SpatialNotificationManager;
            IJTXChangeRuleSet allChangeRules = snManager.ChangeRules;

            // Find the type of change rule specified
            for (int i = 0; i < allChangeRules.Count; i++)
            {
                IJTXChangeRule rule = allChangeRules.get_Item(i);
                if (rule.Name.Equals(name))
                {
                    retVal = rule;
                    break;
                }
            }

            return retVal;
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

                // Parameter indicating the spatial notification with which which the evaluator
                // should be associated
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_ADC_SPATIAL_NOTIFICATION,
                    C_PARAM_SPATIAL_NOTIFICATION,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the data workspace for the evaluator to use
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_ADC_DATA_WORKSPACE,
                    C_PARAM_DATA_WORKSPACE,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the feature class for the evaluator to use
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_ADC_FEATURE_CLASS,
                    C_PARAM_FEATURE_CLASS,
                    new DEFeatureClassTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Parameter indicating the change condition to use for this evaluator
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_ADC_CHANGE_CONDITION,
                    C_PARAM_CHANGE_CONDITION,
                    new GPStringTypeClass(),
                    null);
                paramEdit.Domain = BuildChangeConditionsDomain();
                m_parameters.Add(paramEdit);

                // Parameter indicating the where clauses to use for this evaluator
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_ADC_WHERE_CLAUSES,
                    C_PARAM_WHERE_CLAUSES,
                    BuildWhereClauseValueTable() as IGPDataType,
                    null);
                paramEdit.AddDependency(C_PARAM_FEATURE_CLASS);
                m_parameters.Add(paramEdit);

                // Parameter indicating whether all columns should be monitored
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_MONITOR_ALL_COLUMNS);
                cvDomain.AddCode(GpFalse, C_OPT_MONITOR_SELECTED_COLUMNS);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_ADC_MONITOR_ALL_COLUMNS,
                    C_PARAM_MONITOR_ALL_COLUMNS,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_MONITOR_ALL_COLS));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter indicating the columns to use for this evaluator
                //
                // NOTE: See http://help.arcgis.com/en/sdk/10.0/arcobjects_net/conceptualhelp/index.html#//00010000049w000000
                // ("Building a custom geoprocessing function tool") for an example of the use
                // of the UID class and manually setting the control CLSID of an object.
                // Refer to this page for tips about how to determine the available CLSIDs.
                IGPMultiValueType columnListType = new GPMultiValueTypeClass();
                columnListType.MemberDataType = new FieldTypeClass();
                UID pUID = new UIDClass();
                pUID.Value = "{C15EC6FA-35EF-4204-90FB-01E7B4DD6862}";  // CLSID for MdFieldListCtrl

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_ADC_COLUMNS,
                    C_PARAM_COLUMNS,
                    columnListType as IGPDataType,
                    null);
                paramEdit.AddDependency(C_PARAM_FEATURE_CLASS);
                paramEdit.ControlCLSID = pUID;
                paramEdit.Enabled = !m_monitorAllColumns;
                m_parameters.Add(paramEdit);

                // Parameter indicating whether to attempt to add to a dataset evaluator
                // already present on the change rule, if any exists
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_TRY_EXISTING);
                cvDomain.AddCode(GpFalse, C_OPT_MAKE_NEW);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_ADC_TRY_EXISTING_EVALUATOR,
                    C_PARAM_TRY_EXISTING_EVALUATOR,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_TRY_EXISTING_EVAL));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter echoing the spatial notification name as an output (for
                // usability in a GP model)
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_ADC_SPATIAL_NOTIFICATION_OUT,
                    C_PARAM_OUT_SPATIAL_NOTIFICATION,
                    new GPStringTypeClass(),
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

            // Build a hash of which parameter is at which index, for ease of access
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);

            IGPParameter3 snName = paramMap.GetParam(C_PARAM_SPATIAL_NOTIFICATION);
            IGPParameter3 dataWorkspace = paramMap.GetParam(C_PARAM_DATA_WORKSPACE);

            // Ensure that there is at least one existing spatial notification in the database
            if (snName.Domain == null || (snName.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                WmauError error = new WmauError(WmauErrorCodes.C_NO_SPATIAL_NOTIFICATIONS_FOUND);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_SPATIAL_NOTIFICATION), error.ErrorCodeAsInt, error.Message);
            }

            // Ensure that there is at least one data workspace defined in the database
            if (dataWorkspace.Domain == null || (dataWorkspace.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                WmauError error = new WmauError(WmauErrorCodes.C_NO_WORKSPACES_DEFINED_ERROR);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_DATA_WORKSPACE), error.ErrorCodeAsInt, error.Message);
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

            // Build a hash of the parameters for ease of access
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);

            IGPParameter3 snName = paramMap.GetParam(C_PARAM_SPATIAL_NOTIFICATION);
            IGPParameterEdit3 snNameEdit = paramMap.GetParamEdit(C_PARAM_SPATIAL_NOTIFICATION);
            IGPParameter3 dataWorkspace = paramMap.GetParam(C_PARAM_DATA_WORKSPACE);
            IGPParameterEdit3 dataWorkspaceEdit = paramMap.GetParamEdit(C_PARAM_DATA_WORKSPACE);
            IGPParameter3 monitorAllColumns = paramMap.GetParam(C_PARAM_MONITOR_ALL_COLUMNS);
            IGPParameterEdit3 selectColumns = paramMap.GetParamEdit(C_PARAM_COLUMNS);

            // Apply a domain to the list of spatial notifications, if possible
            if (snName.Domain == null || (snName.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                snNameEdit.Domain = Common.WmauGpDomainBuilder.BuildChangeRulesDomain(this.WmxDatabase);
            }

            // Apply a domain to the data workspace list, if possible
            if (dataWorkspace.Domain == null || (dataWorkspace.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                dataWorkspaceEdit.Domain = Common.WmauGpDomainBuilder.BuildWorkspaceDomain(this.WmxDatabase);
            }

            // Enable/disable the column param based on the "monitor all columns" param
            selectColumns.Enabled = !(monitorAllColumns.Value as IGPBoolean).Value;
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

                IJTXSpatialNotificationManager snManager = this.WmxDatabase.SpatialNotificationManager;

                // Look up the change rule that we'll be modifying
                IJTXChangeRule2 changeRule = GetChangeRuleByName(m_spatialNotification) as IJTXChangeRule2;
                IJTXMultiDatasetConditionEvaluator datasetEvaluator = null;

                // Try to get an existing dataset evaluator if one exists.
                if (m_tryExistingEvaluator)
                {
                    IJTXConditionEvaluatorSet allEvaluators = changeRule.Evaluators;
                    for (int i = 0; i < allEvaluators.Count; i++)
                    {
                        IJTXConditionEvaluator tempEval = allEvaluators.get_Item(i);
                        if (tempEval.Name.Equals(C_TYPE_DATASET_EVALUATOR) && tempEval is IJTXMultiDatasetConditionEvaluator)
                        {
                            datasetEvaluator = tempEval as IJTXMultiDatasetConditionEvaluator;
                            break;
                        }
                    }
                }

                // If we don't have an evaluator at this point, then we need to create one                
                if (datasetEvaluator == null)
                {
                    datasetEvaluator = CreateEvaluator(C_TYPE_DATASET_EVALUATOR) as IJTXMultiDatasetConditionEvaluator;
                    datasetEvaluator.DatasetConfigurations = new JTXDatasetConditionConfigurationSetClass();
                    changeRule.Evaluators.Add(datasetEvaluator as IJTXConditionEvaluator);
                }

                // Create a new dataset configuration
                IJTXDatasetConditionConfiguration datasetCondition = new JTXDatasetConditionConfigurationClass();
                datasetCondition.DatabaseID = m_dataWorkspace;
                datasetCondition.DatasetName = m_featureClass;
                datasetCondition.ChangeCondition = m_whenToMonitor;
                datasetCondition.WhereConditions = m_whereClauses;
                datasetCondition.ChangeFields = m_monitorAllColumns ? "*" : m_columns.Replace(';', ',');
                datasetCondition.Name = this.WmxDatabase.GetDataWorkspaceName(m_dataWorkspace).Name + "/" + m_featureClass;

                // Store the configuration in the dataset evaluator
                datasetEvaluator.DatasetConfigurations.Add(datasetCondition);
                changeRule.Store();

                // Set the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_OUT_SPATIAL_NOTIFICATION);
                IGPString outParamStr = new GPStringClass();
                outParamStr.Value = m_spatialNotification;
                outParamEdit.Value = outParamStr as IGPValue;

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
                    WmauError error = new WmauError(WmauErrorCodes.C_ADD_DATASET_COND_ERROR);
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
            }
        }

    }
}
