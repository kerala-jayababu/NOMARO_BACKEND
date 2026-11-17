/* Country field matrix (India / UAE) - step 1: new fields
   Run on EVERY company database (NOMARO_<CODE>) AND on every country template database
   (NomaroCountries.SourceDatabaseName, e.g. NOMARO_IN_TEMPLATE) so new companies get the columns.
   Do NOT run on NOMARO_GLOBAL. Safe to run more than once.
   Run it BEFORE deploying the new API: the API reads these columns on every Employees / Offices / Banks query. */
IF DB_NAME() = 'NOMARO_GLOBAL'
BEGIN
    RAISERROR('Run this script on a company or template database, not NOMARO_GLOBAL.', 16, 1);
    SET NOEXEC ON;
END
GO

/* dbo.Employees */
IF COL_LENGTH('dbo.Employees', 'PassportExpiry') IS NULL
    ALTER TABLE dbo.Employees ADD PassportExpiry DATE NULL;
IF COL_LENGTH('dbo.Employees', 'EmiratesIdExpiry') IS NULL
    ALTER TABLE dbo.Employees ADD EmiratesIdExpiry DATE NULL;
IF COL_LENGTH('dbo.Employees', 'VisaNumber') IS NULL
    ALTER TABLE dbo.Employees ADD VisaNumber NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.Employees', 'VisaSponsor') IS NULL
    ALTER TABLE dbo.Employees ADD VisaSponsor NVARCHAR(30) NULL;
IF COL_LENGTH('dbo.Employees', 'LabourCardNumber') IS NULL
    ALTER TABLE dbo.Employees ADD LabourCardNumber VARCHAR(30) NULL;
IF COL_LENGTH('dbo.Employees', 'LabourCardExpiry') IS NULL
    ALTER TABLE dbo.Employees ADD LabourCardExpiry DATE NULL;
IF COL_LENGTH('dbo.Employees', 'PaymentMode') IS NULL
    ALTER TABLE dbo.Employees ADD PaymentMode VARCHAR(20) NULL;

/* dbo.EmployeeStatutoryDetails */
IF COL_LENGTH('dbo.EmployeeStatutoryDetails', 'IsPensionApplicable') IS NULL
    ALTER TABLE dbo.EmployeeStatutoryDetails ADD IsPensionApplicable BIT NOT NULL CONSTRAINT DF_EmployeeStatutoryDetails_IsPensionApplicable DEFAULT (0);
IF COL_LENGTH('dbo.EmployeeStatutoryDetails', 'PensionScheme') IS NULL
    ALTER TABLE dbo.EmployeeStatutoryDetails ADD PensionScheme NVARCHAR(30) NULL;
IF COL_LENGTH('dbo.EmployeeStatutoryDetails', 'PensionRegistrationNumber') IS NULL
    ALTER TABLE dbo.EmployeeStatutoryDetails ADD PensionRegistrationNumber NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.EmployeeStatutoryDetails', 'UnemploymentInsuranceNo') IS NULL
    ALTER TABLE dbo.EmployeeStatutoryDetails ADD UnemploymentInsuranceNo NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.EmployeeStatutoryDetails', 'IdTaxRegime') IS NULL
    ALTER TABLE dbo.EmployeeStatutoryDetails ADD IdTaxRegime INT NULL;

/* dbo.EmployeeFamilyMembers */
IF COL_LENGTH('dbo.EmployeeFamilyMembers', 'DependentEmiratesId') IS NULL
    ALTER TABLE dbo.EmployeeFamilyMembers ADD DependentEmiratesId VARCHAR(20) NULL;

/* dbo.Banks */
IF COL_LENGTH('dbo.Banks', 'RoutingCode') IS NULL
    ALTER TABLE dbo.Banks ADD RoutingCode VARCHAR(20) NULL;

/* dbo.Offices */
IF COL_LENGTH('dbo.Offices', 'TradeLicenceNumber') IS NULL
    ALTER TABLE dbo.Offices ADD TradeLicenceNumber NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.Offices', 'TradeLicenceExpiry') IS NULL
    ALTER TABLE dbo.Offices ADD TradeLicenceExpiry DATE NULL;
IF COL_LENGTH('dbo.Offices', 'EstablishmentId') IS NULL
    ALTER TABLE dbo.Offices ADD EstablishmentId VARCHAR(20) NULL;
IF COL_LENGTH('dbo.Offices', 'PFEstablishmentCode') IS NULL
    ALTER TABLE dbo.Offices ADD PFEstablishmentCode VARCHAR(30) NULL;
IF COL_LENGTH('dbo.Offices', 'ESIEmployerCode') IS NULL
    ALTER TABLE dbo.Offices ADD ESIEmployerCode VARCHAR(17) NULL;
IF COL_LENGTH('dbo.Offices', 'TAN') IS NULL
    ALTER TABLE dbo.Offices ADD TAN VARCHAR(10) NULL;
IF COL_LENGTH('dbo.Offices', 'PTRegistrationNumber') IS NULL
    ALTER TABLE dbo.Offices ADD PTRegistrationNumber NVARCHAR(50) NULL;
GO

/* Check: every row should say 1 */
SELECT t.TableName, t.ColumnName, CASE WHEN COL_LENGTH('dbo.' + t.TableName, t.ColumnName) IS NULL THEN 0 ELSE 1 END AS Added
FROM (VALUES
    ('Employees', 'PassportExpiry'),
    ('Employees', 'EmiratesIdExpiry'),
    ('Employees', 'VisaNumber'),
    ('Employees', 'VisaSponsor'),
    ('Employees', 'LabourCardNumber'),
    ('Employees', 'LabourCardExpiry'),
    ('Employees', 'PaymentMode'),
    ('EmployeeStatutoryDetails', 'IsPensionApplicable'),
    ('EmployeeStatutoryDetails', 'PensionScheme'),
    ('EmployeeStatutoryDetails', 'PensionRegistrationNumber'),
    ('EmployeeStatutoryDetails', 'UnemploymentInsuranceNo'),
    ('EmployeeStatutoryDetails', 'IdTaxRegime'),
    ('EmployeeFamilyMembers', 'DependentEmiratesId'),
    ('Banks', 'RoutingCode'),
    ('Offices', 'TradeLicenceNumber'),
    ('Offices', 'TradeLicenceExpiry'),
    ('Offices', 'EstablishmentId'),
    ('Offices', 'PFEstablishmentCode'),
    ('Offices', 'ESIEmployerCode'),
    ('Offices', 'TAN'),
    ('Offices', 'PTRegistrationNumber')
) AS t(TableName, ColumnName);
GO
SET NOEXEC OFF;
GO
