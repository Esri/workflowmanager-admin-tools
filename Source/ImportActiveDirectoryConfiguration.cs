using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;
using ESRI.ArcGIS.JTXUI;


namespace WorkflowManagerAdministrationUtilities
{
    class ImportActiveDirectoryConfiguration : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_USER_GROUP = "in_string_groupOfUsers";
        private const string C_PARAM_GROUP_GROUP = "in_string_groupofGroups";
        private const string C_PARAM_PRESERVE_CURRENT_USER = "in_bool_preserveCurrentUser";
        private const string C_PARAM_OUT_NUM_USERS = "out_long_numberOfUsers";
        private const string C_PARAM_OUT_NUM_GROUPS = "out_long_numberOfGroups";

        private const string C_OPT_PRESERVE_USER = "PRESERVE";
        private const string C_OPT_DO_NOT_PRESERVE_USER = "NO_PRESERVE";

        private const bool C_DEFAULT_PRESERVE_CURRENT_USER = false;
        #endregion

        #region MemberVariables
        private string m_userGroup = string.Empty;
        private string m_groupGroup = string.Empty;
        private bool m_preserveCurrentUser = C_DEFAULT_PRESERVE_CURRENT_USER;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "ImportActiveDirectoryConfiguration"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_IMPORT_AD_CONFIG; } }
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
            param = paramMap.GetParam(C_PARAM_USER_GROUP);
            m_userGroup = param.Value.GetAsText();

            param = paramMap.GetParam(C_PARAM_GROUP_GROUP);
            m_groupGroup = param.Value.GetAsText();

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

                // Group containing the user list
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_IADC_USER_GROUP,
                    C_PARAM_USER_GROUP,
                    new GPStringTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Group containing the group list
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_IADC_GROUP_GROUP,
                    C_PARAM_GROUP_GROUP,
                    new GPStringTypeClass(),
                    null);
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
                    Properties.Resources.DESC_IADC_PRESERVE_CURRENT_USER,
                    C_PARAM_PRESERVE_CURRENT_USER,
                    GpBooleanType,
                    ToGpBoolean(C_DEFAULT_PRESERVE_CURRENT_USER));
                paramEdit.Domain = cvDomain as IGPDomain;
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // Number of users imported, as output
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_IADC_OUT_NUM_USERS,
                    C_PARAM_OUT_NUM_USERS,
                    new GPStringTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

                // Number of groups imported, as output
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_IADC_OUT_NUM_GROUPS,
                    C_PARAM_OUT_NUM_GROUPS,
                    new GPStringTypeClass(),
                    null);
                m_parameters.Add(paramEdit);

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

                // Stash away the executing user's information, if appropriate
                string username = ESRI.ArcGIS.JTXUI.ConfigurationCache.GetCurrentSystemUser(ESRI.ArcGIS.JTXUI.ConfigurationCache.UseUserDomain);
                IJTXConfiguration3 configMgr = this.WmxDatabase.ConfigurationManager as IJTXConfiguration3;
                IJTXUser3 executingUser = configMgr.GetUser(username) as IJTXUser3;

                // Import the AD information
                string domain = System.Environment.UserDomainName;
                string domainUsername = string.Empty;
                string domainPassword = string.Empty;
                int numUsers = 0;
                int numGroups = 0;
                ActiveDirectoryHelper.SyncronizeJTXDatabaseWithActiveDirectory(this.WmxDatabase, domain, domainUsername, domainPassword, m_userGroup, m_groupGroup, out numGroups, out numUsers);

                // If the tool was set to preserve the current user's account and the user
                // was removed from the DB, then re-add their account
                if (configMgr.GetUser(username) == null)
                {
                    if (m_preserveCurrentUser)
                    {
                        IJTXConfigurationEdit2 configEdit = this.WmxDatabase.ConfigurationManager as IJTXConfigurationEdit2;
                        IJTXUserConfig newUser = configEdit.CreateUser() as IJTXUserConfig;
                        newUser.FirstName_2 = executingUser.FirstName;
                        newUser.FullName_2 = executingUser.FullName;
                        newUser.LastName_2 = executingUser.LastName;
                        newUser.UserName_2 = executingUser.UserName;
                        (newUser as IJTXUser3).IsAdministrator = executingUser.IsAdministrator;
                        newUser.Store();

                        msgs.AddMessage("User '" + username + "' not found in Active Directory group '" + m_userGroup + "'; re-added placeholder to Workflow Manager database");
                    }
                    else
                    {
                        msgs.AddWarning("User '" + username + "' removed from Workflow Manager database");
                    }
                }

                // Update the output parameters
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParam = paramMap.GetParamEdit(C_PARAM_OUT_NUM_USERS);
                IGPLong value = new GPLongClass();
                value.Value = numUsers;
                outParam.Value = value as IGPValue;

                outParam = paramMap.GetParamEdit(C_PARAM_OUT_NUM_GROUPS);
                value = new GPLongClass();
                value.Value = numGroups;
                outParam.Value = value as IGPValue;

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
