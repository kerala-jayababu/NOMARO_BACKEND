/* =====================================================================
   NOMARO_GLOBAL - country defaults for company registration
   1. CountryScreenAttributeConfig  field labels / visibility per country  (already created by you; kept if present)
   2. CountrySalaryHeads           default salary heads per country
   3. GlobalSystemParameters 'OnboardingSourceDatabase'  one source database for all countries
   4. UAE default salary heads
   5. India default salary heads, copied from an existing India company database (set the name)
   6. Existing companies: copy their country's field settings to CompanyScreenAttributeConfig
   Run on NOMARO_GLOBAL. Safe to run more than once.
   ===================================================================== */
-- wrong database: stop every batch below (THROW alone stops only this batch)
IF DB_NAME() <> 'NOMARO_GLOBAL'
BEGIN
    RAISERROR('Run this script against NOMARO_GLOBAL. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO

/* 1. Field settings per country ------------------------------------------------ */
IF OBJECT_ID('dbo.CountryScreenAttributeConfig', 'U') IS NULL
    CREATE TABLE dbo.CountryScreenAttributeConfig
    (
        IdCountryScreenAttributeConfig INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CountryScreenAttributeConfig PRIMARY KEY,
        CountryCode     VARCHAR(5)    NOT NULL,
        ScreenCode      VARCHAR(50)   NOT NULL,
        AttributeID     VARCHAR(100)  NOT NULL,
        DisplayLabel    NVARCHAR(100) NULL,
        FieldVisibility BIT           NULL,
        MaxLength       INT           NULL,
        DataType        VARCHAR(50)   NULL
    );
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_CountryScreenAttributeConfig' AND object_id = OBJECT_ID('dbo.CountryScreenAttributeConfig'))
   AND NOT EXISTS (SELECT CountryCode, ScreenCode, AttributeID FROM dbo.CountryScreenAttributeConfig
                   GROUP BY CountryCode, ScreenCode, AttributeID HAVING COUNT(*) > 1)
    CREATE UNIQUE INDEX UQ_CountryScreenAttributeConfig ON dbo.CountryScreenAttributeConfig (CountryCode, ScreenCode, AttributeID);
GO

/* 2. Default salary heads per country -------------------------------------------
   Same columns as a company's dbo.SalaryHeads, except the links to other heads are kept by CODE
   (PercentageOfHeadCode -> IdPercentageSalaryHead, BaseHeadCode -> IdBaseSalaryHead) because ids differ per company.
   CreatedBy / CreatedOn are set to the new company's administrator and the registration time. */
IF OBJECT_ID('dbo.CountrySalaryHeads', 'U') IS NULL
    CREATE TABLE dbo.CountrySalaryHeads
    (
        IdCountrySalaryHead  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CountrySalaryHeads PRIMARY KEY,
        CountryCode          VARCHAR(5)     NOT NULL,
        SalaryHeadCode       VARCHAR(50)    NOT NULL,
        SalaryHeadName       NVARCHAR(60)   NOT NULL,
        HeadType             VARCHAR(50)    NOT NULL,           -- EARNINGS / DEDUCTION / EMPLOYER_CONTRIBUTION / REIMBURSEMENT
        IsTaxable            BIT            NOT NULL CONSTRAINT DF_CSH_IsTaxable DEFAULT (0),
        IsActive             BIT            NOT NULL CONSTRAINT DF_CSH_IsActive DEFAULT (1),
        CalculationMethod    VARCHAR(50)    NOT NULL,           -- FIXEDAMOUNT / PERCENTAGE / FORMULA / STATUTORY / MANUAL
        PercentageOfHeadCode VARCHAR(50)    NULL,
        PercentageValue      DECIMAL(18,4)  NULL,
        FixedValue           DECIMAL(18,2)  NULL,
        CustomFormula        NVARCHAR(500)  NULL,
        OrderNumber          INT            NULL,
        NonTaxableThreshold  DECIMAL(18,2)  NULL,
        DisbursingMonths     VARCHAR(100)   NULL,
        StatutoryType        VARCHAR(50)    NULL,
        IsPartOfGross        BIT            NOT NULL CONSTRAINT DF_CSH_Gross DEFAULT (0),
        IsPartOfCTC          BIT            NOT NULL CONSTRAINT DF_CSH_CTC DEFAULT (0),
        IsPartOfPFWage       BIT            NOT NULL CONSTRAINT DF_CSH_PF DEFAULT (0),
        IsPartOfESIWage      BIT            NOT NULL CONSTRAINT DF_CSH_ESI DEFAULT (0),
        IsPartOfGratuityWage BIT            NOT NULL CONSTRAINT DF_CSH_Gratuity DEFAULT (0),
        IsPartOfPTWage       BIT            NOT NULL CONSTRAINT DF_CSH_PT DEFAULT (0),
        IsProratedOnPaidDays BIT            NOT NULL CONSTRAINT DF_CSH_Prorated DEFAULT (0),
        IsArrearHead         BIT            NOT NULL CONSTRAINT DF_CSH_Arrear DEFAULT (0),
        BaseHeadCode         VARCHAR(50)    NULL,
        MinAmount            DECIMAL(18,2)  NULL,
        MaxAmount            DECIMAL(18,2)  NULL,
        WageCeiling          DECIMAL(18,2)  NULL,
        PayFrequency         VARCHAR(20)    NULL,
        CalcSequence         INT            NULL,
        RoundingRule         VARCHAR(20)    NULL,
        ShowOnPayslip        BIT            NOT NULL CONSTRAINT DF_CSH_Payslip DEFAULT (1),
        CONSTRAINT UQ_CountrySalaryHeads UNIQUE (CountryCode, SalaryHeadCode)
    );
GO

/* 3. One source database for every country ---------------------------------------
   Used when NomaroCountries.SourceDatabaseName is empty (a country can still name its own).
   The source database keeps dbo.SalaryHeads EMPTY; heads come from CountrySalaryHeads. */
IF NOT EXISTS (SELECT 1 FROM dbo.GlobalSystemParameters WHERE ParameterName = 'OnboardingSourceDatabase')
    INSERT INTO dbo.GlobalSystemParameters (ParameterName, ParameterDescription, ParameterValue)
    VALUES ('OnboardingSourceDatabase', 'Database copied to create every new company (unless NomaroCountries.SourceDatabaseName is set)',
            N'NOMARO_SOURCE');   -- <-- change to your source database name
GO
-- To use only the shared source database, clear the per-country names:
-- UPDATE dbo.NomaroCountries SET SourceDatabaseName = NULL;

/* 4. UAE default salary heads ------------------------------------------------------
   Same 20 heads as NOMARO_UAE_SalaryHeads.sql. Added active; a company can switch off the heads it does not use. */
INSERT INTO dbo.CountrySalaryHeads
    (CountryCode, SalaryHeadCode, SalaryHeadName, HeadType, IsTaxable, IsActive, CalculationMethod, PercentageOfHeadCode, PercentageValue,
     FixedValue, CustomFormula, OrderNumber, NonTaxableThreshold, DisbursingMonths, StatutoryType,
     IsPartOfGross, IsPartOfCTC, IsPartOfPFWage, IsPartOfESIWage, IsPartOfGratuityWage, IsPartOfPTWage,
     IsProratedOnPaidDays, IsArrearHead, BaseHeadCode, MinAmount, MaxAmount, WageCeiling, PayFrequency, CalcSequence, RoundingRule, ShowOnPayslip)
SELECT v.*
FROM (VALUES
    ('UAE', 'BASIC', N'Basic Salary', 'EARNINGS', 0, 1, 'FIXEDAMOUNT', NULL, NULL, NULL, NULL, 1, NULL, NULL, NULL, 1, 1, 0, 0, 1, 0, 1, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 1, 'NEAREST', 1),
    ('UAE', 'HRA', N'Housing Allowance', 'EARNINGS', 0, 1, 'PERCENTAGE', 'BASIC', 25, NULL, NULL, 2, NULL, NULL, NULL, 1, 1, 0, 0, 0, 0, 1, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 2, 'NEAREST', 1),
    ('UAE', 'TRANSPORT', N'Transport Allowance', 'EARNINGS', 0, 1, 'PERCENTAGE', 'BASIC', 10, NULL, NULL, 3, NULL, NULL, NULL, 1, 1, 0, 0, 0, 0, 1, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 3, 'NEAREST', 1),
    ('UAE', 'OTHER_ALW', N'Other / Cost of Living Allowance', 'EARNINGS', 0, 1, 'FIXEDAMOUNT', NULL, NULL, NULL, NULL, 4, NULL, NULL, NULL, 1, 1, 0, 0, 0, 0, 1, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 4, 'NEAREST', 1),
    ('UAE', 'MOBILE', N'Mobile Allowance', 'EARNINGS', 0, 1, 'FIXEDAMOUNT', NULL, NULL, NULL, NULL, 5, NULL, NULL, NULL, 1, 1, 0, 0, 0, 0, 1, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 5, 'NEAREST', 1),
    ('UAE', 'FOOD', N'Food Allowance', 'EARNINGS', 0, 1, 'FIXEDAMOUNT', NULL, NULL, NULL, NULL, 6, NULL, NULL, NULL, 1, 1, 0, 0, 0, 0, 1, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 6, 'NEAREST', 1),
    ('UAE', 'OVERTIME', N'Overtime', 'EARNINGS', 0, 1, 'MANUAL', NULL, NULL, NULL, NULL, 7, NULL, NULL, NULL, 1, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 7, 'NEAREST', 1),
    ('UAE', 'AIR_TICKET', N'Annual Air Ticket', 'EARNINGS', 0, 1, 'MANUAL', NULL, NULL, NULL, NULL, 8, NULL, NULL, NULL, 1, 1, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'ANNUAL', 8, 'NEAREST', 1),
    ('UAE', 'LEAVE_ENCASH', N'Leave Encashment', 'EARNINGS', 0, 1, 'MANUAL', NULL, NULL, NULL, NULL, 9, NULL, NULL, NULL, 1, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'ONE_TIME', 9, 'NEAREST', 1),
    ('UAE', 'BONUS', N'Bonus', 'EARNINGS', 0, 1, 'MANUAL', NULL, NULL, NULL, NULL, 10, NULL, NULL, NULL, 1, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'ONE_TIME', 10, 'NEAREST', 1),
    ('UAE', 'EOSB_PAY', N'End of Service Gratuity', 'EARNINGS', 0, 1, 'MANUAL', NULL, NULL, NULL, NULL, 11, NULL, NULL, NULL, 1, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'ONE_TIME', 11, 'NEAREST', 1),
    ('UAE', 'PENSION_EE', N'Pension - Employee (GPSSA/ADPF)', 'DEDUCTION', 0, 1, 'FORMULA', NULL, NULL, NULL, N'([BASIC] + [HRA] + [TRANSPORT] + [OTHER_ALW]) * 11 / 100', 20, NULL, NULL, NULL, 0, 0, 0, 0, 0, 0, 1, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 20, 'NEAREST', 1),
    ('UAE', 'GCC_PENSION_EE', N'GCC National Pension - Employee', 'DEDUCTION', 0, 1, 'MANUAL', NULL, NULL, NULL, NULL, 21, NULL, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 21, 'NEAREST', 1),
    ('UAE', 'UNPAID_LEAVE', N'Unpaid Leave / Absence', 'DEDUCTION', 0, 1, 'MANUAL', NULL, NULL, NULL, NULL, 22, NULL, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 22, 'NEAREST', 1),
    ('UAE', 'LOAN_REC', N'Loan / Advance Recovery', 'DEDUCTION', 0, 1, 'MANUAL', NULL, NULL, NULL, NULL, 23, NULL, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 23, 'NEAREST', 1),
    ('UAE', 'PENALTY', N'Disciplinary Deduction', 'DEDUCTION', 0, 1, 'MANUAL', NULL, NULL, NULL, NULL, 24, NULL, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 24, 'NEAREST', 1),
    ('UAE', 'PENSION_ER', N'Pension - Employer (GPSSA/ADPF)', 'EMPLOYER_CONTRIBUTION', 0, 1, 'FORMULA', NULL, NULL, NULL, N'([BASIC] + [HRA] + [TRANSPORT] + [OTHER_ALW]) * 12.5 / 100', 30, NULL, NULL, NULL, 0, 1, 0, 0, 0, 0, 1, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 30, 'NEAREST', 0),
    ('UAE', 'EOSB_PROV', N'Gratuity Provision', 'EMPLOYER_CONTRIBUTION', 0, 1, 'FORMULA', NULL, NULL, NULL, N'[BASIC] * 21 / 365', 31, NULL, NULL, NULL, 0, 1, 0, 0, 0, 0, 1, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 31, 'NEAREST', 0),
    ('UAE', 'MED_INS', N'Medical Insurance', 'EMPLOYER_CONTRIBUTION', 0, 1, 'FIXEDAMOUNT', NULL, NULL, NULL, NULL, 32, NULL, NULL, NULL, 0, 1, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'MONTHLY', 32, 'NEAREST', 0),
    ('UAE', 'VISA_COST', N'Visa / Labour Card Cost', 'EMPLOYER_CONTRIBUTION', 0, 1, 'FIXEDAMOUNT', NULL, NULL, NULL, NULL, 33, NULL, NULL, NULL, 0, 1, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, 'ANNUAL', 33, 'NEAREST', 0)
) AS v(CountryCode, SalaryHeadCode, SalaryHeadName, HeadType, IsTaxable, IsActive, CalculationMethod, PercentageOfHeadCode, PercentageValue,
       FixedValue, CustomFormula, OrderNumber, NonTaxableThreshold, DisbursingMonths, StatutoryType,
       IsPartOfGross, IsPartOfCTC, IsPartOfPFWage, IsPartOfESIWage, IsPartOfGratuityWage, IsPartOfPTWage,
       IsProratedOnPaidDays, IsArrearHead, BaseHeadCode, MinAmount, MaxAmount, WageCeiling, PayFrequency, CalcSequence, RoundingRule, ShowOnPayslip)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CountrySalaryHeads c WHERE c.CountryCode = v.CountryCode AND c.SalaryHeadCode = v.SalaryHeadCode);
