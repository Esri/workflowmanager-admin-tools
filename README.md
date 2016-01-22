# workflowmanager-admin-tools
The "workflowmanager-admin-tools" are a collection of supplementary geoprocessing tools and Python scripts that may help individuals and organizations who are working with an ArcGIS Workflow Manager database.

![App](https://github.com/ArcGIS/workflowmanager-admin-tools/master/workflowmanager-admin-tools.png)

## Features
Toolboxes:
  - Workflow Manager Administration Tools.tbx

Data Workspaces:
  - Create Data Workspaces from Excel Spreadsheet
  - Delete Data Workspace
  - Export Data Workspaces to Excel Spreadsheet
  - List All Data Workspaces
  - Set Default Workspace for Job Type

Jobs:
  - Add Attachment to Job
  - Add Comment to Job
  - Assign Job
  - Close Job
  - Create Job
  - Create Jobs Based on Feature Class
  - Delete Job
  - Delete Jobs Matching Criteria
  - Execute Job
  - List Jobs
  - List Jobs Using Query

Map Documents:
  - Delete Map Document
  - Download Map Document
  - List All Map Documents
  - Upload Map Document

Notifications
  - Add Area Evaluator to Spatial Notification
  - Add Dataset Condition to Spatial Notification
  - Create Spatial Notification with E-mail Notifier
  - Create Spatial Notification with E-mail Notifier 2
  - Send Job Notification
  - Send Notification for Jobs in Query

Security
  - Import Active Directory Configuration
  - List Users
  - Modify Administrator Access
  - Modify Privilege Assignment

Task Assistant Workbooks
  - Delete Task Assistant Workbook
  - Download Task Assistant Workbook
  - List All Task Assistant Workbooks
  - Upload All Task Assistant Workbooks
  - Upload Task Assistant Workbook

Workflow Manager Database
  - Backup Workflow Manager Database
  - Delete Orphaned Types
  - Report Possible Errors

## Instructions

1. Verify all requirements have been satisfied.
2. Run install.bat to install the toolbox. 
3. Run and try the tools.

## Requirements

* ArcGIS Desktop 10.4
* ArcGIS Workflow Manager 10.4
* Python 2.7 (included with ArcGIS Desktop)
* Microsoft .NET Framework 4.5
* Microsoft Excel 2013 or later

Additionally, if you need to build the DLL containing the GP tools, you will need Microsoft Visual Studio 2013 to use the included project/solution files.

## Resources

* [ArcGIS Workflow Manager](http://www.esri.com/software/arcgis/extensions/arcgis-workflow-manager)

## Issues

Find a bug or want to request a new feature?  Please let us know by submitting an issue.

## Contributing

Esri welcomes contributions from anyone and everyone. Please see our [guidelines for contributing](https://github.com/esri/contributing).

## Licensing
Copyright 2015 Esri

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

A copy of the license is available in the repository's [license.txt](https://github.com/ArcGIS/workflowmanager-admin-tools/master/license.txt) file.

[](Esri Tags: ArcGIS Workflow Manager, Geoprocessing Tools, Admin, Database)
[](Esri Language: C#)