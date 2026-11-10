/* =====================================================================
   NOMARO HRMS - Employee Family Members (replaces the Children tab)
   Run on each database BEFORE deploying the new API. Safe to re-run.
   ===================================================================== */

/* 1. Table ------------------------------------------------------------ */
IF OBJECT_ID('dbo.EmployeeFamilyMembers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeFamilyMembers
    (
        IdEmployeeFamilyMember   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeFamilyMembers PRIMARY KEY,
        IdEmployee               INT            NOT NULL CONSTRAINT FK_EFM_Employees REFERENCES dbo.Employees (IdEmployee),
        Relationship             VARCHAR(20)    NOT NULL,   -- SPOUSE, SON, DAUGHTER, FATHER, MOTHER, FATHERINLAW, MOTHERINLAW, BROTHER, SISTER, OTHER
        MemberName               NVARCHAR(100)  NOT NULL,
        Gender                   VARCHAR(10)    NULL,       -- MALE, FEMALE, OTHER
        DateOfBirth              DATE           NULL,
        ContactNumber            VARCHAR(20)    NULL,
        Occupation               NVARCHAR(100)  NULL,
        IsDependent              BIT            NOT NULL CONSTRAINT DF_EFM_IsDependent DEFAULT (0),
        IsNominee                BIT            NOT NULL CONSTRAINT DF_EFM_IsNominee DEFAULT (0),
        NomineePercentage        DECIMAL(5,2)   NULL,
        MarriageDate             DATE           NULL,       -- spouse only
        CertificateNumber        VARCHAR(20)    NULL,       -- birth certificate (children)
        DivisionNumber           VARCHAR(20)    NULL,       -- carried over from EmployeeChildren
        Remarks                  NVARCHAR(250)  NULL,
        SourceIdEmployeeChildren INT            NULL,       -- set for rows copied from EmployeeChildren
        CreatedBy                INT            NULL,
        CreatedDateTime          DATETIME       NOT NULL CONSTRAINT DF_EFM_Created DEFAULT (GETDATE()),
        ModifiedBy               INT            NULL,
        ModifiedDateTime         DATETIME       NULL,
        CONSTRAINT CK_EFM_Relationship CHECK (Relationship IN ('SPOUSE','SON','DAUGHTER','FATHER','MOTHER','FATHERINLAW','MOTHERINLAW','BROTHER','SISTER','OTHER')),
        CONSTRAINT CK_EFM_NomineePercentage CHECK (NomineePercentage IS NULL OR (NomineePercentage >= 0 AND NomineePercentage <= 100))
    );
    CREATE INDEX IX_EFM_Employee ON dbo.EmployeeFamilyMembers (IdEmployee, Relationship);
END
GO

/* 2. Copy existing children (once; EmployeeChildren is kept as it is) -- */
INSERT INTO dbo.EmployeeFamilyMembers
       (IdEmployee, Relationship, MemberName, Gender, DateOfBirth, CertificateNumber, DivisionNumber, SourceIdEmployeeChildren, CreatedDateTime)
SELECT c.IdEmployee,
       CASE UPPER(ISNULL(c.Gender, '')) WHEN 'FEMALE' THEN 'DAUGHTER' WHEN 'F' THEN 'DAUGHTER' ELSE 'SON' END,
       c.ChildName,
       CASE UPPER(ISNULL(c.Gender, '')) WHEN 'FEMALE' THEN 'FEMALE' WHEN 'F' THEN 'FEMALE' WHEN 'MALE' THEN 'MALE' WHEN 'M' THEN 'MALE' ELSE NULL END,
       c.DateOfBirth, c.CertificateNumber, c.DivisionNumber, c.IdEmployeeChildren, GETDATE()
FROM dbo.EmployeeChildren c
WHERE NOT EXISTS (SELECT 1 FROM dbo.EmployeeFamilyMembers f WHERE f.SourceIdEmployeeChildren = c.IdEmployeeChildren);
GO
/* Children with no gender recorded were copied as SON; check them in the Family Members tab:
   SELECT * FROM dbo.EmployeeFamilyMembers WHERE SourceIdEmployeeChildren IS NOT NULL AND Gender IS NULL; */

/* Keep Employees.ChildrenCount = number of sons and daughters */
UPDATE e SET ChildrenCount = x.Cnt
FROM dbo.Employees e
JOIN (SELECT IdEmployee, COUNT(*) AS Cnt FROM dbo.EmployeeFamilyMembers WHERE Relationship IN ('SON','DAUGHTER') GROUP BY IdEmployee) x
  ON x.IdEmployee = e.IdEmployee;
GO

/* 3. Self-service profile change: child request types become family request types */
IF OBJECT_ID('dbo.ProfileChangeRequestTypes', 'U') IS NOT NULL
BEGIN
    -- open requests made against the old children table cannot be applied any more
    UPDATE r SET RequestStatus = 'CANCELLED', ApproverRemarks = 'Cancelled: Children was replaced by Family Members. Please submit a new request.', ModifiedDateTime = GETDATE()
    FROM dbo.ProfileChangeRequests r
    JOIN dbo.ProfileChangeRequestTypes t ON t.IdProfileChangeRequestType = r.IdProfileChangeRequestType
    WHERE t.RequestTypeCode IN ('CHILD_ADD','CHILD_EDIT','CHILD_REMOVE')
      AND r.RequestStatus IN ('DRAFT','SUBMITTED','INTERIM APPROVED','CLARIFICATION REQUIRED');

    UPDATE dbo.ProfileChangeRequestTypes SET RequestCategory = 'FAMILY', RequestTypeCode = 'FAMILY_ADD',    RequestTypeName = 'Add Family Member',            ProofHint = 'Birth / marriage certificate or ID of the family member' WHERE RequestTypeCode = 'CHILD_ADD' AND NOT EXISTS (SELECT 1 FROM dbo.ProfileChangeRequestTypes x WHERE x.RequestTypeCode = 'FAMILY_ADD');
    UPDATE dbo.ProfileChangeRequestTypes SET RequestCategory = 'FAMILY', RequestTypeCode = 'FAMILY_EDIT',   RequestTypeName = 'Correct Family Member Details', ProofHint = 'Document supporting the correction'                     WHERE RequestTypeCode = 'CHILD_EDIT' AND NOT EXISTS (SELECT 1 FROM dbo.ProfileChangeRequestTypes x WHERE x.RequestTypeCode = 'FAMILY_EDIT');
    UPDATE dbo.ProfileChangeRequestTypes SET RequestCategory = 'FAMILY', RequestTypeCode = 'FAMILY_REMOVE', RequestTypeName = 'Remove Family Member'                                                                            WHERE RequestTypeCode = 'CHILD_REMOVE' AND NOT EXISTS (SELECT 1 FROM dbo.ProfileChangeRequestTypes x WHERE x.RequestTypeCode = 'FAMILY_REMOVE');
END
GO

/* 4. Make sure the family request types exist and are active
      (needed when this script ran before ESS_ProfileChange_DB.sql, or the child types were never created) */
IF OBJECT_ID('dbo.ProfileChangeRequestTypes', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.ProfileChangeRequestTypes (RequestCategory, RequestTypeCode, RequestTypeName, EntityCode, ProofRequired, ProofHint, DisplayOrder, IsActive)
    SELECT v.Cat, v.Code, v.Name, 'PROFCHG', v.Proof, v.Hint, v.Ord, 1
    FROM (VALUES
        ('FAMILY', 'FAMILY_ADD',    'Add Family Member',             1, 'Birth / marriage certificate or ID of the family member', 110),
        ('FAMILY', 'FAMILY_EDIT',   'Correct Family Member Details', 1, 'Document supporting the correction',                       120),
        ('FAMILY', 'FAMILY_REMOVE', 'Remove Family Member',          0, NULL,                                                       130)
    ) v (Cat, Code, Name, Proof, Hint, Ord)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ProfileChangeRequestTypes t WHERE t.RequestTypeCode = v.Code);

    -- child request types left over from the old Children tab are no longer used
    UPDATE dbo.ProfileChangeRequestTypes SET IsActive = 0 WHERE RequestTypeCode IN ('CHILD_ADD','CHILD_EDIT','CHILD_REMOVE');
END
GO

/* Check */
SELECT Relationship, COUNT(*) AS Members FROM dbo.EmployeeFamilyMembers GROUP BY Relationship;
SELECT RequestCategory, RequestTypeCode, RequestTypeName, IsActive FROM dbo.ProfileChangeRequestTypes WHERE RequestCategory IN ('FAMILY','CHILDREN');
