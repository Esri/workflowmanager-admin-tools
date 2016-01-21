using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace WorkflowManagerAdministrationUtilities.Common
{
    /// <summary>
    /// Helper class used in place of "IJTXWorkspaceConfiguration".  This class
    /// provides access/storage for the same information without any of the
    /// behind-the-scenes baggage.
    /// </summary>
    class WorkspaceInfo
    {
        #region Helper classes
        /// <summary>
        /// Helper class used to store information about an individual login.
        /// </summary>
        public class LoginInfo
        {
            private string m_wmxUsername = string.Empty;
            private string m_dbUsername = string.Empty;
            private string m_dbPassword = string.Empty;
            private bool m_isPasswordEncrypted = false;

            public LoginInfo() { }

            #region Accessors
            public string WmxUsername
            {
                get { return m_wmxUsername; }
                set { m_wmxUsername = value; }
            }

            public string DatabaseUsername
            {
                get { return m_dbUsername; }
                set { m_dbUsername = value; }
            }

            public string DatabasePassword
            {
                get { return m_dbPassword; }
                set { m_dbPassword = value; }
            }

            public bool IsPasswordEncrypted
            {
                get { return m_isPasswordEncrypted; }
                set { m_isPasswordEncrypted = value; }
            }
            #endregion
        }
        #endregion

        private string m_name = string.Empty;
        private string m_server = string.Empty;
        private string m_instance = string.Empty;
        private string m_database = string.Empty;
        private string m_version = string.Empty;
        private bool m_useOsAuthentication = false;
        private bool m_useIndividualLogins = false;

        private List<LoginInfo> m_loginInfoList = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public WorkspaceInfo()
        {
            m_loginInfoList = new List<LoginInfo>();
        }

        #region Accessor Methods
        /// <summary>
        /// Name of the workspace
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        /// <summary>
        /// Server string used to connect to the database
        /// </summary>
        public string Server
        {
            get { return m_server; }
            set { m_server = value; }
        }

        /// <summary>
        /// Instance string used to connect to the database
        /// </summary>
        public string Instance
        {
            get { return m_instance; }
            set { m_instance = value; }
        }

        /// <summary>
        /// Database name, if needed, to connect to the database
        /// </summary>
        public string Database
        {
            get { return m_database; }
            set { m_database = value; }
        }

        /// <summary>
        /// Version to use once connected to the database
        /// </summary>
        public string Version
        {
            get { return m_version; }
            set { m_version = value; }
        }

        /// <summary>
        /// True if OS authentication should be used when connecting to the database;
        /// false otherwise.
        /// </summary>
        public bool UseOsAuthentication
        {
            get { return m_useOsAuthentication; }
            set { m_useOsAuthentication = value; }
        }

        /// <summary>
        /// True if each Workflow Manager user will use a different database login
        /// when connecting to the database; false otherwise.
        /// </summary>
        public bool UseIndividualLogins
        {
            get { return m_useIndividualLogins; }
            set { m_useIndividualLogins = value; }
        }

        /// <summary>
        /// Provides read-only access to the list of logins associated with this
        /// workspace.
        /// </summary>
        public IList<LoginInfo> Logins
        {
            get
            {
                return m_loginInfoList.AsReadOnly();
            }
        }
        #endregion

        /// <summary>
        /// Adds information about a DB login to the internal list
        /// </summary>
        /// <param name="wmxUsername">The Workflow Manager username associated with this login</param>
        /// <param name="dbUsername">The username with which to log into the database</param>
        /// <param name="dbPassword">The password with which to log into the database</param>
        /// <param name="isEncrypted">True if the password is already encrypted; false otherwise</param>
        public void AddLogin(string wmxUsername, string dbUsername, string dbPassword, bool isEncrypted)
        {
            LoginInfo info = new LoginInfo();
            info.WmxUsername = wmxUsername;
            info.DatabaseUsername = dbUsername;
            info.DatabasePassword = dbPassword;
            info.IsPasswordEncrypted = isEncrypted;

            m_loginInfoList.Add(info);
        }
    }
}
