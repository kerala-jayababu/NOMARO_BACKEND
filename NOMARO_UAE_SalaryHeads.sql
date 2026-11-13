/* Generic UAE salary heads (UAE Labour Law - Federal Decree-Law 33/2021; GPSSA / ADPF pension).
   Run on the UAE company database (or the UAE template database). NOT on NOMARO_GLOBAL.
   - Heads whose code already exists are skipped, so it is safe to run again.
   - @Active = 0 adds them inactive so payroll can review them first; set 1 to add them active.
   - No head is taxable (no personal income tax in the UAE). Only BASIC is part of the gratuity wage.
   - Pension heads are FORMULA heads on the contract wage (BASIC + HRA + TRANSPORT + OTHER_ALW) until PENSION_EE /
     PENSION_ER statutory types exist in the code. The AED 3,000 minimum / AED 70,000 maximum pension wage is NOT
     applied by a formula - check high/low earners manually until then.
   - Percentages / formulas are typical defaults; adjust per company policy. Confirm pension rates with GPSSA / ADPF. */
SET NOCOUNT ON;
DECLARE @Active BIT = 0;
DECLARE @CreatedBy INT = ISNULL((SELECT TOP 1 IdEmployee FROM dbo.Employees WHERE EmployeeCode = 'ADMIN' ORDER BY IdEmployee), 1);
DECLARE @Now DATETIME = GETDATE();

DECLARE @H TABLE (
    Seq INT, Code VARCHAR(50), Name NVARCHAR(60), HeadType VARCHAR(50), Method VARCHAR(50),
    BaseCode VARCHAR(50) NULL, Pct DECIMAL(18,4) NULL, Formula NVARCHAR(500) NULL,
    IsGross BIT, IsCTC BIT, IsGratuity BIT, IsProrated BIT, PayFrequency VARCHAR(20), ShowOnPayslip BIT);

INSERT INTO @H VALUES
-- Earnings
( 1, 'BASIC',          N'Basic Salary',                     'EARNINGS',  'FIXEDAMOUNT', NULL,    NULL, NULL, 1, 1, 1, 1, 'MONTHLY',  1),
( 2, 'HRA',            N'Housing Allowance',                'EARNINGS',  'PERCENTAGE',  'BASIC', 25,   NULL, 1, 1, 0, 1, 'MONTHLY',  1),
( 3, 'TRANSPORT',      N'Transport Allowance',              'EARNINGS',  'PERCENTAGE',  'BASIC', 10,   NULL, 1, 1, 0, 1, 'MONTHLY',  1),
( 4, 'OTHER_ALW',      N'Other / Cost of Living Allowance', 'EARNINGS',  'FIXEDAMOUNT', NULL,    NULL, NULL, 1, 1, 0, 1, 'MONTHLY',  1),
( 5, 'MOBILE',         N'Mobile Allowance',                 'EARNINGS',  'FIXEDAMOUNT', NULL,    NULL, NULL, 1, 1, 0, 1, 'MONTHLY',  1),
( 6, 'FOOD',           N'Food Allowance',                   'EARNINGS',  'FIXEDAMOUNT', NULL,    NULL, NULL, 1, 1, 0, 1, 'MONTHLY',  1),
( 7, 'OVERTIME',       N'Overtime',                         'EARNINGS',  'MANUAL',      NULL,    NULL, NULL, 1, 0, 0, 0, 'MONTHLY',  1),
( 8, 'AIR_TICKET',     N'Annual Air Ticket',                'EARNINGS',  'MANUAL',      NULL,    NULL, NULL, 1, 1, 0, 0, 'ANNUAL',   1),
( 9, 'LEAVE_ENCASH',   N'Leave Encashment',                 'EARNINGS',  'MANUAL',      NULL,    NULL, NULL, 1, 0, 0, 0, 'ONE_TIME', 1),
(10, 'BONUS',          N'Bonus',                            'EARNINGS',  'MANUAL',      NULL,    NULL, NULL, 1, 0, 0, 0, 'ONE_TIME', 1),
(11, 'EOSB_PAY',       N'End of Service Gratuity',          'EARNINGS',  'MANUAL',      NULL,    NULL, NULL, 1, 0, 0, 0, 'ONE_TIME', 1),
-- Deductions
(20, 'PENSION_EE',     N'Pension - Employee (GPSSA/ADPF)',  'DEDUCTION', 'FORMULA',     NULL,    NULL, N'([BASIC] + [HRA] + [TRANSPORT] + [OTHER_ALW]) * 11 / 100', 0, 0, 0, 1, 'MONTHLY', 1),
(21, 'GCC_PENSION_EE', N'GCC National Pension - Employee',  'DEDUCTION', 'MANUAL',      NULL,    NULL, NULL, 0, 0, 0, 0, 'MONTHLY',  1),
(22, 'UNPAID_LEAVE',   N'Unpaid Leave / Absence',           'DEDUCTION', 'MANUAL',      NULL,    NULL, NULL, 0, 0, 0, 0, 'MONTHLY',  1),
(23, 'LOAN_REC',       N'Loan / Advance Recovery',          'DEDUCTION', 'MANUAL',      NULL,    NULL, NULL, 0, 0, 0, 0, 'MONTHLY',  1),
(24, 'PENALTY',        N'Disciplinary Deduction',           'DEDUCTION', 'MANUAL',      NULL,    NULL, NULL, 0, 0, 0, 0, 'MONTHLY',  1),
-- Employer contributions / provisions (part of CTC, not gross)
(30, 'PENSION_ER',     N'Pension - Employer (GPSSA/ADPF)',  'EMPLOYER_CONTRIBUTION', 'FORMULA', NULL, NULL, N'([BASIC] + [HRA] + [TRANSPORT] + [OTHER_ALW]) * 12.5 / 100', 0, 1, 0, 1, 'MONTHLY', 0),
(31, 'EOSB_PROV',      N'Gratuity Provision',               'EMPLOYER_CONTRIBUTION', 'FORMULA', NULL, NULL, N'[BASIC] * 21 / 365', 0, 1, 0, 1, 'MONTHLY', 0),
(32, 'MED_INS',        N'Medical Insurance',                'EMPLOYER_CONTRIBUTION', 'FIXEDAMOUNT', NULL, NULL, NULL, 0, 1, 0, 0, 'MONTHLY', 0),
(33, 'VISA_COST',      N'Visa / Labour Card Cost',          'EMPLOYER_CONTRIBUTION', 'FIXEDAMOUNT', NULL, NULL, NULL, 0, 1, 0, 0, 'ANNUAL',  0);

