/* =====================================================================
   Sub menu sections (headings inside a parent menu)
   Run this BEFORE deploying the new API: the API reads the new column.
   ===================================================================== */

IF COL_LENGTH('dbo.PayrollScreens', 'MenuSection') IS NULL
    ALTER TABLE dbo.PayrollScreens ADD MenuSection VARCHAR(50) NULL;
GO

/* Employee Life Cycle (16): two sections. OrderNumber sets the order inside a section;
   a section is placed by its lowest OrderNumber, so Records (1-6) comes before Service & Exit (11-15). */
UPDATE dbo.PayrollScreens SET MenuSection = 'Employee Records', OrderNumber = 1  WHERE ScreenCode = 'EMPPROF';
UPDATE dbo.PayrollScreens SET MenuSection = 'Employee Records', OrderNumber = 2  WHERE ScreenCode = 'EMPDOCVIEW';
UPDATE dbo.PayrollScreens SET MenuSection = 'Employee Records', OrderNumber = 3  WHERE ScreenCode = 'EMPAGREE';
UPDATE dbo.PayrollScreens SET MenuSection = 'Employee Records', OrderNumber = 4  WHERE ScreenCode = 'PROFCHGREQ';
UPDATE dbo.PayrollScreens SET MenuSection = 'Employee Records', OrderNumber = 5  WHERE ScreenCode = 'PROFCHGSET';
UPDATE dbo.PayrollScreens SET MenuSection = 'Employee Records', OrderNumber = 6  WHERE ScreenCode = 'CELEBRATIONS';

UPDATE dbo.PayrollScreens SET MenuSection = 'Service & Exit',   OrderNumber = 11 WHERE ScreenCode = 'SERVICEMGMNT';
UPDATE dbo.PayrollScreens SET MenuSection = 'Service & Exit',   OrderNumber = 12 WHERE ScreenCode = 'SERVICEMGMNTAPP';
UPDATE dbo.PayrollScreens SET MenuSection = 'Service & Exit',   OrderNumber = 13 WHERE ScreenCode = 'RESIGNREQUEST';
UPDATE dbo.PayrollScreens SET MenuSection = 'Service & Exit',   OrderNumber = 14 WHERE ScreenCode = 'OFFBOARDCASES';
UPDATE dbo.PayrollScreens SET MenuSection = 'Service & Exit',   OrderNumber = 15 WHERE ScreenCode = 'OFFBOARDCLEAR';
GO

SELECT IdPayrollScreen, ScreenCode, ScreenName, MenuSection, OrderNumber
FROM dbo.PayrollScreens
WHERE IdParentPayrollScreen = 16
ORDER BY OrderNumber;
