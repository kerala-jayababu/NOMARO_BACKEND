/* =====================================================================
   NOMARO - generic salary engine (company database)
   Run on a company database (NOMARO_<CODE>) and on the source database used to create companies.
   Do NOT run on NOMARO_GLOBAL. Safe to run more than once.

   What it adds
     1. dbo.StatutoryRules        rate / wage base / ceiling / eligibility per statutory type (PF, ESI, PT, LWF, pension ...)
     2. dbo.PTSlabs, dbo.LWFSlabs  PT / LWF slabs per state (existing tables are kept; created only when missing)
     3. EmployeeStatutoryDetails.IdTaxRegime  1 = New regime, 2 = Old regime (India TDS)
     4. dbo.SalaryFormulaToSql      helper that makes a salary head formula safe for decimal arithmetic
     5. dbo.CalculateIncomeTax_IN   India monthly TDS (annual projection, regime slabs, standard deduction, 87A rebate,
                                    marginal relief, cess) using TaxYearConfigs / TaxSlabs
     6. dbo.GenerateSalary_MultipleEmployees  rewritten: everything comes from configuration
     7. Section 7 at the end: copy the country's rules from NOMARO_GLOBAL into this database (set @Country)

   System parameters read by the procedure (dbo.SystemParameters, ParameterName -> ParameterValue)
     TaxEngine            NONE (no income tax, e.g. UAE) or IN_TDS (India). Missing = NONE.
     DefaultTaxRegime     1 or 2, used when an employee has no IdTaxRegime. Missing = 1 (New regime).
     OvertimeCutoffDay    e.g. 12 -> overtime approved from the 13th of last month to the 12th of this month.
                          Missing = overtime approved within the salary month.
     OverTimeSalaryHeadID salary head that carries overtime (existing parameter). Missing = head with code OVERTIME.
     ReportingCurrency    e.g. USD -> AmountInUSD columns filled using CurrencyConversions (FromCurrency = this).
                          Missing = AmountInUSD left empty.
   ===================================================================== */
IF DB_NAME() = 'NOMARO_GLOBAL'
BEGIN
    RAISERROR('Run this script on a company database, not NOMARO_GLOBAL.', 16, 1);
    SET NOEXEC ON;
END
GO

/* 1. Statutory rules ----------------------------------------------------------------- */
IF OBJECT_ID('dbo.StatutoryRules', 'U') IS NULL
    CREATE TABLE dbo.StatutoryRules
    (
        IdStatutoryRule              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StatutoryRules PRIMARY KEY,
        StatutoryType                VARCHAR(30)    NOT NULL,   -- matches SalaryHeads.StatutoryType
        Description                  NVARCHAR(150)  NULL,
        RateType                     VARCHAR(20)    NOT NULL,   -- PERCENTAGE / EMPLOYEE_RATE (VPF) / DIFFERENCE / SLAB (PT, LWF)
        Rate                         DECIMAL(9,4)   NULL,       -- % of the wage base
        RateThreshold                DECIMAL(18,2)  NULL,       -- wage base at or above this uses RateAboveThreshold
        RateAboveThreshold           DECIMAL(9,4)   NULL,
        WageBase                     VARCHAR(20)    NOT NULL,   -- PF / ESI / PT / GRATUITY / GROSS / CONTRACT (SalaryHeads IsPartOf... flags)
        WageCeiling                  DECIMAL(18,2)  NULL,       -- wage base capped at this
        CeilingWaivedBy              VARCHAR(50)    NULL,       -- EmployeeStatutoryDetails flag that removes the cap (PFOnActualWage)
        MinWage                      DECIMAL(18,2)  NULL,       -- wage base raised to this (UAE pension AED 3,000)
        EligibilityWageLimit         DECIMAL(18,2)  NULL,       -- not applied when the wage base is above this (ESI 21,000)
        EligibilityWageLimitDisabled DECIMAL(18,2)  NULL,       -- same, for persons with disability (ESI 25,000)
        MinAge                       INT            NULL,
        MaxAge                       INT            NULL,       -- applied only below this age (EPS: 58)
        EmployeeFlag                 VARCHAR(50)    NULL,       -- EmployeeStatutoryDetails column that must be 1
        DifferenceOfType             VARCHAR(30)    NULL,       -- DIFFERENCE: Rate% of base minus this type (EPF employer = 12% - EPS)
        MaxAmount                    DECIMAL(18,2)  NULL,
        EffectiveFrom                DATE           NOT NULL CONSTRAINT DF_StatutoryRules_From DEFAULT ('2000-01-01'),
        EffectiveTo                  DATE           NULL,
        IsActive                     BIT            NOT NULL CONSTRAINT DF_StatutoryRules_Active DEFAULT (1)
    );
GO

