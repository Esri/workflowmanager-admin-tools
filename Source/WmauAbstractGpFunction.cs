using System;
using System.Collections.Generic;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.JTX;


namespace WorkflowManagerAdministrationUtilities
{
    /// <summary>
    /// Abstract base class for GP tools. Provides default implementations for
    /// several of the IGPFunction members.
    /// </summary>
    public abstract class WmauAbstractGpFunction : IGPFunction2
    {
        #region Constants
        private const string C_PARAM_WMX_DATABASE_ALIAS = "in_string_wmxDatabaseAlias";

        private const string C_ENV_VAR_DEFAULT_WMX_DB = "DEFAULT_JTX_DB";
        #endregion

        #region Class Variables
        private static IAoInitialize m_aoInit = null;
        #endregion

        #region Member Variables
        protected IArray m_parameters = null;
        protected IGPUtilities m_gpUtilities = new GPUtilitiesClass();
        private IJTXDatabaseManager2 m_wmxDbMgr = null;
        private IJTXDatabase3 m_previousWmxDb = null;
        private string m_wmxDbAlias = string.Empty;
        private HashSet<string> m_dependentParamNames = new HashSet<string>();
        private Dictionary<string, IJTXDatabase3> m_wmxDbInfo = new Dictionary<string, IJTXDatabase3>();

        private IGPBoolean m_gpTrue = null;
        private IGPBoolean m_gpFalse = null;
        #endregion

        #region IComparer Functionality
        /// <summary>
        /// Required by the IComparer interface
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public int Compare(WmauAbstractGpFunction f1, WmauAbstractGpFunction f2)
        {
            return f1.Name.CompareTo(f2.Name);
        }
        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        public WmauAbstractGpFunction()
        {
            // WORKAROUND: Encountered a problem where the background GP tools did not
            // appear to be checking out the extension licenses soon enough for the
            // admin utilities.  The problem arose when a utility would try to access
            // the WMX database to determine what its parameter domains should be, but
            // the WMX license wasn't yet checked out... so the tool's ParameterInfo
            // method would fail.
            //
            // By trying to check out the extension here, we can work around this problem
            // and (hopefully) not introduce any licensing problems elsewhere.
            if (WmauAbstractGpFunction.m_aoInit == null)
            {
                WmauAbstractGpFunction.m_aoInit = new AoInitializeClass();
                esriLicenseStatus wmxLicenseStatus =
                    WmauAbstractGpFunction.m_aoInit.CheckOutExtension(esriLicenseExtensionCode.esriLicenseExtensionCodeWorkflowManager);
                if (wmxLicenseStatus == esriLicenseStatus.esriLicenseFailure ||
                    wmxLicenseStatus == esriLicenseStatus.esriLicenseNotLicensed ||
                    wmxLicenseStatus == esriLicenseStatus.esriLicenseUnavailable)
                {
                    throw new WmauException(WmauErrorCodes.C_LICENSE_RELATED_ERROR);
                }
            }
            // END WORKAROUND

            m_gpTrue = new GPBooleanClass();
            m_gpTrue.Value = true;
            m_gpFalse = new GPBooleanClass();
            m_gpFalse.Value = false;
        }

        #region Accessors
        protected IJTXDatabaseManager2 WmxDatabaseManager
        {
            get
            {
                if (this.IsWorkflowManagerDatabaseSet())
                {
                    return m_wmxDbMgr;
                }
                else
                {
                    return null;
                }
            }
        }

        protected IJTXDatabase3 WmxDatabase
        {
            get
            {
                if (this.IsWorkflowManagerDatabaseSet())
                {
                    IJTXDatabase3 retVal = m_wmxDbInfo[m_wmxDbAlias];

                    if (retVal != m_previousWmxDb)
                    {
                        // If the database in use has changed, invalidate and reinitialize the
                        // configuration cache.  This is used internally by various Workflow
                        // Manager items; if it is not initialized, some of them will (right
                        // or wrong) throw errors.
                        ESRI.ArcGIS.JTXUI.ConfigurationCache.InvalidateCache();
                        ESRI.ArcGIS.JTXUI.ConfigurationCache.InitializeCache(m_wmxDbInfo[m_wmxDbAlias]);
                        m_previousWmxDb = retVal;
                    }

                    return retVal;
                }
                else
                {
                    // Invalidate the configuration cache if there's a problem finding
                    // the database
                    ESRI.ArcGIS.JTXUI.ConfigurationCache.InvalidateCache();

                    return null;
                }
            }
        }
        #endregion

