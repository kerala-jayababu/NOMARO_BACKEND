/* =====================================================================
   NOMARO HRMS - Employee Self-Service: Profile Change Requests
   Run on each database (NOMARO_DEV and the server DB). Safe to re-run.
   ===================================================================== */

/* ---------------------------------------------------------------------
   1. Request types (configuration)
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.ProfileChangeRequestTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfileChangeRequestTypes
    (
        IdProfileChangeRequestType INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProfileChangeRequestTypes PRIMARY KEY,
        RequestCategory   VARCHAR(30)   NOT NULL,   -- PERSONAL, CONTACT, ADDRESS, EMERGENCY, IDENTITY, STATUTORY, CHILDREN, BANK, DOCUMENT
        RequestTypeCode   VARCHAR(30)   NOT NULL,   -- must match the code list in ProfileChangeRegistry.cs
        RequestTypeName   NVARCHAR(100) NOT NULL,
        EntityCode        VARCHAR(20)   NOT NULL,   -- workflow: PROFCHG, PROFCHGID, PROFCHGBANK
        ProofRequired     BIT           NOT NULL CONSTRAINT DF_PCRT_ProofRequired DEFAULT (0),
        ProofHint         NVARCHAR(250) NULL,
        DisplayOrder      INT           NOT NULL CONSTRAINT DF_PCRT_DisplayOrder DEFAULT (0),
        IsActive          BIT           NOT NULL CONSTRAINT DF_PCRT_IsActive DEFAULT (1),
        CreatedBy         INT           NULL,
        CreatedDateTime   DATETIME      NOT NULL CONSTRAINT DF_PCRT_Created DEFAULT (GETDATE()),
        ModifiedBy        INT           NULL,
        ModifiedDateTime  DATETIME      NULL,
        CONSTRAINT UQ_ProfileChangeRequestTypes_Code UNIQUE (RequestTypeCode)
    );
END
GO

/* ---------------------------------------------------------------------
   2. Requests (header)
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.ProfileChangeRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfileChangeRequests
    (
        IdProfileChangeRequest     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProfileChangeRequests PRIMARY KEY,
        RequestNumber              VARCHAR(20)   NULL,          -- ESS-000001, set on first submit
        IdEmployee                 INT           NOT NULL CONSTRAINT FK_PCR_Employees REFERENCES dbo.Employees (IdEmployee),
        IdProfileChangeRequestType INT           NOT NULL CONSTRAINT FK_PCR_Types REFERENCES dbo.ProfileChangeRequestTypes (IdProfileChangeRequestType),
        EntityCode                 VARCHAR(20)   NOT NULL,
        RecordId                   INT           NULL,          -- child / bank account / document being changed
        Reason                     NVARCHAR(500) NULL,
        RequestStatus              VARCHAR(30)   NOT NULL,      -- DRAFT, SUBMITTED, INTERIM APPROVED, CLARIFICATION REQUIRED, FINAL APPROVED, REJECTED, CANCELLED
        CurrentLevel               INT           NULL,
        SubmittedDate              DATETIME      NULL,
        FinalActionDate            DATETIME      NULL,
        FinalActionBy              INT           NULL,
        AppliedDate                DATETIME      NULL,          -- set once, when the change is written to the master
        ApproverRemarks            NVARCHAR(500) NULL,
        EmployeeResponse           NVARCHAR(500) NULL,
        CreatedBy                  INT           NOT NULL,
        CreatedDateTime            DATETIME      NOT NULL CONSTRAINT DF_PCR_Created DEFAULT (GETDATE()),
        ModifiedBy                 INT           NULL,
        ModifiedDateTime           DATETIME      NULL
    );
    CREATE INDEX IX_PCR_Employee_Status ON dbo.ProfileChangeRequests (IdEmployee, RequestStatus);
    CREATE INDEX IX_PCR_Status ON dbo.ProfileChangeRequests (RequestStatus) INCLUDE (SubmittedDate, FinalActionDate);
    CREATE UNIQUE INDEX UX_PCR_RequestNumber ON dbo.ProfileChangeRequests (RequestNumber) WHERE RequestNumber IS NOT NULL;
END
GO

/* ---------------------------------------------------------------------
   3. Field-level changes
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.ProfileChangeRequestDetails', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfileChangeRequestDetails
    (
        IdProfileChangeRequestDetail INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProfileChangeRequestDetails PRIMARY KEY,
        IdProfileChangeRequest       INT            NOT NULL CONSTRAINT FK_PCRD_Request REFERENCES dbo.ProfileChangeRequests (IdProfileChangeRequest),
        TableName                    VARCHAR(50)    NOT NULL,
        ColumnName                   VARCHAR(50)    NOT NULL,   -- '*' for a REMOVE
        RecordId                     INT            NULL,
        Operation                    VARCHAR(10)    NOT NULL,   -- ADD, UPDATE, REMOVE
        OldValue                     NVARCHAR(1000) NULL,       -- encrypted when IsEncrypted = 1
        NewValue                     NVARCHAR(1000) NULL,
        IsEncrypted                  BIT            NOT NULL CONSTRAINT DF_PCRD_IsEncrypted DEFAULT (0),
        OldValueDisplay              NVARCHAR(200)  NULL,       -- masked for bank / ID numbers
        NewValueDisplay              NVARCHAR(200)  NULL
    );
    CREATE INDEX IX_PCRD_Request ON dbo.ProfileChangeRequestDetails (IdProfileChangeRequest);
END
GO

/* ---------------------------------------------------------------------
   4. Supporting documents (files are stored outside wwwroot)
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.ProfileChangeRequestDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfileChangeRequestDocuments
    (
        IdProfileChangeRequestDocument INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProfileChangeRequestDocuments PRIMARY KEY,
        IdProfileChangeRequest         INT           NOT NULL CONSTRAINT FK_PCRDoc_Request REFERENCES dbo.ProfileChangeRequests (IdProfileChangeRequest),
        IdDocumentType                 INT           NULL CONSTRAINT FK_PCRDoc_DocType REFERENCES dbo.DocumentTypes (IdDocumentType),
        OriginalFileName               NVARCHAR(255) NOT NULL,
        StoredFileName                 VARCHAR(100)  NOT NULL,
        MimeType                       VARCHAR(100)  NULL,
        FileSize                       BIGINT        NOT NULL,
        UploadedBy                     INT           NOT NULL,
        UploadedOn                     DATETIME      NOT NULL CONSTRAINT DF_PCRDoc_Uploaded DEFAULT (GETDATE()),
        IsDeleted                      BIT           NOT NULL CONSTRAINT DF_PCRDoc_IsDeleted DEFAULT (0),
        IdEmployeeDocument             INT           NULL          -- set when copied into EmployeeDocuments on approval
    );
    CREATE INDEX IX_PCRDoc_Request ON dbo.ProfileChangeRequestDocuments (IdProfileChangeRequest);
END
GO

/* ---------------------------------------------------------------------
   5. Default request types
   --------------------------------------------------------------------- */
