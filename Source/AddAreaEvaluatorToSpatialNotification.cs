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


namespace WorkflowManagerAdministrationUtilities
{
    class AddAreaEvaluatorToSpatialNotification : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_SPATIAL_NOTIFICATION = "in_string_spatialNotification";
        private const string C_PARAM_GEOMETRIC_OPERATION = "in_string_geometricOperation";
        private const string C_PARAM_USE_INVERSE = "in_bool_useInverse";
        private const string C_PARAM_USE_JOB_AOI = "in_bool_useJobAoi";
        private const string C_PARAM_ALTERNATE_AOI = "in_layer_alternateAoi";
        private const string C_PARAM_OUT_SPATIAL_NOTIFICATION = "out_string_spatialNotification";

        private const string C_OPT_USE_INVERSE = "USE_INVERSE";
        private const string C_OPT_USE_OPERATION = "USE_OPERATION";

        private const string C_OPT_USE_JOB_AOI = "USE_JOB_AOI";
        private const string C_OPT_USE_SELECTED_FEATURE = "USE_SELECTED_FEATURE";

        private const string C_OPT_OP_CONTAINS = "CONTAINS";
        private const string C_OPT_OP_CROSSES = "CROSSES";
        private const string C_OPT_OP_ENVELOPE_INTERSECTS = "ENVELOPE_INTERSECTS";
        private const string C_OPT_OP_INTERSECTS = "INTERSECTS";
        private const string C_OPT_OP_OVERLAPS = "OVERLAPS";
        private const string C_OPT_OP_TOUCHES = "TOUCHES";
        private const string C_OPT_OP_WITHIN = "WITHIN";

        private const bool C_DEFAULT_USE_INVERSE = false;
        private const bool C_DEFAULT_USE_JOB_AOI = true;

        // These values match up with other predetermined values referenced by Workflow
        // Manager.  Do not modify.
        private const string C_TYPE_AREA_EVALUATOR = "Area Evaluator";

        #endregion

        #region MemberVariables
        private string m_spatialNotification = string.Empty;
        private string m_geometricOperation = C_OPT_OP_INTERSECTS;
        private bool m_useInverse = C_DEFAULT_USE_INVERSE;
        private bool m_useJobAoi = C_DEFAULT_USE_JOB_AOI;
        private ILayer m_alternateAoi = null;

