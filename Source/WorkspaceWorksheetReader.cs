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

using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.JTX;

using Microsoft.Office.Interop.Excel;


namespace WorkflowManagerAdministrationUtilities
{
    class WorkspaceWorksheetReader
    {
        #region Constants
        public const string C_DB_WORKSHEET_NAME = "Databases";
        public const string C_DB_ALIAS = "Database Alias";
        public const string C_SERVER = "Server";
        public const string C_INSTANCE = "Instance";
        public const string C_DATABASE = "Database";
        public const string C_VERSION = "Version";
        public const string C_OS_AUTH = "OS Authentication";
        public const string C_INDIVIDUAL_LOGINS = "Individual Logins";
        public const string C_USERNAME = "Username";
        public const string C_PASSWORD = "Password";
        public const string C_IS_ENCRYPTED = "Is Encrypted";

        public const string C_WMX_USERNAME = "Workflow Manager Username";
        public const string C_DB_USERNAME = "Database Username";
        public const string C_DB_PASSWORD = "Database Password";
        #endregion

        #region Member variables
        private string m_filename;
        private bool m_isOpen;
        private IGPMessages m_gpMessages;
        private Application m_excelObj = null;
        private Workbook m_workbook = null;
        #endregion

        #region Accessor methods
        public string Filename
        {
            get
            {
                return m_filename;
            }
        }

        public bool IsOpen
        {
            get
            {
                return m_isOpen;
            }
        }
        #endregion

        #region Helper functions
        /// <summary>
        /// Helper function to convert a 1-based column index to its Excel "column" notation
        /// (ex: "A", "D", "BF", etc.)
        /// </summary>
        /// <param name="col">The 1-based index of the column</param>
        /// <returns>The Excel column letter(s) for this column</returns>
        private string GetExcelColumnFromIndex(int col)
        {
            string excelColumn = ((char)((int)'A' + ((col-1) % 26))).ToString();
            if (col > 26)
            {
                excelColumn = GetExcelColumnFromIndex(col / 26) + excelColumn;
            }

            return excelColumn;
        }

        /// <summary>
        /// Looks up a worksheet by name, once a workbook has been opened
        /// </summary>
        /// <param name="worksheetName"></param>
        /// <returns></returns>
        private Worksheet GetWorksheetByName(string worksheetName)
        {
            Worksheet worksheet = null;

            foreach (Worksheet sheet in m_workbook.Worksheets)
            {
                if (sheet.Name.Equals(worksheetName, StringComparison.CurrentCultureIgnoreCase))
                {
                    worksheet = sheet;
                    break;
                }
            }

            return worksheet;
        }

        /// <summary>
        /// Fetches the value from a cell as a bool
        /// </summary>
        /// <param name="cells">The range of cells that serve as the source</param>
        /// <param name="row">The index of the row to retrieve (1-based)</param>
        /// <param name="col">The index of the column to retrieve (1-based)</param>
        /// <returns>
        /// The value from the specified cell, as a bool; returns false if
        /// the cell is empty
        /// </returns>
        private bool GetValueAsBool(Range cells, int row, int col)
        {
            bool retVal = false;

            string tempStr = GetValueAsText(cells, row, col);
            try
            {
                retVal = bool.Parse(tempStr);
            }
            catch (FormatException)
            {
                // If a value can't be parsed, let it default to false
            }

            return retVal;
        }

        /// <summary>
        /// Fetches the value from a cell as a bool; assumes a range with a single row
        /// </summary>
        /// <param name="cells">The range of cells that serve as the source</param>
        /// <param name="col">The index of the column to retrieve (1-based)</param>
        /// <returns>
        /// The value from the specified cell in the first row of the range, as a bool; returns
        /// false if the cell is empty
        /// </returns>
        private bool GetValueAsBool(Range cells, int col)
        {
            return GetValueAsBool(cells, 1, col);
        }

