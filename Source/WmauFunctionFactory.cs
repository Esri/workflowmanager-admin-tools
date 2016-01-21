using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;


namespace WorkflowManagerAdministrationUtilities
{
    [Guid("2a1fb4f5-5d39-423d-b5c4-f37ef21ce7c4"), ComVisible(true)]
    public class WmauFunctionFactory : IGPFunctionFactory
    {
        // Register the Function Factory with the ESRI Geoprocessor Function Factory Component Category.
        #region "Component Category Registration"
        [ComRegisterFunction()]
        static void Reg(string regKey)
        {
            GPFunctionFactories.Register(regKey);
        }

        [ComUnregisterFunction()]
        static void Unreg(string regKey)
        {
            GPFunctionFactories.Unregister(regKey);
        }
        #endregion

        // Implementation of the Function Factory
        #region IGPFunctionFactory Members

        private static SortedList<string, WmauAbstractGpFunction> wmxUtilityFunctions = null;


        /// <summary>
        /// Default constructor; initializes a list with all of the GP functions available from this
        /// factory
        /// </summary>
        public WmauFunctionFactory()
        {
            if (WmauFunctionFactory.wmxUtilityFunctions == null)
            {
                WmauFunctionFactory.wmxUtilityFunctions = new SortedList<string, WmauAbstractGpFunction>();

                // Add the supported GP functions to this factory tool
                this.AddGpFunction(new AddAreaEvaluatorToSpatialNotification());
                this.AddGpFunction(new AddAttachmentToJob());
                this.AddGpFunction(new AddCommentToJob());
                this.AddGpFunction(new AddDatasetConditionToSpatialNotification());
                this.AddGpFunction(new AssignJob());
                this.AddGpFunction(new BackupWorkflowManagerDatabase());
                this.AddGpFunction(new CloseJob());
                this.AddGpFunction(new CreateDataWorkspacesFromExcel());
                this.AddGpFunction(new CreateJob());
                this.AddGpFunction(new CreateSpatialNotification());
                this.AddGpFunction(new CreateSpatialNotification2());
                this.AddGpFunction(new DeleteDataWorkspace());
                this.AddGpFunction(new DeleteJob());
                this.AddGpFunction(new DeleteMapDocument());
                this.AddGpFunction(new DeleteOrphanedTypes());
                this.AddGpFunction(new DeleteTaskAssistantWorkbook());
                this.AddGpFunction(new DownloadMapDocument());
                this.AddGpFunction(new DownloadTaskAssistantWorkbook());
                this.AddGpFunction(new ExecuteJob());
                this.AddGpFunction(new ExportDataWorkspacesToExcel());
                this.AddGpFunction(new ImportActiveDirectoryConfiguration());
                this.AddGpFunction(new ListAllDataWorkspaces());
                this.AddGpFunction(new ListAllMapDocuments());
                this.AddGpFunction(new ListAllTaskAssistantWorkbooks());
                this.AddGpFunction(new ListJobs());
                this.AddGpFunction(new ListJobsUsingQuery());
                this.AddGpFunction(new ListUsers());
                this.AddGpFunction(new ModifyAdministratorAccess());
                this.AddGpFunction(new ModifyPrivilegeAssignment());
                this.AddGpFunction(new ReportPossibleErrors());
                this.AddGpFunction(new SendJobNotification());
                this.AddGpFunction(new SetDefaultWorkspaceForJobType());
                this.AddGpFunction(new UploadMapDocument());
                this.AddGpFunction(new UploadTaskAssistantWorkbook());
            }
        }
        
        /// <summary>
        /// Registers a GP function with the internal list maintained by this factory
        /// </summary>
        /// <param name="gpFunc"></param>
        private void AddGpFunction(WmauAbstractGpFunction gpFunc)
        {
            WmauFunctionFactory.wmxUtilityFunctions.Add(gpFunc.Name, gpFunc);
        }

        // This is the name of the function factory. 
        // This is used when generating the Toolbox containing the function tools of the factory.
        public string Name
        {
            get { return "Workflow Manager Administration Utilites"; }
        }

        // This is the alias name of the factory.
        public string Alias
        {
            get { return "wmxadminutils"; }
        }

        // This is the class id of the factory. 
        public UID CLSID
        {
            get
            {
                UID id = new UIDClass();
                id.Value = this.GetType().GUID.ToString("B");
                return id;
            }
        }

        // This method will create and return a function object based upon the input name.
        public IGPFunction GetFunction(string Name)
        {
            IGPFunction2 gpFunc = null;
            
            // Look up the item with the matching "Name" field
            gpFunc = WmauFunctionFactory.wmxUtilityFunctions[Name];
            if( gpFunc != null ) {
                return Activator.CreateInstance((gpFunc as System.Object).GetType()) as IGPFunction2;
            }

            return null;
        }

        // Utility Function added to create the function names.
        private IGPFunctionName CreateGPFunctionName(string sName)
        {
            GPFunctionNameClass pName = new GPFunctionNameClass();
            pName.Category = WmauFunctionFactory.wmxUtilityFunctions[sName].DisplayToolset;
            pName.Factory = (IGPFunctionFactory)this;

            pName.Description = string.Empty;
            pName.DisplayName = WmauFunctionFactory.wmxUtilityFunctions[sName].DisplayName;
            pName.Name = sName;

            return pName;
        }

        // This method will create and return a function name object based upon the input name.
        public IGPName GetFunctionName(string name)
        {
            return CreateGPFunctionName(name) as IGPName;
        }

        // This method will create and return an enumeration of function names that the factory supports.
        public IEnumGPName GetFunctionNames()
        {
            IArray nameArray = new EnumGPNameClass();
            foreach (string s in WmauFunctionFactory.wmxUtilityFunctions.Keys)
            {
                nameArray.Add(CreateGPFunctionName(s));
            }
            return (IEnumGPName)nameArray;
        }

        // This method will create and return an enumeration of GPEnvironment objects. 
        // If tools published by this function factory required new environment settings, 
        //then you would define the additional environment settings here. 
        // This would be similar to how parameters are defined. 
        public IEnumGPEnvironment GetFunctionEnvironments()
        {
            return null;
        }

        #endregion
    }
}
