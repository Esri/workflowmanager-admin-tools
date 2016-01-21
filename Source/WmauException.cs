using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkflowManagerAdministrationUtilities
{
    class WmauException : Exception
    {
        private WmauError m_errorDetails;

        /// <summary>
        /// Default constructor; sets up a default error type
        /// </summary>
        private WmauException() : base()
        {
            m_errorDetails = new WmauError();
        }

        /// <summary>
        /// Creates a new WmauException
        /// </summary>
        /// <param name="errorObj">The WmauError describing specifically what caused this exception</param>
        public WmauException(WmauError errorObj)
        {
            m_errorDetails = errorObj;
        }

        /// <summary>
        /// Creates a new WmauException
        /// </summary>
        /// <param name="errorCode">An error code describing specifically what caused this exception</param>
        public WmauException(WmauErrorCodes errorCode)
        {
            m_errorDetails = new WmauError(errorCode);
        }

        /// <summary>
        /// Creates a new WmauException
        /// </summary>
        /// <param name="errorObj">The WmauError describing specifically what caused this exception</param>
        /// <param name="ex">An exception that was the root cause of this one</param>
        public WmauException(WmauError errorObj, Exception ex) : base(string.Empty, ex)
        {
            m_errorDetails = errorObj;
        }

        /// <summary>
        /// Creates a new WmauException
        /// </summary>
        /// <param name="errorCode">An error code describing specifically what caused this exception</param>
        /// <param name="ex">An exception that was the root cause of this one</param>
        public WmauException(WmauErrorCodes errorCode, Exception ex)
            : base(string.Empty, ex)
        {
            m_errorDetails = new WmauError(errorCode);
        }

        /// <summary>
        /// Provides access to this exception's error message
        /// </summary>
        public override string Message
        {
            get
            {
                if (this.InnerException == null)
                {
                    return m_errorDetails.Message;
                }
                else
                {
                    return m_errorDetails.Message + "; " + this.InnerException.Message;
                }
            }
        }

        /// <summary>
        /// Provides access to this exception's error code
        /// </summary>
        public WmauErrorCodes ErrorCode
        {
            get
            {
                return m_errorDetails.ErrorCode;
            }
        }

        /// <summary>
        /// Provides access to this exception's error code, as an int
        /// </summary>
        public int ErrorCodeAsInt
        {
            get
            {
                return (int)ErrorCode;
            }
        }
    }
}
