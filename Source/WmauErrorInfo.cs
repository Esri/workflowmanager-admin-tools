using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace WorkflowManagerAdministrationUtilities
{
    /// <summary>
    /// A list of errors that are monitored and reported by the
    /// Workflow Manager Administration Utilities.
    /// </summary>
    public enum WmauErrorCodes
    {
        C_UNITIALIZED = 0,

        C_INVALID_WMX_DB_ERROR = 125001,
        C_FILE_ACCESS_ERROR = 125002,
        C_USER_NOT_ADMIN_ERROR = 125003,
        C_USER_NOT_FOUND_ERROR = 125004,
        C_LICENSE_RELATED_ERROR = 125005,
        C_TAM_UPLOAD_ERROR = 125011,
        C_TAM_DOWNLOAD_ERROR = 125012,
        C_MXD_UPLOAD_ERROR = 125021,
        C_MXD_DOWNLOAD_ERROR = 125022,
        C_JXL_BACKUP_ERROR = 125031,
        C_WORKSPACE_LOAD_ERROR = 125041,
        C_EXCEL_WORKBOOK_ERROR = 125042,
        C_EXCEL_WORKSHEET_ERROR = 125043,
        C_WORKSPACE_FETCH_ERROR = 125044,
        C_DELETE_JOB_ERROR = 125051,
        C_DELETE_JOB_VERSION_ERROR = 125052,
        C_NO_DELETE_JOB_PRIV_ERROR = 125053,
        C_CREATE_JOB_ERROR = 125061,
        C_JOB_ASSIGNMENT_IGNORED_ERROR = 125062,
        C_EXPECTED_ONE_SELECTED_FEATURE_ERROR = 125063,
        C_AOI_NOT_POLYGON_ERROR = 125064,
        C_DUE_DATE_LT_START_DATE_ERROR = 125065,
        C_NO_CREATE_JOB_PRIV_ERROR = 125066,
        C_AOI_OVERLAP_ERROR = 125067,
        C_AOI_INPUT_ERROR = 125068,
        C_WORKSPACE_ERROR = 125069,
        C_SN_CREATION_ERROR = 125081,
        C_SN_EXISTS_ERROR = 125082,
        C_ADD_AREA_EVAL_ERROR = 125091,
        C_NO_SPATIAL_NOTIFICATIONS_FOUND = 125092,
        C_ADD_DATASET_COND_ERROR = 125101,
        C_NO_WORKSPACES_DEFINED_ERROR = 125102,
        C_OPERATOR_NOT_FOUND_ERROR = 125103,
        C_WORKSPACE_NOT_FOUND_ERROR = 125111,
        C_DELETE_MXD_ERROR = 125121,
        C_DELETE_TAM_ERROR = 125131,
        C_NO_CLOSE_JOB_PRIV_ERROR = 125141,
        C_CANNOT_CLOSE_JOB_ERROR = 125142,
        C_NO_ASSIGN_JOB_PRIV_ERROR = 125151,
        C_NO_MANAGE_ATTACHMENTS_PRIV_ERROR = 125161,
        C_NO_ADD_ATTACHMENTS_HELD_JOBS_ERROR = 125162,
        C_NO_ADD_COMMENTS_HELD_JOBS_ERROR = 125171,
        C_VERSION_LOOKUP_ERROR = 125181,
        C_JOB_ID_PARSE_ERROR = 125191,
        C_UNKNOWN_QUERY_ERROR = 125201,
        C_NO_OR_MULTIPLE_STEPS_ERROR = 125501,
        C_JOB_EXECUTION_ERROR = 125502,

        C_UNSPECIFIED_ERROR = 125999
    }

    /// <summary>
    /// A helper class that translates WmauError codes into human-readable strings.
    /// </summary>
    class WmauErrorInfo
    {
        private static Dictionary<WmauErrorCodes, string> m_errorMsgs = null;

        /// <summary>
        /// Static constructor; set up the array of error messages
        /// </summary>
        static WmauErrorInfo()
        {
            m_errorMsgs = new Dictionary<WmauErrorCodes,string>();
            m_errorMsgs.Add(WmauErrorCodes.C_INVALID_WMX_DB_ERROR, Properties.Resources.ERROR_INVALID_WMX_DB);
            m_errorMsgs.Add(WmauErrorCodes.C_FILE_ACCESS_ERROR, Properties.Resources.ERROR_FILE_ACCESS);
            m_errorMsgs.Add(WmauErrorCodes.C_USER_NOT_ADMIN_ERROR, Properties.Resources.ERROR_USER_NOT_ADMIN);
            m_errorMsgs.Add(WmauErrorCodes.C_USER_NOT_FOUND_ERROR, Properties.Resources.ERROR_USER_NOT_FOUND);
            m_errorMsgs.Add(WmauErrorCodes.C_LICENSE_RELATED_ERROR, Properties.Resources.ERROR_LICENSE_RELATED);
            m_errorMsgs.Add(WmauErrorCodes.C_TAM_UPLOAD_ERROR, Properties.Resources.ERROR_TAM_UPLOAD);
            m_errorMsgs.Add(WmauErrorCodes.C_TAM_DOWNLOAD_ERROR, Properties.Resources.ERROR_TAM_DOWNLOAD);
            m_errorMsgs.Add(WmauErrorCodes.C_MXD_UPLOAD_ERROR, Properties.Resources.ERROR_MXD_UPLOAD);
            m_errorMsgs.Add(WmauErrorCodes.C_MXD_DOWNLOAD_ERROR, Properties.Resources.ERROR_MXD_DOWNLOAD);
            m_errorMsgs.Add(WmauErrorCodes.C_JXL_BACKUP_ERROR, Properties.Resources.ERROR_JXL_BACKUP);
            m_errorMsgs.Add(WmauErrorCodes.C_EXCEL_WORKBOOK_ERROR, Properties.Resources.ERROR_EXCEL_WORKBOOK);
            m_errorMsgs.Add(WmauErrorCodes.C_EXCEL_WORKSHEET_ERROR, Properties.Resources.ERROR_EXCEL_WORKSHEET);
            m_errorMsgs.Add(WmauErrorCodes.C_WORKSPACE_FETCH_ERROR, Properties.Resources.ERROR_WORKSPACE_FETCH);
            m_errorMsgs.Add(WmauErrorCodes.C_DELETE_JOB_ERROR, Properties.Resources.ERROR_DELETE_JOB);
            m_errorMsgs.Add(WmauErrorCodes.C_DELETE_JOB_VERSION_ERROR, Properties.Resources.ERROR_DELETE_JOB_VERSION);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_DELETE_JOB_PRIV_ERROR, Properties.Resources.ERROR_NO_DELETE_JOB_PRIVILEGE);
            m_errorMsgs.Add(WmauErrorCodes.C_CREATE_JOB_ERROR, Properties.Resources.ERROR_CREATE_JOB);
            m_errorMsgs.Add(WmauErrorCodes.C_JOB_ASSIGNMENT_IGNORED_ERROR, Properties.Resources.ERROR_JOB_ASSIGNMENT_IGNORED);
            m_errorMsgs.Add(WmauErrorCodes.C_EXPECTED_ONE_SELECTED_FEATURE_ERROR, Properties.Resources.ERROR_EXPECTED_ONE_SELECTED_FEATURE);
            m_errorMsgs.Add(WmauErrorCodes.C_AOI_NOT_POLYGON_ERROR, Properties.Resources.ERROR_AOI_NOT_POLYGON);
            m_errorMsgs.Add(WmauErrorCodes.C_DUE_DATE_LT_START_DATE_ERROR, Properties.Resources.ERROR_DUE_DATE_LT_START_DATE);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_CREATE_JOB_PRIV_ERROR, Properties.Resources.ERROR_NO_CREATE_JOB_PRIVILEGE);
            m_errorMsgs.Add(WmauErrorCodes.C_AOI_OVERLAP_ERROR, Properties.Resources.ERROR_AOI_OVERLAP);
            m_errorMsgs.Add(WmauErrorCodes.C_AOI_INPUT_ERROR, Properties.Resources.ERROR_AOI_INPUT);
            m_errorMsgs.Add(WmauErrorCodes.C_WORKSPACE_ERROR, Properties.Resources.ERROR_WORKSPACE);
            m_errorMsgs.Add(WmauErrorCodes.C_SN_CREATION_ERROR, Properties.Resources.ERROR_SPATIAL_NOTIFICATION_CREATION);
            m_errorMsgs.Add(WmauErrorCodes.C_SN_EXISTS_ERROR, Properties.Resources.ERROR_SPATIAL_NOTIFICATION_EXISTS);
            m_errorMsgs.Add(WmauErrorCodes.C_ADD_AREA_EVAL_ERROR, Properties.Resources.ERROR_ADD_AREA_EVALUATOR);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_SPATIAL_NOTIFICATIONS_FOUND, Properties.Resources.ERROR_NO_SPATIAL_NOTIFICATIONS_FOUND);
            m_errorMsgs.Add(WmauErrorCodes.C_ADD_DATASET_COND_ERROR, Properties.Resources.ERROR_ADD_DATASET_CONDITION);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_WORKSPACES_DEFINED_ERROR, Properties.Resources.ERROR_NO_WORKSPACES_DEFINED);
            m_errorMsgs.Add(WmauErrorCodes.C_OPERATOR_NOT_FOUND_ERROR, Properties.Resources.ERROR_OPERATOR_NOT_FOUND);
            m_errorMsgs.Add(WmauErrorCodes.C_WORKSPACE_NOT_FOUND_ERROR, Properties.Resources.ERROR_WORKSPACE_NOT_FOUND);
            m_errorMsgs.Add(WmauErrorCodes.C_DELETE_MXD_ERROR, Properties.Resources.ERROR_DELETE_MXD);
            m_errorMsgs.Add(WmauErrorCodes.C_DELETE_TAM_ERROR, Properties.Resources.ERROR_DELETE_TAM);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_CLOSE_JOB_PRIV_ERROR, Properties.Resources.ERROR_NO_CLOSE_JOB_PRIVILEGE);
            m_errorMsgs.Add(WmauErrorCodes.C_CANNOT_CLOSE_JOB_ERROR, Properties.Resources.ERROR_CANNOT_CLOSE_JOB);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_ASSIGN_JOB_PRIV_ERROR, Properties.Resources.ERROR_NO_ASSIGN_JOB_PRIVILEGE);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_MANAGE_ATTACHMENTS_PRIV_ERROR, Properties.Resources.ERROR_NO_MANAGE_ATTACHMENTS_PRIVILEGE);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_ADD_ATTACHMENTS_HELD_JOBS_ERROR, Properties.Resources.ERROR_NO_ADD_ATTACHMENTS_HELD_JOBS);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_ADD_COMMENTS_HELD_JOBS_ERROR, Properties.Resources.ERROR_NO_ADD_COMMENTS_HELD_JOBS);
            m_errorMsgs.Add(WmauErrorCodes.C_VERSION_LOOKUP_ERROR, Properties.Resources.ERROR_VERSION_LOOKUP);
            m_errorMsgs.Add(WmauErrorCodes.C_JOB_ID_PARSE_ERROR, Properties.Resources.ERROR_JOB_ID_PARSE);
            m_errorMsgs.Add(WmauErrorCodes.C_UNKNOWN_QUERY_ERROR, Properties.Resources.ERROR_UNKNOWN_QUERY);
            m_errorMsgs.Add(WmauErrorCodes.C_NO_OR_MULTIPLE_STEPS_ERROR, Properties.Resources.ERROR_NO_OR_MULTIPLE_STEPS);
            m_errorMsgs.Add(WmauErrorCodes.C_JOB_EXECUTION_ERROR, Properties.Resources.ERROR_JOB_EXECUTION);

            m_errorMsgs.Add(WmauErrorCodes.C_UNSPECIFIED_ERROR, Properties.Resources.ERROR_UNSPECIFIED);
        }

        /// <summary>
        /// Translates error codes into human-readable messages
        /// </summary>
        /// <param name="errorCode">The error code for which to retrieve an error message</param>
        /// <returns>
        /// A string containing detail about the given error code
        /// </returns>
        public static string GetErrorMsg(WmauErrorCodes errorCode)
        {
            if (m_errorMsgs.ContainsKey(errorCode))
            {
                return Properties.Resources.ERROR + " " + (int)errorCode + ": " + m_errorMsgs[errorCode];
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