GO

/* 5. India default salary heads - copied from an India company / template database on THIS server.
   Set @IndiaDb to the database whose SalaryHeads are the India standard. Only ACTIVE heads are copied. */
DECLARE @IndiaDb SYSNAME = N'NOMARO_GGHOSPITAL';   -- <-- India database with the standard heads
IF DB_ID(@IndiaDb) IS NULL
    PRINT 'Skipped India heads: database ' + @IndiaDb + ' not found.';
ELSE
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'
INSERT INTO dbo.CountrySalaryHeads
    (CountryCode, SalaryHeadCode, SalaryHeadName, HeadType, IsTaxable, IsActive, CalculationMethod, PercentageOfHeadCode, PercentageValue,
     FixedValue, CustomFormula, OrderNumber, NonTaxableThreshold, DisbursingMonths, StatutoryType,
     IsPartOfGross, IsPartOfCTC, IsPartOfPFWage, IsPartOfESIWage, IsPartOfGratuityWage, IsPartOfPTWage,
     IsProratedOnPaidDays, IsArrearHead, BaseHeadCode, MinAmount, MaxAmount, WageCeiling, PayFrequency, CalcSequence, RoundingRule, ShowOnPayslip)
SELECT ''IND'', s.SalaryHeadCode, s.SalaryHeadName, s.HeadType, s.IsTaxable, s.IsActive, s.CalculationMethod, p.SalaryHeadCode, s.PercentageValue,
       s.FixedValue, s.CustomFormula, s.OrderNumber, s.NonTaxableThreshold, s.DisbursingMonths, s.StatutoryType,
       s.IsPartOfGross, s.IsPartOfCTC, s.IsPartOfPFWage, s.IsPartOfESIWage, s.IsPartOfGratuityWage, s.IsPartOfPTWage,
       s.IsProratedOnPaidDays, s.IsArrearHead, b.SalaryHeadCode, s.MinAmount, s.MaxAmount, s.WageCeiling, s.PayFrequency, s.CalcSequence, s.RoundingRule, s.ShowOnPayslip