        /// <summary>
        /// Helper function to check to see if a Workflow Manager database can be
        /// determined/set.  (Considers any manually specified databases first; if
        /// none were specified, attempts to use the default Workflow Manager
        /// database.)
        /// </summary>
        /// <returns></returns>
        protected bool IsWorkflowManagerDatabaseSet()
        {
            bool isDatabaseSet = false;

            try
            {
                // Get a handle to the database manager, if need be
                if (m_wmxDbMgr == null)
                {
                    m_wmxDbMgr = new JTXDatabaseManagerClass();
                }

                // Determine the default Workflow Manager database if none has been specified
                if (string.IsNullOrEmpty(m_wmxDbAlias))
                {
                    // WORKAROUND: Query the environment variable directly for the alias
                    // of the default workflow manager DB.  IJTXDatabaseManager.GetActiveDatabase()
                    // is a fairly expensive call, so this provides a way to avoid making
                    // it unnecessarily.
                    m_wmxDbAlias = System.Environment.GetEnvironmentVariable(C_ENV_VAR_DEFAULT_WMX_DB);
                    if (m_wmxDbAlias == null)
                    {
                        m_wmxDbAlias = string.Empty;
                    }
                }

                // If we don't already have a database object for this database, get one
                // and cache it away
                if (!m_wmxDbInfo.ContainsKey(m_wmxDbAlias))
                {
                    IJTXDatabase3 tempDb = m_wmxDbMgr.GetDatabase(m_wmxDbAlias) as IJTXDatabase3;

                    // If this is the first time we've retrieved the information for the
                    // default database, store it away
                    if (!m_wmxDbInfo.ContainsKey(m_wmxDbAlias) && tempDb != null)
                    {
                        m_wmxDbInfo[m_wmxDbAlias] = tempDb;
                        ESRI.ArcGIS.JTXUI.ConfigurationCache.InvalidateCache();
                        ESRI.ArcGIS.JTXUI.ConfigurationCache.InitializeCache(tempDb);
                    }
                }

                // At this point, the alias should be set, and the wmx DB connection should
                // be stored in the table.  Do one last set of sanity checks before setting
                // the return value.
                if (m_wmxDbInfo.ContainsKey(m_wmxDbAlias) && m_wmxDbInfo[m_wmxDbAlias] != null)
                {
                    isDatabaseSet = true;
                }
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                m_wmxDbInfo.Remove(m_wmxDbAlias);
                m_wmxDbMgr = null;
                m_wmxDbAlias = string.Empty;

                int errorCode;

                // Check for license-related problems
                if (comEx.ErrorCode == (int)ESRI.ArcGIS.JTX.jtxCoreError.E_JTXCORE_ERR_LICENSE_NOT_CHECKED_OUT ||
                    comEx.ErrorCode == (int)ESRI.ArcGIS.JTX.jtxCoreError.E_JTXCORE_ERR_LICENSE_MISSING_PRODUCT ||
                    comEx.ErrorCode == (int)ESRI.ArcGIS.JTX.jtxCoreError.E_JTXCORE_ERR_LICENSE_INCOMPATIBLE_PRODUCT)
                {
                    throw new WmauException(WmauErrorCodes.C_LICENSE_RELATED_ERROR, comEx);
                }

                unchecked
                {
                    errorCode = (int)0x80040AF4;
                }

                if (comEx.ErrorCode != errorCode)
                {
                    throw comEx;
                }
            }

            return isDatabaseSet;
        }

