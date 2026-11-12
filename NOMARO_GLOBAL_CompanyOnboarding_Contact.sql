/* Company onboarding: contact person (name and mobile) on CompanyRegistrations.
   Run on NOMARO_GLOBAL. Safe to run more than once. */
IF DB_NAME() <> 'NOMARO_GLOBAL'
BEGIN
    RAISERROR('Run this script on NOMARO_GLOBAL.', 16, 1);
    SET NOEXEC ON;
END
GO
IF COL_LENGTH('dbo.CompanyRegistrations', 'ContactName') IS NULL
    ALTER TABLE dbo.CompanyRegistrations ADD ContactName NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.CompanyRegistrations', 'ContactMobile') IS NULL
    ALTER TABLE dbo.CompanyRegistrations ADD ContactMobile VARCHAR(20) NULL;
GO
/* Optional: greet the person by name in the confirmation email (#ADMINNAME# is now filled in). */
UPDATE dbo.GlobalNotificationsConfig
SET EmailContent = REPLACE(EmailContent, N'<p>Dear Administrator,</p>', N'<p>Dear #ADMINNAME#,</p>')
WHERE EntityCode = 'COMPANY_ONBOARDING' AND EmailContent LIKE N'%<p>Dear Administrator,</p>%';
GO
SET NOEXEC OFF;
GO
