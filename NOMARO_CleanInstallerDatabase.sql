/* =====================================================================
   NOMARO - clean the installer / source database
   Run on a COPY (e.g. NOMARO_SOURCE). NEVER on NOMARO_GLOBAL, NOMARO_DEV or a company database.
   Select the right database in SSMS first.

   Step 1  stop foreign keys and triggers for the cleanup
   Step 2  drop old copy / temporary tables
   Step 3  delete the data of each table and restart its Id at 1
   Step 4  clear the company values in SystemParameters
   Step 5  switch foreign keys and triggers back on
   ===================================================================== */

-- Safety: stop if this is NOMARO_GLOBAL or a company database by mistake
IF DB_NAME() IN ('NOMARO_GLOBAL', 'NOMARO_DEV', 'NOMARO_GGHOSPITAL', 'master')
BEGIN
    RAISERROR('Wrong database selected. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO

/* ---------- Step 1: foreign keys and triggers off ---------- */
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
EXEC sp_MSforeachtable 'DISABLE TRIGGER ALL ON ?';
GO

/* ---------- Step 2: drop tables that are not needed ---------- */
DROP TABLE IF EXISTS dbo.clockdetailscopy_24_02_2025;
DROP TABLE IF EXISTS dbo.ClockInOut_RAWDATA_2;
DROP TABLE IF EXISTS dbo.ClockInOutDetails_COPY_10_FEB_2026_09_15AM;
DROP TABLE IF EXISTS dbo.EMPL_SALDETAIL_COPY_23_JUN_2025;
DROP TABLE IF EXISTS dbo.employees_copy;
DROP TABLE IF EXISTS dbo.employees_copy_17_Jul;
DROP TABLE IF EXISTS dbo.employees_copy28May;
DROP TABLE IF EXISTS dbo.employeesalarydetails_COPY_09_JUNE_2026;
DROP TABLE IF EXISTS dbo.EMPLOYEESALARYDETAILS_COPY31_03_2026;
DROP TABLE IF EXISTS dbo.JsonCheckEmployees;
DROP TABLE IF EXISTS dbo.LeaveTypes_old;
DROP TABLE IF EXISTS dbo.notif_copy;
DROP TABLE IF EXISTS dbo.overtimetransactions_copy_17_feb_2026;
DROP TABLE IF EXISTS dbo.rentfreequarters_copy;
DROP TABLE IF EXISTS dbo.systemparameters_NOMARO;
DROP TABLE IF EXISTS dbo.systemparameters_PayslipGuyana;
DROP TABLE IF EXISTS dbo.tmpWFAllocations;
DROP TABLE IF EXISTS dbo.PayrollScreens_copy;
DROP TABLE IF EXISTS dbo.AprilSalary;
DROP TABLE IF EXISTS dbo.AprilYTD_AMOUNT;
DROP TABLE IF EXISTS dbo.GIASALARYDATA;
DROP TABLE IF EXISTS dbo.MAY_ALL_DATA_FROM_GIA;
DROP TABLE IF EXISTS dbo.MAYSALCONFIG;
DROP TABLE IF EXISTS dbo.ImportSalaryConfig_RAWDATA;
DROP TABLE IF EXISTS dbo.tmp_paye_202603;
DROP TABLE IF EXISTS dbo.TMP_TAX_202603;
DROP TABLE IF EXISTS dbo.tmp_uploadAccounts;
DROP TABLE IF EXISTS dbo.tmp_uploadAccounts_amt;
DROP TABLE IF EXISTS dbo.tmpAttendance;
DROP TABLE IF EXISTS dbo.tmpConfigApproval;
DROP TABLE IF EXISTS dbo.tmpDeductions;
DROP TABLE IF EXISTS dbo.tmpEmployeeConfig;
DROP TABLE IF EXISTS dbo.TMPempsalaryconfig;
DROP TABLE IF EXISTS dbo.tmpEmpSalaryDetails;
DROP TABLE IF EXISTS dbo.tmpImport;
DROP TABLE IF EXISTS dbo.tmplESALDET_paye;
DROP TABLE IF EXISTS dbo.tmpOvertime;
DROP TABLE IF EXISTS dbo.tmpSalaryConfig2604;
DROP TABLE IF EXISTS dbo.tmpUnpivotedSalaryConfig;
DROP TABLE IF EXISTS dbo.Upload_BankDetails;
DROP TABLE IF EXISTS dbo.upload_EmployeeBankDetails;
DROP TABLE IF EXISTS dbo.YTD_APRIL2025;
DROP TABLE IF EXISTS dbo.YTD_DECEMBER2024;
DROP TABLE IF EXISTS dbo.FinalResult;
DROP TABLE IF EXISTS dbo.CountryStatutoryRules;
DROP TABLE IF EXISTS dbo.CountryPTSlabs;
DROP TABLE IF EXISTS dbo.CountryLWFSlabs;
GO

/* ---------- Step 3: empty each table and restart its Id at 1 ---------- */
DELETE FROM dbo.AbsenceNotifications;

DELETE FROM dbo.ApprovalWorkFlowAllocations;
DBCC CHECKIDENT ('dbo.ApprovalWorkFlowAllocations', RESEED, 0);

DELETE FROM dbo.AssetAssignments;
DBCC CHECKIDENT ('dbo.AssetAssignments', RESEED, 0);

DELETE FROM dbo.Assets;
DBCC CHECKIDENT ('dbo.Assets', RESEED, 0);

DELETE FROM dbo.AuditLog;
DBCC CHECKIDENT ('dbo.AuditLog', RESEED, 0);

DELETE FROM dbo.BambooHRIntegrationLogs;

DELETE FROM dbo.BambooHRLeaveIntegrationLastRun;

DELETE FROM dbo.BankBranches;
DBCC CHECKIDENT ('dbo.BankBranches', RESEED, 0);

DELETE FROM dbo.BankRemittance;
DBCC CHECKIDENT ('dbo.BankRemittance', RESEED, 0);

DELETE FROM dbo.Banks;
DBCC CHECKIDENT ('dbo.Banks', RESEED, 0);

DELETE FROM dbo.BudgetCodes;
DBCC CHECKIDENT ('dbo.BudgetCodes', RESEED, 0);

DELETE FROM dbo.ChildTaxThresholds;

DELETE FROM dbo.ClearanceTemplateDepartments;
DBCC CHECKIDENT ('dbo.ClearanceTemplateDepartments', RESEED, 0);

DELETE FROM dbo.ClearanceTemplates;
DBCC CHECKIDENT ('dbo.ClearanceTemplates', RESEED, 0);

DELETE FROM dbo.ClockInOut_RAWDATA;
DBCC CHECKIDENT ('dbo.ClockInOut_RAWDATA', RESEED, 0);

DELETE FROM dbo.ClockInOutDetails;
DBCC CHECKIDENT ('dbo.ClockInOutDetails', RESEED, 0);

DELETE FROM dbo.CompanyBankAccounts;
DBCC CHECKIDENT ('dbo.CompanyBankAccounts', RESEED, 0);

DELETE FROM dbo.CurrencyConversions;
DBCC CHECKIDENT ('dbo.CurrencyConversions', RESEED, 0);

DELETE FROM dbo.DayAttendance;
DBCC CHECKIDENT ('dbo.DayAttendance', RESEED, 0);

DELETE FROM dbo.Departments;
DBCC CHECKIDENT ('dbo.Departments', RESEED, 0);

DELETE FROM dbo.Designations;
DBCC CHECKIDENT ('dbo.Designations', RESEED, 0);

DELETE FROM dbo.Devices;

DELETE FROM dbo.DeviceUserMappings;

DELETE FROM dbo.EmployeeActions;

DELETE FROM dbo.EmployeeAgreementEvents;

DELETE FROM dbo.EmployeeAgreements;

DELETE FROM dbo.EmployeeAgreementTokens;

DELETE FROM dbo.EmployeeBankAccounts;
DBCC CHECKIDENT ('dbo.EmployeeBankAccounts', RESEED, 0);

DELETE FROM dbo.EmployeeChildren;
DBCC CHECKIDENT ('dbo.EmployeeChildren', RESEED, 0);

DELETE FROM dbo.EmployeeDocuments;
DBCC CHECKIDENT ('dbo.EmployeeDocuments', RESEED, 0);

DELETE FROM dbo.EmployeeExperiences;
DBCC CHECKIDENT ('dbo.EmployeeExperiences', RESEED, 0);

DELETE FROM dbo.EmployeeFamilyMembers;
DBCC CHECKIDENT ('dbo.EmployeeFamilyMembers', RESEED, 0);

DELETE FROM dbo.EmployeeLeaveConfigDetails;
DBCC CHECKIDENT ('dbo.EmployeeLeaveConfigDetails', RESEED, 0);

DELETE FROM dbo.EmployeeLeaveConfigs;
DBCC CHECKIDENT ('dbo.EmployeeLeaveConfigs', RESEED, 0);

DELETE FROM dbo.EmployeeLeaveDetails;

DELETE FROM dbo.EmployeeLeaves;

DELETE FROM dbo.EmployeeOfficePostings;

DELETE FROM dbo.EmployeeOvertimeConfig;
DBCC CHECKIDENT ('dbo.EmployeeOvertimeConfig', RESEED, 0);

DELETE FROM dbo.EmployeePermissions;
DBCC CHECKIDENT ('dbo.EmployeePermissions', RESEED, 0);

DELETE FROM dbo.EmployeeQualifications;
DBCC CHECKIDENT ('dbo.EmployeeQualifications', RESEED, 0);

DELETE FROM dbo.EmployeeRentDetails;

DELETE FROM dbo.Employees;
DBCC CHECKIDENT ('dbo.Employees', RESEED, 0);

DELETE FROM dbo.EmployeeSalaries;
DBCC CHECKIDENT ('dbo.EmployeeSalaries', RESEED, 0);

DELETE FROM dbo.EmployeeSalaryConfig;
DBCC CHECKIDENT ('dbo.EmployeeSalaryConfig', RESEED, 0);

DELETE FROM dbo.EmployeeSalaryConfigDetails;
DBCC CHECKIDENT ('dbo.EmployeeSalaryConfigDetails', RESEED, 0);

DELETE FROM dbo.EmployeeSalaryDetails;
DBCC CHECKIDENT ('dbo.EmployeeSalaryDetails', RESEED, 0);

DELETE FROM dbo.EmployeeSalaryDetailsUploadHistory;
DBCC CHECKIDENT ('dbo.EmployeeSalaryDetailsUploadHistory', RESEED, 0);

DELETE FROM dbo.EmployeeServiceChanges;

DELETE FROM dbo.EmployeeTaxDeclarationDetails;
DBCC CHECKIDENT ('dbo.EmployeeTaxDeclarationDetails', RESEED, 0);

DELETE FROM dbo.EmployeeTaxDeclarations;
DBCC CHECKIDENT ('dbo.EmployeeTaxDeclarations', RESEED, 0);

DELETE FROM dbo.EmployeeUnAuthorizedAbsence;
DBCC CHECKIDENT ('dbo.EmployeeUnAuthorizedAbsence', RESEED, 0);

DELETE FROM dbo.ExitCaseClearanceAssignments;

DELETE FROM dbo.ExitCaseDepartmentClearanceLines;

DELETE FROM dbo.ExitCases;

DELETE FROM dbo.ExitCaseStatusHistory;

DELETE FROM dbo.GeneralDocumentAcknowledgements;

DELETE FROM dbo.GeneralDocumentAudit;
DBCC CHECKIDENT ('dbo.GeneralDocumentAudit', RESEED, 0);

DELETE FROM dbo.GeneralDocumentFiles;
DBCC CHECKIDENT ('dbo.GeneralDocumentFiles', RESEED, 0);

DELETE FROM dbo.GeneralDocumentRecords;
DBCC CHECKIDENT ('dbo.GeneralDocumentRecords', RESEED, 0);

DELETE FROM dbo.GeneralDocuments;
DBCC CHECKIDENT ('dbo.GeneralDocuments', RESEED, 0);

DELETE FROM dbo.GeneralDocumentSubtypes;
DBCC CHECKIDENT ('dbo.GeneralDocumentSubtypes', RESEED, 0);

DELETE FROM dbo.GeneralDocumentTypes;
DBCC CHECKIDENT ('dbo.GeneralDocumentTypes', RESEED, 0);

DELETE FROM dbo.Grades;
DBCC CHECKIDENT ('dbo.Grades', RESEED, 0);

DELETE FROM dbo.Holidays;
DBCC CHECKIDENT ('dbo.Holidays', RESEED, 0);

DELETE FROM dbo.LeaveApplicationDocuments;
DBCC CHECKIDENT ('dbo.LeaveApplicationDocuments', RESEED, 0);

DELETE FROM dbo.LeaveApplications;
DBCC CHECKIDENT ('dbo.LeaveApplications', RESEED, 0);

DELETE FROM dbo.LeavePassageAmounts;
DBCC CHECKIDENT ('dbo.LeavePassageAmounts', RESEED, 0);

DELETE FROM dbo.LeavePassages;
DBCC CHECKIDENT ('dbo.LeavePassages', RESEED, 0);

DELETE FROM dbo.LeaveTemplateDetails;
DBCC CHECKIDENT ('dbo.LeaveTemplateDetails', RESEED, 0);

DELETE FROM dbo.LeaveTemplates;
DBCC CHECKIDENT ('dbo.LeaveTemplates', RESEED, 0);

DELETE FROM dbo.LeaveTypes;
DBCC CHECKIDENT ('dbo.LeaveTypes', RESEED, 0);

DELETE FROM dbo.LiveDashboardDetails;

DELETE FROM dbo.LoginOTP;
DBCC CHECKIDENT ('dbo.LoginOTP', RESEED, 0);

DELETE FROM dbo.LWFSlabs;
DBCC CHECKIDENT ('dbo.LWFSlabs', RESEED, 0);

DELETE FROM dbo.MaternityLeaveSalaries;
DBCC CHECKIDENT ('dbo.MaternityLeaveSalaries', RESEED, 0);

DELETE FROM dbo.MaternityLeaveSalaryDetail;
DBCC CHECKIDENT ('dbo.MaternityLeaveSalaryDetail', RESEED, 0);

DELETE FROM dbo.MissedClockInOutDetails;

DELETE FROM dbo.MobileNotifications;
DBCC CHECKIDENT ('dbo.MobileNotifications', RESEED, 0);

DELETE FROM dbo.MobileNotificationsALLReadStatus;

DELETE FROM dbo.NomaroAttributeConfig;

DELETE FROM dbo.NomaroScreenConfig;

DELETE FROM dbo.NoticePeriodPolicies;
DBCC CHECKIDENT ('dbo.NoticePeriodPolicies', RESEED, 0);

DELETE FROM dbo.Notifications;
DBCC CHECKIDENT ('dbo.Notifications', RESEED, 0);

DELETE FROM dbo.OfficeHierarchy;

DELETE FROM dbo.Offices;
DBCC CHECKIDENT ('dbo.Offices', RESEED, 0);

DELETE FROM dbo.OvertimeTransactions;
DBCC CHECKIDENT ('dbo.OvertimeTransactions', RESEED, 0);

DELETE FROM dbo.ProfileChangeRequestDetails;
DBCC CHECKIDENT ('dbo.ProfileChangeRequestDetails', RESEED, 0);

DELETE FROM dbo.ProfileChangeRequestDocuments;
DBCC CHECKIDENT ('dbo.ProfileChangeRequestDocuments', RESEED, 0);

DELETE FROM dbo.ProfileChangeRequests;
DBCC CHECKIDENT ('dbo.ProfileChangeRequests', RESEED, 0);

DELETE FROM dbo.PTSlabs;
DBCC CHECKIDENT ('dbo.PTSlabs', RESEED, 0);

DELETE FROM dbo.RentFreeQuarterAllowance;
DBCC CHECKIDENT ('dbo.RentFreeQuarterAllowance', RESEED, 0);

DELETE FROM dbo.RentFreeQuarters;

DELETE FROM dbo.SalaryAdjustments;
DBCC CHECKIDENT ('dbo.SalaryAdjustments', RESEED, 0);

DELETE FROM dbo.SalaryGenerationLogs;

DELETE FROM dbo.SalaryHeads;
DBCC CHECKIDENT ('dbo.SalaryHeads', RESEED, 0);

DELETE FROM dbo.SalaryTemplateDetails;
DBCC CHECKIDENT ('dbo.SalaryTemplateDetails', RESEED, 0);

DELETE FROM dbo.SalaryTemplates;
DBCC CHECKIDENT ('dbo.SalaryTemplates', RESEED, 0);

DELETE FROM dbo.ScheduledDeductionDetails;
DBCC CHECKIDENT ('dbo.ScheduledDeductionDetails', RESEED, 0);

DELETE FROM dbo.ScheduledDeductions;

DELETE FROM dbo.ShiftAssignments;
DBCC CHECKIDENT ('dbo.ShiftAssignments', RESEED, 0);

DELETE FROM dbo.ShiftDefinitions;
DBCC CHECKIDENT ('dbo.ShiftDefinitions', RESEED, 0);

DELETE FROM dbo.ShiftEmployees;
DBCC CHECKIDENT ('dbo.ShiftEmployees', RESEED, 0);

DELETE FROM dbo.ShiftSchedules;
DBCC CHECKIDENT ('dbo.ShiftSchedules', RESEED, 0);

DELETE FROM dbo.ShiftTypes;

DELETE FROM dbo.SickLeaveSalaryDeductions;

DELETE FROM dbo.StatutoryConfig;
DBCC CHECKIDENT ('dbo.StatutoryConfig', RESEED, 0);

DELETE FROM dbo.StatutoryRules;
DBCC CHECKIDENT ('dbo.StatutoryRules', RESEED, 0);

DELETE FROM dbo.TaxDeductionSections;
DBCC CHECKIDENT ('dbo.TaxDeductionSections', RESEED, 0);

DELETE FROM dbo.TaxLogs;

DELETE FROM dbo.TaxLogs2;
DBCC CHECKIDENT ('dbo.TaxLogs2', RESEED, 0);

DELETE FROM dbo.TaxRegimes;
DBCC CHECKIDENT ('dbo.TaxRegimes', RESEED, 0);

DELETE FROM dbo.TaxSlabs;
DBCC CHECKIDENT ('dbo.TaxSlabs', RESEED, 0);

DELETE FROM dbo.TaxSurchargeSlabs;
DBCC CHECKIDENT ('dbo.TaxSurchargeSlabs', RESEED, 0);

DELETE FROM dbo.TaxYearConfigs;
DBCC CHECKIDENT ('dbo.TaxYearConfigs', RESEED, 0);

DELETE FROM dbo.UserLastActivity;
DBCC CHECKIDENT ('dbo.UserLastActivity', RESEED, 0);

DELETE FROM dbo.EmployeeStatutoryDetails;

DELETE FROM dbo.EmployeesForSalaryGenerationStatus;

DELETE FROM dbo.RentFreeQuarterDurations;

GO

/* ---------- Step 4: company values in SystemParameters (registration fills them per company) ---------- */
UPDATE dbo.SystemParameters
SET ParameterValue = NULL, ParameterBinaryValue = NULL
WHERE ParameterName IN ('CompanyName', 'CompanyAddress', 'CompanyLogo', 'ProductLogo', 'BrowserConfig',
                        'EmailSettings', 'OverTimeSalaryHeadID', 'TaxEngine');
GO

/* ---------- Step 5: foreign keys and triggers back on ---------- */
EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL';
EXEC sp_MSforeachtable 'ENABLE TRIGGER ALL ON ?';
GO

SET NOEXEC OFF;
GO
