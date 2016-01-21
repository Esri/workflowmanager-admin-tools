using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace WorkflowManagerAdministrationUtilities
{
    class WmxDefaultDbNotSetException : WmauException
    {
        public WmxDefaultDbNotSetException() : base(WmauErrorCodes.C_INVALID_WMX_DB_ERROR)
        {
        }
    }
}
