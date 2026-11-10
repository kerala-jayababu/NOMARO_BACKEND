/* =====================================================================
   NOMARO HRMS - Document acknowledgement (public documents)
   - Document Subtypes: "Visible to All Employees", "Acknowledgement Required"
   - Self Portal menu "Documents": employees view and acknowledge
   - Payroll menu "Document Ack Dashboard" (under Document Manager)
   Run AFTER GeneralDocuments_DB.sql, before deploying the new API. Safe to re-run.
   ===================================================================== */

/* 1. Subtype flags ---------------------------------------------------- */
IF COL_LENGTH('dbo.GeneralDocumentSubtypes', 'VisibleToAllEmployees') IS NULL
    ALTER TABLE dbo.GeneralDocumentSubtypes ADD VisibleToAllEmployees BIT NOT NULL CONSTRAINT DF_GDS_Visible DEFAULT (0);
GO
IF COL_LENGTH('dbo.GeneralDocumentSubtypes', 'AcknowledgementRequired') IS NULL
    ALTER TABLE dbo.GeneralDocumentSubtypes ADD AcknowledgementRequired BIT NOT NULL CONSTRAINT DF_GDS_AckReq DEFAULT (0);
GO

/* 2. Acknowledgements: one row per employee per document version ------- */
IF OBJECT_ID('dbo.GeneralDocumentAcknowledgements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeneralDocumentAcknowledgements
    (
        IdGeneralDocumentAcknowledgement INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GeneralDocumentAcknowledgements PRIMARY KEY,
        IdGeneralDocument        INT            NOT NULL CONSTRAINT FK_GDK_Document REFERENCES dbo.GeneralDocuments (IdGeneralDocument),
        IdGeneralDocumentRecord  INT            NOT NULL CONSTRAINT FK_GDK_Record REFERENCES dbo.GeneralDocumentRecords (IdGeneralDocumentRecord),
        IdEmployee               INT            NOT NULL CONSTRAINT FK_GDK_Employee REFERENCES dbo.Employees (IdEmployee),
        FirstViewedDateTime      DATETIME       NULL,       -- first time the employee opened a file of this version
        LastViewedDateTime       DATETIME       NULL,
        ViewCount                INT            NOT NULL CONSTRAINT DF_GDK_Views DEFAULT (0),
        AcknowledgedDateTime     DATETIME       NULL,       -- "Viewed and Acknowledged" clicked
        IPAddress                VARCHAR(50)    NULL,
        UserAgent                NVARCHAR(300)  NULL,
        CONSTRAINT UQ_GDK_RecordEmployee UNIQUE (IdGeneralDocumentRecord, IdEmployee)
    );
    CREATE INDEX IX_GDK_Document ON dbo.GeneralDocumentAcknowledgements (IdGeneralDocument, IdGeneralDocumentRecord);
    CREATE INDEX IX_GDK_Employee ON dbo.GeneralDocumentAcknowledgements (IdEmployee);
END
GO

/* 3. Reminder email template (edit later in Notification Types)
      Placeholders: #EMPLOYEENAME# #EMPLOYEECODE# #DOCUMENTLIST# #DOCUMENTCOUNT# #COMPANYNAME# #LINK#   */
IF NOT EXISTS (SELECT 1 FROM dbo.NotificationsConfig WHERE EntityCode = 'DOCACKREMINDER')
BEGIN
    DECLARE @s NVARCHAR(300) = N'Reminder: please read and acknowledge #DOCUMENTCOUNT# document(s)';
    DECLARE @b NVARCHAR(MAX) = N'<p>Dear #EMPLOYEENAME#,</p><p>The following document(s) published by #COMPANYNAME# are waiting for your acknowledgement:</p>#DOCUMENTLIST#<p>Please log in to the Self Portal, open <b>Documents</b>, read each document and click <b>Viewed and Acknowledged</b>.</p><p><a href="#LINK#" style="display:inline-block;padding:10px 18px;background:#0d384d;color:#fff;text-decoration:none;border-radius:6px">Open NOMARO</a></p><p style="color:#666;font-size:12px">This is an automated reminder from HR.</p>';
    IF COLUMNPROPERTY(OBJECT_ID('dbo.NotificationsConfig'), 'IdNotificationConfig', 'IsIdentity') = 1
        INSERT INTO dbo.NotificationsConfig (EntityCode, NotificationType, NotificationTypeDesc, EmailSubject, EmailContent, EmailNotificationEnabled, AppNotificationEnabled, LevelNumber, TemplateSelection)
        VALUES ('DOCACKREMINDER', 'DOCACKREMINDER', 'Reminder to acknowledge published documents', @s, @b, 1, 0, 0, 'HTMLTEXT');
    ELSE
        INSERT INTO dbo.NotificationsConfig (IdNotificationConfig, EntityCode, NotificationType, NotificationTypeDesc, EmailSubject, EmailContent, EmailNotificationEnabled, AppNotificationEnabled, LevelNumber, TemplateSelection)
        SELECT ISNULL(MAX(IdNotificationConfig), 0) + 1, 'DOCACKREMINDER', 'DOCACKREMINDER', 'Reminder to acknowledge published documents', @s, @b, 1, 0, 0, 'HTMLTEXT'
        FROM dbo.NotificationsConfig;
END
GO
/* The link in the email uses SystemParameters AGREEMENTPORTALURL (web address of the NOMARO UI), if present. */

/* 4. Menus -------------------------------------------------------------
      Self Portal : "Documents"               -> /dashboard/documents             (ESSDOCS)
      Payroll     : "Document Ack Dashboard"  -> /dashboard/document-ack-dashboard (GENDOCACK)
                    V = view dashboard and export, A = send reminder emails
      ScreenName must stay exactly as below (the route is built from it).    */
DECLARE @IsIdentity BIT = COLUMNPROPERTY(OBJECT_ID('dbo.PayrollScreens'), 'IdPayrollScreen', 'IsIdentity');

IF NOT EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE ScreenCode = 'ESSDOCS')
BEGIN
    DECLARE @EssParent INT = ISNULL((SELECT TOP 1 IdParentPayrollScreen FROM dbo.PayrollScreens WHERE ScreenCode = 'MYPROFILE'), 0);
    DECLARE @EssOrder INT = ISNULL((SELECT MAX(OrderNumber) FROM dbo.PayrollScreens WHERE APPTYPE = 'SELFPORTAL'), 0) + 1;
    IF @IsIdentity = 1
        INSERT INTO dbo.PayrollScreens (ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
        VALUES ('Documents', 'ESSDOCS', 'V', @EssParent, @EssOrder, 'SELFPORTAL', 1);
    ELSE
        INSERT INTO dbo.PayrollScreens (IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
        SELECT ISNULL(MAX(IdPayrollScreen), 0) + 1, 'Documents', 'ESSDOCS', 'V', @EssParent, @EssOrder, 'SELFPORTAL', 1 FROM dbo.PayrollScreens;
END

IF NOT EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE ScreenCode = 'GENDOCACK')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE IdPayrollScreen = 1907)
        RAISERROR ('PayrollScreens ID 1907 is already used. Change the ID in this block before running it.', 16, 1);
    ELSE
        INSERT INTO dbo.PayrollScreens (IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled, MenuSection)
        VALUES (1907, 'Document Ack Dashboard', 'GENDOCACK', 'VA', 19, 5, 'PAYROLL', 1, 'Documents');
END
GO

/* Then: tick "Visible to All Employees" / "Acknowledgement Required" on the subtypes in Document Subtypes,
   and give "Document Ack Dashboard" to the HR role in role permissions.                               */

/* Check */
SELECT IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, APPTYPE, MenuSection
FROM dbo.PayrollScreens WHERE ScreenCode IN ('ESSDOCS', 'GENDOCACK');
SELECT SubtypeName, VisibleToAllEmployees, AcknowledgementRequired FROM dbo.GeneralDocumentSubtypes;