        private Dictionary<string, esriSpatialRelEnum> m_geometricOperations = new Dictionary<string, esriSpatialRelEnum>();
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "AddAreaEvaluatorToSN"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_ADD_AREA_EVALUATOR_TO_SN; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_NOTIFICATION_UTILS; } }
        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        public AddAreaEvaluatorToSpatialNotification()
            : base()
        {
            // Ensure that the geometric operation map is properly initialized
            m_geometricOperations.Add(C_OPT_OP_CONTAINS, esriSpatialRelEnum.esriSpatialRelContains);
            m_geometricOperations.Add(C_OPT_OP_CROSSES, esriSpatialRelEnum.esriSpatialRelCrosses);
            m_geometricOperations.Add(C_OPT_OP_ENVELOPE_INTERSECTS, esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects);
            m_geometricOperations.Add(C_OPT_OP_INTERSECTS, esriSpatialRelEnum.esriSpatialRelIntersects);
            m_geometricOperations.Add(C_OPT_OP_OVERLAPS, esriSpatialRelEnum.esriSpatialRelOverlaps);
            m_geometricOperations.Add(C_OPT_OP_TOUCHES, esriSpatialRelEnum.esriSpatialRelTouches);
            m_geometricOperations.Add(C_OPT_OP_WITHIN, esriSpatialRelEnum.esriSpatialRelWithin);
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

            // Update the internal values of whatever parameters we're maintaining
            param = paramMap.GetParam(C_PARAM_SPATIAL_NOTIFICATION);
            m_spatialNotification = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_GEOMETRIC_OPERATION);
            m_geometricOperation = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_USE_INVERSE);
            m_useInverse = (param.Value as IGPBoolean).Value;

            param = paramMap.GetParam(C_PARAM_USE_JOB_AOI);
            m_useJobAoi = (param.Value as IGPBoolean).Value;

            param = paramMap.GetParam(C_PARAM_ALTERNATE_AOI);
            m_alternateAoi = m_gpUtilities.DecodeLayer(param.Value);
        }

        /// <summary>
        /// Set up a coded value domain with the supported geometric operations for area
        /// evaluators
        /// </summary>
        /// <returns></returns>
        private IGPDomain BuildGeometricOperationsDomain()
        {
            IGPCodedValueDomain geomOpDomain = new GPCodedValueDomainClass();
            foreach (string s in m_geometricOperations.Keys)
            {
                geomOpDomain.AddStringCode(s, s);
            }

            return geomOpDomain as IGPDomain;
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

                IGPParameterEdit paramEdit = null;
                IGPCodedValueDomain cvDomain = null;

                // Parameter indicating the spatial notification to which the area evaluator
                // should be added
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_AAE_SPATIAL_NOTIFICATION,
                    C_PARAM_SPATIAL_NOTIFICATION,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating the geometric operation for the area evaluator to use
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_AAE_GEOMETRIC_OPERATION,
                    C_PARAM_GEOMETRIC_OPERATION,
                    new GPStringTypeClass(),
                    null);
                paramEdit.Domain = BuildGeometricOperationsDomain();
                m_parameters.Add(paramEdit);

                // Parameter indicating whether to use the inverse of the spatial operation
                // specified above
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_USE_INVERSE);
                cvDomain.AddCode(GpFalse, C_OPT_USE_OPERATION);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_AAE_USE_INVERSE,
                    C_PARAM_USE_INVERSE,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_USE_INVERSE));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter indicating whether to use the AOI of the current job for the
                // spatial operation specified above
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_USE_JOB_AOI);
                cvDomain.AddCode(GpFalse, C_OPT_USE_SELECTED_FEATURE);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_AAE_USE_JOB_AOI,
                    C_PARAM_USE_JOB_AOI,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_USE_JOB_AOI));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter indicating the AOI for the area evaluator to use
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_AAE_ALTERNATE_AOI,
                    C_PARAM_ALTERNATE_AOI,
                    new GPFeatureLayerTypeClass(),
                    null);
                paramEdit.Enabled = !m_useJobAoi;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Parameter echoing the spatial notification name as an output (for
                // usability in a GP model)
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_AAE_SPATIAL_NOTIFICATION_OUT,
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
            IGPParameter3 altAoi = paramMap.GetParam(C_PARAM_ALTERNATE_AOI);
            IGPParameterEdit3 altAoiEdit = paramMap.GetParamEdit(C_PARAM_ALTERNATE_AOI);

            // Ensure that there is at least one existing spatial notification in the database
            if (snName.Domain == null || (snName.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                WmauError error = new WmauError(WmauErrorCodes.C_NO_SPATIAL_NOTIFICATIONS_FOUND);
                msgs.ReplaceError(paramMap.GetIndex(C_PARAM_SPATIAL_NOTIFICATION), error.ErrorCodeAsInt, error.Message);
            }

            // Check the AOI; ensure that there is exactly one feature selected
            if (altAoi.Value != null && !altAoi.Value.GetAsText().Equals(string.Empty))
            {
                try
                {
                    ILayer aoiLayer = m_gpUtilities.DecodeLayer(altAoi.Value);
                    IFeatureLayer featLayer = aoiLayer as IFeatureLayer;
                    IFeatureSelection featSel = aoiLayer as IFeatureSelection;
                    ISelectionSet selSet = featSel.SelectionSet as ISelectionSet;

                    if (featLayer.FeatureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                    {
                        WmauError error = new WmauError(WmauErrorCodes.C_AOI_NOT_POLYGON_ERROR);
                        msgs.ReplaceWarning(paramMap.GetIndex(C_PARAM_ALTERNATE_AOI), error.Message);
                    }
                    else if (selSet.Count != 1)
                    {
                        WmauError error = new WmauError(WmauErrorCodes.C_EXPECTED_ONE_SELECTED_FEATURE_ERROR);
                        msgs.ReplaceWarning(paramMap.GetIndex(C_PARAM_ALTERNATE_AOI), error.Message);
                    }
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    WmauError error = new WmauError(WmauErrorCodes.C_AOI_INPUT_ERROR);
                    msgs.ReplaceError(paramMap.GetIndex(C_PARAM_ALTERNATE_AOI), error.ErrorCodeAsInt, error.Message + "; " + comEx.Message);
                }
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
            IGPParameter3 useJobAoi = paramMap.GetParam(C_PARAM_USE_JOB_AOI);
            IGPParameterEdit3 alternateAoiEdit = paramMap.GetParamEdit(C_PARAM_ALTERNATE_AOI);

            // Apply a domain to the list of spatial notifications, if possible
            if (snName.Domain == null || (snName.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                snNameEdit.Domain = Common.WmauGpDomainBuilder.BuildChangeRulesDomain(this.WmxDatabase);
            }

            // Enable/disable the alternate AOI parameter based on the status of the
            // "use job AOI" parameter
            alternateAoiEdit.Enabled = !(useJobAoi.Value as IGPBoolean).Value;
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
            ICursor cursor = null;

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

                // Find the rule that we're modifying
                IJTXChangeRule2 changeRule = GetChangeRuleByName(m_spatialNotification) as IJTXChangeRule2;
                
                // Create and configure the area evaluator
                IJTXAOIConditionEvaluator areaEvaluator = CreateEvaluator(C_TYPE_AREA_EVALUATOR) as IJTXAOIConditionEvaluator;
                areaEvaluator.SpatialRel = m_geometricOperations[m_geometricOperation];
                areaEvaluator.UseInverse = m_useInverse;
                areaEvaluator.UseJobAOI = m_useJobAoi;

                // Set the AOI of the job, if there is one
                // Ensure that there's nothing wrong with the AOI feature that is selected, if any
                if (!m_useJobAoi)
                {
                    if (m_alternateAoi != null)
                    {
                        IFeatureLayer featLayer = m_alternateAoi as IFeatureLayer;
                        IFeatureSelection featSel = m_alternateAoi as IFeatureSelection;
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
                        areaEvaluator.AreaOfInterest = aoiCandidate.Shape as IPolygon;
                    }
                }

                // Associate the evaluator with the change rule and save the changes
                changeRule.Evaluators.Add(areaEvaluator as IJTXConditionEvaluator);
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
                    WmauError error = new WmauError(WmauErrorCodes.C_ADD_AREA_EVAL_ERROR);
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
                if (cursor != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(cursor);
                }
            }
        }
    }
}