INSERT INTO dbo.ProfileChangeRequestTypes (RequestCategory, RequestTypeCode, RequestTypeName, EntityCode, ProofRequired, ProofHint, DisplayOrder, IsActive)
SELECT v.Cat, v.Code, v.Name, v.Entity, v.Proof, v.Hint, v.Ord, 1
FROM (VALUES
    ('PERSONAL',  'NAME_CHANGE',        'Name Change',                 'PROFCHG',     1, 'Government ID or legal name-change document', 10),
    ('PERSONAL',  'DOB_CHANGE',         'Date of Birth Correction',    'PROFCHG',     1, 'Birth certificate or government ID',           20),
    ('PERSONAL',  'GENDER_CHANGE',      'Gender Correction',           'PROFCHG',     1, 'Government ID',                                30),
    ('PERSONAL',  'MARITAL_STATUS',     'Marital Status Change',       'PROFCHG',     0, 'Marriage certificate (if available)',          40),
    ('PERSONAL',  'NATIONALITY_CHANGE', 'Nationality / Citizenship',   'PROFCHG',     1, 'Passport or citizenship certificate',          50),
    ('CONTACT',   'CONTACT_CHANGE',     'Contact Details Change',      'PROFCHG',     0, NULL,                                           60),
    ('ADDRESS',   'ADDRESS_CHANGE',     'Address Change',              'PROFCHG',     1, 'Utility bill, rental agreement or govt address proof', 70),
    ('EMERGENCY', 'EMERGENCY_CONTACT',  'Emergency Contact Change',    'PROFCHG',     0, NULL,                                           80),
    ('IDENTITY',  'IDENTITY_CHANGE',    'Passport / ID Details',       'PROFCHGID',   1, 'Copy of the passport / ID (all pages with details)', 90),
    ('STATUTORY', 'STATUTORY_CHANGE',   'PAN / UAN / PF / ESI',        'PROFCHGID',   1, 'PAN card, UAN card or PF / ESI document',      100),
    ('FAMILY',    'FAMILY_ADD',         'Add Family Member',           'PROFCHG',     1, 'Birth / marriage certificate or ID of the family member', 110),
    ('FAMILY',    'FAMILY_EDIT',        'Correct Family Member Details','PROFCHG',    1, 'Document supporting the correction',           120),
    ('FAMILY',    'FAMILY_REMOVE',      'Remove Family Member',        'PROFCHG',     0, NULL,                                           130),
    ('BANK',      'BANK_ADD',           'Add Bank Account',            'PROFCHGBANK', 1, 'Cancelled cheque or passbook first page',      140),
    ('BANK',      'BANK_EDIT',          'Change Bank Account Details', 'PROFCHGBANK', 1, 'Cancelled cheque or passbook first page',      150),
    ('BANK',      'BANK_REMOVE',        'Remove Bank Account',         'PROFCHGBANK', 0, NULL,                                           160),
    ('DOCUMENT',  'DOC_ADD',            'Add Document',                'PROFCHG',     1, 'The document (PDF, JPG or PNG)',               170),
    ('DOCUMENT',  'DOC_REPLACE',        'Replace / Renew Document',    'PROFCHG',     1, 'The new document (PDF, JPG or PNG)',           180)
) v (Cat, Code, Name, Entity, Proof, Hint, Ord)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProfileChangeRequestTypes t WHERE t.RequestTypeCode = v.Code);
GO

