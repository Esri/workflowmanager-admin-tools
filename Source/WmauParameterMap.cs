using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geoprocessing;


namespace WorkflowManagerAdministrationUtilities
{
    /// <summary>
    /// A utility class meant to allow name-based lookup of the GP parameters stored
    /// in an IArray object.
    /// </summary>
    class WmauParameterMap
    {
        private Dictionary<string, int> m_indexMapping = new Dictionary<string, int>();
        private IArray m_paramArray = null;

        /// <summary>
        /// Default constructor is not supported; a valid IArray containing GP parameters
        /// must be passed in
        /// </summary>
        private WmauParameterMap()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Constructor; object must be initialized with an IArray containing GP
        /// parameters
        /// </summary>
        /// <param name="paramArray">An IArray containing some number of GP parameters</param>
        public WmauParameterMap(IArray paramArray)
        {
            if (paramArray == null)
            {
                throw new NullReferenceException();
            }

            m_paramArray = paramArray;

            for (int i = 0; i < m_paramArray.Count; i++)
            {
                AddParam(paramArray.get_Element(i));
            }
        }

        /// <summary>
        /// Helper function to add a new parameter object to the internal mapping
        /// </summary>
        /// <param name="value">The object to be added (should meet the IGPParameter3 interface)</param>
        private void AddParam(object value)
        {
            if (value == null)
            {
                throw new NullReferenceException();
            }
            else if (!(value is IGPParameter3))
            {
                throw new ArgumentException();
            }
            else
            {
                IGPParameter3 param = value as IGPParameter3;
                int newIndex = m_indexMapping.Count;
                m_indexMapping.Add(param.Name, newIndex);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="paramName">The name of the parameter to look up</param>
        /// <returns>The index at which the parameter with the given name is located</returns>
        public int GetIndex(string paramName)
        {
            return m_indexMapping[paramName];
        }

        /// <summary>
        /// </summary>
        /// <param name="paramName">The name of the parameter to look up</param>
        /// <returns>The object matching the given name, as an IGPParameter3 object</returns>
        public IGPParameter3 GetParam(string paramName)
        {
            return m_paramArray.get_Element(m_indexMapping[paramName]) as IGPParameter3;
        }

        /// <summary>
        /// </summary>
        /// <param name="paramName">The name of the parameter to look up</param>
        /// <returns>The object matching the given name, as an IGPParameterEdit3 object</returns>
        public IGPParameterEdit3 GetParamEdit(string paramName)
        {
            return m_paramArray.get_Element(m_indexMapping[paramName]) as IGPParameterEdit3;
        }
    }
}
