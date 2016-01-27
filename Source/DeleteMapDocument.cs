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
    class DeleteMapDocument : WmauAbstractGpFunction
    {
        #region Constants
        private const string C_PARAM_MXD_NAME = "in_string_mxdName";
        private const string C_PARAM_OUT_MXD_NAME = "out_string_mxdName";
        #endregion

        #region MemberVariables
        private string m_mxdName = string.Empty;
        #endregion

        #region SimpleAccessors
        public override string Name { get { return "DeleteMapDocument"; } }
        public override string DisplayName { get { return Properties.Resources.TOOL_DELETE_MAP_DOCUMENT; } }
        public override string DisplayToolset { get { return Properties.Resources.CAT_MXD_UTILS; } }
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
            param = paramMap.GetParam(C_PARAM_MXD_NAME);
            m_mxdName = param.Value.GetAsText();
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

                // The name of the MXD to be deleted
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionInput,
                    esriGPParameterType.esriGPParameterTypeRequired,
                    Properties.Resources.DESC_DELMXD_MAP_DOCUMENT,
                    C_PARAM_MXD_NAME,
                    new GPStringTypeClass(),
                    null,
                    true);
                m_parameters.Add(paramEdit);

                // Parameter for specifying the WMX database
                m_parameters.Add(BuildWmxDbParameter());

                // The name of the MXD that was deleted
                paramEdit = BuildParameter(
                    esriGPParameterDirection.esriGPParameterDirectionOutput,
                    esriGPParameterType.esriGPParameterTypeDerived,
                    Properties.Resources.DESC_DELMXD_OUT_MAP_DOCUMENT,
                    C_PARAM_OUT_MXD_NAME,
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

            WmauParameterMap paramMap = new WmauParameterMap(paramValues);

            // Retrieve the parameter for source name and add a domain to it
            IGPParameter3 param = paramMap.GetParam(C_PARAM_MXD_NAME);
            IGPParameterEdit3 paramEdit = paramMap.GetParamEdit(C_PARAM_MXD_NAME);
            if (param.Domain == null)
            {
                // If there isn't a domain on this parameter yet, that means that it has
                // not yet had a list of possible MXDs populated.  In that case, do
                // so at this time.
                paramEdit.Domain = Common.WmauGpDomainBuilder.BuildMapDocumentDomain(this.WmxDatabase);
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

                // Retrieve the MXD and delete it
                IJTXConfiguration3 configMgr = WmxDatabase.ConfigurationManager as IJTXConfiguration3;
                IJTXMap map = configMgr.GetJTXMap(m_mxdName);
                configMgr.DeleteJTXMap(map.ID);

                // Update the output parameter
                WmauParameterMap paramMap = new WmauParameterMap(paramValues);
                IGPParameterEdit3 outParamEdit = paramMap.GetParamEdit(C_PARAM_OUT_MXD_NAME);
                IGPString outValue = new GPStringClass();
                outValue.Value = m_mxdName;
                outParamEdit.Value = outValue as IGPValue;

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
                    WmauError error = new WmauError(WmauErrorCodes.C_DELETE_MXD_ERROR);
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