-- only heads not already present
DELETE h FROM @H h WHERE EXISTS (SELECT 1 FROM dbo.SalaryHeads s WHERE s.SalaryHeadCode = h.Code);

BEGIN TRAN;

IF COLUMNPROPERTY(OBJECT_ID('dbo.SalaryHeads'), 'IdSalaryHead', 'IsIdentity') = 1
    INSERT INTO dbo.SalaryHeads
        (SalaryHeadCode, SalaryHeadName, HeadType, IsTaxable, IsActive, CalculationMethod, IdPercentageSalaryHead, PercentageValue,
         FixedValue, CustomFormula, CreatedBy, CreatedOn, OrderNumber, NonTaxableThreshold, DisbursingMonths, StatutoryType,
         IsPartOfGross, IsPartOfCTC, IsPartOfPFWage, IsPartOfESIWage, IsPartOfGratuityWage, IsPartOfPTWage,
         IsProratedOnPaidDays, IsArrearHead, IdBaseSalaryHead, MinAmount, MaxAmount, WageCeiling, PayFrequency, CalcSequence,
         RoundingRule, ShowOnPayslip)
    SELECT Code, Name, HeadType, 0, @Active, Method, NULL, Pct,
           NULL, Formula, @CreatedBy, @Now, Seq, NULL, NULL, NULL,
           IsGross, IsCTC, 0, 0, IsGratuity, 0,
           IsProrated, 0, NULL, NULL, NULL, NULL, PayFrequency, Seq,
           'NEAREST', ShowOnPayslip
    FROM @H ORDER BY Seq;
ELSE
    INSERT INTO dbo.SalaryHeads
        (IdSalaryHead, SalaryHeadCode, SalaryHeadName, HeadType, IsTaxable, IsActive, CalculationMethod, IdPercentageSalaryHead, PercentageValue,
         FixedValue, CustomFormula, CreatedBy, CreatedOn, OrderNumber, NonTaxableThreshold, DisbursingMonths, StatutoryType,
         IsPartOfGross, IsPartOfCTC, IsPartOfPFWage, IsPartOfESIWage, IsPartOfGratuityWage, IsPartOfPTWage,
         IsProratedOnPaidDays, IsArrearHead, IdBaseSalaryHead, MinAmount, MaxAmount, WageCeiling, PayFrequency, CalcSequence,
         RoundingRule, ShowOnPayslip)
    SELECT (SELECT ISNULL(MAX(IdSalaryHead), 0) FROM dbo.SalaryHeads) + ROW_NUMBER() OVER (ORDER BY Seq),
           Code, Name, HeadType, 0, @Active, Method, NULL, Pct,
           NULL, Formula, @CreatedBy, @Now, Seq, NULL, NULL, NULL,
           IsGross, IsCTC, 0, 0, IsGratuity, 0,
           IsProrated, 0, NULL, NULL, NULL, NULL, PayFrequency, Seq,
           'NEAREST', ShowOnPayslip
    FROM @H;

-- percentage heads point at their base head (BASIC)
UPDATE s SET s.IdPercentageSalaryHead = b.IdSalaryHead
FROM dbo.SalaryHeads s
JOIN @H h ON h.Code = s.SalaryHeadCode AND h.BaseCode IS NOT NULL
JOIN dbo.SalaryHeads b ON b.SalaryHeadCode = h.BaseCode;

COMMIT;

SELECT IdSalaryHead, SalaryHeadCode, SalaryHeadName, HeadType, CalculationMethod, PercentageValue, IdPercentageSalaryHead,
       CustomFormula, IsPartOfGross, IsPartOfCTC, IsPartOfGratuityWage, PayFrequency, IsActive
FROM dbo.SalaryHeads ORDER BY CalcSequence, OrderNumber;
