/* =====================================================================
   NOMARO_GLOBAL - screens shown per country
   ---------------------------------------------------------------------
   dbo.CountryScreenConfig lists the screens (PayrollScreens.ScreenCode) to switch OFF (or ON) for a country.
   At company registration the new company's PayrollScreens.Enabled is set from it, so the menu only shows the
   screens of that country. Screens not listed keep the value they have in the source database.
   Run on NOMARO_GLOBAL. Safe to run more than once.
   ===================================================================== */
IF DB_NAME() <> 'NOMARO_GLOBAL'
BEGIN
    RAISERROR('Run this script against NOMARO_GLOBAL. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO

IF OBJECT_ID('dbo.CountryScreenConfig', 'U') IS NULL
    CREATE TABLE dbo.CountryScreenConfig
    (
        IdCountryScreenConfig INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CountryScreenConfig PRIMARY KEY,
        CountryCode           VARCHAR(5)     NOT NULL,
        ScreenCode            VARCHAR(50)    NOT NULL,     -- PayrollScreens.ScreenCode
        Enabled               BIT            NOT NULL,     -- 0 = hidden for this country, 1 = shown
        Notes                 NVARCHAR(200)  NULL,
        CONSTRAINT UQ_CountryScreenConfig UNIQUE (CountryCode, ScreenCode)
    );
GO

/* Defaults - edit freely (screen codes as in appsettings ScreenCodes / PayrollScreens) */
INSERT INTO dbo.CountryScreenConfig (CountryCode, ScreenCode, Enabled, Notes)
SELECT v.* FROM (VALUES
 ('UAE', 'INCOMeTAXCO', 0, N'No personal income tax in the UAE'),
 ('UAE', 'RNTFEQT',     0, N'Rent Free Quarters - Guyana only'),
 ('IND', 'RNTFEQT',     0, N'Rent Free Quarters - Guyana only')
) AS v(CountryCode, ScreenCode, Enabled, Notes)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CountryScreenConfig c WHERE c.CountryCode = v.CountryCode AND c.ScreenCode = v.ScreenCode);
GO

/* Existing companies: apply the settings to a company database on this server (set the two values, run, repeat)
DECLARE @Country VARCHAR(5) = 'UAE', @CompanyDb SYSNAME = N'NOMARO_ASIB';
DECLARE @sql NVARCHAR(MAX) = N'
UPDATE s SET s.Enabled = c.Enabled
FROM ' + QUOTENAME(@CompanyDb) + N'.dbo.PayrollScreens s
JOIN dbo.CountryScreenConfig c ON c.ScreenCode = s.ScreenCode AND c.CountryCode = @Country;
PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + '' screens updated.'';';
EXEC sp_executesql @sql, N'@Country VARCHAR(5)', @Country = @Country;
*/

SELECT CountryCode, ScreenCode, Enabled, Notes FROM dbo.CountryScreenConfig ORDER BY CountryCode, ScreenCode;
GO
SET NOEXEC OFF;
GO
