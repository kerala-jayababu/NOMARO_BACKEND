/* =====================================================================
   NOMARO HRMS - Employee policy / agreement acceptance
   Run on each database (NOMARO_DEV and the server DB). Safe to re-run.
   ===================================================================== */

/* 1. Agreements ------------------------------------------------------ */
IF OBJECT_ID('dbo.EmployeeAgreements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeAgreements
    (
        IdEmployeeAgreement INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeAgreements PRIMARY KEY,
        IdEmployee          INT            NOT NULL CONSTRAINT FK_EA_Employees REFERENCES dbo.Employees (IdEmployee),
        IdDocumentType      INT            NULL     CONSTRAINT FK_EA_DocumentTypes REFERENCES dbo.DocumentTypes (IdDocumentType),
        AgreementTitle      NVARCHAR(200)  NOT NULL,
        AgreementVersion    NVARCHAR(20)   NULL,
        OriginalFileName    NVARCHAR(255)  NOT NULL,
        StoredFileName      VARCHAR(100)   NOT NULL,
        FileSize            BIGINT         NOT NULL,
        FileHash            CHAR(64)       NOT NULL,      -- SHA-256 of the PDF that was sent
        Status              VARCHAR(20)    NOT NULL,      -- SENT, VIEWED, ACCEPTED, REJECTED, CANCELLED (expired = past LinkExpiresOnUtc)
        SentTo              NVARCHAR(300)  NULL,
        SentOnUtc           DATETIME2(7)   NULL,
        SentBy              INT            NULL,
        LinkExpiresOnUtc    DATETIME2(7)   NULL,
        ViewedOnUtc         DATETIME2(7)   NULL,
        RespondedOnUtc      DATETIME2(7)   NULL,
        ResponseName        NVARCHAR(150)  NULL,          -- full name typed by the employee
        ResponseRemarks     NVARCHAR(500)  NULL,
        ResponseIPAddress   VARCHAR(64)    NULL,
        ResponseUserAgent   NVARCHAR(500)  NULL,
        CertificateFileName VARCHAR(100)   NULL,
        CertificateHash     CHAR(64)       NULL,
        Remarks             NVARCHAR(500)  NULL,
        CancelReason        NVARCHAR(500)  NULL,
        CreatedBy           INT            NOT NULL,
        CreatedDateTime     DATETIME       NOT NULL CONSTRAINT DF_EA_Created DEFAULT (GETDATE()),
        ModifiedBy          INT            NULL,
        ModifiedDateTime    DATETIME       NULL
    );
    CREATE INDEX IX_EA_Employee ON dbo.EmployeeAgreements (IdEmployee);
    CREATE INDEX IX_EA_Status ON dbo.EmployeeAgreements (Status, LinkExpiresOnUtc);
END
GO

/* 2. Link tokens and one-time codes (hashes only) --------------------- */
IF OBJECT_ID('dbo.EmployeeAgreementTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeAgreementTokens
    (
        IdEmployeeAgreementToken INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeAgreementTokens PRIMARY KEY,
        IdEmployeeAgreement      INT          NOT NULL CONSTRAINT FK_EAT_Agreement REFERENCES dbo.EmployeeAgreements (IdEmployeeAgreement),
        TokenHash                CHAR(64)     NOT NULL,
        CreatedOnUtc             DATETIME2(7) NOT NULL,
        ExpiresOnUtc             DATETIME2(7) NOT NULL,
        IsRevoked                BIT          NOT NULL CONSTRAINT DF_EAT_Revoked DEFAULT (0),
        OtpHash                  CHAR(64)     NULL,
        OtpExpiresOnUtc          DATETIME2(7) NULL,
        OtpAttempts              INT          NOT NULL CONSTRAINT DF_EAT_Attempts DEFAULT (0),
        OtpSentCount             INT          NOT NULL CONSTRAINT DF_EAT_SentCount DEFAULT (0),
        OtpSentOnUtc             DATETIME2(7) NULL,
        CONSTRAINT UQ_EAT_TokenHash UNIQUE (TokenHash)
    );
END
GO

/* 3. Evidence trail: append-only, hash-chained ------------------------- */
IF OBJECT_ID('dbo.EmployeeAgreementEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeAgreementEvents
    (
        IdEmployeeAgreementEvent BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeAgreementEvents PRIMARY KEY,
        IdEmployeeAgreement      INT            NOT NULL CONSTRAINT FK_EAE_Agreement REFERENCES dbo.EmployeeAgreements (IdEmployeeAgreement),
        EventType                VARCHAR(30)    NOT NULL,
        EventTimeUtc             DATETIME2(7)   NOT NULL,   -- datetime2(7) keeps the exact time used in the hash
        ActorType                VARCHAR(10)    NOT NULL,   -- HR, EMPLOYEE, SYSTEM
        ActorId                  INT            NULL,
        IPAddress                VARCHAR(64)    NULL,
        UserAgent                NVARCHAR(500)  NULL,
        Details                  NVARCHAR(1000) NULL,
        PrevHash                 VARCHAR(64)    NOT NULL,   -- 'GENESIS' for the first row
        EventHash                CHAR(64)       NOT NULL
    );
    CREATE INDEX IX_EAE_Agreement ON dbo.EmployeeAgreementEvents (IdEmployeeAgreement, IdEmployeeAgreementEvent);
END
GO

/* Events can never be changed or deleted */
IF OBJECT_ID('dbo.TR_EmployeeAgreementEvents_NoChange', 'TR') IS NULL
    EXEC ('CREATE TRIGGER dbo.TR_EmployeeAgreementEvents_NoChange ON dbo.EmployeeAgreementEvents
           INSTEAD OF UPDATE, DELETE
           AS
           BEGIN
               RAISERROR (''Agreement evidence records cannot be changed or deleted.'', 16, 1);
               ROLLBACK TRANSACTION;
           END');
GO

/* 4. System parameters -------------------------------------------------
      HRDEPARTMENTEMAIL      : HR mailbox that receives every accept / reject (comma-separate several)
      AGREEMENTLINKVALIDDAYS : days the email link stays valid
      AGREEMENTPORTALURL     : web address of the NOMARO UI, used to build the link, e.g. https://hr.company.com
   ---------------------------------------------------------------------- */
DECLARE @SpIdentity BIT = COLUMNPROPERTY(OBJECT_ID('dbo.SystemParameters'), 'IdSystemParameter', 'IsIdentity');
DECLARE @Params TABLE (Name VARCHAR(100), Descr VARCHAR(250), Val VARCHAR(250), DataType VARCHAR(20), ValidValues VARCHAR(100));
INSERT INTO @Params VALUES
    ('HRDEPARTMENTEMAIL',      'HR department email that receives employee agreement responses', 'hr@yourcompany.com', 'STRING', NULL),
    ('AGREEMENTLINKVALIDDAYS', 'Days an agreement review link stays valid',                       '7',                  'INTEGER', '1-60'),
    ('AGREEMENTPORTALURL',     'Web address of the NOMARO UI used in agreement emails',           'http://217.216.50.125:9095', 'STRING', NULL);

DECLARE @n VARCHAR(100), @d VARCHAR(250), @v VARCHAR(250), @t VARCHAR(20), @vv VARCHAR(100);
DECLARE pc CURSOR LOCAL FAST_FORWARD FOR SELECT Name, Descr, Val, DataType, ValidValues FROM @Params;
OPEN pc; FETCH NEXT FROM pc INTO @n, @d, @v, @t, @vv;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.SystemParameters WHERE ParameterName = @n)
    BEGIN
        IF @SpIdentity = 1
            INSERT INTO dbo.SystemParameters (ParameterName, ParameterDescription, ParameterValue, DataType, ValidValues, ShowInUIToConfigure, ParameterValueEditable, ParameterBinaryValueEditable)
            VALUES (@n, @d, @v, @t, @vv, 1, 1, 0);
        ELSE
            INSERT INTO dbo.SystemParameters (IdSystemParameter, ParameterName, ParameterDescription, ParameterValue, DataType, ValidValues, ShowInUIToConfigure, ParameterValueEditable, ParameterBinaryValueEditable)
            SELECT ISNULL(MAX(IdSystemParameter), 0) + 1, @n, @d, @v, @t, @vv, 1, 1, 0 FROM dbo.SystemParameters;
    END
    FETCH NEXT FROM pc INTO @n, @d, @v, @t, @vv;
END
CLOSE pc; DEALLOCATE pc;
GO

/* 5. Email templates (NotificationsConfig.EntityCode). Edit them later in Notification Types.
      Placeholders: #EMPLOYEENAME# #EMPLOYEECODE# #AGREEMENTTITLE# #AGREEMENTVERSION# #LINK# #EXPIRYDATE#
                    #DOCUMENTHASH# #FILENAME# #COMPANYNAME# #OTP# #OTPMINUTES# #DECISION# #RESPONDEDON#
                    #REMARKS# #IPADDRESS# #CERTIFICATEHASH#
   ---------------------------------------------------------------------- */
DECLARE @NcIdentity BIT = COLUMNPROPERTY(OBJECT_ID('dbo.NotificationsConfig'), 'IdNotificationConfig', 'IsIdentity');
DECLARE @Tpl TABLE (Code VARCHAR(50), Descr VARCHAR(200), Subj NVARCHAR(300), Body NVARCHAR(MAX));
INSERT INTO @Tpl VALUES
 ('AGREEMENTSEND', 'Agreement sent to employee for acceptance', N'Please review and accept: #AGREEMENTTITLE#',
  N'<p>Dear #EMPLOYEENAME#,</p><p>#COMPANYNAME# has sent you <b>#AGREEMENTTITLE#</b> #AGREEMENTVERSION# for your review and acceptance. The document is attached to this email.</p><p><a href="#LINK#" style="display:inline-block;padding:10px 18px;background:#0d384d;color:#fff;text-decoration:none;border-radius:6px">Review Agreement</a></p><p>The link is valid until #EXPIRYDATE#. On the review page you can accept or reject the agreement; a one-time code will be sent to your email address to confirm it is you.</p><p style="color:#666;font-size:12px">Document: #FILENAME#<br>SHA-256 fingerprint: #DOCUMENTHASH#<br>If you did not expect this email, please contact HR.</p>'),
 ('AGREEMENTOTP', 'One-time code for agreement response', N'Your confirmation code: #OTP#',
  N'<p>Dear #EMPLOYEENAME#,</p><p>Your one-time code to respond to <b>#AGREEMENTTITLE#</b> is:</p><p style="font-size:24px;font-weight:bold;letter-spacing:4px">#OTP#</p><p>The code is valid for #OTPMINUTES# minutes. Do not share it with anyone.</p>'),
 ('AGREEMENTRESPONSEEMP', 'Confirmation to employee after accept / reject', N'You have #DECISION# #AGREEMENTTITLE#',
  N'<p>Dear #EMPLOYEENAME#,</p><p>This confirms that you <b>#DECISION#</b> <b>#AGREEMENTTITLE#</b> #AGREEMENTVERSION# on #RESPONDEDON#.</p><p>The certificate is attached. Please keep this email for your records.</p><p style="color:#666;font-size:12px">Document SHA-256: #DOCUMENTHASH#<br>Certificate SHA-256: #CERTIFICATEHASH#</p>'),
 ('AGREEMENTRESPONSEHR', 'Agreement response sent to HR department', N'#EMPLOYEECODE# #EMPLOYEENAME# #DECISION# #AGREEMENTTITLE#',
  N'<p>#EMPLOYEECODE# #EMPLOYEENAME# has <b>#DECISION#</b> <b>#AGREEMENTTITLE#</b> #AGREEMENTVERSION# on #RESPONDEDON# (IP #IPADDRESS#).</p><p>Remarks: #REMARKS#</p><p>The certificate with the full evidence trail is attached.</p><p style="color:#666;font-size:12px">Document SHA-256: #DOCUMENTHASH#<br>Certificate SHA-256: #CERTIFICATEHASH#</p>');

DECLARE @c VARCHAR(50), @ds VARCHAR(200), @s NVARCHAR(300), @b NVARCHAR(MAX);
DECLARE tc CURSOR LOCAL FAST_FORWARD FOR SELECT Code, Descr, Subj, Body FROM @Tpl;
OPEN tc; FETCH NEXT FROM tc INTO @c, @ds, @s, @b;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.NotificationsConfig WHERE EntityCode = @c)
    BEGIN
        IF @NcIdentity = 1
            INSERT INTO dbo.NotificationsConfig (EntityCode, NotificationType, NotificationTypeDesc, EmailSubject, EmailContent, EmailNotificationEnabled, AppNotificationEnabled, LevelNumber, TemplateSelection)
            VALUES (@c, @c, @ds, @s, @b, 1, 0, 0, 'HTMLTEXT');
        ELSE
            INSERT INTO dbo.NotificationsConfig (IdNotificationConfig, EntityCode, NotificationType, NotificationTypeDesc, EmailSubject, EmailContent, EmailNotificationEnabled, AppNotificationEnabled, LevelNumber, TemplateSelection)
            SELECT ISNULL(MAX(IdNotificationConfig), 0) + 1, @c, @c, @ds, @s, @b, 1, 0, 0, 'HTMLTEXT' FROM dbo.NotificationsConfig;
    END
    FETCH NEXT FROM tc INTO @c, @ds, @s, @b;
END
CLOSE tc; DEALLOCATE tc;
GO

/* 6. Menu: "Employee Agreements" (route /dashboard/employee-agreements), next to Employee Profile */
IF NOT EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE ScreenCode = 'EMPAGREE')
BEGIN
    DECLARE @Parent INT = (SELECT TOP 1 IdParentPayrollScreen FROM dbo.PayrollScreens WHERE ScreenCode = 'EMPPROF');
    DECLARE @Ord INT = (SELECT TOP 1 OrderNumber FROM dbo.PayrollScreens WHERE ScreenCode = 'EMPPROF');
    IF COLUMNPROPERTY(OBJECT_ID('dbo.PayrollScreens'), 'IdPayrollScreen', 'IsIdentity') = 1
        INSERT INTO dbo.PayrollScreens (ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
        VALUES ('Employee Agreements', 'EMPAGREE', 'VAU', @Parent, ISNULL(@Ord, 0) + 2, 'PAYROLL', 1);
    ELSE
        INSERT INTO dbo.PayrollScreens (IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
        SELECT MAX(IdPayrollScreen) + 1, 'Employee Agreements', 'EMPAGREE', 'VAU', @Parent, ISNULL(@Ord, 0) + 2, 'PAYROLL', 1 FROM dbo.PayrollScreens;
END
GO

/* Check */
SELECT ParameterName, ParameterValue FROM dbo.SystemParameters WHERE ParameterName IN ('HRDEPARTMENTEMAIL', 'AGREEMENTLINKVALIDDAYS', 'AGREEMENTPORTALURL');
SELECT EntityCode, EmailSubject FROM dbo.NotificationsConfig WHERE EntityCode LIKE 'AGREEMENT%';
SELECT * FROM dbo.PayrollScreens WHERE ScreenCode = 'EMPAGREE';
