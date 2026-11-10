/* =====================================================================
   NOMARO HRMS - Employee Grade / Level
   Run on each database (NOMARO_DEV and the server DB).
   Safe to re-run: each step checks before creating.
   ===================================================================== */

/* 1. Grades master table */
IF OBJECT_ID('dbo.Grades', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Grades
    (
        IdGrade           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Grades PRIMARY KEY,
        GradeCode         VARCHAR(10)   NOT NULL,          -- G1, G2 ...
        GradeName         VARCHAR(100)  NOT NULL,          -- Entry Level, Junior ...
        GradeLevel        INT           NOT NULL,          -- rank: 1 = lowest
        Description       VARCHAR(250)  NULL,
        IsActive          BIT           NOT NULL CONSTRAINT DF_Grades_IsActive DEFAULT (1),
        IdCreatedBy       INT           NULL,
        CreatedDateTime   DATETIME      NOT NULL CONSTRAINT DF_Grades_CreatedDateTime DEFAULT (GETDATE()),
        IdModifiedBy      INT           NULL,
        ModifiedDateTime  DATETIME      NULL,
        CONSTRAINT UQ_Grades_GradeCode UNIQUE (GradeCode)
    );
END
GO

/* 2. Grade column on Employees (optional, so existing employees stay valid) */
IF COL_LENGTH('dbo.Employees', 'IdGrade') IS NULL
    ALTER TABLE dbo.Employees ADD IdGrade INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Employees_Grades')
    ALTER TABLE dbo.Employees
        ADD CONSTRAINT FK_Employees_Grades FOREIGN KEY (IdGrade) REFERENCES dbo.Grades (IdGrade);
GO

/* 3. Default grade structure */
INSERT INTO dbo.Grades (GradeCode, GradeName, GradeLevel, IsActive, CreatedDateTime)
SELECT v.GradeCode, v.GradeName, v.GradeLevel, 1, GETDATE()
FROM (VALUES
    ('G1',  'Entry Level',          1),
    ('G2',  'Junior',               2),
    ('G3',  'Intermediate',         3),
    ('G4',  'Senior',               4),
    ('G5',  'Lead',                 5),
    ('G6',  'Assistant Manager',    6),
    ('G7',  'Manager',              7),
    ('G8',  'Senior Manager',       8),
    ('G9',  'General Manager',      9),
    ('G10', 'Director',            10),
    ('G11', 'Executive Leadership',11)
) v (GradeCode, GradeName, GradeLevel)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Grades g WHERE g.GradeCode = v.GradeCode);
GO

/* 4. Menu entry "Employee Grades" (route /dashboard/employee-grades),
      placed under the same parent menu as Designations. */
IF NOT EXISTS (SELECT 1 FROM dbo.PayrollScreens WHERE ScreenCode = 'GRADES')
BEGIN
    DECLARE @IdParent INT, @OrderNo INT;
    SELECT TOP 1 @IdParent = IdParentPayrollScreen, @OrderNo = OrderNumber
    FROM dbo.PayrollScreens WHERE ScreenCode = 'DESNS';

    IF COLUMNPROPERTY(OBJECT_ID('dbo.PayrollScreens'), 'IdPayrollScreen', 'IsIdentity') = 1
        INSERT INTO dbo.PayrollScreens (ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
        VALUES ('Employee Grades', 'GRADES', 'VAU', @IdParent, ISNULL(@OrderNo, 0) + 1, 'PAYROLL', 1);
    ELSE
        INSERT INTO dbo.PayrollScreens (IdPayrollScreen, ScreenName, ScreenCode, ValidPermissions, IdParentPayrollScreen, OrderNumber, APPTYPE, Enabled)
        SELECT ISNULL(MAX(IdPayrollScreen), 0) + 1, 'Employee Grades', 'GRADES', 'VAU', @IdParent, ISNULL(@OrderNo, 0) + 1, 'PAYROLL', 1
        FROM dbo.PayrollScreens;
END
GO

/* Check */
SELECT * FROM dbo.Grades ORDER BY GradeLevel;
SELECT * FROM dbo.PayrollScreens WHERE ScreenCode = 'GRADES';
