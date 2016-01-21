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
    class ListUsers : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_GROUP_NAME = "in_string_groupName";
        private const string C_PARAM_USER_LIST = "out_mvString_userList";
        #endregion

        #region MemberVariables
        private string m_groupName = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ListUsers"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_LIST_USERS; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_SECURITY_UTILS; } }
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
            param = paramMap.GetParam(C_PARAM_GROUP_NAME);
            m_groupName = param.Value.GetAsText();
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

                // Name of group for which to list the users
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_LU_GROUP_NAME,
                    C_PARAM_GROUP_NAME,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // List of user names (as output)
                IGPMultiValueType mvType = new GPMultiValueTypeClass();
                mvType.MemberDataType = new GPStringTypeClass();

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_LU_USER_NAME_LIST,
                    C_PARAM_USER_LIST,
                    mvType as IGPDataType,
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

            // Build a hash of which parameter is at which index for ease of access
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);

            IGPParameter3 groupParam = paramMap.GetParam(C_PARAM_GROUP_NAME);
            IGPParameterEdit3 groupParamEdit = paramMap.GetParamEdit(C_PARAM_GROUP_NAME);

            // Add a domain to the groups parameter
            if (groupParam.Domain == null || (groupParam.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                groupParamEdit.Domain = Common.WmauGpDomainBuilder.BuildGroupsDomain(this.WmxDatabase);
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

                IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;

                // Prep to retrieve the users in the specified groups (if any were specified)
                string[] userNames = null;
                if (!string.IsNullOrEmpty(m_groupName))
                {
                    msgs.AddMessage("Users in group '" + m_groupName + "':");
                    string[] groupNameArray = new string[] { m_groupName };
                    userNames = configMgr.GetUserNamesForGroups(groupNameArray);
                }
                else
                {
                    msgs.AddMessage("All users:");
                    userNames = new string[configMgr.Users.Count];
                    for (int i = 0; i < configMgr.Users.Count; i++)
                    {
                        userNames[i] = configMgr.Users.get_Item(i).UserName;
                    }
                }

                // Sort the user list
                SortedList<string, string> sortedNames = new SortedList<string, string>();
                foreach (string s in userNames)
                {
                    sortedNames.Add(s, null);
                }

                // Retrieve the parameter in which the list of user names will be stored
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameter3 param = paramMap.GetParam(C_PARAM_USER_LIST);
                IGPParameterEdit3 paramEdit = paramMap.GetParamEdit(C_PARAM_USER_LIST);

                // Set up the multi-value objects
                IGPMultiValue mvValue = new GPMultiValueClass();
                mvValue.MemberDataType = param.DataType;

                // Add the user list to the multivalue param
                foreach (string user in sortedNames.Keys)
                {
                    msgs.AddMessage("  " + user);
                    IGPString strVal = new GPStringClass();
                    strVal.Value = user;
                    mvValue.AddValue(strVal as IGPValue);
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
            finally
            {
                // Release any COM objects here!
            }
        }
    }
}