        /// <summary>
        /// Fetches the value from a cell as a string
        /// </summary>
        /// <param name="cells">The range of cells that serve as the source</param>
        /// <param name="row">The index of the row to retrieve (1-based)</param>
        /// <param name="col">The index of the column to retrieve (1-based)</param>
        /// <returns>
        /// The value from a particular cell, as a string; returns the empty string if
        /// a cell is empty
        /// </returns>
        private string GetValueAsText(Range cells, int row, int col)
        {
            Range cell = cells[row, col] as Range;
            object obj = cell.Value2;
            string retVal = string.Empty;
            if (obj != null)
            {
                retVal = obj.ToString();
            }
            return retVal;
        }

        /// <summary>
        /// Fetches the value from a cell as a string; assumes a range with a single row
        /// </summary>
        /// <param name="cells">The range of cells that serve as the source</param>
        /// <param name="col">The index of the column to retrieve (1-based)</param>
        /// <returns>
        /// The value from the specified cell in the first row of the range, as a string; returns
        /// the empty string if the cell is empty
        /// </returns>
        private string GetValueAsText(Range cells, int col)
        {
            return GetValueAsText(cells, 1, col);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchRange"></param>
        /// <param name="text"></param>
        /// <returns>Returns the first cell that contains a value matching the given text string</returns>
        private Range FindCellMatchingText(Range searchRange, string text)
        {
            Range retVal = searchRange.Find(
                text,
                Type.Missing,
                XlFindLookIn.xlValues,
                XlLookAt.xlWhole,
                XlSearchOrder.xlByColumns,
                XlSearchDirection.xlNext,
                true,
                Type.Missing,
                Type.Missing);
            return retVal;
        }

        /// <summary>
        /// Helper function to hide all these ridiculous "Missing" directives
        /// </summary>
        /// <param name="excelObj"></param>
        /// <returns></returns>
        private Workbook OpenWorkbook(Application excelObj)
        {
            Workbook retVal = excelObj.Workbooks.Open(
                Filename,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing,
                Type.Missing);

            excelObj.WindowState = XlWindowState.xlMinimized;
            return retVal;
        }

        /// <summary>
        /// A helper function used to retrieve individual login information from
        /// a workspace-specific spreadsheet.
        /// </summary>
        /// <param name="worksheetName"></param>
        /// <returns></returns>
        private List<Common.WorkspaceInfo.LoginInfo> GetLoginInfoFromSpreadsheet(string worksheetName)
        {
            // Error checking: make sure that the required worksheet can be found
            if (!this.Open())
            {
                throw new Exception("Failed to open Excel workbook");
            }
            Worksheet worksheet = this.GetWorksheetByName(worksheetName);
            if (worksheet == null)
            {
                throw new Exception("Failed to open worksheet: '" + worksheetName + "'");
            }

            // Create the output element
            List<Common.WorkspaceInfo.LoginInfo> loginList = new List<Common.WorkspaceInfo.LoginInfo>();

            // Do some math and set up the "column header" range (used to determine which field is which)
            int numColumns = worksheet.UsedRange.Columns.Count;
            int numRows = worksheet.UsedRange.Rows.Count;
            int dataStartRowNum = 2;
            int dataEndRowNum = dataStartRowNum + numRows;
            string headerStartCell = "A1";
            string headerEndCell = GetExcelColumnFromIndex(numColumns) + "1";
            Range headerRange = worksheet.get_Range(headerStartCell, headerEndCell);

            // Iterate through all of the data rows (i.e., workspaces) in the worksheet
            for (int rowNum = dataStartRowNum; rowNum < dataEndRowNum; rowNum++)
            {
                // Create a range for this row of cells
                string dataStartCell = "A" + rowNum.ToString();
                string dataEndCell = GetExcelColumnFromIndex(numColumns) + rowNum.ToString();
                Range dataRange = worksheet.get_Range(dataStartCell, dataEndCell);

                // Initialize the various values of the workspace configuration object
                string name = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_WMX_USERNAME).Column);
                if (string.IsNullOrEmpty(name))
                {
                    // Stop reading through the worksheet once we've found a row without any workspace listed
                    break;
                }

                string username = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_DB_USERNAME).Column);
                string password = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_DB_PASSWORD).Column);
                bool isEncrypted = GetValueAsBool(dataRange, FindCellMatchingText(headerRange, C_IS_ENCRYPTED).Column);

                // Only base64-encoded encrypted passwords are supported by this
                // utility, so unencode the password before sending it along to
                // the WMX object
                if (isEncrypted)
                {
                    byte[] pwBytes = System.Convert.FromBase64String(password);
                    password = System.Text.UTF8Encoding.UTF8.GetString(pwBytes);
                }

                Common.WorkspaceInfo.LoginInfo loginInfo = new Common.WorkspaceInfo.LoginInfo();
                loginInfo.WmxUsername = name;
                loginInfo.DatabaseUsername = username;
                loginInfo.DatabasePassword = password;
                loginInfo.IsPasswordEncrypted = isEncrypted;
                loginList.Add(loginInfo);
            }

            return loginList;
        }
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="msgs"></param>
        public WorkspaceWorksheetReader(string filename, IGPMessages msgs)
        {
            m_filename = filename;
            m_isOpen = false;
            m_gpMessages = msgs;
        }

        /// <summary>
        /// Destructor/finalizer, just to make sure that everything closes as expected
        /// </summary>
        ~WorkspaceWorksheetReader()
        {
            this.Close();
        }

        /// <summary>
        /// Opens up the workbook, using the filename specified
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            if(!IsOpen)
            {
                try
                {
                    m_excelObj = new Application();
                    m_workbook = OpenWorkbook(m_excelObj);
                    m_isOpen = true;
                }
                catch (NullReferenceException nullEx)
                {
                    WmauError error = new WmauError(WmauErrorCodes.C_EXCEL_WORKBOOK_ERROR);
                    this.m_gpMessages.AddError(error.ErrorCodeAsInt, error.Message + "; Stack Trace: " + nullEx.StackTrace);
                    Close();
                }
            }

            return IsOpen;
        }

        // Closes the TFDM job worksheet (and related objects)
        public bool Close()
        {
            if (m_workbook != null)
            {
                m_workbook.Close(false, Type.Missing, Type.Missing);
                m_workbook = null;
            }
            if (m_excelObj != null)
            {
                m_excelObj.Quit();
                m_excelObj = null;
            }

            m_isOpen = false;

            return true;
        }

        /// <summary>
        /// Returns a list of all of the workspaces in the C_DB_WORKSHEET_NAME worksheet.
        /// Workspaces that could not be opened will not be included.
        /// </summary>
        /// <returns></returns>
        public List<Common.WorkspaceInfo> GetWorkspacesFromSpreadsheet()
        {
            // Error checking: make sure that the required worksheet can be found
            if (!this.Open())
            {
                WmauError error = new WmauError(WmauErrorCodes.C_EXCEL_WORKBOOK_ERROR);
                m_gpMessages.AddError(error.ErrorCodeAsInt, error.Message);
                throw new Exception(error.Message);
            }
            Worksheet worksheet = this.GetWorksheetByName(C_DB_WORKSHEET_NAME);
            if (worksheet == null)
            {
                WmauError error = new WmauError(WmauErrorCodes.C_EXCEL_WORKSHEET_ERROR);
                m_gpMessages.AddError(error.ErrorCodeAsInt, error.Message);
                throw new Exception(error.Message);
            }

            // Create the output element
            List<Common.WorkspaceInfo> workspaceList = new List<Common.WorkspaceInfo>();

            // Do some math and set up the "column header" range (used to determine which field is which)
            int numColumns = worksheet.UsedRange.Columns.Count;
            int numRows = worksheet.UsedRange.Rows.Count;
            int dataStartRowNum = 2;
            int dataEndRowNum = dataStartRowNum + numRows;
            string headerStartCell = "A1";
            string headerEndCell = GetExcelColumnFromIndex(numColumns) + "1";
            Range headerRange = worksheet.get_Range(headerStartCell, headerEndCell);

            // Iterate through all of the data rows (i.e., workspaces) in the worksheet
            for (int rowNum = dataStartRowNum; rowNum < dataEndRowNum; rowNum++)
            {
                string name = string.Empty;
                try
                {
                    // Create a range for this row of cells
                    string dataStartCell = "A" + rowNum.ToString();
                    string dataEndCell = GetExcelColumnFromIndex(numColumns) + rowNum.ToString();
                    Range dataRange = worksheet.get_Range(dataStartCell, dataEndCell);

                    // Initialize the various values of the workspace configuration object
                    name = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_DB_ALIAS).Column);
                    if (string.IsNullOrEmpty(name))
                    {
                        // Stop reading through the worksheet once we've found a row without any workspace listed
                        break;
                    }

                    // Create the workspace configuration object that will be set up from this row
                    Common.WorkspaceInfo workspaceInfo = new Common.WorkspaceInfo();
                    workspaceInfo.Name = name;
                    workspaceInfo.Server = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_SERVER).Column);
                    workspaceInfo.Instance = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_INSTANCE).Column);
                    workspaceInfo.Database = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_DATABASE).Column);
                    workspaceInfo.Version = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_VERSION).Column);
                    workspaceInfo.UseOsAuthentication = GetValueAsBool(dataRange, FindCellMatchingText(headerRange, C_OS_AUTH).Column);
                    workspaceInfo.UseIndividualLogins = GetValueAsBool(dataRange, FindCellMatchingText(headerRange, C_INDIVIDUAL_LOGINS).Column);
                    if (workspaceInfo.UseOsAuthentication)
                    {
                        if (workspaceInfo.UseIndividualLogins)
                        {
                            throw new Exception("Workspace '" + name + "': OS authentication and individual logins are mutually exclusive");
                        }

                        // Nothing else to do if OS authentication is being used
                    }
                    else
                    {
                        if (workspaceInfo.UseIndividualLogins)
                        {
                            List<Common.WorkspaceInfo.LoginInfo> loginList =
                                GetLoginInfoFromSpreadsheet(workspaceInfo.Name);
                            foreach (Common.WorkspaceInfo.LoginInfo login in loginList)
                            {
                                workspaceInfo.AddLogin(
                                    login.WmxUsername,
                                    login.DatabaseUsername,
                                    login.DatabasePassword,
                                    login.IsPasswordEncrypted);
                            }
                        }
                        else
                        {
                            string username = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_USERNAME).Column);
                            string password = GetValueAsText(dataRange, FindCellMatchingText(headerRange, C_PASSWORD).Column);
                            bool isEncrypted = GetValueAsBool(dataRange, FindCellMatchingText(headerRange, C_IS_ENCRYPTED).Column);

                            // Only base64-encoded encrypted passwords are supported by this
                            // utility, so unencode the password before sending it along to
                            // the WMX object
                            if (isEncrypted)
                            {
                                byte[] pwBytes = System.Convert.FromBase64String(password);
                                password = System.Text.UTF8Encoding.UTF8.GetString(pwBytes);
                            }

                            // Store the shared username and password
                            workspaceInfo.AddLogin(string.Empty, username, password, isEncrypted);
                        }
                    }

                    workspaceList.Add(workspaceInfo);
                }
                catch (Exception ex)
                {
                    WmauError error = new WmauError(WmauErrorCodes.C_WORKSPACE_FETCH_ERROR);
                    string errMsg = error.Message;
                    errMsg += System.Environment.NewLine + ex.Message;
                    errMsg += System.Environment.NewLine + "Skipping workspace '" + name + "'";
                    m_gpMessages.AddError(error.ErrorCodeAsInt, errMsg);
                    continue;
                }
            }

            this.Close();
            return workspaceList;
        }
    }
}
