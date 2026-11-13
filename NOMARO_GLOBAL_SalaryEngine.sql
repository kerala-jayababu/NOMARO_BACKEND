/* =====================================================================
   NOMARO_GLOBAL - country rules for the generic salary engine
   Run on NOMARO_GLOBAL BEFORE NOMARO_SalaryEngine_Company.sql. Safe to run more than once.
     1. NomaroCountries.TaxEngine         NONE / IN_TDS - copied to the company's SystemParameters 'TaxEngine'
     2. CountryStatutoryRules             statutory rules per country (copied to dbo.StatutoryRules of a new company)
     3. CountryPTSlabs / CountryLWFSlabs  PT / LWF amounts per state (StateCode = company States.StateCode)
     4. India and UAE rules; India PT / LWF slabs copied from an India company database
     5. UAE pension heads become STATUTORY heads (PENSION_EE / PENSION_ER)
     6. Field setting for the employee's tax regime (CountryScreenAttributeConfig + existing companies)
   Rates are as published in 2025-26; payroll / legal should confirm them before go-live.
   ===================================================================== */
-- wrong database: stop every batch below (THROW alone stops only this batch)
IF DB_NAME() <> 'NOMARO_GLOBAL'
BEGIN
    RAISERROR('Run this script against NOMARO_GLOBAL. Nothing was changed.', 16, 1);
    SET NOEXEC ON;
END
GO

/* 1. Tax engine per country */
IF COL_LENGTH('dbo.NomaroCountries', 'TaxEngine') IS NULL
    ALTER TABLE dbo.NomaroCountries ADD TaxEngine VARCHAR(20) NULL;
GO
UPDATE dbo.NomaroCountries SET TaxEngine = 'IN_TDS' WHERE CountryCode = 'IND' AND TaxEngine IS NULL;
UPDATE dbo.NomaroCountries SET TaxEngine = 'NONE'   WHERE CountryCode = 'UAE' AND TaxEngine IS NULL;
GO

/* 2. Statutory rules per country (columns as dbo.StatutoryRules in a company database) */
IF OBJECT_ID('dbo.CountryStatutoryRules', 'U') IS NULL
    CREATE TABLE dbo.CountryStatutoryRules
    (
        IdCountryStatutoryRule       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CountryStatutoryRules PRIMARY KEY,
        CountryCode                  VARCHAR(5)     NOT NULL,
        StatutoryType                VARCHAR(30)    NOT NULL,
        Description                  NVARCHAR(150)  NULL,
        RateType                     VARCHAR(20)    NOT NULL,
        Rate                         DECIMAL(9,4)   NULL,
        RateThreshold                DECIMAL(18,2)  NULL,
        RateAboveThreshold           DECIMAL(9,4)   NULL,
        WageBase                     VARCHAR(20)    NOT NULL,
        WageCeiling                  DECIMAL(18,2)  NULL,
        CeilingWaivedBy              VARCHAR(50)    NULL,
        MinWage                      DECIMAL(18,2)  NULL,
        EligibilityWageLimit         DECIMAL(18,2)  NULL,
        EligibilityWageLimitDisabled DECIMAL(18,2)  NULL,
        MinAge                       INT            NULL,
        MaxAge                       INT            NULL,
        EmployeeFlag                 VARCHAR(50)    NULL,
        DifferenceOfType             VARCHAR(30)    NULL,
        MaxAmount                    DECIMAL(18,2)  NULL,
        EffectiveFrom                DATE           NOT NULL CONSTRAINT DF_CSR_From DEFAULT ('2000-01-01'),
        EffectiveTo                  DATE           NULL,
        IsActive                     BIT            NOT NULL CONSTRAINT DF_CSR_Active DEFAULT (1),
        CONSTRAINT UQ_CountryStatutoryRules UNIQUE (CountryCode, StatutoryType, EffectiveFrom)
    );