FROM ' + QUOTENAME(@IndiaDb) + N'.dbo.SalaryHeads s
LEFT JOIN ' + QUOTENAME(@IndiaDb) + N'.dbo.SalaryHeads p ON p.IdSalaryHead = s.IdPercentageSalaryHead
LEFT JOIN ' + QUOTENAME(@IndiaDb) + N'.dbo.SalaryHeads b ON b.IdSalaryHead = s.IdBaseSalaryHead
WHERE s.IsActive = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.CountrySalaryHeads c WHERE c.CountryCode = ''IND'' AND c.SalaryHeadCode = s.SalaryHeadCode);
PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + '' India heads added.'';';
    EXEC sp_executesql @sql;
END
GO

/* 6. Existing companies: copy their country's field settings (rows a company already has are kept) */
INSERT INTO dbo.CompanyScreenAttributeConfig (CompanyCode, ScreenCode, AttributeID, DisplayLabel, FieldVisibility, MaxLength, DataType)
SELECT i.CompanyCode, c.ScreenCode, c.AttributeID, c.DisplayLabel, c.FieldVisibility, c.MaxLength, c.DataType
FROM dbo.NomaroInstanceConfig i
JOIN dbo.CountryScreenAttributeConfig c ON c.CountryCode = i.CountryCode
WHERE NOT EXISTS (SELECT 1 FROM dbo.CompanyScreenAttributeConfig x
                  WHERE x.CompanyCode = i.CompanyCode AND x.ScreenCode = c.ScreenCode AND x.AttributeID = c.AttributeID);
GO

/* Check */
SELECT CountryCode, COUNT(*) AS FieldSettings FROM dbo.CountryScreenAttributeConfig GROUP BY CountryCode;
SELECT CountryCode, COUNT(*) AS SalaryHeads   FROM dbo.CountrySalaryHeads           GROUP BY CountryCode;
SELECT c.CountryCode, c.CountryName, c.SourceDatabaseName,
       (SELECT ParameterValue FROM dbo.GlobalSystemParameters WHERE ParameterName = 'OnboardingSourceDatabase') AS SharedSourceDatabase
FROM dbo.NomaroCountries c;
GO
SET NOEXEC OFF;
GO