/* 2. PT / LWF slabs per state ---------------------------------------------------------- */
-- same structure as the PTSlabs / LWFSlabs tables already used for India; created only when missing
IF OBJECT_ID('dbo.PTSlabs', 'U') IS NULL
    CREATE TABLE dbo.PTSlabs
    (
        IdPTSlab           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PTSlabs PRIMARY KEY,
        IdState            INT            NOT NULL,              -- States.IdState (employee's PT state)
        Gender             CHAR(1)        NULL,                  -- M / F / NULL = all
        SalaryFrom         NUMERIC(18,2)  NOT NULL,
        SalaryTo           NUMERIC(18,2)  NULL,                  -- NULL = no upper limit
        PTAmount           NUMERIC(18,2)  NOT NULL,
        ApplicableMonth    TINYINT        NULL,                  -- this amount only in this month (e.g. 2 = February Rs 300)
        DeductionFrequency VARCHAR(20)    NOT NULL CONSTRAINT DF_PTSlabs_Freq DEFAULT ('MONTHLY'),  -- other than MONTHLY: only in ApplicableMonth
        WageBasis          VARCHAR(20)    NOT NULL CONSTRAINT DF_PTSlabs_Basis DEFAULT ('GROSS'),   -- GROSS / PT / PF / ESI / BASIC... wage the slab is read on
        EffectiveFrom      DATE           NOT NULL,
        EffectiveTo        DATE           NULL,
        IsActive           BIT            NOT NULL CONSTRAINT DF_PTSlabs_Active DEFAULT (1)
    );
GO
IF OBJECT_ID('dbo.LWFSlabs', 'U') IS NULL
    CREATE TABLE dbo.LWFSlabs
    (
        IdLWFSlab        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LWFSlabs PRIMARY KEY,
        IdState          INT            NOT NULL,                -- employee's PT state is used as the LWF state
        SalaryFrom       NUMERIC(18,2)  NULL,
        SalaryTo         NUMERIC(18,2)  NULL,
        EmployeeAmount   NUMERIC(18,2)  NOT NULL,
        EmployerAmount   NUMERIC(18,2)  NOT NULL,
        DeductionMonths  VARCHAR(50)    NOT NULL,                -- e.g. '6,12'
        EffectiveFrom    DATE           NOT NULL,
        EffectiveTo      DATE           NULL,
        IsActive         BIT            NOT NULL CONSTRAINT DF_LWFSlabs_Active DEFAULT (1)
    );
GO

/* 3. Tax regime per employee (India) --------------------------------------------------- */
IF COL_LENGTH('dbo.EmployeeStatutoryDetails', 'IdTaxRegime') IS NULL
    ALTER TABLE dbo.EmployeeStatutoryDetails ADD IdTaxRegime INT NULL;   -- 1 = New regime, 2 = Old regime
GO

/* 4. Formula helper ----------------------------------------------------------------------
   Salary head formulas use [CODE], numbers, + - * / ( ). SQL Server divides integers as integers (21 / 365 = 0),
   so every whole number outside [ ] gets ".0" appended. */
CREATE OR ALTER FUNCTION dbo.SalaryFormulaToSql (@Formula NVARCHAR(1000))
RETURNS NVARCHAR(2000)
AS
BEGIN
    DECLARE @out NVARCHAR(2000) = N'', @i INT = 1, @len INT = LEN(ISNULL(@Formula, N'')), @c NCHAR(1), @num NVARCHAR(50);
    WHILE @i <= @len
    BEGIN
        SET @c = SUBSTRING(@Formula, @i, 1);
        IF @c = N'['
        BEGIN
            DECLARE @close INT = CHARINDEX(N']', @Formula, @i);
            IF @close = 0 SET @close = @len;
            SET @out += SUBSTRING(@Formula, @i, @close - @i + 1);
            SET @i = @close + 1;
        END
        ELSE IF @c LIKE N'[0-9]' OR (@c = N'.' AND SUBSTRING(@Formula, @i + 1, 1) LIKE N'[0-9]')
        BEGIN
            SET @num = N'';
            WHILE @i <= @len AND (SUBSTRING(@Formula, @i, 1) LIKE N'[0-9]' OR SUBSTRING(@Formula, @i, 1) = N'.')
            BEGIN
                SET @num += SUBSTRING(@Formula, @i, 1);
                SET @i += 1;
            END
            SET @out += CASE WHEN CHARINDEX(N'.', @num) > 0 THEN @num ELSE @num + N'.0' END;
        END
        ELSE
        BEGIN
            SET @out += @c;
            SET @i += 1;
        END
    END
    RETURN @out;
END
GO

/* 5. India TDS ---------------------------------------------------------------------------
   Called by GenerateSalary_MultipleEmployees with #Calc / #Heads of the employee being processed.
   Annual taxable income = taxable earnings already paid this financial year + this month
                           + this month's recurring taxable earnings x remaining months
                           - standard deduction (both regimes)
                           - Old regime only: professional tax and employee PF / VPF (80C, max 1,50,000)
   Tax = slabs of TaxYearConfigs (financial year + regime) for the age category, 87A rebate / marginal relief, + cess.
   This month's TDS = (annual tax - TDS already deducted this year) / months left including this one.
   Not covered: surcharge (income above 50 lakh), employee investment declarations, HRA / LTA exemptions, previous employer. */
CREATE OR ALTER PROCEDURE dbo.CalculateIncomeTax_IN
    @IdEmployee      INT,
    @MonthFrom       DATE,
    @FYFrom          DATE,
    @FYTo            DATE,
    @Age             INT,
    @IdTaxRegime     INT,
    @Tds             DECIMAL(18,2) OUTPUT,
    @AnnualTaxable   DECIMAL(18,2) OUTPUT,
    @AnnualTax       DECIMAL(18,2) OUTPUT,
    @Log             NVARCHAR(1000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT @Tds = 0, @AnnualTaxable = 0, @AnnualTax = 0, @Log = N'';

    DECLARE @IdFinancialYear INT = (SELECT TOP 1 IdFinancialYear FROM dbo.FinancialYears
                                    WHERE @MonthFrom BETWEEN CAST(FinancialYearFrom AS DATE) AND CAST(FinancialYearTo AS DATE));
    DECLARE @IdTaxYearConfig INT, @StdDeduction DECIMAL(18,2), @RebateLimit DECIMAL(18,2), @RebateMax DECIMAL(18,2),
            @CessRate DECIMAL(9,4), @MarginalRelief BIT;
    SELECT TOP 1 @IdTaxYearConfig = IdTaxYearConfig, @StdDeduction = StandardDeduction, @RebateLimit = RebateIncomeLimit,
                 @RebateMax = RebateMaxAmount, @CessRate = CessRate, @MarginalRelief = AllowMarginalRelief
    FROM dbo.TaxYearConfigs WHERE IdFinancialYear = @IdFinancialYear AND IdTaxRegime = @IdTaxRegime;
    IF @IdTaxYearConfig IS NULL
    BEGIN
        SET @Log = N'No tax configuration for this financial year and regime - TDS not calculated.';
        RETURN;
    END

    -- months still to be paid after this one in the financial year
    DECLARE @Remaining INT = DATEDIFF(MONTH, @MonthFrom, @FYTo);
    IF @Remaining < 0 SET @Remaining = 0;

    -- this month: taxable earnings actually paid, and the recurring monthly part (for the projection)
    DECLARE @CurrentTaxable DECIMAL(18,2), @RecurringTaxable DECIMAL(18,2);
    SELECT @CurrentTaxable = ISNULL(SUM(CASE WHEN c.Amount > ISNULL(h.NonTaxableThreshold, 0) THEN c.Amount - ISNULL(h.NonTaxableThreshold, 0) ELSE 0 END), 0),
           @RecurringTaxable = ISNULL(SUM(CASE WHEN c.IsRecurring = 1 AND c.FullAmount > ISNULL(h.NonTaxableThreshold, 0)
                                               THEN c.FullAmount - ISNULL(h.NonTaxableThreshold, 0) ELSE 0 END), 0)
    FROM #Calc c JOIN #Heads h ON h.IdSalaryHead = c.IdSalaryHead
    WHERE h.HeadType = 'EARNINGS' AND h.IsTaxable = 1;

    -- earlier months of this financial year
    DECLARE @PrevTaxable DECIMAL(18,2), @PrevTds DECIMAL(18,2), @PrevPT DECIMAL(18,2), @PrevPF DECIMAL(18,2);
    SELECT @PrevTaxable = ISNULL(SUM(CASE WHEN (s.HeadType IN ('EARNINGS', 'EARNING')) AND s.IsTaxable = 1 AND d.Amount > ISNULL(s.NonTaxableThreshold, 0)
                                          THEN d.Amount - ISNULL(s.NonTaxableThreshold, 0) ELSE 0 END), 0),
           @PrevTds = ISNULL(SUM(CASE WHEN s.StatutoryType = 'TDS' THEN d.Amount ELSE 0 END), 0),
           @PrevPT = ISNULL(SUM(CASE WHEN s.StatutoryType = 'PT' THEN d.Amount ELSE 0 END), 0),
           @PrevPF = ISNULL(SUM(CASE WHEN s.StatutoryType IN ('PF_EE', 'VPF') THEN d.Amount ELSE 0 END), 0)
    FROM dbo.EmployeeSalaries es
    JOIN dbo.SalaryMonths sm ON sm.IdSalaryMonth = es.IdSalaryMonth
    JOIN dbo.EmployeeSalaryDetails d ON d.IdEmployeeSalary = es.IdEmployeeSalary
    JOIN dbo.SalaryHeads s ON s.IdSalaryHead = d.IdSalaryHead
    WHERE es.IdEmployee = @IdEmployee
      AND CAST(sm.SalaryMonthDate AS DATE) >= @FYFrom AND CAST(sm.SalaryMonthDate AS DATE) < @MonthFrom;

    DECLARE @CurPT DECIMAL(18,2), @CurPF DECIMAL(18,2);
    SELECT @CurPT = ISNULL(SUM(CASE WHEN h.StatutoryType = 'PT' THEN c.Amount ELSE 0 END), 0),
           @CurPF = ISNULL(SUM(CASE WHEN h.StatutoryType IN ('PF_EE', 'VPF') THEN c.Amount ELSE 0 END), 0)
    FROM #Calc c JOIN #Heads h ON h.IdSalaryHead = c.IdSalaryHead;

    DECLARE @Gross DECIMAL(18,2) = @PrevTaxable + @CurrentTaxable + @RecurringTaxable * @Remaining;
    DECLARE @Deductions DECIMAL(18,2) = ISNULL(@StdDeduction, 0);
    IF @IdTaxRegime = 2
    BEGIN
        DECLARE @PFYear DECIMAL(18,2) = @PrevPF + @CurPF * (@Remaining + 1);
        SET @Deductions += (@PrevPT + @CurPT * (@Remaining + 1)) + CASE WHEN @PFYear > 150000 THEN 150000 ELSE @PFYear END;
    END

    SET @AnnualTaxable = @Gross - @Deductions;
    IF @AnnualTaxable < 0 SET @AnnualTaxable = 0;
    SET @AnnualTaxable = ROUND(@AnnualTaxable, -1);   -- rounded to the nearest 10 (section 288A)

    -- slabs for the age category; a configuration with only 'ALL' slabs applies to every age
    DECLARE @AgeCategory VARCHAR(20) = CASE WHEN @Age >= 80 THEN 'SUPER' WHEN @Age >= 60 THEN 'SENIOR' ELSE 'BELOW60' END;
    IF NOT EXISTS (SELECT 1 FROM dbo.TaxSlabs WHERE IdTaxYearConfig = @IdTaxYearConfig AND AgeCategory = @AgeCategory)
        SET @AgeCategory = 'ALL';

    DECLARE @Tax DECIMAL(18,2);
    SELECT @Tax = ISNULL(SUM(
               CASE WHEN @AnnualTaxable > IncomeFrom
                    THEN ((CASE WHEN IncomeTo IS NULL OR @AnnualTaxable < IncomeTo THEN @AnnualTaxable ELSE IncomeTo END) - IncomeFrom) * TaxRate / 100
                    ELSE 0 END), 0)
    FROM dbo.TaxSlabs WHERE IdTaxYearConfig = @IdTaxYearConfig AND AgeCategory = @AgeCategory;

    -- 87A rebate, or marginal relief just above the rebate limit
    IF @RebateLimit IS NOT NULL AND @AnnualTaxable <= @RebateLimit
        SET @Tax = CASE WHEN @Tax > ISNULL(@RebateMax, @Tax) THEN @Tax - @RebateMax ELSE 0 END;
    ELSE IF @RebateLimit IS NOT NULL AND @MarginalRelief = 1 AND @Tax > @AnnualTaxable - @RebateLimit
        SET @Tax = @AnnualTaxable - @RebateLimit;

    SET @AnnualTax = ROUND(@Tax * (1 + ISNULL(@CessRate, 0) / 100), 0);
    SET @Tds = ROUND((@AnnualTax - @PrevTds) / (@Remaining + 1), 0);
    IF @Tds < 0 SET @Tds = 0;

    SET @Log = CONCAT(N'Regime ', @IdTaxRegime, N'; projected taxable ', @AnnualTaxable, N' (gross ', @Gross, N' - deductions ', @Deductions,
                      N'); annual tax incl. cess ', @AnnualTax, N'; TDS so far ', @PrevTds, N'; months left ', @Remaining + 1);
END
GO

/* 6. Salary generation -------------------------------------------------------------------
   Same parameters and result set as before, so GenerateMonthlySalary and the API are unchanged.
   Per employee:
     a. paid days = days employed in the month (joining date / last working day)
     b. heads of the approved salary configuration that are paid this month (monthly, or DisbursingMonths has this month)
          FIXEDAMOUNT  configured amount, prorated when IsProratedOnPaidDays
          PERCENTAGE   % of its base head (WageCeiling on the base), worked out on the prorated base
          FORMULA      SalaryHeads.CustomFormula on the values already worked out
        then Min / Max amount and the head's rounding rule, in CalcSequence order (as SalaryStructureCalculator)
     c. maternity leave salary instead of b. when one covers the month
     d. overtime, salary adjustments, scheduled deductions
     e. statutory heads (CalculationMethod STATUTORY) from dbo.StatutoryRules, for every eligible employee
     f. income tax by TaxEngine (IN_TDS -> dbo.CalculateIncomeTax_IN into the head with StatutoryType TDS)
     g. totals: earnings + reimbursements, deductions; employer contributions are stored but not in net pay
     h. YTD for the financial year (FinancialYears), bank remittance */
CREATE OR ALTER PROCEDURE dbo.GenerateSalary_MultipleEmployees
    @IdEmployeesString VARCHAR(MAX),
    @idSalaryMonth     INT,
    @IdEmployeeCreated INT
AS
BEGIN
    SET NOCOUNT ON;

    /* ---------- month, financial year, settings ---------- */
    DECLARE @MonthDate DATE, @MonthText VARCHAR(50);
    SELECT @MonthDate = CAST(SalaryMonthDate AS DATE), @MonthText = SalaryMonthText FROM dbo.SalaryMonths WHERE IdSalaryMonth = @idSalaryMonth;
    DECLARE @MonthFrom DATE = DATEFROMPARTS(YEAR(@MonthDate), MONTH(@MonthDate), 1);
    DECLARE @MonthTo DATE = EOMONTH(@MonthFrom);
    DECLARE @DaysInMonth INT = DAY(@MonthTo);
    DECLARE @MonthName VARCHAR(20) = UPPER(DATENAME(MONTH, @MonthFrom));

    DECLARE @FYFrom DATE, @FYTo DATE;
    SELECT TOP 1 @FYFrom = CAST(FinancialYearFrom AS DATE), @FYTo = CAST(FinancialYearTo AS DATE)
    FROM dbo.FinancialYears WHERE @MonthFrom BETWEEN CAST(FinancialYearFrom AS DATE) AND CAST(FinancialYearTo AS DATE);
    IF @FYFrom IS NULL SELECT @FYFrom = DATEFROMPARTS(YEAR(@MonthFrom), 1, 1), @FYTo = DATEFROMPARTS(YEAR(@MonthFrom), 12, 31);

    DECLARE @TaxEngine VARCHAR(20) = ISNULL(UPPER(LTRIM(RTRIM((SELECT TOP 1 ParameterValue FROM dbo.SystemParameters WHERE ParameterName = 'TaxEngine')))), 'NONE');
    DECLARE @DefaultRegime INT = ISNULL(TRY_CONVERT(INT, (SELECT TOP 1 ParameterValue FROM dbo.SystemParameters WHERE ParameterName = 'DefaultTaxRegime')), 1);
    DECLARE @OTCutoffDay INT = TRY_CONVERT(INT, (SELECT TOP 1 ParameterValue FROM dbo.SystemParameters WHERE ParameterName = 'OvertimeCutoffDay'));
    DECLARE @ReportingCurrency VARCHAR(10) = NULLIF(LTRIM(RTRIM((SELECT TOP 1 ParameterValue FROM dbo.SystemParameters WHERE ParameterName = 'ReportingCurrency'))), '');

    DECLARE @ConversionRate DECIMAL(18,6) = NULL;
    IF @ReportingCurrency IS NOT NULL
        SELECT TOP 1 @ConversionRate = NULLIF(ConversionRate, 0) FROM dbo.CurrencyConversions
        WHERE FromCurrency = @ReportingCurrency AND CAST(RateDate AS DATE) <= @MonthTo ORDER BY RateDate DESC;

    -- overtime window
    DECLARE @OTFrom DATETIME, @OTTo DATETIME;
    IF @OTCutoffDay BETWEEN 1 AND 28
        SELECT @OTFrom = DATEADD(DAY, 1, DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(@MonthFrom), MONTH(@MonthFrom), @OTCutoffDay))),
               @OTTo   = DATEADD(DAY, 1, DATEFROMPARTS(YEAR(@MonthFrom), MONTH(@MonthFrom), @OTCutoffDay));
    ELSE
        SELECT @OTFrom = @MonthFrom, @OTTo = DATEADD(DAY, 1, @MonthTo);

    DECLARE @OTIdSalaryHead INT = TRY_CONVERT(INT, (SELECT TOP 1 ParameterValue FROM dbo.SystemParameters WHERE ParameterName = 'OverTimeSalaryHeadID'));
    IF @OTIdSalaryHead IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.SalaryHeads WHERE IdSalaryHead = @OTIdSalaryHead)
        SET @OTIdSalaryHead = (SELECT TOP 1 IdSalaryHead FROM dbo.SalaryHeads WHERE SalaryHeadCode = 'OVERTIME');

    /* ---------- salary heads (old data used EARNING; the app uses EARNINGS) ---------- */
    IF OBJECT_ID('tempdb..#Heads') IS NOT NULL DROP TABLE #Heads;
    SELECT IdSalaryHead, SalaryHeadCode, SalaryHeadName, HeadType AS StoredHeadType,
           CASE WHEN UPPER(HeadType) = 'EARNING' THEN 'EARNINGS' ELSE UPPER(HeadType) END AS HeadType,
           IsTaxable, IsActive, UPPER(CalculationMethod) AS CalculationMethod, IdPercentageSalaryHead, PercentageValue, FixedValue,
           CustomFormula, OrderNumber, NonTaxableThreshold, DisbursingMonths, UPPER(StatutoryType) AS StatutoryType,
           IsPartOfGross, IsPartOfPFWage, IsPartOfESIWage, IsPartOfGratuityWage, IsPartOfPTWage, IsProratedOnPaidDays,
           MinAmount, MaxAmount, WageCeiling, UPPER(ISNULL(PayFrequency, 'MONTHLY')) AS PayFrequency, CalcSequence,
           UPPER(ISNULL(RoundingRule, 'NEAREST')) AS RoundingRule,
           -- paid this month?
           CASE WHEN NULLIF(LTRIM(RTRIM(DisbursingMonths)), '') IS NOT NULL
                     THEN CASE WHEN CHARINDEX(@MonthName, UPPER(DisbursingMonths)) > 0 THEN 1 ELSE 0 END
                WHEN UPPER(ISNULL(PayFrequency, 'MONTHLY')) = 'MONTHLY' THEN 1 ELSE 0 END AS PaidThisMonth
    INTO #Heads FROM dbo.SalaryHeads;

    CREATE TABLE #SGStatus (IdEmployee INT, SalaryGenerationStatus VARCHAR(300), Remarks VARCHAR(300));

    -- one employee's amounts; FullAmount = contract (not prorated) monthly value, used for CONTRACT wage and the TDS projection
    CREATE TABLE #Calc (IdSalaryHead INT, Amount DECIMAL(18,4), FullAmount DECIMAL(18,4) NULL, IsRecurring BIT NOT NULL DEFAULT 0,
                        Remarks VARCHAR(300) NULL, SourceTableId INT NULL, SourceTableName VARCHAR(100) NULL);
    CREATE TABLE #Stat (StatutoryType VARCHAR(30), Amount DECIMAL(18,4));

    DECLARE @Ids TABLE (Id INT);
    INSERT INTO @Ids (Id) SELECT TRY_CONVERT(INT, value) FROM STRING_SPLIT(@IdEmployeesString, ',') WHERE TRY_CONVERT(INT, value) IS NOT NULL;

    DECLARE @IdEmployee INT, @JoiningDate DATE, @LastWorkingDay DATE, @CurrentStatus VARCHAR(25), @DateOfBirth DATE, @Gender VARCHAR(10),
            @Age INT, @PaidDays INT, @StartDay DATE, @EndDay DATE, @ValidationMessage VARCHAR(200),
            @idEmployeeSalaryConfig INT, @IdMaternityLeaveSalary INT, @idEmployeeSalary INT, @TaxRemarks NVARCHAR(1000);
    DECLARE @PF BIT, @EPS BIT, @PFActual BIT, @VPFRate DECIMAL(9,4), @ESI BIT, @Disabled BIT, @IdPTState INT, @PT BIT, @LWF BIT,
            @Pension BIT, @IdTaxRegime INT;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM @Ids;
    OPEN cur;
    FETCH NEXT FROM cur INTO @IdEmployee;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            TRUNCATE TABLE #Calc;
            TRUNCATE TABLE #Stat;
            SELECT @idEmployeeSalaryConfig = NULL, @IdMaternityLeaveSalary = NULL, @TaxRemarks = NULL;

            SELECT @JoiningDate = CAST(JoiningDate AS DATE), @LastWorkingDay = CAST(LastWorkingDay AS DATE), @CurrentStatus = CurrentStatus,
                   @DateOfBirth = CAST(DateOfBirth AS DATE), @Gender = UPPER(Gender)
            FROM dbo.Employees WHERE IdEmployee = @IdEmployee;

            SET @ValidationMessage = dbo.ValidateBeforeSalaryGeneration(@IdEmployee, @idSalaryMonth, @CurrentStatus);
            IF @ValidationMessage <> 'VALID'
            BEGIN
                INSERT INTO #SGStatus VALUES (@IdEmployee, @ValidationMessage, '');
                GOTO NextEmployee;
            END

            -- statutory details (no row = nothing applicable)
            SELECT @PF = 0, @EPS = 0, @PFActual = 0, @VPFRate = NULL, @ESI = 0, @Disabled = 0, @IdPTState = NULL, @PT = 0, @LWF = 0,
                   @Pension = 0, @IdTaxRegime = NULL;
            SELECT @PF = IsPFApplicable, @EPS = IsEPSApplicable, @PFActual = PFOnActualWage, @VPFRate = VPFRate, @ESI = IsESIApplicable,
                   @Disabled = IsDisabled, @IdPTState = IdPTState, @PT = IsPTApplicable, @LWF = IsLWFApplicable,
                   @Pension = IsPensionApplicable, @IdTaxRegime = IdTaxRegime
            FROM dbo.EmployeeStatutoryDetails WHERE IdEmployee = @IdEmployee;
            SET @IdTaxRegime = ISNULL(@IdTaxRegime, @DefaultRegime);

            SET @Age = CASE WHEN @DateOfBirth IS NULL THEN NULL
                            ELSE DATEDIFF(YEAR, @DateOfBirth, @MonthFrom) - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, @DateOfBirth, @MonthFrom), @DateOfBirth) > @MonthFrom THEN 1 ELSE 0 END END;

            -- a. paid days
            SET @StartDay = CASE WHEN @JoiningDate > @MonthFrom THEN @JoiningDate ELSE @MonthFrom END;
            SET @EndDay = CASE WHEN @LastWorkingDay IS NOT NULL AND @LastWorkingDay < @MonthTo THEN @LastWorkingDay ELSE @MonthTo END;
            SET @PaidDays = CASE WHEN @EndDay < @StartDay THEN 0 ELSE DATEDIFF(DAY, @StartDay, @EndDay) + 1 END;

            -- c. maternity leave salary for this month?
            SELECT @IdMaternityLeaveSalary = IdMaternityLeaveSalary FROM dbo.MaternityLeaveSalaries
            WHERE IdEmployee = @IdEmployee AND @idSalaryMonth BETWEEN IdSalaryMonthFrom AND IdSalaryMonthTo;

            IF ISNULL(@IdMaternityLeaveSalary, 0) > 0
            BEGIN
                INSERT INTO #Calc (IdSalaryHead, Amount, FullAmount, IsRecurring, Remarks)
                SELECT A.IdSalaryHead, A.Amount, A.Amount, 0, 'Maternity Leave Salary'
                FROM dbo.MaternityLeaveSalaryDetail A WHERE A.IdMaternityLeaveSalary = @IdMaternityLeaveSalary;
            END
            ELSE
            BEGIN
                -- b. approved salary configuration valid in this month
                SELECT TOP 1 @idEmployeeSalaryConfig = IdEmployeeSalaryConfig FROM dbo.EmployeeSalaryConfig
                WHERE IdEmployee = @IdEmployee AND ApprovalStatus = 'APPROVED' AND CAST(ValidFrom AS DATE) <= @MonthTo
                ORDER BY ValidFrom DESC, IdEmployeeSalaryConfig DESC;

                IF @idEmployeeSalaryConfig IS NULL
                BEGIN
                    INSERT INTO #SGStatus VALUES (@IdEmployee, 'No Approved Salary Configuration is available', '');
                    GOTO NextEmployee;
                END

                DECLARE @hId INT, @hMethod VARCHAR(20), @hBase INT, @hPct DECIMAL(18,4), @hFormula NVARCHAR(1000), @hCeiling DECIMAL(18,2),
                        @hMin DECIMAL(18,2), @hMax DECIMAL(18,2), @hRound VARCHAR(10), @hProrate BIT, @rowFixed DECIMAL(18,4),
                        @rowPct DECIMAL(18,4), @rowAmount DECIMAL(18,4), @full DECIMAL(18,4), @earned DECIMAL(18,4), @baseFull DECIMAL(18,4),
                        @baseEarned DECIMAL(18,4), @expr NVARCHAR(MAX), @exprFull NVARCHAR(MAX), @code VARCHAR(50), @p1 INT, @p2 INT;

                DECLARE heads CURSOR LOCAL FAST_FORWARD FOR
                    SELECT h.IdSalaryHead, h.CalculationMethod, h.IdPercentageSalaryHead, ISNULL(d.PercentageValue, h.PercentageValue),
                           h.CustomFormula, h.WageCeiling, h.MinAmount, h.MaxAmount, h.RoundingRule, h.IsProratedOnPaidDays,
                           d.FixedAmount, d.SalaryAmount
                    FROM dbo.EmployeeSalaryConfigDetails d
                    JOIN #Heads h ON h.IdSalaryHead = d.IdSalaryHead
                    WHERE d.IdEmployeeSalaryConfig = @idEmployeeSalaryConfig AND h.PaidThisMonth = 1
                      AND h.CalculationMethod IN ('FIXEDAMOUNT', 'PERCENTAGE', 'FORMULA')
                    ORDER BY ISNULL(h.CalcSequence, 2147483647),
                             CASE h.CalculationMethod WHEN 'FIXEDAMOUNT' THEN 0 WHEN 'PERCENTAGE' THEN 1 ELSE 2 END,
                             ISNULL(h.OrderNumber, 2147483647);
                OPEN heads;
                FETCH NEXT FROM heads INTO @hId, @hMethod, @hBase, @hPct, @hFormula, @hCeiling, @hMin, @hMax, @hRound, @hProrate, @rowFixed, @rowAmount;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    IF @hMethod = 'FIXEDAMOUNT'
                    BEGIN
                        SET @full = ISNULL(@rowFixed, ISNULL(@rowAmount, 0));
                        SET @earned = CASE WHEN @hProrate = 1 THEN @full * @PaidDays / @DaysInMonth ELSE @full END;
                    END
                    ELSE IF @hMethod = 'PERCENTAGE'
                    BEGIN
                        SELECT @baseFull = ISNULL(MAX(FullAmount), 0), @baseEarned = ISNULL(MAX(Amount), 0) FROM #Calc WHERE IdSalaryHead = @hBase;
                        IF @hCeiling IS NOT NULL AND @baseFull > @hCeiling SET @baseFull = @hCeiling;
                        IF @hCeiling IS NOT NULL AND @baseEarned > @hCeiling SET @baseEarned = @hCeiling;
                        SET @full = @baseFull * ISNULL(@hPct, 0) / 100;
                        SET @earned = CASE WHEN @hProrate = 1 THEN @baseEarned * ISNULL(@hPct, 0) / 100 ELSE @full END;
                    END
                    ELSE
                    BEGIN
                        -- FORMULA: [CODE] -> value already worked out (0 when the head is not in this salary)
                        SET @expr = dbo.SalaryFormulaToSql(@hFormula);
                        SET @exprFull = @expr;
                        WHILE CHARINDEX('[', @expr) > 0
                        BEGIN
                            SET @p1 = CHARINDEX('[', @expr);
                            SET @p2 = CHARINDEX(']', @expr, @p1);
                            IF @p2 = 0 BREAK;
                            SET @code = SUBSTRING(@expr, @p1 + 1, @p2 - @p1 - 1);
                            SET @expr = REPLACE(@expr, '[' + @code + ']',
                                '(' + CONVERT(VARCHAR(40), ISNULL((SELECT SUM(c.Amount) FROM #Calc c JOIN #Heads x ON x.IdSalaryHead = c.IdSalaryHead WHERE x.SalaryHeadCode = @code), 0)) + ')');
                            SET @exprFull = REPLACE(@exprFull, '[' + @code + ']',
                                '(' + CONVERT(VARCHAR(40), ISNULL((SELECT SUM(ISNULL(c.FullAmount, c.Amount)) FROM #Calc c JOIN #Heads x ON x.IdSalaryHead = c.IdSalaryHead WHERE x.SalaryHeadCode = @code), 0)) + ')');
                        END
                        SELECT @full = 0, @earned = 0;
                        IF @expr NOT LIKE '%[^0-9.+*/() -]%' AND @exprFull NOT LIKE '%[^0-9.+*/() -]%'
                        BEGIN
                            BEGIN TRY
                                DECLARE @sql NVARCHAR(MAX) = N'SELECT @r1 = CONVERT(DECIMAL(18,4), ' + @expr + N'), @r2 = CONVERT(DECIMAL(18,4), ' + @exprFull + N');';
                                EXEC sp_executesql @sql, N'@r1 DECIMAL(18,4) OUTPUT, @r2 DECIMAL(18,4) OUTPUT', @r1 = @earned OUTPUT, @r2 = @full OUTPUT;
                            END TRY
                            BEGIN CATCH
                                SELECT @full = 0, @earned = 0;   -- division by zero / invalid formula -> 0
                            END CATCH
                        END
                        IF @hProrate = 0 SET @earned = @full;
                    END

                    IF @hMin IS NOT NULL AND @full < @hMin SET @full = @hMin;
                    IF @hMax IS NOT NULL AND @full > @hMax SET @full = @hMax;
                    IF @hMax IS NOT NULL AND @earned > @hMax SET @earned = @hMax;
                    SET @earned = CASE @hRound WHEN 'UP' THEN CEILING(@earned) WHEN 'DOWN' THEN FLOOR(@earned)
                                               WHEN 'NONE' THEN ROUND(@earned, 2) ELSE ROUND(@earned, 0) END;

                    INSERT INTO #Calc (IdSalaryHead, Amount, FullAmount, IsRecurring) VALUES (@hId, @earned, @full, 1);
                    FETCH NEXT FROM heads INTO @hId, @hMethod, @hBase, @hPct, @hFormula, @hCeiling, @hMin, @hMax, @hRound, @hProrate, @rowFixed, @rowAmount;
                END
                CLOSE heads;
                DEALLOCATE heads;
            END

            -- d. overtime (approved, not yet paid)
            IF @OTIdSalaryHead IS NOT NULL
                INSERT INTO #Calc (IdSalaryHead, Amount, Remarks)
                SELECT @OTIdSalaryHead, CEILING(SUM(A.DurationInHours * (B.StandardRate * B.DayRate))), ''
                FROM dbo.OvertimeTransactions A
                JOIN dbo.EmployeeOvertimeConfig B ON A.IdEmployee = B.IdEmployee AND A.DayType = B.DayType
                WHERE A.IdEmployee = @IdEmployee AND A.ApprovalStatus = 'APPROVED' AND A.IdSalaryMonthAccounted IS NULL
                  AND A.ApprovalStatusDate >= @OTFrom AND A.ApprovalStatusDate < @OTTo
                HAVING SUM(A.DurationInHours * (B.StandardRate * B.DayRate)) > 0;

            -- d. salary adjustments of this month
            INSERT INTO #Calc (IdSalaryHead, Amount, Remarks, SourceTableId, SourceTableName)
            SELECT A.AllocatingSalaryHead, SUM(A.Amount), LEFT(MAX(A.PayAdjustmentDetails), 300), MAX(A.IdSalaryAdjustment), 'SalaryAdjustments'
            FROM dbo.SalaryAdjustments A
            WHERE A.IdEmployee = @IdEmployee AND A.AllocatingSalaryMonth = @idSalaryMonth
            GROUP BY A.AllocatingSalaryHead;

            -- d. scheduled deductions of this month
            INSERT INTO #Calc (IdSalaryHead, Amount, Remarks)
            SELECT A.AllocatingSalaryHead, CEILING(SUM(B.AmountTobeDeducted)), 'ScheduledDeductionDetails'
            FROM dbo.ScheduledDeductions A
            JOIN dbo.ScheduledDeductionDetails B ON A.IdScheduledSalaryDeduction = B.IdScheduledSalaryDeduction
            WHERE A.IdEmployee = @IdEmployee AND B.IdSalaryMonth = @idSalaryMonth
            GROUP BY A.AllocatingSalaryHead;

            -- e. statutory heads from StatutoryRules
            DECLARE @wPF DECIMAL(18,4), @wESI DECIMAL(18,4), @wPT DECIMAL(18,4), @wGrat DECIMAL(18,4), @wGross DECIMAL(18,4), @wContract DECIMAL(18,4);
            SELECT @wPF       = ISNULL(SUM(CASE WHEN h.IsPartOfPFWage = 1 THEN c.Amount END), 0),
                   @wESI      = ISNULL(SUM(CASE WHEN h.IsPartOfESIWage = 1 THEN c.Amount END), 0),
                   @wPT       = ISNULL(SUM(CASE WHEN h.IsPartOfPTWage = 1 THEN c.Amount END), 0),
                   @wGrat     = ISNULL(SUM(CASE WHEN h.IsPartOfGratuityWage = 1 THEN c.Amount END), 0),
                   @wGross    = ISNULL(SUM(CASE WHEN h.IsPartOfGross = 1 THEN c.Amount END), 0),
                   @wContract = ISNULL(SUM(CASE WHEN h.IsPartOfGross = 1 AND c.IsRecurring = 1 THEN c.FullAmount END), 0)
            FROM #Calc c JOIN #Heads h ON h.IdSalaryHead = c.IdSalaryHead
            WHERE h.HeadType IN ('EARNINGS', 'REIMBURSEMENT');

            DECLARE @sHead INT, @sType VARCHAR(30), @sRound VARCHAR(10), @rRateType VARCHAR(20), @rRate DECIMAL(9,4), @rThreshold DECIMAL(18,2),
                    @rRateAbove DECIMAL(9,4), @rBase VARCHAR(20), @rCeiling DECIMAL(18,2), @rWaivedBy VARCHAR(50), @rMinWage DECIMAL(18,2),
                    @rElig DECIMAL(18,2), @rEligDis DECIMAL(18,2), @rMinAge INT, @rMaxAge INT, @rFlag VARCHAR(50), @rDiffOf VARCHAR(30),
                    @rMax DECIMAL(18,2), @base DECIMAL(18,4), @rateUsed DECIMAL(9,4), @amt DECIMAL(18,4), @ok BIT;

            DECLARE stat CURSOR LOCAL FAST_FORWARD FOR
                SELECT h.IdSalaryHead, h.StatutoryType, h.RoundingRule, r.RateType, r.Rate, r.RateThreshold, r.RateAboveThreshold, UPPER(r.WageBase),
                       r.WageCeiling, r.CeilingWaivedBy, r.MinWage, r.EligibilityWageLimit, r.EligibilityWageLimitDisabled, r.MinAge, r.MaxAge,
                       r.EmployeeFlag, UPPER(r.DifferenceOfType), r.MaxAmount
                FROM #Heads h
                CROSS APPLY (SELECT TOP 1 * FROM dbo.StatutoryRules sr
                             WHERE UPPER(sr.StatutoryType) = h.StatutoryType AND sr.IsActive = 1
                               AND sr.EffectiveFrom <= @MonthTo AND (sr.EffectiveTo IS NULL OR sr.EffectiveTo >= @MonthFrom)
                             ORDER BY sr.EffectiveFrom DESC, sr.IdStatutoryRule DESC) r
                WHERE h.IsActive = 1 AND h.CalculationMethod = 'STATUTORY' AND h.StatutoryType IS NOT NULL AND h.StatutoryType <> 'TDS'
                  AND ISNULL(@IdMaternityLeaveSalary, 0) = 0
                ORDER BY CASE WHEN UPPER(r.RateType) = 'DIFFERENCE' THEN 1 ELSE 0 END, ISNULL(h.CalcSequence, 2147483647), ISNULL(h.OrderNumber, 2147483647);
            OPEN stat;
            FETCH NEXT FROM stat INTO @sHead, @sType, @sRound, @rRateType, @rRate, @rThreshold, @rRateAbove, @rBase, @rCeiling, @rWaivedBy, @rMinWage,
                                      @rElig, @rEligDis, @rMinAge, @rMaxAge, @rFlag, @rDiffOf, @rMax;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @ok = CASE UPPER(ISNULL(@rFlag, ''))
                              WHEN '' THEN 1
                              WHEN 'ISPFAPPLICABLE' THEN @PF
                              WHEN 'ISEPSAPPLICABLE' THEN CASE WHEN @PF = 1 AND @EPS = 1 THEN 1 ELSE 0 END
                              WHEN 'ISESIAPPLICABLE' THEN @ESI
                              WHEN 'ISPTAPPLICABLE' THEN @PT
                              WHEN 'ISLWFAPPLICABLE' THEN @LWF
                              WHEN 'ISPENSIONAPPLICABLE' THEN @Pension
                              WHEN 'ISDISABLED' THEN @Disabled
                              ELSE 0 END;
                IF @ok = 1 AND @rMinAge IS NOT NULL AND (@Age IS NULL OR @Age < @rMinAge) SET @ok = 0;
                IF @ok = 1 AND @rMaxAge IS NOT NULL AND @Age IS NOT NULL AND @Age >= @rMaxAge SET @ok = 0;

                SET @base = CASE @rBase WHEN 'PF' THEN @wPF WHEN 'ESI' THEN @wESI WHEN 'PT' THEN @wPT WHEN 'GRATUITY' THEN @wGrat
                                        WHEN 'CONTRACT' THEN @wContract ELSE @wGross END;
                -- eligibility on the monthly wage (e.g. ESI up to 21,000; 25,000 for persons with disability)
                IF @ok = 1 AND @rElig IS NOT NULL
                   AND @base > CASE WHEN @Disabled = 1 AND @rEligDis IS NOT NULL THEN @rEligDis ELSE @rElig END
                    SET @ok = 0;

                SET @amt = 0;
                IF @ok = 1
                BEGIN
                    IF @rMinWage IS NOT NULL AND @base < @rMinWage SET @base = @rMinWage;
                    IF @rCeiling IS NOT NULL AND @base > @rCeiling
                       AND NOT (UPPER(ISNULL(@rWaivedBy, '')) = 'PFONACTUALWAGE' AND @PFActual = 1)
                        SET @base = @rCeiling;
                    SET @rateUsed = CASE WHEN @rThreshold IS NOT NULL AND @base >= @rThreshold THEN ISNULL(@rRateAbove, @rRate) ELSE @rRate END;

                    IF UPPER(@rRateType) = 'PERCENTAGE'
                        SET @amt = @base * ISNULL(@rateUsed, 0) / 100;
                    ELSE IF UPPER(@rRateType) = 'EMPLOYEE_RATE'
                        SET @amt = @base * ISNULL(@VPFRate, 0) / 100;
                    ELSE IF UPPER(@rRateType) = 'DIFFERENCE'
                        SET @amt = @base * ISNULL(@rateUsed, 0) / 100 - ISNULL((SELECT SUM(Amount) FROM #Stat WHERE StatutoryType = @rDiffOf), 0);
                    ELSE IF UPPER(@rRateType) = 'SLAB' AND @sType = 'PT'
                        -- the slab's WageBasis decides which wage is compared with SalaryFrom / SalaryTo
                        SELECT TOP 1 @amt = s.PTAmount
                        FROM dbo.PTSlabs s
                        CROSS APPLY (SELECT CASE UPPER(s.WageBasis) WHEN 'PT' THEN @wPT WHEN 'PF' THEN @wPF WHEN 'ESI' THEN @wESI
                                                                    WHEN 'GRATUITY' THEN @wGrat WHEN 'BASIC' THEN @wGrat ELSE @wGross END AS Wage) w
                        WHERE s.IdState = @IdPTState AND s.IsActive = 1
                          AND s.EffectiveFrom <= @MonthTo AND (s.EffectiveTo IS NULL OR s.EffectiveTo >= @MonthFrom)
                          AND (s.Gender IS NULL OR s.Gender = LEFT(@Gender, 1))
                          AND w.Wage >= s.SalaryFrom AND (s.SalaryTo IS NULL OR w.Wage <= s.SalaryTo)
                          AND (s.ApplicableMonth IS NULL OR s.ApplicableMonth = MONTH(@MonthFrom))
                          AND (UPPER(s.DeductionFrequency) = 'MONTHLY' OR s.ApplicableMonth = MONTH(@MonthFrom))
                        ORDER BY CASE WHEN s.ApplicableMonth IS NOT NULL THEN 0 ELSE 1 END,
                                 CASE WHEN s.Gender IS NOT NULL THEN 0 ELSE 1 END, s.EffectiveFrom DESC;
                    ELSE IF UPPER(@rRateType) = 'SLAB' AND @sType IN ('LWF_EE', 'LWF_ER')
                        SELECT TOP 1 @amt = CASE WHEN @sType = 'LWF_EE' THEN s.EmployeeAmount ELSE s.EmployerAmount END FROM dbo.LWFSlabs s
                        WHERE s.IdState = @IdPTState AND s.IsActive = 1
                          AND s.EffectiveFrom <= @MonthTo AND (s.EffectiveTo IS NULL OR s.EffectiveTo >= @MonthFrom)
                          AND (s.SalaryFrom IS NULL OR @base >= s.SalaryFrom) AND (s.SalaryTo IS NULL OR @base <= s.SalaryTo)
                          AND (NULLIF(s.DeductionMonths, '') IS NULL OR CHARINDEX(',' + CAST(MONTH(@MonthFrom) AS VARCHAR(2)) + ',', ',' + REPLACE(s.DeductionMonths, ' ', '') + ',') > 0)
                        ORDER BY s.EffectiveFrom DESC;

                    IF @amt < 0 SET @amt = 0;
                    IF @rMax IS NOT NULL AND @amt > @rMax SET @amt = @rMax;
                    SET @amt = CASE @sRound WHEN 'UP' THEN CEILING(@amt) WHEN 'DOWN' THEN FLOOR(@amt)
                                            WHEN 'NONE' THEN ROUND(@amt, 2) ELSE ROUND(@amt, 0) END;
                END

                INSERT INTO #Stat VALUES (@sType, @amt);
                IF @amt <> 0
                    INSERT INTO #Calc (IdSalaryHead, Amount, FullAmount, IsRecurring, Remarks) VALUES (@sHead, @amt, @amt, 1, NULL);

                FETCH NEXT FROM stat INTO @sHead, @sType, @sRound, @rRateType, @rRate, @rThreshold, @rRateAbove, @rBase, @rCeiling, @rWaivedBy, @rMinWage,
                                          @rElig, @rEligDis, @rMinAge, @rMaxAge, @rFlag, @rDiffOf, @rMax;
            END
            CLOSE stat;
            DEALLOCATE stat;

            -- f. income tax
            DECLARE @Tds DECIMAL(18,2) = 0, @AnnualTaxable DECIMAL(18,2) = 0, @AnnualTax DECIMAL(18,2) = 0;
            DECLARE @TdsHead INT = (SELECT TOP 1 IdSalaryHead FROM #Heads WHERE StatutoryType = 'TDS' AND IsActive = 1 ORDER BY IdSalaryHead);
            IF @TaxEngine = 'IN_TDS' AND @TdsHead IS NOT NULL
            BEGIN
                EXEC dbo.CalculateIncomeTax_IN @IdEmployee, @MonthFrom, @FYFrom, @FYTo, @Age, @IdTaxRegime,
                     @Tds OUTPUT, @AnnualTaxable OUTPUT, @AnnualTax OUTPUT, @TaxRemarks OUTPUT;
                IF @Tds > 0
                    INSERT INTO #Calc (IdSalaryHead, Amount, Remarks) VALUES (@TdsHead, @Tds, LEFT(@TaxRemarks, 300));
            END

            -- g. totals and the salary rows
            DECLARE @TotalEarnings DECIMAL(18,2), @TotalDeductions DECIMAL(18,2);
            SELECT @TotalEarnings = ISNULL(SUM(CASE WHEN h.HeadType IN ('EARNINGS', 'REIMBURSEMENT') THEN c.Amount END), 0),
                   @TotalDeductions = ISNULL(SUM(CASE WHEN h.HeadType = 'DEDUCTION' THEN c.Amount END), 0)
            FROM #Calc c JOIN #Heads h ON h.IdSalaryHead = c.IdSalaryHead;

            BEGIN TRAN;   -- one employee's salary is written completely or not at all

            INSERT INTO dbo.EmployeeSalaries (IdEmployee, IdSalaryMonth, SalaryMonthText, GeneratedDate, TotalEarnings, TotalDeductions,
                                              TaxableIncome, TaxAmountAccounted, TaxAmountDeducted, CreatedBy, CreatedDate, ApprovalStatus)
            VALUES (@IdEmployee, @idSalaryMonth, @MonthText, GETDATE(), ROUND(@TotalEarnings, 0), ROUND(@TotalDeductions, 0),
                    @AnnualTaxable, @AnnualTax, @Tds, @IdEmployeeCreated, GETDATE(), 'DRAFT');
            SET @idEmployeeSalary = SCOPE_IDENTITY();

            INSERT INTO dbo.EmployeeSalaryDetails (IdEmployeeSalary, IdSalaryHead, SalaryHeadName, SalaryHeadType, Amount, AmountInUSD,
                                                   Remarks, OrderNumber, PercentageOfIdSalaryHead)
            SELECT @idEmployeeSalary, h.IdSalaryHead, h.SalaryHeadName, h.StoredHeadType, ROUND(SUM(c.Amount), 2),
                   CASE WHEN @ConversionRate IS NULL THEN NULL ELSE ROUND(SUM(c.Amount) / @ConversionRate, 2) END,
                   LEFT(MAX(c.Remarks), 300), h.OrderNumber, h.IdPercentageSalaryHead
            FROM #Calc c JOIN #Heads h ON h.IdSalaryHead = c.IdSalaryHead
            GROUP BY h.IdSalaryHead, h.SalaryHeadName, h.StoredHeadType, h.OrderNumber, h.IdPercentageSalaryHead;

            -- h. heads paid earlier this financial year but not this month (keeps their YTD on the payslip)
            DECLARE @PrevIdEmployeeSalary INT;
            SELECT @PrevIdEmployeeSalary = MAX(A.IdEmployeeSalary)
            FROM dbo.EmployeeSalaries A JOIN dbo.SalaryMonths B ON A.IdSalaryMonth = B.IdSalaryMonth
            WHERE A.IdEmployee = @IdEmployee AND CAST(B.SalaryMonthDate AS DATE) >= @FYFrom AND CAST(B.SalaryMonthDate AS DATE) < @MonthFrom;

            IF @PrevIdEmployeeSalary IS NOT NULL
                INSERT INTO dbo.EmployeeSalaryDetails (IdEmployeeSalary, IdSalaryHead, SalaryHeadName, SalaryHeadType, Amount, AmountInUSD, OrderNumber)
                SELECT @idEmployeeSalary, A.IdSalaryHead, A.SalaryHeadName, A.SalaryHeadType, 0, 0, A.OrderNumber
                FROM dbo.EmployeeSalaryDetails A
                WHERE A.IdEmployeeSalary = @PrevIdEmployeeSalary
                  AND NOT EXISTS (SELECT 1 FROM #Calc c WHERE c.IdSalaryHead = A.IdSalaryHead);

            -- YTD within the financial year
            UPDATE ESD SET ESD.YTDAmount = YTD.TotalAmount, ESD.YTDAmountUSD = YTD.TotalAmountUSD
            FROM dbo.EmployeeSalaryDetails ESD
            JOIN (SELECT A.IdSalaryHead, SUM(A.Amount) AS TotalAmount, SUM(A.AmountInUSD) AS TotalAmountUSD
                  FROM dbo.EmployeeSalaryDetails A
                  JOIN dbo.EmployeeSalaries B ON A.IdEmployeeSalary = B.IdEmployeeSalary
                  JOIN dbo.SalaryMonths M ON M.IdSalaryMonth = B.IdSalaryMonth
                  WHERE B.IdEmployee = @IdEmployee
                    AND CAST(M.SalaryMonthDate AS DATE) >= @FYFrom AND CAST(M.SalaryMonthDate AS DATE) <= @MonthTo
                  GROUP BY A.IdSalaryHead) YTD ON YTD.IdSalaryHead = ESD.IdSalaryHead
            WHERE ESD.IdEmployeeSalary = @idEmployeeSalary;

            -- totals from the stored rows; employer contributions are not part of net pay
            UPDATE ES SET
                TotalEarnings = ISNULL((SELECT SUM(D.Amount) FROM dbo.EmployeeSalaryDetails D
                                        WHERE D.IdEmployeeSalary = ES.IdEmployeeSalary AND D.SalaryHeadType IN ('EARNINGS', 'EARNING', 'REIMBURSEMENT')), 0),
                TotalDeductions = ISNULL((SELECT SUM(D.Amount) FROM dbo.EmployeeSalaryDetails D
                                          WHERE D.IdEmployeeSalary = ES.IdEmployeeSalary AND D.SalaryHeadType = 'DEDUCTION'), 0)
            FROM dbo.EmployeeSalaries ES WHERE ES.IdEmployeeSalary = @idEmployeeSalary;

            UPDATE dbo.EmployeeSalaries SET NetSalary = TotalEarnings - TotalDeductions, FinalTaxableIncome = @AnnualTaxable, FinalTaxAmount = @Tds
            WHERE IdEmployeeSalary = @idEmployeeSalary;

            -- overtime is now accounted
            UPDATE A SET A.IdSalaryMonthAccounted = @idSalaryMonth, A.SalaryAccountedAmount = A.DurationInHours * (B.StandardRate * B.DayRate)
            FROM dbo.OvertimeTransactions A
            JOIN dbo.EmployeeOvertimeConfig B ON A.IdEmployee = B.IdEmployee AND A.DayType = B.DayType
            WHERE A.IdEmployee = @IdEmployee AND A.ApprovalStatus = 'APPROVED' AND A.IdSalaryMonthAccounted IS NULL
              AND A.ApprovalStatusDate >= @OTFrom AND A.ApprovalStatusDate < @OTTo AND @OTIdSalaryHead IS NOT NULL;

            DECLARE @RemitRate DECIMAL(18,6) = ISNULL(@ConversionRate, 1);
            EXEC dbo.GenerateSalary_BankRemittance @IdEmployee, @idEmployeeSalary, @idSalaryMonth, @RemitRate;

            COMMIT;

            INSERT INTO #SGStatus VALUES (@IdEmployee,
                CASE WHEN ISNULL(@IdMaternityLeaveSalary, 0) > 0 THEN 'SUCCESS - Maternity Leave Salary' ELSE 'SUCCESS' END,
                CASE WHEN @PaidDays < @DaysInMonth THEN CONCAT('Paid days ', @PaidDays, ' of ', @DaysInMonth) ELSE '' END);
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK;
            IF CURSOR_STATUS('local', 'heads') >= 0 BEGIN CLOSE heads; DEALLOCATE heads; END
            ELSE IF CURSOR_STATUS('local', 'heads') = -1 DEALLOCATE heads;
            IF CURSOR_STATUS('local', 'stat') >= 0 BEGIN CLOSE stat; DEALLOCATE stat; END
            ELSE IF CURSOR_STATUS('local', 'stat') = -1 DEALLOCATE stat;
            INSERT INTO #SGStatus VALUES (@IdEmployee, LEFT('FAILED - ' + ERROR_MESSAGE(), 300), '');
        END CATCH

        NextEmployee:
        FETCH NEXT FROM cur INTO @IdEmployee;
    END
    CLOSE cur;
    DEALLOCATE cur;

    SELECT A.IdEmployee, B.EmployeeCode, dbo.GetFullName(B.FirstName, B.MiddleName, B.LastName) AS EmployeeName,
           C.DesignationName, A.SalaryGenerationStatus
    FROM #SGStatus A
    JOIN dbo.Employees B ON A.IdEmployee = B.IdEmployee
    LEFT JOIN dbo.Designations C ON B.IdDesignation = C.IdDesignation
    ORDER BY B.EmployeeCode;
END
GO

/* 7. Copy the country's statutory rules, PT / LWF slabs and tax setting from NOMARO_GLOBAL ----------
   Companies created by registration get these automatically. Run this for an existing company.
   Rows already present (same type / state) are kept. NOMARO_GLOBAL must be on the same SQL Server. */
DECLARE @Country VARCHAR(5) = 'IND';   -- <-- IND or UAE (NomaroCountries.CountryCode of this company)

IF DB_ID('NOMARO_GLOBAL') IS NOT NULL
BEGIN
    INSERT INTO dbo.StatutoryRules (StatutoryType, Description, RateType, Rate, RateThreshold, RateAboveThreshold, WageBase, WageCeiling,
                                    CeilingWaivedBy, MinWage, EligibilityWageLimit, EligibilityWageLimitDisabled, MinAge, MaxAge, EmployeeFlag,
                                    DifferenceOfType, MaxAmount, EffectiveFrom, EffectiveTo, IsActive)
    SELECT g.StatutoryType, g.Description, g.RateType, g.Rate, g.RateThreshold, g.RateAboveThreshold, g.WageBase, g.WageCeiling,
           g.CeilingWaivedBy, g.MinWage, g.EligibilityWageLimit, g.EligibilityWageLimitDisabled, g.MinAge, g.MaxAge, g.EmployeeFlag,
           g.DifferenceOfType, g.MaxAmount, g.EffectiveFrom, g.EffectiveTo, g.IsActive
    FROM NOMARO_GLOBAL.dbo.CountryStatutoryRules g
    WHERE g.CountryCode = @Country
      AND NOT EXISTS (SELECT 1 FROM dbo.StatutoryRules s WHERE s.StatutoryType = g.StatutoryType AND s.EffectiveFrom = g.EffectiveFrom);

    INSERT INTO dbo.PTSlabs (IdState, Gender, SalaryFrom, SalaryTo, PTAmount, ApplicableMonth, DeductionFrequency, WageBasis, EffectiveFrom, EffectiveTo, IsActive)
    SELECT st.IdState, g.Gender, g.SalaryFrom, g.SalaryTo, g.PTAmount, g.ApplicableMonth, g.DeductionFrequency, g.WageBasis, g.EffectiveFrom, g.EffectiveTo, g.IsActive
    FROM NOMARO_GLOBAL.dbo.CountryPTSlabs g
    JOIN dbo.States st ON st.StateCode = g.StateCode
    WHERE g.CountryCode = @Country
      AND NOT EXISTS (SELECT 1 FROM dbo.PTSlabs p WHERE p.IdState = st.IdState);

    INSERT INTO dbo.LWFSlabs (IdState, SalaryFrom, SalaryTo, EmployeeAmount, EmployerAmount, DeductionMonths, EffectiveFrom, EffectiveTo, IsActive)
    SELECT st.IdState, g.SalaryFrom, g.SalaryTo, g.EmployeeAmount, g.EmployerAmount, g.DeductionMonths, g.EffectiveFrom, g.EffectiveTo, g.IsActive
    FROM NOMARO_GLOBAL.dbo.CountryLWFSlabs g
    JOIN dbo.States st ON st.StateCode = g.StateCode
    WHERE g.CountryCode = @Country
      AND NOT EXISTS (SELECT 1 FROM dbo.LWFSlabs l WHERE l.IdState = st.IdState);

    DECLARE @Engine VARCHAR(20) = (SELECT TOP 1 TaxEngine FROM NOMARO_GLOBAL.dbo.NomaroCountries WHERE CountryCode = @Country);
    IF @Engine IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.SystemParameters WHERE ParameterName = 'TaxEngine')
            UPDATE dbo.SystemParameters SET ParameterValue = @Engine WHERE ParameterName = 'TaxEngine';
        ELSE IF COLUMNPROPERTY(OBJECT_ID('dbo.SystemParameters'), 'IdSystemParameter', 'IsIdentity') = 1
            INSERT INTO dbo.SystemParameters (ParameterName, ParameterDescription, ParameterValue, DataType, ShowInUIToConfigure, ParameterValueEditable, ParameterBinaryValueEditable)
            VALUES ('TaxEngine', 'Income tax calculation in salary generation: NONE or IN_TDS', @Engine, 'STRING', 0, 1, 0);
        ELSE
            INSERT INTO dbo.SystemParameters (IdSystemParameter, ParameterName, ParameterDescription, ParameterValue, DataType, ShowInUIToConfigure, ParameterValueEditable, ParameterBinaryValueEditable)
            SELECT ISNULL(MAX(IdSystemParameter), 0) + 1, 'TaxEngine', 'Income tax calculation in salary generation: NONE or IN_TDS', @Engine, 'STRING', 0, 1, 0
            FROM dbo.SystemParameters;
    END
END
GO

SELECT StatutoryType, RateType, Rate, WageBase, WageCeiling, EmployeeFlag FROM dbo.StatutoryRules ORDER BY StatutoryType;
SELECT ParameterName, ParameterValue FROM dbo.SystemParameters WHERE ParameterName IN ('TaxEngine', 'DefaultTaxRegime', 'OvertimeCutoffDay', 'ReportingCurrency', 'OverTimeSalaryHeadID');
GO
SET NOEXEC OFF;
GO