GO
-- PT / LWF slabs: same columns as the company PTSlabs / LWFSlabs tables, with the state as StateCode.
-- A first version of this script created them with other column names (WageFrom ...); those are rebuilt.
IF COL_LENGTH('dbo.CountryPTSlabs', 'WageFrom') IS NOT NULL DROP TABLE dbo.CountryPTSlabs;
IF COL_LENGTH('dbo.CountryLWFSlabs', 'WageFrom') IS NOT NULL DROP TABLE dbo.CountryLWFSlabs;
GO
IF OBJECT_ID('dbo.CountryPTSlabs', 'U') IS NULL
    CREATE TABLE dbo.CountryPTSlabs
    (
        IdCountryPTSlab    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CountryPTSlabs PRIMARY KEY,
        CountryCode        VARCHAR(5)     NOT NULL,
        StateCode          VARCHAR(10)    NOT NULL,              -- company States.StateCode
        Gender             CHAR(1)        NULL,                  -- M / F / NULL = all
        SalaryFrom         NUMERIC(18,2)  NOT NULL,
        SalaryTo           NUMERIC(18,2)  NULL,
        PTAmount           NUMERIC(18,2)  NOT NULL,
        ApplicableMonth    TINYINT        NULL,
        DeductionFrequency VARCHAR(20)    NOT NULL CONSTRAINT DF_CPT_Freq DEFAULT ('MONTHLY'),
        WageBasis          VARCHAR(20)    NOT NULL CONSTRAINT DF_CPT_Basis DEFAULT ('GROSS'),
        EffectiveFrom      DATE           NOT NULL,
        EffectiveTo        DATE           NULL,
        IsActive           BIT            NOT NULL CONSTRAINT DF_CPT_Active DEFAULT (1)
    );
GO
IF OBJECT_ID('dbo.CountryLWFSlabs', 'U') IS NULL
    CREATE TABLE dbo.CountryLWFSlabs
    (
        IdCountryLWFSlab INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CountryLWFSlabs PRIMARY KEY,
        CountryCode      VARCHAR(5)     NOT NULL,
        StateCode        VARCHAR(10)    NOT NULL,
        SalaryFrom       NUMERIC(18,2)  NULL,
        SalaryTo         NUMERIC(18,2)  NULL,
        EmployeeAmount   NUMERIC(18,2)  NOT NULL,
        EmployerAmount   NUMERIC(18,2)  NOT NULL,
        DeductionMonths  VARCHAR(50)    NOT NULL,
        EffectiveFrom    DATE           NOT NULL,
        EffectiveTo      DATE           NULL,
        IsActive         BIT            NOT NULL CONSTRAINT DF_CLWF_Active DEFAULT (1)
    );
GO

/* 4. Rules
   RateType   PERCENTAGE = Rate % of the wage base; EMPLOYEE_RATE = employee's VPF rate; DIFFERENCE = Rate % minus DifferenceOfType;
              SLAB = PT / LWF slab of the employee's PT state
   WageBase   PF / ESI / PT / GRATUITY / GROSS = sum of this month's heads with that IsPartOf... flag;
              CONTRACT = monthly contract value of the Gross heads (not prorated)
   EmployeeFlag = EmployeeStatutoryDetails column that must be ticked */
INSERT INTO dbo.CountryStatutoryRules
    (CountryCode, StatutoryType, Description, RateType, Rate, RateThreshold, RateAboveThreshold, WageBase, WageCeiling, CeilingWaivedBy,
     MinWage, EligibilityWageLimit, EligibilityWageLimitDisabled, MinAge, MaxAge, EmployeeFlag, DifferenceOfType, MaxAmount)
