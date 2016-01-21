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
    class ModifyPrivilegeAssignment : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_PRIVILEGE_NAME = "in_string_privilegeName";
        private const string C_PARAM_GROUP_NAME = "in_string_groupName";
        private const string C_PARAM_PRIVILEGE_ACTION = "in_string_privilegeAction";
        private const string C_PARAM_OUT_PRIVILEGE_NAME = "out_string_privilegeName";

        private const string C_OPT_ALL_PRIVILEGES = "[All]";
        private const string C_OPT_ALL_GROUPS = "[All]";

        private const string C_OPT_GRANT = "GRANT";
        private const string C_OPT_REVOKE = "REVOKE";

        private const string C_DEFAULT_PRIVILEGE_ACTION = C_OPT_GRANT;
        #endregion

        #region MemberVariables
        private string m_privilegeName = string.Empty;
        private string m_groupName = string.Empty;
        private string m_privilegeAction = C_DEFAULT_PRIVILEGE_ACTION;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ModifyPrivilegeAssignment"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_MODIFY_PRIVILEGE_ASSIGNMENT; } }
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
            param = paramMap.GetParam(C_PARAM_PRIVILEGE_NAME);
            m_privilegeName = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_GROUP_NAME);
            m_groupName = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_PRIVILEGE_ACTION);
            m_privilegeAction = param.Value.GetAsText();
        }

        /// <summary>
        /// Builds a domain consisting of the Workflow Manager privileges
        /// </summary>
        /// <returns>A coded value domain of strings</returns>
        private IGPDomain BuildPrivilegesDomain()
        {
            IGPCodedValueDomain domain = new GPCodedValueDomainClass();

            // Sort the types first
            SortedList<string, string> sortedValues = new SortedList<string, string>();
            IJTXPrivilegeSet privileges = this.WmxDatabase.ConfigurationManager.Privileges;
            for (int i = 0; i < privileges.Count; i++)
            {
                IJTXPrivilege2 priv = privileges.get_Item(i) as IJTXPrivilege2;
                sortedValues.Add(priv.Name, null);
            }

            // Add the "all privileges" option to the list
            sortedValues.Add(C_OPT_ALL_PRIVILEGES, null);

            // Add the sorted types to the domain
            foreach (string value in sortedValues.Keys)
            {
                IGPValue tempGpVal = new GPStringClass();
                tempGpVal.SetAsText(value);
                domain.AddCode(tempGpVal, value);
            }

            return domain as IGPDomain;
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

                // Privilege name
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_MPA_PRIVILEGE_NAME,
                    C_PARAM_PRIVILEGE_NAME,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Name of group affected by the privilege assignment
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_MPA_GROUP_NAME,
                    C_PARAM_GROUP_NAME,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating whether to the privilege(s) should be granted to
                // or revoked from the specified group(s)
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddStringCode(C_OPT_GRANT, C_OPT_GRANT);
                cvDomain.AddStringCode(C_OPT_REVOKE, C_OPT_REVOKE);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_MPA_PRIVILEGE_ACTION,
                    C_PARAM_PRIVILEGE_ACTION,
                    cvDomain.FindValue(C_DEFAULT_PRIVILEGE_ACTION).DataType,
                    cvDomain.FindValue(C_DEFAULT_PRIVILEGE_ACTION));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Privilege name (as output)
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_MPA_OUT_PRIVILEGE_NAME,
                    C_PARAM_OUT_PRIVILEGE_NAME,
                    new GPStringTypeClass(),
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

            IGPParameter3 privParam = paramMap.GetParam(C_PARAM_PRIVILEGE_NAME);
            IGPParameterEdit3 privParamEdit = paramMap.GetParamEdit(C_PARAM_PRIVILEGE_NAME);
            IGPParameter3 groupParam = paramMap.GetParam(C_PARAM_GROUP_NAME);
            IGPParameterEdit3 groupParamEdit = paramMap.GetParamEdit(C_PARAM_GROUP_NAME);

            // Add a domain to the privilege parameter
            if (privParam.Domain == null || (privParam.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                privParamEdit.Domain = Common.WmauGpDomainBuilder.BuildPrivilegesDomain(this.WmxDatabase, new string[] { C_OPT_ALL_PRIVILEGES });
            }

            // Add a domain to the groups parameter
            if (groupParam.Domain == null || (groupParam.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                groupParamEdit.Domain = Common.WmauGpDomainBuilder.BuildGroupsDomain(this.WmxDatabase, new string[] { C_OPT_ALL_GROUPS });
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

                IJTXConfigurationEdit2 configEdit = this.WmxDatabase.ConfigurationManager as IJTXConfigurationEdit2;

                // Look up the appropriate privilege(s) and group(s)
                IJTXPrivilegeSet privileges = null;
                if (m_privilegeName.Equals(C_OPT_ALL_PRIVILEGES))
                {
                    privileges = configEdit.Privileges;
                }
                else
                {
                    privileges = new JTXPrivilegeSetClass();
                    (privileges as IJTXPrivilegeSetEdit).Add(configEdit.GetPrivilege(m_privilegeName));
                }

                IJTXUserGroupSet groups = null;
                if (m_groupName.Equals(C_OPT_ALL_GROUPS))
                {
                    groups = configEdit.UserGroups;
                }
                else
                {
                    groups = new JTXUserGroupSetClass();
                    (groups as IJTXUserGroupSetEdit).Add(configEdit.GetUserGroup(m_groupName));
                }
                
                // Add/remove the privilege(s) to the group(s)
                for (int i = 0; i < privileges.Count; i++)
                {
                    IJTXPrivilege2 privilege = privileges.get_Item(i) as IJTXPrivilege2;
                    for (int j = 0; j < groups.Count; j++)
                    {
                        IJTXUserGroupConfig2 targetGroup = groups.get_Item(j) as IJTXUserGroupConfig2;
                        if (m_privilegeAction.Equals(C_OPT_GRANT))
                        {
                            targetGroup.AssignPrivilegeToGroup2(privilege.UID);
                        }
                        else
                        {
                            targetGroup.RemovePrivilegeFromGroup2(privilege.UID);
                        }
                        targetGroup.Store();
                    }
                }

                // Update the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParam = paramMap.GetParamEdit(C_PARAM_OUT_PRIVILEGE_NAME);
                IGPString strValue = new GPStringClass();
                strValue.Value = m_privilegeName;
                outParam.Value = strValue as IGPValue;

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
