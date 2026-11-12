/* =====================================================================
   NOMARO - Company self-onboarding (public registration page)
   Run against NOMARO_GLOBAL, BEFORE deploying the new API. Safe to re-run.

   After running it, fill in per country (section 6 at the end):
     NomaroCountries.SourceDatabaseName      e.g. NOMARO_IN_TEMPLATE
     NomaroCountries.SourceServerConnection  connection string of the SQL Server that holds the
                                             template (the new company database is created on
                                             the SAME server); empty = same server as NOMARO_GLOBAL
     NomaroCountries.BaseURL                 web address of the NOMARO UI for that country,
                                             e.g. https://nomaro.com/in  (company URL = BaseURL/CompanyCode)
     GlobalSystemParameters 'EmailSettings'  SMTP used for OTP and confirmation emails
   The SQL login in SourceServerConnection needs: dbcreator (CREATE / RESTORE DATABASE),
   BACKUP DATABASE on the template, and read/write on the new database.
   ===================================================================== */

IF DB_NAME() <> 'NOMARO_GLOBAL'
    THROW 51000, 'Run this script against NOMARO_GLOBAL.', 1;
GO

/* 1. Country configuration columns ------------------------------------ */
IF COL_LENGTH('dbo.NomaroCountries', 'SourceDatabaseName') IS NULL     ALTER TABLE dbo.NomaroCountries ADD SourceDatabaseName     VARCHAR(128)  NULL;
IF COL_LENGTH('dbo.NomaroCountries', 'SourceServerConnection') IS NULL ALTER TABLE dbo.NomaroCountries ADD SourceServerConnection NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.NomaroCountries', 'DateFormat') IS NULL             ALTER TABLE dbo.NomaroCountries ADD DateFormat             VARCHAR(20)   NULL;
IF COL_LENGTH('dbo.NomaroCountries', 'CurrencySymbol') IS NULL         ALTER TABLE dbo.NomaroCountries ADD CurrencySymbol         NVARCHAR(10)  NULL;
IF COL_LENGTH('dbo.NomaroCountries', 'CurrencyName') IS NULL           ALTER TABLE dbo.NomaroCountries ADD CurrencyName           VARCHAR(50)   NULL;
IF COL_LENGTH('dbo.NomaroCountries', 'CurrencyCode') IS NULL           ALTER TABLE dbo.NomaroCountries ADD CurrencyCode           VARCHAR(5)    NULL;
IF COL_LENGTH('dbo.NomaroCountries', 'BaseURL') IS NULL                ALTER TABLE dbo.NomaroCountries ADD BaseURL                VARCHAR(250)  NULL;
IF COL_LENGTH('dbo.NomaroCountries', 'TimeZoneId') IS NULL             ALTER TABLE dbo.NomaroCountries ADD TimeZoneId             NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.NomaroInstanceConfig', 'TimeZoneId') IS NULL
    ALTER TABLE dbo.NomaroInstanceConfig ADD TimeZoneId NVARCHAR(100) NULL;
GO