SELECT v.* FROM (VALUES
 -- India
 ('IND', 'PF_EE',     N'PF employee 12% (wage up to 15,000 unless PF on actual wage)', 'PERCENTAGE',    12.00, NULL, NULL, 'PF',  15000, 'PFOnActualWage', NULL, NULL,  NULL,  NULL, NULL, 'IsPFApplicable',  NULL,        NULL),
 ('IND', 'VPF',       N'Voluntary PF at the employee''s VPF rate',                       'EMPLOYEE_RATE', NULL,  NULL, NULL, 'PF',  NULL,  NULL,             NULL, NULL,  NULL,  NULL, NULL, 'IsPFApplicable',  NULL,        NULL),
 ('IND', 'PF_ER_EPS', N'Employer pension (EPS) 8.33% of wage up to 15,000, below age 58',  'PERCENTAGE',     8.33, NULL, NULL, 'PF',  15000, NULL,             NULL, NULL,  NULL,  NULL, 58,   'IsEPSApplicable', NULL,        1250),
 ('IND', 'PF_ER_EPF', N'Employer EPF = 12% minus EPS',                                    'DIFFERENCE',    12.00, NULL, NULL, 'PF',  15000, 'PFOnActualWage', NULL, NULL,  NULL,  NULL, NULL, 'IsPFApplicable',  'PF_ER_EPS', NULL),
 ('IND', 'EDLI',      N'EDLI 0.5% of wage up to 15,000',                                  'PERCENTAGE',     0.50, NULL, NULL, 'PF',  15000, NULL,             NULL, NULL,  NULL,  NULL, NULL, 'IsPFApplicable',  NULL,        75),
 ('IND', 'PF_ADMIN',  N'PF admin charges 0.5%',                                           'PERCENTAGE',     0.50, NULL, NULL, 'PF',  15000, 'PFOnActualWage', NULL, NULL,  NULL,  NULL, NULL, 'IsPFApplicable',  NULL,        NULL),
 ('IND', 'ESI_EE',    N'ESI employee 0.75% (wage up to 21,000; 25,000 with disability)',  'PERCENTAGE',     0.75, NULL, NULL, 'ESI', NULL,  NULL,             NULL, 21000, 25000, NULL, NULL, 'IsESIApplicable', NULL,        NULL),
 ('IND', 'ESI_ER',    N'ESI employer 3.25%',                                              'PERCENTAGE',     3.25, NULL, NULL, 'ESI', NULL,  NULL,             NULL, 21000, 25000, NULL, NULL, 'IsESIApplicable', NULL,        NULL),
 ('IND', 'PT',        N'Professional tax - slab of the PT state',                          'SLAB',           NULL,  NULL, NULL, 'PT',  NULL,  NULL,             NULL, NULL,  NULL,  NULL, NULL, 'IsPTApplicable',  NULL,        NULL),
 ('IND', 'LWF_EE',    N'Labour welfare fund - employee',                                   'SLAB',           NULL,  NULL, NULL, 'GROSS', NULL, NULL,             NULL, NULL,  NULL,  NULL, NULL, 'IsLWFApplicable', NULL,        NULL),
 ('IND', 'LWF_ER',    N'Labour welfare fund - employer',                                   'SLAB',           NULL,  NULL, NULL, 'GROSS', NULL, NULL,             NULL, NULL,  NULL,  NULL, NULL, 'IsLWFApplicable', NULL,        NULL),
 -- UAE (GPSSA; contract wage AED 3,000 - 70,000; employer 15% from AED 20,000)
 ('UAE', 'PENSION_EE', N'Pension employee 11% of contract wage (UAE nationals)',           'PERCENTAGE',    11.00, NULL,  NULL,  'CONTRACT', 70000, NULL, 3000, NULL, NULL, NULL, NULL, 'IsPensionApplicable', NULL, NULL),
 ('UAE', 'PENSION_ER', N'Pension employer 12.5%, 15% at AED 20,000 and above',             'PERCENTAGE',    12.50, 20000, 15.00, 'CONTRACT', 70000, NULL, 3000, NULL, NULL, NULL, NULL, 'IsPensionApplicable', NULL, NULL)
) AS v(CountryCode, StatutoryType, Description, RateType, Rate, RateThreshold, RateAboveThreshold, WageBase, WageCeiling, CeilingWaivedBy,
       MinWage, EligibilityWageLimit, EligibilityWageLimitDisabled, MinAge, MaxAge, EmployeeFlag, DifferenceOfType, MaxAmount)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CountryStatutoryRules c WHERE c.CountryCode = v.CountryCode AND c.StatutoryType = v.StatutoryType);
GO

-- PT / LWF slabs for new India companies: copied from an India company database on this server that already has them.
-- Set @IndiaDb; each state is taken once (states already in the country tables are skipped).
DECLARE @IndiaDb SYSNAME = N'NOMARO_GGHOSPITAL';   -- <-- India database with the PT / LWF slabs
IF DB_ID(@IndiaDb) IS NULL
    PRINT 'Skipped PT / LWF slabs: database ' + @IndiaDb + ' not found.';
