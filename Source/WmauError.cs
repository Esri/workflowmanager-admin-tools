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

namespace WorkflowManagerAdministrationUtilities
{
    class WmauError
    {
        private WmauErrorCodes m_errorCode = WmauErrorCodes.C_UNSPECIFIED_ERROR;
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public WmauError()
        {
        }

        /// <summary>
        /// Constructor; initializes the error object with an error code
        /// </summary>
        /// <param name="errorCode">The error code associated with this object</param>
        public WmauError(WmauErrorCodes errorCode)
        {
            this.ErrorCode = errorCode;
        }

        /// <summary>
        /// Returns the error message associated with this object's error code
        /// </summary>
        public string Message
        {
            get
            {
                return WmauErrorInfo.GetErrorMsg(m_errorCode);
            }
        }

        /// <summary>
        /// Accessor for the error code associated with this object
        /// </summary>
        public WmauErrorCodes ErrorCode
        {
            get
            {
                return m_errorCode;
            }
            set
            {
                string msg = WmauErrorInfo.GetErrorMsg(value);
                if (!string.IsNullOrEmpty(msg))
                {
                    m_errorCode = value;
                }
            }
        }

        /// <summary>
        /// Accessor for the error code associated with this object (provides access as a string)
        /// </summary>
        public int ErrorCodeAsInt
        {
            get
            {
                return (int)m_errorCode;
            }
        }
    }
}