/* 2. Global settings (SMTP for registration emails) -------------------- */
IF OBJECT_ID('dbo.GlobalSystemParameters', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GlobalSystemParameters
    (
        IdGlobalSystemParameter INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GlobalSystemParameters PRIMARY KEY,
        ParameterName           VARCHAR(100)   NOT NULL CONSTRAINT UQ_GlobalSystemParameters UNIQUE,
        ParameterDescription    NVARCHAR(250)  NULL,
        ParameterValue          NVARCHAR(MAX)  NULL
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.GlobalSystemParameters WHERE ParameterName = 'EmailSettings')
    INSERT INTO dbo.GlobalSystemParameters (ParameterName, ParameterDescription, ParameterValue)
    VALUES ('EmailSettings', 'SMTP account used for company registration emails (same JSON as a company''s EmailSettings)',
            N'{"Email":"noreply@yourdomain.com","Password":"<app password>","SmtpHost":"smtp.office365.com","SmtpPort":587,"EnableSsl":true,"DisplayName":"NOMARO"}');
IF NOT EXISTS (SELECT 1 FROM dbo.GlobalSystemParameters WHERE ParameterName = 'OnboardingSupportEmail')
    INSERT INTO dbo.GlobalSystemParameters (ParameterName, ParameterDescription, ParameterValue)
    VALUES ('OnboardingSupportEmail', 'Shown to users when a registration fails', N'support@yourdomain.com');
GO

/* 3. Email templates --------------------------------------------------- */
IF OBJECT_ID('dbo.GlobalNotificationsConfig', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GlobalNotificationsConfig
    (
        IdGlobalNotificationConfig INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GlobalNotificationsConfig PRIMARY KEY,
        EntityCode                 VARCHAR(50)    NOT NULL,
        NotificationTypeDesc       NVARCHAR(200)  NULL,
        EmailSubject               NVARCHAR(300)  NULL,
        EmailContent               NVARCHAR(MAX)  NULL
    );
END
GO
/* Placeholders: #COMPANYNAME# #COMPANYCODE# #COUNTRYNAME# #COMPANYURL# #ADMINEMAIL# #ADMINNAME# #OTP# #OTPMINUTES#
   Works whether the table was created above or already existed with NotificationType (NOT NULL) like NotificationsConfig. */
IF OBJECT_ID('tempdb..#Tpl') IS NOT NULL DROP TABLE #Tpl;
CREATE TABLE #Tpl (Code VARCHAR(50), Descr NVARCHAR(200), Subj NVARCHAR(300), Body NVARCHAR(MAX));
INSERT INTO #Tpl VALUES
 ('COMPANY_ONBOARDING', N'Company registration completed', N'Welcome to NOMARO, #COMPANYNAME#',
  N'<p>Dear #ADMINNAME#,</p><p><b>#COMPANYNAME#</b> (#COMPANYCODE#, #COUNTRYNAME#) is now registered with NOMARO.</p><p>Your company site:<br><a href="#COMPANYURL#">#COMPANYURL#</a></p><p><a href="#COMPANYURL#" style="display:inline-block;padding:10px 18px;background:#0d384d;color:#fff;text-decoration:none;border-radius:6px">Open NOMARO</a></p><p>Log in with <b>#ADMINEMAIL#</b> using "Login with OTP". You have full access to every screen and can add your employees and set permissions.</p><p style="color:#666;font-size:12px">Please keep this email for your records.</p>'),
 ('COMPANY_ONBOARDING_OTP', N'One-time code to verify the email of a new company registration', N'Your NOMARO verification code: #OTP#',
  N'<p>Your one-time code to verify this email address for the NOMARO company registration is:</p><p style="font-size:26px;font-weight:bold;letter-spacing:5px">#OTP#</p><p>The code is valid for #OTPMINUTES# minutes. If you did not request it, ignore this email.</p>');

DECLARE @Cols NVARCHAR(400) = N'EntityCode, NotificationTypeDesc, EmailSubject, EmailContent';
DECLARE @Vals NVARCHAR(400) = N't.Code, t.Descr, t.Subj, t.Body';
IF COL_LENGTH('dbo.GlobalNotificationsConfig', 'NotificationType') IS NOT NULL
    SELECT @Cols += N', NotificationType', @Vals += N', t.Code';
IF COL_LENGTH('dbo.GlobalNotificationsConfig', 'EmailNotificationEnabled') IS NOT NULL
    SELECT @Cols += N', EmailNotificationEnabled', @Vals += N', 1';
IF COL_LENGTH('dbo.GlobalNotificationsConfig', 'AppNotificationEnabled') IS NOT NULL
    SELECT @Cols += N', AppNotificationEnabled', @Vals += N', 0';
IF COL_LENGTH('dbo.GlobalNotificationsConfig', 'TemplateSelection') IS NOT NULL
    SELECT @Cols += N', TemplateSelection', @Vals += N', N''HTMLTEXT''';

DECLARE @Sql NVARCHAR(MAX) = N'INSERT INTO dbo.GlobalNotificationsConfig (' + @Cols + N') SELECT ' + @Vals
    + N' FROM #Tpl t WHERE NOT EXISTS (SELECT 1 FROM dbo.GlobalNotificationsConfig g WHERE g.EntityCode = t.Code);';
EXEC sp_executesql @Sql;
DROP TABLE #Tpl;
GO

/* 4. One-time codes (stored only as a hash) ---------------------------- */
IF OBJECT_ID('dbo.CompanyRegistrationOtps', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyRegistrationOtps
    (
        IdCompanyRegistrationOtp INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CompanyRegistrationOtps PRIMARY KEY,
        Email                    VARCHAR(200)  NOT NULL,
        OtpHash                  CHAR(64)      NOT NULL,
        ExpiresOn                DATETIME2(0)  NOT NULL,   -- UTC
        FailedAttempts           INT           NOT NULL CONSTRAINT DF_CRO_Failed DEFAULT (0),
        VerifiedOn               DATETIME2(0)  NULL,
        TokenHash                CHAR(64)      NULL,       -- issued after verification, used by the submit
        TokenExpiresOn           DATETIME2(0)  NULL,
        UsedOn                   DATETIME2(0)  NULL,
        IPAddress                VARCHAR(50)   NULL,
        CreatedOn                DATETIME2(0)  NOT NULL CONSTRAINT DF_CRO_Created DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_CRO_Email ON dbo.CompanyRegistrationOtps (Email, CreatedOn);
    CREATE INDEX IX_CRO_Ip ON dbo.CompanyRegistrationOtps (IPAddress, CreatedOn);
END
GO

/* 5. Registrations: every attempt, its step and its error (for safe retries) */
IF OBJECT_ID('dbo.CompanyRegistrations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CompanyRegistrations
    (
        IdCompanyRegistration INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CompanyRegistrations PRIMARY KEY,
        CountryCode           VARCHAR(5)     NOT NULL,
        CompanyCode           VARCHAR(10)    NOT NULL,
        CompanyName           NVARCHAR(200)  NOT NULL,
        CompanyAddress        NVARCHAR(500)  NULL,
        ContactName           NVARCHAR(100)  NULL,
        ContactMobile         VARCHAR(20)    NULL,
        Email                 VARCHAR(200)   NOT NULL,
        LogoContent           VARBINARY(MAX) NULL,
        LogoMimeType          VARCHAR(50)    NULL,
        DatabaseName          VARCHAR(128)   NULL,
        -- PENDING > DB_CREATED > DB_CONFIGURED > ADMIN_CREATED > ACTIVE > COMPLETED ; FAILED keeps LastStep
        Status                VARCHAR(20)    NOT NULL,
        LastStep              VARCHAR(20)    NULL,          -- last step finished successfully
        ErrorMessage          NVARCHAR(MAX)  NULL,
        Attempts              INT            NOT NULL CONSTRAINT DF_CR_Attempts DEFAULT (0),
        CompanyUrl            VARCHAR(300)   NULL,
        IPAddress             VARCHAR(50)    NULL,
        CreatedOn             DATETIME2(0)   NOT NULL CONSTRAINT DF_CR_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedOn             DATETIME2(0)   NULL,
        CompletedOn           DATETIME2(0)   NULL
    );
    CREATE INDEX IX_CR_Code ON dbo.CompanyRegistrations (CompanyCode, Status);
    CREATE INDEX IX_CR_Email ON dbo.CompanyRegistrations (Email);
END
GO
-- contact person, for databases where the table already existed
IF COL_LENGTH('dbo.CompanyRegistrations', 'ContactName') IS NULL
    ALTER TABLE dbo.CompanyRegistrations ADD ContactName NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.CompanyRegistrations', 'ContactMobile') IS NULL
    ALTER TABLE dbo.CompanyRegistrations ADD ContactMobile VARCHAR(20) NULL;
GO

/* 6. Fill per country - edit and run once ------------------------------- */
/*
UPDATE dbo.NomaroCountries SET
    SourceDatabaseName     = 'NOMARO_IN_TEMPLATE',
    SourceServerConnection = 'Server=<sql server>;User Id=<login>;Password=<password>;TrustServerCertificate=True',
    DateFormat = 'dd-MM-yyyy', CurrencySymbol = N'₹', CurrencyName = 'Indian Rupee', CurrencyCode = 'INR',
    BaseURL = 'https://<ui address>/in', TimeZoneId = 'Asia/Kolkata'
WHERE CountryCode = 'IN';

UPDATE dbo.NomaroCountries SET
    SourceDatabaseName     = 'NOMARO_AE_TEMPLATE',
    SourceServerConnection = 'Server=<sql server>;User Id=<login>;Password=<password>;TrustServerCertificate=True',
    DateFormat = 'dd/MM/yyyy', CurrencySymbol = N'AED', CurrencyName = 'UAE Dirham', CurrencyCode = 'AED',
    BaseURL = 'https://<ui address>/ae', TimeZoneId = 'Asia/Dubai'
WHERE CountryCode = 'AE';

UPDATE dbo.GlobalSystemParameters
SET ParameterValue = N'{"Email":"...","Password":"...","SmtpHost":"...","SmtpPort":587,"EnableSsl":true,"DisplayName":"NOMARO"}'
WHERE ParameterName = 'EmailSettings';
*/

/* Check */
SELECT CountryCode, CountryName, SourceDatabaseName, CASE WHEN SourceServerConnection IS NULL THEN '(same as NOMARO_GLOBAL)' ELSE '(set)' END AS SourceServer,
       DateFormat, CurrencySymbol, CurrencyCode, BaseURL, TimeZoneId
FROM dbo.NomaroCountries;