ELSE
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'
INSERT INTO dbo.CountryPTSlabs (CountryCode, StateCode, Gender, SalaryFrom, SalaryTo, PTAmount, ApplicableMonth, DeductionFrequency, WageBasis, EffectiveFrom, EffectiveTo, IsActive)
SELECT ''IND'', st.StateCode, p.Gender, p.SalaryFrom, p.SalaryTo, p.PTAmount, p.ApplicableMonth, p.DeductionFrequency, p.WageBasis, p.EffectiveFrom, p.EffectiveTo, p.IsActive
FROM ' + QUOTENAME(@IndiaDb) + N'.dbo.PTSlabs p
JOIN ' + QUOTENAME(@IndiaDb) + N'.dbo.States st ON st.IdState = p.IdState
WHERE NOT EXISTS (SELECT 1 FROM dbo.CountryPTSlabs c WHERE c.CountryCode = ''IND'' AND c.StateCode = st.StateCode);
PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + '' PT slab rows added.'';

INSERT INTO dbo.CountryLWFSlabs (CountryCode, StateCode, SalaryFrom, SalaryTo, EmployeeAmount, EmployerAmount, DeductionMonths, EffectiveFrom, EffectiveTo, IsActive)
SELECT ''IND'', st.StateCode, l.SalaryFrom, l.SalaryTo, l.EmployeeAmount, l.EmployerAmount, l.DeductionMonths, l.EffectiveFrom, l.EffectiveTo, l.IsActive
FROM ' + QUOTENAME(@IndiaDb) + N'.dbo.LWFSlabs l
JOIN ' + QUOTENAME(@IndiaDb) + N'.dbo.States st ON st.IdState = l.IdState
WHERE NOT EXISTS (SELECT 1 FROM dbo.CountryLWFSlabs c WHERE c.CountryCode = ''IND'' AND c.StateCode = st.StateCode);
PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + '' LWF slab rows added.'';';
    IF OBJECT_ID(QUOTENAME(@IndiaDb) + N'.dbo.PTSlabs') IS NOT NULL
        EXEC sp_executesql @sql;
    ELSE
        PRINT 'Skipped PT / LWF slabs: ' + @IndiaDb + ' has no PTSlabs table.';
END
GO

/* 5. UAE pension heads: statutory instead of a formula */
IF OBJECT_ID('dbo.CountrySalaryHeads', 'U') IS NOT NULL
    UPDATE dbo.CountrySalaryHeads
    SET CalculationMethod = 'STATUTORY', StatutoryType = SalaryHeadCode, CustomFormula = NULL, PercentageValue = NULL
    WHERE CountryCode = 'UAE' AND SalaryHeadCode IN ('PENSION_EE', 'PENSION_ER');
GO

/* 6. Tax regime field (India shows it, UAE hides it) */
IF OBJECT_ID('dbo.CountryScreenAttributeConfig', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.CountryScreenAttributeConfig (CountryCode, ScreenCode, AttributeID, DisplayLabel, FieldVisibility, MaxLength, DataType)
    SELECT v.* FROM (VALUES ('IND', 'EMPPROF', 'IdTaxRegime', N'Tax Regime', 1, NULL, 'dropdown'),
                            ('UAE', 'EMPPROF', 'IdTaxRegime', NULL,          0, NULL, 'dropdown')) AS v(CountryCode, ScreenCode, AttributeID, DisplayLabel, FieldVisibility, MaxLength, DataType)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CountryScreenAttributeConfig c WHERE c.CountryCode = v.CountryCode AND c.ScreenCode = v.ScreenCode AND c.AttributeID = v.AttributeID);

    INSERT INTO dbo.CompanyScreenAttributeConfig (CompanyCode, ScreenCode, AttributeID, DisplayLabel, FieldVisibility, MaxLength, DataType)
    SELECT i.CompanyCode, c.ScreenCode, c.AttributeID, c.DisplayLabel, c.FieldVisibility, c.MaxLength, c.DataType
    FROM dbo.NomaroInstanceConfig i
    JOIN dbo.CountryScreenAttributeConfig c ON c.CountryCode = i.CountryCode AND c.AttributeID = 'IdTaxRegime'
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CompanyScreenAttributeConfig x
                      WHERE x.CompanyCode = i.CompanyCode AND x.ScreenCode = c.ScreenCode AND x.AttributeID = c.AttributeID);
END
GO

SELECT CountryCode, StatutoryType, RateType, Rate, WageBase, WageCeiling, EmployeeFlag FROM dbo.CountryStatutoryRules ORDER BY CountryCode, StatutoryType;
SELECT CountryCode, CountryName, TaxEngine FROM dbo.NomaroCountries;
GO
SET NOEXEC OFF;
GO