        /// <summary>
        /// Helper function to change the active Workflow Manager database to the
        /// database with the given alias.
        /// </summary>
        /// <param name="newDbAlias"></param>
        private void ChangeWmxDatabase(string newDbAlias, WmauParameterMap paramMap)
        {
            m_wmxDbAlias = newDbAlias;
            if (!IsWorkflowManagerDatabaseSet())
            {
                throw new WmauException(WmauErrorCodes.C_INVALID_WMX_DB_ERROR);
            }

            // Once the Workflow Manager database has been changed, 
            foreach (string paramName in m_dependentParamNames)
            {
                paramMap.GetParamEdit(paramName).Domain = null;
            }
        }

        /// <summary>
        /// Set the name of the function tool. 
        /// This name appears when executing the tool at the command line or in scripting. 
        /// This name should be unique to each toolbox and must not contain spaces.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Set the function tool Display Name as seen in ArcToolbox.
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Sets the toolset of the tool as seen in ArcToolbox (
        /// </summary>
        public abstract string DisplayToolset { get; }

        /// <summary>
        /// Mandatory override. Provide IArray object containing input and output parameters of the tool.
        /// </summary>
        /// <remarks>
        /// NOTE #1: you must wrap your code in a check to see if m_parameters has already been populated,
        /// e.g. 'if (m_parameters.Count == 0) { ... }'
        /// 
        /// NOTE #2 (BJD): The following link (and numerous examples) seem to directly contradict note #1:
        /// http://help.arcgis.com/en/sdk/10.0/arcobjects_net/conceptualhelp/index.html#//00010000049w000000
        /// "You must always create a new array in ParameterInfo(); never copy or clone an existing array."
        /// Plus, I'm not sure where note #1 came from.
        /// 
        /// Who's right?
        /// </remarks>
        public abstract IArray ParameterInfo { get; }

        /// <summary>
        /// Helper function for the "UpdateMessages" interface; meant to be called directly by
        /// child classes who override "UpdateMessages", rather than calling "UpdateMessages"
        /// itself.
        /// </summary>
        /// <param name="paramValues">The IArray of parameters passed into UpdateMessages</param>
        /// <param name="pEnvMgr">The GP environment manager object</param>
        /// <param name="msgs">The GP messages object to be updated by this function</param>
        protected void UpdateMessagesCommon(IArray paramValues, IGPEnvironmentManager pEnvMgr, IGPMessages msgs)
        {
            // Ensure that a Workflow Manager database is set.
            if (!IsWorkflowManagerDatabaseSet())
            {
                if (msgs.Count > 0)
                {
                    WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                    WmauError error = new WmauError(WmauErrorCodes.C_INVALID_WMX_DB_ERROR);
                    msgs.ReplaceError(paramMap.GetIndex(C_PARAM_WMX_DATABASE_ALIAS), error.ErrorCodeAsInt, error.Message);
                }

                throw new WmxDefaultDbNotSetException();
            }
        }

        /// <summary>
        /// <b>Post</b> validates the given set of values.
        /// This is where you flag parameters with warnings and error messages, among other things.
        /// 
        /// Any function that overrides UpdateMessages should itself call "updateMessagesCommon"
        /// first and do its own exception handling.  The parent class' implementation is only meant
        /// to be used by those GP tools that are simple enough to not need their own implementation
        /// of UpdateMessages().
        /// </summary>
        public virtual void UpdateMessages(IArray paramValues, IGPEnvironmentManager pEnvMgr, IGPMessages msgs)
        {
            try
            {
                UpdateMessagesCommon(paramValues, pEnvMgr, msgs);
            }
            catch (WmxDefaultDbNotSetException)
            {
                // If the default DB wasn't set, stop executing
                return;
            }
        }

        /// <summary>
        /// Helper function for the "UpdateParameters" interface; meant to be called directly by
        /// child classes who override "UpdateParameters", rather than calling "UpdateParameters"
        /// itself.
        /// </summary>
        /// <param name="paramValues">The IArray of parameters passed into UpdateParameters</param>
        /// <param name="pEnvMgr">The GP environment manager object</param>
        protected void UpdateParametersCommon(IArray paramValues, IGPEnvironmentManager pEnvMgr)
        {
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);
            IGPParameter3 param = null;

            // Update the internal values of whatever parameters the parent class
            // is maintaining
            param = paramMap.GetParam(C_PARAM_WMX_DATABASE_ALIAS);
            string newDbAlias = param.Value.GetAsText();

            // If the WMX database has changed, update it.
            if (!newDbAlias.Equals(m_wmxDbAlias))
            {
                ChangeWmxDatabase(newDbAlias, paramMap);
            }

            // Ensure that the default Workflow Manager database is set.
            if (!IsWorkflowManagerDatabaseSet())
            {
                throw new WmxDefaultDbNotSetException();
            }
            if (paramValues == null || pEnvMgr == null)
            {
                throw new NullReferenceException();
            }
        }

