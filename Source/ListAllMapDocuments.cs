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
    class ListAllMapDocuments : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_MAP_DOCUMENT_LIST = "out_stringlist_mapDocumentNames";
        #endregion

        #region MemberVariables
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ListAllMapDocuments"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_LIST_ALL_MAP_DOCUMENTS; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_MXD_UTILS; } }
        #endregion

        #region Private helper functions
        /// <summary>
        /// Gets a list of all the map documents stored in the current WMX database
        /// </summary>
        /// <returns>A sorted list of the MXDs</returns>
        private SortedList<string, string> ListMapDocumentsInDatabase()
        {
            SortedList<string, string> mapDocumentNames = new SortedList<string, string>();

            IJTXConfiguration3 configMgr = WmxDatabase.ConfigurationManager as IJTXConfiguration3;
            IJTXMapSet mapDocuments = configMgr.JTXMaps;
            for (int i = 0; i < mapDocuments.Count; i++)
            {
                IJTXMap map = mapDocuments.get_Item(i);
                mapDocumentNames.Add(map.Name, null);
            }

            return mapDocumentNames;
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
                m_parameters = new ArrayClass();

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Map document list
                IGPMultiValueType mxdListType = new GPMultiValueTypeClass();
                mxdListType.MemberDataType = new GPStringTypeClass();

                IGPParameterEdit3 mxdList = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_LMXD_MAP_DOCUMENT_LIST,
                    C_PARAM_MAP_DOCUMENT_LIST,
                    mxdListType as IGPDataType,
                    null);
                m_parameters.Add(mxdList);

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

                // Retrieve the parameter in which the list of MXDs will be stored
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameter3 param = paramMap.GetParam(C_PARAM_MAP_DOCUMENT_LIST);
                IGPParameterEdit3 paramEdit = paramMap.GetParamEdit(C_PARAM_MAP_DOCUMENT_LIST);

                // Set up the multi-value objects
                IGPMultiValue mvValue = new GPMultiValueClass();
                mvValue.MemberDataType = param.DataType;

                // Get the list of MXD names and add them all to the multivalue
                SortedList<string, string> mapDocuments = this.ListMapDocumentsInDatabase();
                foreach (string mapDocName in mapDocuments.Keys)
                {
                    IGPString strVal = new GPStringClass();
                    strVal.Value = mapDocName;
                    mvValue.AddValue(strVal as IGPValue);
                    msgs.AddMessage("Map Document: " + mapDocName);
                }

                paramEdit.Value = (IGPValue)mvValue;

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
