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
    class WorkspaceWorksheetWriter
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

        public readonly string[] C_HEADER_COLUMNS =
        {
            C_DB_ALIAS,
            C_SERVER,
            C_INSTANCE,
            C_DATABASE,
            C_VERSION,
            C_OS_AUTH,
            C_INDIVIDUAL_LOGINS,
            C_USERNAME,
            C_PASSWORD,
            C_IS_ENCRYPTED
        };

        public const string C_WMX_USERNAME = "Workflow Manager Username";
        public const string C_DB_USERNAME = "Database Username";
        public const string C_DB_PASSWORD = "Database Password";

        public readonly string[] C_LOGIN_HEADER_COLUMNS =
        {
            C_WMX_USERNAME,
            C_DB_USERNAME,
            C_DB_PASSWORD,
            C_IS_ENCRYPTED
        };
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
        /// A function to create a header row based on an array of strings.
        /// </summary>
        /// <param name="worksheet">The worksheet object for which the header row will be created</param>
        /// <param name="cellValues">An array of strings to be stored in the header row cells</param>
        /// <returns>The range of cells comprising the worksheet header</returns>
        private Range CreateHeaderRow(Worksheet worksheet, string[] cellValues)
        {
            // Do some math and set up the "column header" range (used to determine
            // which field is which)
            int numColumns = cellValues.Length;
            string headerStartCell = "A1";
            string headerEndCell = GetExcelColumnFromIndex(numColumns) + "1";
            Range headerRange = worksheet.get_Range(headerStartCell, headerEndCell);
            for (int i = 0; i < numColumns; i++)
            {
                SetValueAsText(headerRange, i + 1, cellValues[i]);
            }

            // Apply some formatting to these cells (...no real reason)
            headerRange.Font.Bold = true;
            headerRange.HorizontalAlignment = XlHAlign.xlHAlignCenter;
            headerRange.VerticalAlignment = XlVAlign.xlVAlignCenter;
            headerRange.EntireColumn.AutoFit();

            return headerRange;
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
        /// Helper function to convert a 1-based column index to its Excel "column" notation
        /// (ex: "A", "D", "BF", etc.)
        /// </summary>
        /// <param name="col">The 1-based index of the column</param>
        /// <returns>The Excel column letter(s) for this column</returns>
        private string GetExcelColumnFromIndex(int col)
        {
            string excelColumn = ((char)((int)'A' + ((col - 1) % 26))).ToString();
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
        /// Helper function to hide all these ridiculous "Missing" directives
        /// </summary>
        /// <param name="excelObj"></param>
        /// <returns></returns>
        private Workbook OpenWorkbook(Application excelObj)
        {
            Workbook retVal = excelObj.Workbooks.Add(XlWBATemplate.xlWBATWorksheet);
            excelObj.WindowState = XlWindowState.xlMinimized;
            return retVal;
        }

        /// <summary>
        /// Sets the value of a cell as a string
        /// </summary>
        /// <param name="cells">The range of cells that serve as the source</param>
        /// <param name="row">The index of the row to set (1-based)</param>
        /// <param name="col">The index of the column to set (1-based)</param>
        /// <param name="value">The value to set for the specified cell</param>
        private void SetValueAsText(Range cells, int row, int col, string value)
        {
            Range cell = cells[row, col] as Range;
            cell.Value2 = value;
        }

        /// <summary>
        /// Sets the value of a cell as a string; assumes a range with a single row
        /// </summary>
        /// <param name="cells">A single row of cells containing the target cell</param>
        /// <param name="col">The index of the column to set (1-based)</param>
        /// <param name="value">The value to set for the specified cell</param>
        private void SetValueAsText(Range cells, int col, string value)
        {
            SetValueAsText(cells, 1, col, value);
        }

        /// <summary>
        /// A helper function used to save user-specific login information to a
        /// worksheet.
        /// </summary>
        /// <param name="worksheetName">
        /// The name of the worksheet to which the information should be saved.  If it does
        /// not already exist, the worksheet will be created.
        /// </param>
        /// <param name="loginList">A list of the login information that should be saved</param>
        private void SaveLoginInfoToSpreadsheet(
            string worksheetName,
            IList<Common.WorkspaceInfo.LoginInfo> loginList)
        {
            // Error checking: make sure that the required worksheet can be found
            if (!this.Open())
            {
                throw new Exception("Failed to open Excel workbook");
            }
            Worksheet worksheet = this.GetWorksheetByName(worksheetName);
            if (worksheet != null)
            {
                worksheet.Delete();
            }

            // Create a new worksheet with the appropriate name
            worksheet = m_workbook.Worksheets.Add(
                Type.Missing, Type.Missing, 1, XlWBATemplate.xlWBATWorksheet) as Worksheet;
            worksheet.Name = worksheetName;
            Range headerRange = CreateHeaderRow(worksheet, C_LOGIN_HEADER_COLUMNS);

            // Get a mapping of which attribute name maps to which excel column
            Dictionary<string, int> columnMap = new Dictionary<string, int>();
            for (int i = 0; i < C_LOGIN_HEADER_COLUMNS.Length; i++)
            {
                Range tempRange = FindCellMatchingText(headerRange, C_LOGIN_HEADER_COLUMNS[i]);
                columnMap[C_LOGIN_HEADER_COLUMNS[i]] = tempRange.Column;
            }

            // Fill in the contents of the worksheet based on the info for each of the
            // workspaces
            int currentRow = 2;
            foreach (Common.WorkspaceInfo.LoginInfo login in loginList)
            {
                // Get a handle to the correct range
                string startCell = "A" + currentRow.ToString();
                string endCell = GetExcelColumnFromIndex(C_LOGIN_HEADER_COLUMNS.Length) + currentRow.ToString();
                Range tempRange = worksheet.get_Range(startCell, endCell);

                // Set the values of the cells within the range
                SetValueAsText(tempRange, columnMap[C_WMX_USERNAME], login.WmxUsername);
                SetValueAsText(tempRange, columnMap[C_DB_USERNAME], login.DatabaseUsername);

                // Save out the password in base64 format so as to not end up putting binary
                // info into the Excel spreadsheet
                byte[] passwordAsBytes = System.Text.UTF8Encoding.UTF8.GetBytes(login.DatabasePassword);
                string password64 = System.Convert.ToBase64String(passwordAsBytes);
                SetValueAsText(tempRange, columnMap[C_DB_PASSWORD], password64);

                SetValueAsText(tempRange, columnMap[C_IS_ENCRYPTED], login.IsPasswordEncrypted.ToString());

                // This "auto fit" shouldn't really be done in here, but the range already
                // exists, so...
                tempRange.EntireColumn.AutoFit();

                currentRow++;
            }
        }
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="msgs"></param>
        public WorkspaceWorksheetWriter(string filename, IGPMessages msgs)
        {
            m_filename = filename;
            m_isOpen = false;
            m_gpMessages = msgs;
        }

        /// <summary>
        /// Destructor/finalizer, just to make sure that everything closes as expected
        /// </summary>
        ~WorkspaceWorksheetWriter()
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

        /// <summary>
        /// Saves the worksheet 
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            if (IsOpen)
            {
                // Excel doesn't seem to automatically determine the desired file format
                // based on the extension (and will give you an error if you later try to
                // open a ".xls" file that internally looks like a more modern format),
                // so this logic should hopefully prevent that from happening.
                //
                // TODO: The options below don't seem to cause problems, but are they
                // really "right"?  Does it matter?
                XlFileFormat fileFormat;
                if (m_filename.EndsWith(".xlsx", StringComparison.CurrentCultureIgnoreCase))
                {
                    fileFormat = XlFileFormat.xlOpenXMLWorkbook;
                }
                else if (m_filename.EndsWith(".xls", StringComparison.CurrentCultureIgnoreCase))
                {
                    fileFormat = XlFileFormat.xlExcel5;
                }
                else
                {
                    fileFormat = XlFileFormat.xlWorkbookDefault;
                }

                m_excelObj.DisplayAlerts = false;
                m_workbook.SaveAs(m_filename,
                    fileFormat,
                    Type.Missing,
                    Type.Missing,
                    false,
                    false,
                    XlSaveAsAccessMode.xlNoChange,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing);
                m_excelObj.DisplayAlerts = true;

                return true;
            }

            return false;
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
        /// Saves all of the specified workspace information to an Excel spreadsheet
        /// </summary>
        /// <param name="workspaceInfo">A list containing the information for each workspace</param>
        public void SaveWorkspacesToSpreadsheet(IList<Common.WorkspaceInfo> workspaceInfo)
        {
            // Error checking: delete the required worksheet if it already exists
            if (!this.Open())
            {
                WmauError error = new WmauError(WmauErrorCodes.C_EXCEL_WORKBOOK_ERROR);
                m_gpMessages.AddError(error.ErrorCodeAsInt, error.Message);
                throw new Exception(error.Message);
            }
            Worksheet worksheet = this.GetWorksheetByName(C_DB_WORKSHEET_NAME);
            if (worksheet != null)
            {
                worksheet.Delete();
            }

            // Create a new worksheet with the appropriate header row
            worksheet = m_workbook.Worksheets.Add(
                Type.Missing, Type.Missing, 1, XlWBATemplate.xlWBATWorksheet) as Worksheet;
            worksheet.Name = C_DB_WORKSHEET_NAME;
            Range headerRange = CreateHeaderRow(worksheet, C_HEADER_COLUMNS);

            // Get a mapping of which attribute name maps to which excel column
            Dictionary<string, int> columnMap = new Dictionary<string, int>();
            for (int i = 0; i < C_HEADER_COLUMNS.Length; i++)
            {
                Range tempRange = FindCellMatchingText(headerRange, C_HEADER_COLUMNS[i]);
                columnMap[C_HEADER_COLUMNS[i]] = tempRange.Column;
            }

            // Fill in the contents of the worksheet based on the info for each of the
            // workspaces
            int currentRow = 2;
            foreach (Common.WorkspaceInfo workspace in workspaceInfo)
            {
                // Get a handle to the correct range
                string startCell = "A" + currentRow.ToString();
                string endCell = GetExcelColumnFromIndex(C_HEADER_COLUMNS.Length) + currentRow.ToString();
                Range tempRange = worksheet.get_Range(startCell, endCell);
                
                // Set the values of the cells within the range
                SetValueAsText(tempRange, columnMap[C_DB_ALIAS], workspace.Name);
                SetValueAsText(tempRange, columnMap[C_SERVER], workspace.Server);
                SetValueAsText(tempRange, columnMap[C_INSTANCE], workspace.Instance);
                SetValueAsText(tempRange, columnMap[C_DATABASE], workspace.Database);
                SetValueAsText(tempRange, columnMap[C_VERSION], workspace.Version);
                SetValueAsText(tempRange, columnMap[C_OS_AUTH], workspace.UseOsAuthentication.ToString());
                SetValueAsText(tempRange, columnMap[C_INDIVIDUAL_LOGINS], workspace.UseIndividualLogins.ToString());
                if (!workspace.UseOsAuthentication && !workspace.UseIndividualLogins)
                {
                    if (workspace.Logins.Count > 0)
                    {
                        Common.WorkspaceInfo.LoginInfo tempLogin = workspace.Logins.ElementAt(0);
                        SetValueAsText(tempRange, columnMap[C_USERNAME], tempLogin.DatabaseUsername);

                        // Save out the password in base64 format so as to not end up putting binary
                        // info into the Excel spreadsheet
                        byte[] passwordAsBytes = System.Text.UTF8Encoding.UTF8.GetBytes(tempLogin.DatabasePassword);
                        string password64 = System.Convert.ToBase64String(passwordAsBytes);
                        SetValueAsText(tempRange, columnMap[C_PASSWORD], password64);

                        SetValueAsText(tempRange, columnMap[C_IS_ENCRYPTED], true.ToString());
                    }
                }
                else if (workspace.UseIndividualLogins)
                {
                    SaveLoginInfoToSpreadsheet(workspace.Name, workspace.Logins);
                }

                // This "auto fit" shouldn't really be done in here, but the range already
                // exists, so...
                tempRange.EntireColumn.AutoFit();

                currentRow++;
            }

            Save();
        }
    }
}