        /// <summary>
        /// <b>Pre</b> validates the given set of values.
        /// This is where you populate derived parameters based on input, among other things.
        /// 
        /// Any function that overrides UpdateParameters should itself call "updateParametersCommon"
        /// first and do its own exception handling.  The parent class' implementation is only meant
        /// to be used by those GP tools that are simple enough to not need their own implementation
        /// of UpdateParameters().
        /// </summary>
        /// <param name="paramValues"></param>
        /// <param name="pEnvMgr"></param>
        public virtual void UpdateParameters(IArray paramValues, IGPEnvironmentManager pEnvMgr)
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
        }

        /// <summary>
        /// Obsolete method; no longer called by the ArcObjects framework, but
        /// still required to meet the IGPFunction2 interface.
        /// </summary>
        /// <param name="paramvalues"></param>
        /// <param name="updateValues"></param>
        /// <param name="envMgr"></param>
        /// <returns></returns>
        public virtual IGPMessages Validate(IArray paramValues, bool updateValues, IGPEnvironmentManager envMgr)
        {
            return null;
        }

        /// <summary>
        /// Mandatory override. Provide GP tool body and logic.
        /// </summary>
        public virtual void Execute(IArray paramValues, ITrackCancel trackCancel, IGPEnvironmentManager envMgr, IGPMessages msgs)
        {
            // Basic error checking; ensure a Workflow Manager database is defined
            if (!IsWorkflowManagerDatabaseSet())
            {
                WmauError error = new WmauError(WmauErrorCodes.C_INVALID_WMX_DB_ERROR);
                msgs.AddError(error.ErrorCodeAsInt, error.Message);
                throw new WmxDefaultDbNotSetException();
            }

            // Update the internal parameters used by this GP tool
            this.ExtractParameters(paramValues);
        }

        /// <summary>
        /// This is the name of the (.xml) file containing the default metadata for this function tool. 
        /// The metadata file is used to supply the parameter descriptions in the help panel in the dialog. 
        /// If no (.chm) file is provided, the help is based on the metadata file. 
        /// </summary>
        /// <remarks>ESRI Knowledge Base article #27000 provides more information about creating a metadata file.</remarks>
        public virtual string MetadataFile
        {
            get
            {
                return this.Name + "_WMXAdminUtils.xml";
            }
        }

        /// <summary>
        /// This is the function name object for the Geoprocessing Function Tool. 
        /// This name object is created and returned by the Function Factory.
        /// The Function Factory must first be created before implementing this property.
        /// </summary>
        public IName FullName
        {
            get
            {
                IGPFunctionFactory functionFactory = new WmauFunctionFactory();
                return (IName)functionFactory.GetFunctionName(this.Name);
            }
        }

        /// <summary>
        /// This is used to set a custom renderer for the output of the Function Tool.
        /// </summary>
        public virtual object GetRenderer(IGPParameter pParam)
        {
            return null;
        }

        /// <summary>
        /// This is the unique context identifier in a [MAP] file (.h). 
        /// </summary>
        /// <remarks>ESRI Knowledge Base article #27680 provides more information about creating a [MAP] file.</remarks>
        public virtual int HelpContext
        {
            get { return 0; }
        }