/* ---------------------------------------------------------------------
   6. Menu entries
      Self portal : My Profile (/dashboard/my-profile), My Change Requests (/dashboard/my-change-requests)
      Payroll     : Profile Change Requests (/dashboard/profile-change-requests)  V = see all requests,
                                                                                   D = see full bank / ID numbers
                    Profile Change Settings (/dashboard/profile-change-settings)  V / U
      The ScreenName must stay exactly as below: the menu turns it into the route.
   --------------------------------------------------------------------- */
DECLARE @IsIdentity BIT = COLUMNPROPERTY(OBJECT_ID('dbo.PayrollScreens'), 'IdPayrollScreen', 'IsIdentity');
DECLARE @HrParent INT = (SELECT TOP 1 IdParentPayrollScreen FROM dbo.PayrollScreens WHERE ScreenCode = 'SERVICEMGMNT');
IF @HrParent IS NULL SET @HrParent = (SELECT TOP 1 IdParentPayrollScreen FROM dbo.PayrollScreens WHERE ScreenCode = 'DESNS');
DECLARE @Order INT = (SELECT ISNULL(MAX(OrderNumber), 0) FROM dbo.PayrollScreens);

DECLARE @Screens TABLE (ScreenName VARCHAR(100), ScreenCode VARCHAR(30), ValidPermissions VARCHAR(10), IdParent INT NULL, AppType VARCHAR(20), Ord INT);
INSERT INTO @Screens VALUES
    ('My Profile',              'MYPROFILE',  'V',   NULL,      'SELFPORTAL', @Order + 1),
    ('My Change Requests',      'MYCHGREQ',   'V',   NULL,      'SELFPORTAL', @Order + 2),
    ('Profile Change Requests', 'PROFCHGREQ', 'VUD', @HrParent, 'PAYROLL',    @Order + 3),
    ('Profile Change Settings', 'PROFCHGSET', 'VU',  @HrParent, 'PAYROLL',    @Order + 4);

DECLARE @Name VARCHAR(100), @Code VARCHAR(30), @Perm VARCHAR(10), @Parent INT, @App VARCHAR(20), @Ord INT;
DECLARE sc CURSOR LOCAL FAST_FORWARD FOR SELECT ScreenName, ScreenCode, ValidPermissions, IdParent, AppType, Ord FROM @Screens;
OPEN sc;
FETCH NEXT FROM sc INTO @Name, @Code, @Perm, @Parent, @App, @Ord;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE ScreenCode = @Code)
    BEGIN
        IF @IsIdentity = 1
            INSERT INTO dbo.PayrollScreens (ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
            VALUES (@Name, @Code, @Perm, @Parent, @Ord, @App, 1);
        ELSE
            INSERT INTO dbo.PayrollScreens (IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
            SELECT ISNULL(MAX(IdPayrollScreen), 0) + 1, @Name, @Code, @Perm, @Parent, @Ord, @App, 1 FROM dbo.PayrollScreens;
    END
    FETCH NEXT FROM sc INTO @Name, @Code, @Perm, @Parent, @App, @Ord;
END
CLOSE sc; DEALLOCATE sc;
GO

/* ---------------------------------------------------------------------
   7. Approval levels
      Easiest: Payroll menu -> Profile Change Settings -> Approval Levels (no SQL needed).
      Or set them here: ApprovalAuthorityType ROLE = a designation (ApprovalAuthorityID = IdDesignation),
      REPOFFICER = reporting officer (ID 0), EMPLOYEE = one employee (ApprovalAuthorityID = IdEmployee).
      Until levels exist, employees can save drafts but cannot submit.

   -- Example: HR Manager designation approves personal changes; HR Manager then Payroll Manager approve bank changes
   DECLARE @HRManager INT = <IdDesignation>, @PayrollManager INT = <IdDesignation>;
   INSERT INTO dbo.WorkFlowConfig (EntityCode, EntityName, ApprovalCycleCount, MainTableName, MainColumnName)
   VALUES ('PROFCHG', 'Profile Change - Personal, Contact, Family & Documents', 1, 'ProfileChangeRequests', 'IdEmployee');
   INSERT INTO dbo.WorkFlowConfigDetails (IdWorkFlowConfig, LevelNumber, ApprovalAuthorityType, ApprovalAuthorityID, ApprovalStatusName)
   VALUES (SCOPE_IDENTITY(), 1, 'ROLE', @HRManager, 'FINAL APPROVED');
   --------------------------------------------------------------------- */

/* Check */
SELECT RequestTypeCode, RequestTypeName, EntityCode, ProofRequired, IsActive FROM dbo.ProfileChangeRequestTypes ORDER BY DisplayOrder;
SELECT IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, APPTYPE FROM dbo.PayrollScreens
WHERE ScreenCode IN ('MYPROFILE', 'MYCHGREQ', 'PROFCHGREQ', 'PROFCHGSET');
