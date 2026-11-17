/* Country fields step 1 - add the new country and statutory columns to EVERY NOMARO company / template database on THIS SQL Server.
   Run it while connected to any database on the server (for example master). Run it once on each SQL Server
   that holds company databases (see the query at the bottom of this file to find which server each company uses).
   Skips NOMARO_GLOBAL and any database without dbo.Employees. Safe to run more than once. */
SET NOCOUNT ON;
DECLARE @db SYSNAME, @sql NVARCHAR(MAX);
DECLARE @alter NVARCHAR(MAX) = N'
IF COL_LENGTH(''dbo.Employees'', ''PassportExpiry'') IS NULL ALTER TABLE dbo.Employees ADD PassportExpiry DATE NULL;
IF COL_LENGTH(''dbo.Employees'', ''EmiratesIdExpiry'') IS NULL ALTER TABLE dbo.Employees ADD EmiratesIdExpiry DATE NULL;
IF COL_LENGTH(''dbo.Employees'', ''VisaNumber'') IS NULL ALTER TABLE dbo.Employees ADD VisaNumber NVARCHAR(50) NULL;
IF COL_LENGTH(''dbo.Employees'', ''VisaSponsor'') IS NULL ALTER TABLE dbo.Employees ADD VisaSponsor NVARCHAR(30) NULL;
IF COL_LENGTH(''dbo.Employees'', ''LabourCardNumber'') IS NULL ALTER TABLE dbo.Employees ADD LabourCardNumber VARCHAR(30) NULL;
IF COL_LENGTH(''dbo.Employees'', ''LabourCardExpiry'') IS NULL ALTER TABLE dbo.Employees ADD LabourCardExpiry DATE NULL;
IF COL_LENGTH(''dbo.Employees'', ''PaymentMode'') IS NULL ALTER TABLE dbo.Employees ADD PaymentMode VARCHAR(20) NULL;
IF COL_LENGTH(''dbo.EmployeeStatutoryDetails'', ''IsPensionApplicable'') IS NULL ALTER TABLE dbo.EmployeeStatutoryDetails ADD IsPensionApplicable BIT NOT NULL CONSTRAINT DF_EmployeeStatutoryDetails_IsPensionApplicable DEFAULT (0);
IF COL_LENGTH(''dbo.EmployeeStatutoryDetails'', ''PensionScheme'') IS NULL ALTER TABLE dbo.EmployeeStatutoryDetails ADD PensionScheme NVARCHAR(30) NULL;
IF COL_LENGTH(''dbo.EmployeeStatutoryDetails'', ''PensionRegistrationNumber'') IS NULL ALTER TABLE dbo.EmployeeStatutoryDetails ADD PensionRegistrationNumber NVARCHAR(50) NULL;
IF COL_LENGTH(''dbo.EmployeeStatutoryDetails'', ''UnemploymentInsuranceNo'') IS NULL ALTER TABLE dbo.EmployeeStatutoryDetails ADD UnemploymentInsuranceNo NVARCHAR(50) NULL;
IF COL_LENGTH(''dbo.EmployeeStatutoryDetails'', ''IdTaxRegime'') IS NULL ALTER TABLE dbo.EmployeeStatutoryDetails ADD IdTaxRegime INT NULL;
IF COL_LENGTH(''dbo.EmployeeFamilyMembers'', ''DependentEmiratesId'') IS NULL ALTER TABLE dbo.EmployeeFamilyMembers ADD DependentEmiratesId VARCHAR(20) NULL;
IF COL_LENGTH(''dbo.Banks'', ''RoutingCode'') IS NULL ALTER TABLE dbo.Banks ADD RoutingCode VARCHAR(20) NULL;
IF COL_LENGTH(''dbo.Offices'', ''TradeLicenceNumber'') IS NULL ALTER TABLE dbo.Offices ADD TradeLicenceNumber NVARCHAR(50) NULL;
IF COL_LENGTH(''dbo.Offices'', ''TradeLicenceExpiry'') IS NULL ALTER TABLE dbo.Offices ADD TradeLicenceExpiry DATE NULL;
IF COL_LENGTH(''dbo.Offices'', ''EstablishmentId'') IS NULL ALTER TABLE dbo.Offices ADD EstablishmentId VARCHAR(20) NULL;
IF COL_LENGTH(''dbo.Offices'', ''PFEstablishmentCode'') IS NULL ALTER TABLE dbo.Offices ADD PFEstablishmentCode VARCHAR(30) NULL;
IF COL_LENGTH(''dbo.Offices'', ''ESIEmployerCode'') IS NULL ALTER TABLE dbo.Offices ADD ESIEmployerCode VARCHAR(17) NULL;
IF COL_LENGTH(''dbo.Offices'', ''TAN'') IS NULL ALTER TABLE dbo.Offices ADD TAN VARCHAR(10) NULL;
IF COL_LENGTH(''dbo.Offices'', ''PTRegistrationNumber'') IS NULL ALTER TABLE dbo.Offices ADD PTRegistrationNumber NVARCHAR(50) NULL;
';

DECLARE dbs CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.databases
    WHERE state_desc = 'ONLINE' AND database_id > 4 AND name <> 'NOMARO_GLOBAL';
OPEN dbs;
FETCH NEXT FROM dbs INTO @db;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'USE ' + QUOTENAME(@db) + N';
IF OBJECT_ID(''dbo.Employees'', ''U'') IS NOT NULL AND OBJECT_ID(''dbo.Offices'', ''U'') IS NOT NULL
BEGIN
    EXEC sp_executesql @alter;
    PRINT ''Updated: '' + DB_NAME();
END';
    BEGIN TRY
        EXEC sp_executesql @sql, N'@alter NVARCHAR(MAX)', @alter = @alter;
    END TRY
    BEGIN CATCH
        PRINT 'FAILED: ' + @db + ' - ' + ERROR_MESSAGE();
    END CATCH;
    FETCH NEXT FROM dbs INTO @db;
END
CLOSE dbs; DEALLOCATE dbs;

/* Which database does each company use? Run on NOMARO_GLOBAL:
SELECT CompanyCode, CompanyName, DBConnectionString FROM dbo.NomaroInstanceConfig ORDER BY CompanyCode;
*/