        /// <summary>
        /// This is the path to a .chm file which is used to describe and explain the function and its operation. 
        /// </summary>
        public virtual string HelpFile
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// This is used to return whether the function tool is licensed to execute.
        /// </summary>
        /// <remarks>Override to provide custom licensing check</remarks>
        /// <returns></returns>
        public virtual bool IsLicensed()
        {
            IAoInitialize aoi = new AoInitializeClass();
            esriLicenseStatus status = aoi.CheckOutExtension(esriLicenseExtensionCode.esriLicenseExtensionCodeWorkflowManager);
            if (status == esriLicenseStatus.esriLicenseCheckedOut ||
                status == esriLicenseStatus.esriLicenseAlreadyInitialized ||
                status == esriLicenseStatus.esriLicenseAvailable)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This is the class id used to override the default dialog for a tool. 
        /// By default ArcToolbox will create a dialog based upon the parameters returned 
        /// by the ParameterInfo property.
        /// </summary>
        /// <remarks>Override to provide a custom UI dialog in ArcToolbox</remarks>
        public virtual UID DialogCLSID
        {
            get { return null; }
        }

        /// <summary>
        /// Function to return a GP type with a "true" value
        /// </summary>
        public IGPValue GpTrue
        {
            get
            {
                return m_gpTrue as IGPValue;
            }
        }

        /// <summary>
        /// Function to return a GP type with a "false" value
        /// </summary>
        public IGPValue GpFalse
        {
            get
            {
                return m_gpFalse as IGPValue;
            }
        }

        /// <summary>
        /// Function to return a GPBooleanType object.  Helpful for avoiding
        /// warnings when building tool parameters.
        /// </summary>
        public IGPDataType GpBooleanType
        {
            get
            {
                return GpTrue.DataType;
            }
        }

        /// <summary>
        /// Converts a boolean value to a GP Boolean type.  Helpful for avoiding
        /// warnings when building tool parameters
        /// </summary>
        /// <param name="value">The boolean value to convert</param>
        /// <returns>An IGPValue representing the specified boolean value</returns>
        public IGPValue ToGpBoolean(bool value)
        {
            return value ? GpTrue : GpFalse;
        }

        /// <summary>
        /// Helper function to assemble the parameter objects for the GP tool.  Assumes
        /// that the parameter's domain is not dependent on the chosen Workflow Manager
        /// database.
        /// </summary>
        /// <param name="dir">
        /// The "direction" of the parameter (input, output, ...); one of the
        /// esriGPParameterDirection enum values</param>
        /// <param name="paramType">One of the esriGPParameterType enum values</param>
        /// <param name="dispName">The name of the parameter displayed to the user</param>
        /// <param name="name">The internal name of the parameter</param>
        /// <param name="dataType">The type of data stored by the parameter</param>
        /// <param name="value">The initial value of the parameter</param>
        /// <returns>The GP parameter object that was created</returns>
        protected IGPParameterEdit3 BuildParameter(
            esriGPParameterDirection dir,
            esriGPParameterType paramType,
            string dispName,
            string name,
            IGPDataType dataType,
            IGPValue value)
        {
            return BuildParameter(dir, paramType, dispName, name, dataType, value, false);
        }

        /// <summary>
        /// Helper function to assemble the parameter objects for the GP tool
        /// </summary>
        /// <param name="dir">
        /// The "direction" of the parameter (input, output, ...); one of the
        /// esriGPParameterDirection enum values</param>
        /// <param name="paramType">One of the esriGPParameterType enum values</param>
        /// <param name="dispName">The name of the parameter displayed to the user</param>
        /// <param name="name">The internal name of the parameter</param>
        /// <param name="dataType">The type of data stored by the parameter</param>
        /// <param name="value">The initial value of the parameter</param>
        /// <param name="domainDependsOnWmxDb">
        /// Specifying true will cause the parameter's domain to be set to null when
        /// the selected Workflow Manager database changes; false will leave the domain
        /// unchanged.
        /// </param>
        /// <returns>The GP parameter object that was created</returns>
        protected IGPParameterEdit3 BuildParameter(
            esriGPParameterDirection dir,
            esriGPParameterType paramType,
            string dispName,
            string name,
            IGPDataType dataType,
            IGPValue value,
            bool domainDependsOnWmxDb)
        {
            IGPParameterEdit3 param = new GPParameterClass();

            param.Direction = dir;
            param.ParameterType = paramType;
            param.Enabled = true;
            param.DisplayName = dispName;
            param.Name = name;
            param.DataType = dataType;
            if (value == null)
            {
                param.Value = dataType.CreateValue(string.Empty);
            }
            else
            {
                param.Value = value;
            }

            if (domainDependsOnWmxDb)
            {
                m_dependentParamNames.Add(name);
            }

            return param;
        }

        /// <summary>
        /// Helper function to set up a GP tool parameter so that the workflow manager
        /// database can be chosen.
        /// </summary>
        /// <returns>A parameter for selecting the target WMX DB</returns>
        protected IGPParameter3 BuildWmxDbParameter()
        {
            IGPParameterEdit paramEdit = null;
            IGPCodedValueDomain cvDomain = new GPCodedValueDomainClass();

            // When we first build out the parameter list, ensure that we indicate
            // what the current Workflow Manager database is
            m_wmxDbAlias = string.Empty;
            IsWorkflowManagerDatabaseSet();

            // Parameter allowing specification of the Workflow Manager database
            IGPString strVal = new GPStringClass();
            strVal.Value = m_wmxDbAlias;
            paramEdit = BuildParameter(
                esriGPParameterDirection.esriGPParameterDirectionInput,
                esriGPParameterType.esriGPParameterTypeOptional,
                Properties.Resources.DESC_COM_WMX_DATABASE,
                C_PARAM_WMX_DATABASE_ALIAS,
                (strVal as IGPValue).DataType,
                strVal as IGPValue);

            IJTXDatabaseConnectionManager connMgr = new JTXDatabaseConnectionManagerClass();
            foreach (string alias in connMgr.DatabaseNames)
            {
                cvDomain.AddStringCode(alias, alias);
            }
            paramEdit.Domain = cvDomain as IGPDomain;

            return paramEdit as IGPParameter3;
        }

        /// <summary>
        /// Checks to see if the user running this program holds the specified Workflow
        /// Manager privilege
        /// </summary>
        /// <param name="privilegeName">The name of the privilege to check</param>
        /// <returns>true if the user has this privilege; false otherwise</returns>
        protected bool CurrentUserHasPrivilege(string privilegeName)
        {
            return ESRI.ArcGIS.JTXUI.ConfigurationCache.CurrentUserHasPrivilege(privilegeName);
        }

        /// <summary>
        /// Checks to see if the user running this program is an administrator in
        /// the Workflow Manager database.
        /// </summary>
        /// <returns>true if the user is an administrator; false otherwise</returns>
        protected bool CurrentUserIsWmxAdministrator()
        {
            bool retVal = false;

            string username = ESRI.ArcGIS.JTXUI.ConfigurationCache.GetCurrentSystemUser(ESRI.ArcGIS.JTXUI.ConfigurationCache.UseUserDomain);
            if (this.WmxDatabase != null)
            {
                IJTXUser3 user = this.WmxDatabase.ConfigurationManager.GetUser(username) as IJTXUser3;
                if (user != null)
                {
                    retVal = user.IsAdministrator;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Updates the internal values used by this tool based on the parameters from an input array
        /// </summary>
        /// <param name="paramValues"></param>
        protected void ExtractParametersCommon(IArray paramValues)
        {
            WmauParameterMap paramMap = new WmauParameterMap(paramValues);
            IGPParameter3 param = null;

            // Update the internal values of whatever parameters the parent class
            // is maintaining
            param = paramMap.GetParam(C_PARAM_WMX_DATABASE_ALIAS);
            string newDbAlias = param.Value.GetAsText();
            
            // If the WMX database has changed, update it.
            if (!newDbAlias.Equals(m_wmxDbAlias))
            {
                ChangeWmxDatabase(newDbAlias, paramMap);
            }
        }

        /// <summary>
        /// Mandatory override; updates the internal values used by this tool based on
        /// the parameters from an input array
        /// </summary>
        /// <param name="paramValues"></param>
        protected abstract void ExtractParameters(IArray paramValues);
    }
}
