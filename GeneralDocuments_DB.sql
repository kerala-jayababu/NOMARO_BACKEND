/* =====================================================================
   NOMARO HRMS - General Document Management (Phase 1 & 2)
   Masters, document registration, upload, scope, maintenance types,
   register / search, history, single and ZIP download, audit.
   Run on each database BEFORE deploying the new API. Safe to re-run.

   Note: dbo.DocumentTypes already exists for employee documents, so this
   module uses its own masters GeneralDocumentTypes / GeneralDocumentSubtypes.
   ===================================================================== */

/* 1. Document types --------------------------------------------------- */
IF OBJECT_ID('dbo.GeneralDocumentTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeneralDocumentTypes
    (
        IdGeneralDocumentType  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GeneralDocumentTypes PRIMARY KEY,
        TypeCode               VARCHAR(20)    NOT NULL,
        TypeName               NVARCHAR(100)  NOT NULL,
        Description            NVARCHAR(250)  NULL,
        DisplayOrder           INT            NOT NULL CONSTRAINT DF_GDT_Order DEFAULT (0),
        IsActive               BIT            NOT NULL CONSTRAINT DF_GDT_Active DEFAULT (1),
        CreatedBy              INT            NULL,
        CreatedDateTime        DATETIME       NOT NULL CONSTRAINT DF_GDT_Created DEFAULT (GETDATE()),
        ModifiedBy             INT            NULL,
        ModifiedDateTime       DATETIME       NULL,
        CONSTRAINT UQ_GDT_Code UNIQUE (TypeCode),
        CONSTRAINT UQ_GDT_Name UNIQUE (TypeName)
    );
END
GO

/* 2. Document subtypes (carry the defaults for new documents) ---------- */
IF OBJECT_ID('dbo.GeneralDocumentSubtypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeneralDocumentSubtypes
    (
        IdGeneralDocumentSubtype INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GeneralDocumentSubtypes PRIMARY KEY,
        IdGeneralDocumentType    INT            NOT NULL CONSTRAINT FK_GDS_Type REFERENCES dbo.GeneralDocumentTypes (IdGeneralDocumentType),
        SubtypeCode              VARCHAR(20)    NOT NULL,
        SubtypeName              NVARCHAR(100)  NOT NULL,
        Description              NVARCHAR(250)  NULL,
        DefaultMaintenanceType   VARCHAR(10)    NOT NULL CONSTRAINT DF_GDS_Maint DEFAULT ('FIXED'),   -- FIXED | PERIODIC | VERSION
        DefaultScope             VARCHAR(12)    NOT NULL CONSTRAINT DF_GDS_Scope DEFAULT ('ORGANIZATION'), -- ORGANIZATION | OFFICE
        PeriodFrequency          VARCHAR(12)    NULL,       -- periodic only: MONTHLY | QUARTERLY | HALFYEARLY | YEARLY | OTHER
        ValidityRequired         BIT            NOT NULL CONSTRAINT DF_GDS_ValReq DEFAULT (0),     -- "Valid To" must be entered
        ExpiryAlertDays          INT            NOT NULL CONSTRAINT DF_GDS_Alert DEFAULT (30),
        DisplayOrder             INT            NOT NULL CONSTRAINT DF_GDS_Order DEFAULT (0),
        IsActive                 BIT            NOT NULL CONSTRAINT DF_GDS_Active DEFAULT (1),
        CreatedBy                INT            NULL,
        CreatedDateTime          DATETIME       NOT NULL CONSTRAINT DF_GDS_Created DEFAULT (GETDATE()),
        ModifiedBy               INT            NULL,
        ModifiedDateTime         DATETIME       NULL,
        CONSTRAINT UQ_GDS_Code UNIQUE (IdGeneralDocumentType, SubtypeCode),
        CONSTRAINT UQ_GDS_Name UNIQUE (IdGeneralDocumentType, SubtypeName),
        CONSTRAINT CK_GDS_Maint CHECK (DefaultMaintenanceType IN ('FIXED','PERIODIC','VERSION')),
        CONSTRAINT CK_GDS_Scope CHECK (DefaultScope IN ('ORGANIZATION','OFFICE')),
        CONSTRAINT CK_GDS_Alert CHECK (ExpiryAlertDays BETWEEN 0 AND 365)
    );
END
GO

/* 3. Logical documents ------------------------------------------------ */
IF OBJECT_ID('dbo.GeneralDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeneralDocuments
    (
        IdGeneralDocument        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GeneralDocuments PRIMARY KEY,
        DocumentCode             VARCHAR(20)    NULL,       -- GD-000001, set after insert
        Title                    NVARCHAR(200)  NOT NULL,
        IdGeneralDocumentType    INT            NOT NULL CONSTRAINT FK_GD_Type REFERENCES dbo.GeneralDocumentTypes (IdGeneralDocumentType),
        IdGeneralDocumentSubtype INT            NULL     CONSTRAINT FK_GD_Subtype REFERENCES dbo.GeneralDocumentSubtypes (IdGeneralDocumentSubtype),
        Scope                    VARCHAR(12)    NOT NULL,   -- ORGANIZATION | OFFICE
        IdOffice                 INT            NULL     CONSTRAINT FK_GD_Office REFERENCES dbo.Offices (IdOffice),
        MaintenanceType          VARCHAR(10)    NOT NULL,   -- FIXED | PERIODIC | VERSION
        PeriodFrequency          VARCHAR(12)    NULL,
        ValidityRequired         BIT            NOT NULL CONSTRAINT DF_GD_ValReq DEFAULT (0),
        ExpiryAlertDays          INT            NOT NULL CONSTRAINT DF_GD_Alert DEFAULT (30),
        Description              NVARCHAR(500)  NULL,
        Tags                     NVARCHAR(250)  NULL,       -- comma separated keywords for search
        IsActive                 BIT            NOT NULL CONSTRAINT DF_GD_Active DEFAULT (1),
        CreatedBy                INT            NULL,
        CreatedDateTime          DATETIME       NOT NULL CONSTRAINT DF_GD_Created DEFAULT (GETDATE()),
        ModifiedBy               INT            NULL,
        ModifiedDateTime         DATETIME       NULL,
        CONSTRAINT CK_GD_Scope CHECK (Scope IN ('ORGANIZATION','OFFICE')),
        CONSTRAINT CK_GD_ScopeOffice CHECK ((Scope = 'ORGANIZATION' AND IdOffice IS NULL) OR (Scope = 'OFFICE' AND IdOffice IS NOT NULL)),
        CONSTRAINT CK_GD_Maint CHECK (MaintenanceType IN ('FIXED','PERIODIC','VERSION'))
    );
    CREATE INDEX IX_GD_Type   ON dbo.GeneralDocuments (IdGeneralDocumentType, IdGeneralDocumentSubtype);
    CREATE INDEX IX_GD_Office ON dbo.GeneralDocuments (Scope, IdOffice);
END
GO

/* 4. Records: one per upload (version / period / replacement) --------- */
IF OBJECT_ID('dbo.GeneralDocumentRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeneralDocumentRecords
    (
        IdGeneralDocumentRecord  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GeneralDocumentRecords PRIMARY KEY,
        IdGeneralDocument        INT            NOT NULL CONSTRAINT FK_GDR_Document REFERENCES dbo.GeneralDocuments (IdGeneralDocument),
        VersionNumber            INT            NOT NULL,   -- 1, 2, 3 ... per document
        PeriodLabel              NVARCHAR(50)   NULL,       -- periodic: e.g. "FY 2025-26", "Q2 2026"
        PeriodFrom               DATE           NULL,
        PeriodTo                 DATE           NULL,
        ReferenceNumber          NVARCHAR(100)  NULL,       -- certificate / licence / document number
        IssuingAuthority         NVARCHAR(150)  NULL,
        IssueDate                DATE           NULL,
        ValidFrom                DATE           NULL,
        ValidTo                  DATE           NULL,
        Remarks                  NVARCHAR(500)  NULL,
        RecordStatus             VARCHAR(12)    NOT NULL CONSTRAINT DF_GDR_Status DEFAULT ('ACTIVE'), -- ACTIVE | SUPERSEDED | INCORRECT
        IsCurrent                BIT            NOT NULL CONSTRAINT DF_GDR_Current DEFAULT (0),
        StatusRemarks            NVARCHAR(500)  NULL,
        StatusChangedBy          INT            NULL,
        StatusChangedDateTime    DATETIME       NULL,
        SupersededByIdRecord     INT            NULL,
        UploadedBy               INT            NULL,
        UploadedDateTime         DATETIME       NOT NULL CONSTRAINT DF_GDR_Uploaded DEFAULT (GETDATE()),
        CONSTRAINT UQ_GDR_Version UNIQUE (IdGeneralDocument, VersionNumber),
        CONSTRAINT CK_GDR_Status CHECK (RecordStatus IN ('ACTIVE','SUPERSEDED','INCORRECT')),
        CONSTRAINT CK_GDR_Valid CHECK (ValidFrom IS NULL OR ValidTo IS NULL OR ValidTo >= ValidFrom),
        CONSTRAINT CK_GDR_Period CHECK (PeriodFrom IS NULL OR PeriodTo IS NULL OR PeriodTo >= PeriodFrom)
    );
    CREATE INDEX IX_GDR_Document ON dbo.GeneralDocumentRecords (IdGeneralDocument, RecordStatus, IsCurrent);
    CREATE INDEX IX_GDR_ValidTo  ON dbo.GeneralDocumentRecords (ValidTo) WHERE RecordStatus = 'ACTIVE';
END
GO

/* 5. Files of a record (a record may have several files) -------------- */
IF OBJECT_ID('dbo.GeneralDocumentFiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeneralDocumentFiles
    (
        IdGeneralDocumentFile    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GeneralDocumentFiles PRIMARY KEY,
        IdGeneralDocumentRecord  INT            NOT NULL CONSTRAINT FK_GDF_Record REFERENCES dbo.GeneralDocumentRecords (IdGeneralDocumentRecord),
        OriginalFileName         NVARCHAR(255)  NOT NULL,
        StoredFileName           VARCHAR(100)   NOT NULL,   -- guid + extension, kept outside wwwroot
        MimeType                 VARCHAR(100)   NULL,
        FileSize                 BIGINT         NOT NULL,
        FileHash                 CHAR(64)       NULL,       -- SHA-256 (hex) for integrity
        UploadedBy               INT            NULL,
        UploadedDateTime         DATETIME       NOT NULL CONSTRAINT DF_GDF_Uploaded DEFAULT (GETDATE())
    );
    CREATE INDEX IX_GDF_Record ON dbo.GeneralDocumentFiles (IdGeneralDocumentRecord);
END
GO

/* 6. Audit trail (insert only) ---------------------------------------- */
IF OBJECT_ID('dbo.GeneralDocumentAudit', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeneralDocumentAudit
    (
        IdGeneralDocumentAudit   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GeneralDocumentAudit PRIMARY KEY,
        IdGeneralDocument        INT            NULL,
        IdGeneralDocumentRecord  INT            NULL,
        IdGeneralDocumentFile    INT            NULL,
        Action                   VARCHAR(30)    NOT NULL,   -- CREATE, UPDATE, ACTIVATE, DEACTIVATE, UPLOAD, VIEW, DOWNLOAD, ZIP_DOWNLOAD, MARK_INCORRECT, MARK_SUPERSEDED, RECORD_UPDATE
        Details                  NVARCHAR(MAX)  NULL,       -- JSON
        ActionBy                 INT            NULL,
        ActionDateTime           DATETIME       NOT NULL CONSTRAINT DF_GDA_Date DEFAULT (GETDATE()),
        IPAddress                VARCHAR(50)    NULL
    );
    CREATE INDEX IX_GDA_Document ON dbo.GeneralDocumentAudit (IdGeneralDocument, ActionDateTime);
END
GO

IF OBJECT_ID('dbo.TR_GeneralDocumentAudit_NoChange', 'TR') IS NULL
    EXEC ('CREATE TRIGGER dbo.TR_GeneralDocumentAudit_NoChange ON dbo.GeneralDocumentAudit AFTER UPDATE, DELETE AS
           BEGIN
               RAISERROR (''The document audit trail cannot be changed or deleted.'', 16, 1);
               ROLLBACK TRANSACTION;
           END');
GO

/* 7. Starter masters (edit or deactivate as needed) ------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.GeneralDocumentTypes)
BEGIN
    INSERT INTO dbo.GeneralDocumentTypes (TypeCode, TypeName, Description, DisplayOrder) VALUES
    ('STATUTORY',  'Statutory & Registration', 'Incorporation, registrations, tax and labour law certificates', 1),
    ('LICENCE',    'Licences & Permits',       'Trade licences, permits and approvals with an expiry',          2),
    ('POLICY',     'Policies & SOPs',          'Company policies, manuals and procedures',                      3),
    ('AGREEMENT',  'Agreements & Contracts',   'Leases, vendor and service agreements',                         4),
    ('INSURANCE',  'Insurance',                'Group and asset insurance policies',                            5),
    ('RETURNS',    'Returns & Filings',        'Periodic statutory returns and challans',                       6);

    INSERT INTO dbo.GeneralDocumentSubtypes (IdGeneralDocumentType, SubtypeCode, SubtypeName, DefaultMaintenanceType, DefaultScope, PeriodFrequency, ValidityRequired, ExpiryAlertDays, DisplayOrder)
    SELECT t.IdGeneralDocumentType, s.Code, s.Name, s.Maint, s.Scope, s.Freq, s.ValReq, s.Alert, s.Ord
    FROM (VALUES
        ('STATUTORY', 'INCORP',    'Certificate of Incorporation',  'FIXED',    'ORGANIZATION', NULL,        0, 30, 1),
        ('STATUTORY', 'TAXREG',    'Tax Registration Certificate',  'FIXED',    'ORGANIZATION', NULL,        0, 30, 2),
        ('STATUTORY', 'ESTREG',    'Shops & Establishment Registration', 'FIXED', 'OFFICE',     NULL,        1, 60, 3),
        ('LICENCE',   'TRADELIC',  'Trade Licence',                 'PERIODIC', 'OFFICE',       'YEARLY',    1, 60, 1),
        ('LICENCE',   'FIRENOC',   'Fire Safety Certificate',       'PERIODIC', 'OFFICE',       'YEARLY',    1, 60, 2),
        ('POLICY',    'HRPOLICY',  'HR Policy',                     'VERSION',  'ORGANIZATION', NULL,        0, 30, 1),
        ('POLICY',    'SOP',       'Standard Operating Procedure',  'VERSION',  'ORGANIZATION', NULL,        0, 30, 2),
        ('AGREEMENT', 'LEASE',     'Lease / Rent Agreement',        'VERSION',  'OFFICE',       NULL,        1, 90, 1),
        ('AGREEMENT', 'VENDOR',    'Vendor / Service Agreement',    'VERSION',  'ORGANIZATION', NULL,        1, 60, 2),
        ('INSURANCE', 'GRPMED',    'Group Medical Insurance',       'PERIODIC', 'ORGANIZATION', 'YEARLY',    1, 45, 1),
        ('RETURNS',   'MONTHRET',  'Monthly Statutory Return',      'PERIODIC', 'ORGANIZATION', 'MONTHLY',   0, 30, 1),
        ('RETURNS',   'ANNRET',    'Annual Return',                 'PERIODIC', 'ORGANIZATION', 'YEARLY',    0, 30, 2)
    ) s (TypeCode, Code, Name, Maint, Scope, Freq, ValReq, Alert, Ord)
    JOIN dbo.GeneralDocumentTypes t ON t.TypeCode = s.TypeCode;
END
GO

/* 8. Menu: new top-level "Document Manager" --------------------------- */
/*    Routes: /dashboard/document-register, document-upload, expiry-monitoring,
              document-types, document-subtypes
      ScreenName must stay exactly as below (the route is built from it).        */
IF NOT EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE ScreenCode = 'GENDOCMGMT')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE IdPayrollScreen = 19 OR IdPayrollScreen BETWEEN 1901 AND 1999)
        RAISERROR ('PayrollScreens IDs 19 / 19xx are already used. Change the IDs in this block before running it.', 16, 1);
    ELSE
    BEGIN
        INSERT INTO dbo.PayrollScreens (IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled, MenuSection) VALUES
        (19,   'Document Manager',          'GENDOCMGMT',   'V',       0,  19, 'PAYROLL', 1, NULL),
        (1901, 'Document Register',         'GENDOCREG',    'VAUD'   , 19, 1,  'PAYROLL', 1, 'Documents'),
        (1903, 'Expiry Monitoring',         'GENDOCEXPIRY', 'V',       19, 3,  'PAYROLL', 1, 'Documents'),
        (1904, 'Document Types',            'GENDOCTYPE',   'VAU',     19, 11, 'PAYROLL', 1, 'Masters'),
        (1905, 'Document Subtypes',         'GENDOCSUBTYPE','VAU',     19, 12, 'PAYROLL', 1, 'Masters'),
        (1906, 'Document Reports',          'GENDOCREPORT', 'V',       19, 4,  'PAYROLL', 1, 'Documents');
    END
END
GO

/* Give the new screens to a role, the same way as other screens
   (Admin Tools -> role permissions), then log in again.               */

/* Check */
SELECT IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, MenuSection
FROM dbo.PayrollScreens WHERE ScreenCode LIKE 'GENDOC%' ORDER BY IdPayrollScreen;
SELECT t.TypeName, s.SubtypeName, s.DefaultMaintenanceType, s.DefaultScope
FROM dbo.GeneralDocumentSubtypes s JOIN dbo.GeneralDocumentTypes t ON t.IdGeneralDocumentType = s.IdGeneralDocumentType
ORDER BY t.DisplayOrder, s.DisplayOrder;

/* Rename (for databases where the menu was already created with the old names) */
UPDATE dbo.PayrollScreens SET ScreenName = 'Document Manager'  WHERE ScreenCode = 'GENDOCMGMT';
UPDATE dbo.PayrollScreens SET ScreenName = 'Document Types'    WHERE ScreenCode = 'GENDOCTYPE';
UPDATE dbo.PayrollScreens SET ScreenName = 'Document Subtypes' WHERE ScreenCode = 'GENDOCSUBTYPE';

/* Document Reports screen (for databases where the menu was created before reports were added) */
IF NOT EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE ScreenCode = 'GENDOCREPORT')
   AND NOT EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE IdPayrollScreen = 1906)
    INSERT INTO dbo.PayrollScreens (IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled, MenuSection)
    VALUES (1906, 'Document Reports', 'GENDOCREPORT', 'V', 19, 4, 'PAYROLL', 1, 'Documents');
GO

/* Document Upload is done from the Document Register (Register Document / Upload buttons), so its menu item is hidden */
UPDATE dbo.PayrollScreens SET Enabled = 0 WHERE ScreenCode = 'GENDOCUPLOAD';
GO
