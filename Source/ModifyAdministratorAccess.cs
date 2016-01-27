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
    class ModifyAdministratorAccess : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_USER_NAME = "in_string_userName";
        private const string C_PARAM_PRIVILEGE_ACTION = "in_string_privilegeAction";
        private const string C_PARAM_PRESERVE_CURRENT_USER = "in_bool_preserveCurrentUser";
        private const string C_PARAM_OUT_USER_NAME = "out_string_userName";

        private const string C_OPT_ALL_USERS = "[All]";

        private const string C_OPT_GRANT = "GRANT";
        private const string C_OPT_REVOKE = "REVOKE";

        private const string C_OPT_PRESERVE_USER = "PRESERVE";
        private const string C_OPT_DO_NOT_PRESERVE_USER = "NO_PRESERVE";

        private const string C_DEFAULT_PRIVILEGE_ACTION = C_OPT_GRANT;
        private const bool C_DEFAULT_PRESERVE_CURRENT_USER = false;
        #endregion

        #region MemberVariables
        private string m_userName = string.Empty;
        private string m_privilegeAction = C_DEFAULT_PRIVILEGE_ACTION;
        private bool m_preserveCurrentUser = C_DEFAULT_PRESERVE_CURRENT_USER;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ModifyAdministratorAccess"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_MODIFY_ADMIN_ACCESS; } }
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
            param = paramMap.GetParam(C_PARAM_USER_NAME);
            m_userName = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_PRIVILEGE_ACTION);
            m_privilegeAction = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_PRESERVE_CURRENT_USER);
            m_preserveCurrentUser = (param.Value as IGPBoolean).Value;
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

                // Name of user(s) affected by the privilege assignment
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_MAA_USER_NAME,
                    C_PARAM_USER_NAME,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter indicating whether admin access should be granted to
                // or revoked from the specified users
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

                // Option indicating whether or not the information for the user running
                // the GP tool should be preserved/re-added in the event that the update
                // removes this user from the Workflow Manager database
                cvDomain = new GPCodedValueDomainClass();
                cvDomain.AddCode(GpTrue, C_OPT_PRESERVE_USER);
                cvDomain.AddCode(GpFalse, C_OPT_DO_NOT_PRESERVE_USER);

                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeOptional,
                    Properties.Resources.DESC_MAA_PRESERVE_CURRENT_USER,
                    C_PARAM_PRESERVE_CURRENT_USER,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_PRESERVE_CURRENT_USER));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // User name (as output)
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_MAA_OUT_USER_NAME,
                    C_PARAM_OUT_USER_NAME,
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

            IGPParameter3 userParam = paramMap.GetParam(C_PARAM_USER_NAME);
            IGPParameterEdit3 userParamEdit = paramMap.GetParamEdit(C_PARAM_USER_NAME);

            // Add a domain to the users parameter
            if (userParam.Domain == null || (userParam.Domain as IGPCodedValueDomain).CodeCount <= 0)
            {
                userParamEdit.Domain = Common.WmauGpDomainBuilder.BuildUsersDomain(this.WmxDatabase, new string[] { C_OPT_ALL_USERS });
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

                // Look up the appropriate user(s)
                IJTXUserSet users = null;
                if (m_userName.Equals(C_OPT_ALL_USERS))
                {
                    users = configEdit.Users;
                }
                else
                {
                    users = new JTXUserSetClass();
                    (users as IJTXUserSetEdit).Add(configEdit.GetUser(m_userName));
                }

                // Grant/revoke admin access to the specified users
                for (int i = 0; i < users.Count; i++)
                {
                    IJTXUser3 user = users.get_Item(i) as IJTXUser3;
                    user.IsAdministrator = m_privilegeAction.Equals(C_OPT_GRANT) ? true : false;
                    (user as IJTXUserConfig).Store();
                }

                // If the tool was set to preserve the current user's access and the tool removed it,
                // re-grant their access
                string username = ESRI.ArcGIS.JTXUI.ConfigurationCache.GetCurrentSystemUser(ESRI.ArcGIS.JTXUI.ConfigurationCache.UseUserDomain);
                IJTXUser3 userObj = configEdit.GetUser(username) as IJTXUser3;
                if (!userObj.IsAdministrator)
                {
                    if (m_preserveCurrentUser)
                    {
                        userObj.IsAdministrator = true;
                        (userObj as IJTXUserConfig).Store();
                        msgs.AddMessage("Re-granting admin access for user '" + username + "'");
                    }
                    else
                    {
                        msgs.AddWarning("User '" + username + "' is no longer an administrator on this Workflow Manager database");
                    }
                }

                // Update the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParam = paramMap.GetParamEdit(C_PARAM_OUT_USER_NAME);
                IGPString strValue = new GPStringClass();
                strValue.Value = m_userName;
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
